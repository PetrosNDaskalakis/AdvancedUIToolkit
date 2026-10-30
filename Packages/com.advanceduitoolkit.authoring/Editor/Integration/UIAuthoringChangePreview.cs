using System;
using System.Collections.Generic;
using System.Text;
using AdvancedUIAuthoring.Editor;
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Captures explicit Editor baselines and builds read-only change reports. No regeneration writes.</summary>
    public static class UIAuthoringChangePreview
    {
        /// <summary>Snapshot format supported by this package.</summary>
        private const int RecordFormat = 1;

        /// <summary>Capture source provenance and exact generated references immediately after a successful Generate call.</summary>
        public static UIAuthoringPendingGeneration BeginTracking(UIAuthoringIntegrationRecipe recipe,
            RectTransform parent, UIAuthoringBridgeResult generation)
        {
            EnsureEditorReady();
            if (recipe == null || parent == null || generation == null || !generation.Succeeded || generation.CreatedObjects.Count == 0)
                throw new InvalidOperationException("Tracking requires a successful, nonempty generation result.");
            UIAuthoringDocument document = ReadDocument(recipe);
            if (generation.CreatedObjects.Count != document.nodes.Count)
                throw new InvalidOperationException("Generation does not match the current schema node count.");
            var pending = new UIAuthoringPendingGeneration
            {
                recipe = recipe,
                parent = parent,
                schemaJson = JsonUtility.ToJson(document),
                inputStamp = InputStamp(recipe, document)
            };
            foreach (UIAuthoringNode node in document.nodes)
            {
                GameObject instance;
                if (!generation.CreatedObjects.TryGetValue(node.id, out instance) || instance == null)
                    throw new InvalidOperationException("Missing generated object for " + node.id);
                pending.ids.Add(node.id);
                pending.objects.Add(instance);
            }
            return pending;
        }

        /// <summary>
        /// Save the current state of exactly the pending generated objects as a new Editor-only baseline.
        /// Requires a saved, clean scene and unchanged recipe inputs; never saves the user's scene implicitly.
        /// </summary>
        public static bool CaptureBaseline(UIAuthoringPendingGeneration pending,
            out UIAuthoringGenerationRecord record, out string error)
        {
            record = null;
            error = string.Empty;
            UIAuthoringGenerationRecord candidate = null;
            try
            {
                EnsureEditorReady();
                if (pending == null || pending.recipe == null || pending.parent == null)
                    throw new InvalidOperationException("Generate UI in this window first. Older hierarchies are not adopted by name.");
                if (!AssetDatabase.Contains(pending.recipe)) throw new InvalidOperationException("Save the Integration Recipe as an asset first.");
                Scene scene = pending.parent.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path) || scene.isDirty)
                    throw new InvalidOperationException("Save the generated scene before Capture Baseline. The scene is not saved automatically.");
                UIAuthoringDocument document = ReadDocument(pending.recipe);
                if (InputStamp(pending.recipe, document) != pending.inputStamp || JsonUtility.ToJson(document) != pending.schemaJson)
                    throw new InvalidOperationException("Recipe inputs changed after generation. Restore those inputs or generate again before capturing.");
                Dictionary<string, GameObject> objects = PendingObjects(pending);
                if (document.nodes.Count != objects.Count) throw new InvalidOperationException("Pending generation is incomplete.");
                var tracked = new Dictionary<GameObject, string>();
                foreach (var item in objects)
                {
                    if (item.Value == null || item.Value.scene != scene || EditorUtility.IsPersistent(item.Value))
                        throw new InvalidOperationException("A generated object is missing or was moved to another scene.");
                    tracked.Add(item.Value, item.Key);
                }
                candidate = ScriptableObject.CreateInstance<UIAuthoringGenerationRecord>();
                candidate.formatVersion = RecordFormat;
                candidate.recipe = pending.recipe;
                candidate.sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
                candidate.parentObjectId = UIAuthoringSceneFingerprint.Identity(pending.parent.gameObject);
                candidate.capturedUtc = DateTime.UtcNow.ToString("O");
                candidate.stylesheetHash = StylesheetHash(pending.recipe);
                candidate.profileHash = ProfileHash(pending.recipe);
                candidate.binderHash = BinderHash(pending.recipe);
                candidate.parentLayoutHash = UIAuthoringSceneFingerprint.ParentLayout(pending.parent);
                var orders = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (UIAuthoringNode node in document.nodes)
                {
                    GameObject instance = objects[node.id];
                    Transform expectedParent = string.IsNullOrEmpty(node.parentId) ? pending.parent : objects[node.parentId].transform;
                    if (instance.name != node.name || instance.transform.parent != expectedParent)
                        throw new InvalidOperationException(node.id + ": name or parent changed since generation. Restore it before capturing.");
                    candidate.nodes.Add(new UIAuthoringRecordedNode
                    {
                        id = node.id,
                        definition = JsonUtility.ToJson(node),
                        sourceOrder = NextOrder(orders, node.parentId),
                        objectId = UIAuthoringSceneFingerprint.Identity(instance),
                        parentObjectId = UIAuthoringSceneFingerprint.Identity(instance.transform.parent.gameObject),
                        sceneOrder = instance.transform.GetSiblingIndex(),
                        sceneHash = UIAuthoringSceneFingerprint.Node(instance, tracked),
                        templateId = TemplateId(pending.recipe, node),
                        mappingHash = MappingHash(pending.recipe, node)
                    });
                }
                // All inspection succeeds before any asset is created. The snapshot is additive and does not overwrite another record.
                EnsureFolder("Assets", "Editor");
                EnsureFolder("Assets/Editor", "UIAuthoringRecords");
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Editor/UIAuthoringRecords/UIAuthoringGeneration.asset");
                AssetDatabase.CreateAsset(candidate, path);
                record = candidate;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                if (candidate != null && !AssetDatabase.Contains(candidate)) UnityEngine.Object.DestroyImmediate(candidate);
                return false;
            }
        }

        /// <summary>Compare current recipe inputs and loaded scene state with a saved baseline; never writes or rebuilds layout.</summary>
        public static UIAuthoringChangePreviewResult Preview(UIAuthoringGenerationRecord record)
        {
            var rows = new List<UIAuthoringChangeRow>();
            var errors = new List<string>();
            var notices = new List<string>();
            try
            {
                EnsureEditorReady();
                if (record == null || record.formatVersion != RecordFormat || record.recipe == null ||
                    record.nodes == null || record.nodes.Count == 0 || record.nodes.Count > 2048)
                    throw new InvalidOperationException("Assign a valid generation record from this preview format.");
                string scenePath = AssetDatabase.GUIDToAssetPath(record.sceneGuid);
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (string.IsNullOrEmpty(scenePath) || !scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Open the scene recorded by this baseline before previewing: " + scenePath);
                GameObject parentObject = UIAuthoringSceneFingerprint.Resolve(record.parentObjectId) as GameObject;
                RectTransform parent = parentObject != null ? parentObject.GetComponent<RectTransform>() : null;
                if (parent == null || parentObject.scene != scene)
                    throw new InvalidOperationException("The recorded target parent is missing or was moved to another scene.");
                UIAuthoringDocument document = ReadDocument(record.recipe);
                UIAuthoringBridgeResult validation = UIAuthoringToolkitBridge.ValidateForPreview(record.recipe, parent);
                if (!validation.Succeeded) throw new InvalidOperationException("Current recipe is invalid: " + validation.Error);
                var previous = new Dictionary<string, UIAuthoringRecordedNode>(StringComparer.Ordinal);
                var live = new Dictionary<string, GameObject>(StringComparer.Ordinal);
                var tracked = new Dictionary<GameObject, string>();
                foreach (UIAuthoringRecordedNode node in record.nodes)
                {
                    if (node == null || string.IsNullOrEmpty(node.id) || previous.ContainsKey(node.id))
                        throw new InvalidOperationException("Record contains missing or duplicate node identities.");
                    previous.Add(node.id, node);
                    GameObject instance = UIAuthoringSceneFingerprint.Resolve(node.objectId) as GameObject;
                    live.Add(node.id, instance);
                    if (instance != null) tracked.Add(instance, node.id);
                }
                bool stylesChanged = StylesheetHash(record.recipe) != record.stylesheetHash;
                bool profileChanged = ProfileHash(record.recipe) != record.profileHash;
                if (stylesChanged) notices.Add("Stylesheet changed. All matched nodes need source review; this release does not calculate property-level style differences.");
                if (profileChanged) notices.Add("Style Profile or a dependency changed. Matched prefab nodes need source review.");
                if (BinderHash(record.recipe) != record.binderHash)
                    notices.Add("Binder setup settings changed. Preview does not inspect or write generated C#; use Inspect Binder separately.");
                if (UIAuthoringSceneFingerprint.ParentLayout(parent) != record.parentLayoutHash)
                    errors.Add("The recorded external parent's LayoutGroup settings changed. Review the scene context before regeneration.");
                var desired = new HashSet<string>(StringComparer.Ordinal);
                var orders = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (UIAuthoringNode node in document.nodes)
                {
                    desired.Add(node.id);
                    int order = NextOrder(orders, node.parentId);
                    UIAuthoringRecordedNode old;
                    UIAuthoringChangeRow row;
                    if (!previous.TryGetValue(node.id, out old))
                    {
                        row = new UIAuthoringChangeRow { NodeId = node.id, Change = UIAuthoringChangeKind.Add,
                            Message = "New schema ID; would create '" + node.name + "'." };
                    }
                    else
                    {
                        UIAuthoringNode oldDefinition = JsonUtility.FromJson<UIAuthoringNode>(old.definition);
                        if (oldDefinition == null) throw new InvalidOperationException("Invalid stored definition for " + node.id);
                        bool replacement = !string.Equals(oldDefinition.kind, node.kind, StringComparison.OrdinalIgnoreCase) ||
                            old.templateId != TemplateId(record.recipe, node);
                        bool update = old.definition != JsonUtility.ToJson(node) || old.sourceOrder != order ||
                            old.mappingHash != MappingHash(record.recipe, node);
                        bool review = stylesChanged || (profileChanged && !IsContainer(node));
                        row = new UIAuthoringChangeRow
                        {
                            NodeId = node.id,
                            Change = replacement ? UIAuthoringChangeKind.Replace : update ? UIAuthoringChangeKind.Update :
                                review ? UIAuthoringChangeKind.Review : UIAuthoringChangeKind.Unchanged,
                            Message = replacement ? "Kind or resolved source prefab changed; replacement requires review." :
                                update ? "Definition, sibling order or text/template policy changed." :
                                review ? "Shared stylesheet/profile dependency changed." : "No detected source change."
                        };
                        CheckScene(old, live[node.id], tracked, row);
                    }
                    CheckNameCollision(node, parent, live, tracked, row);
                    rows.Add(row);
                }
                foreach (UIAuthoringRecordedNode old in record.nodes)
                {
                    if (desired.Contains(old.id)) continue;
                    var row = new UIAuthoringChangeRow { NodeId = old.id, Change = UIAuthoringChangeKind.Remove,
                        Message = "Schema ID removed; object and any references require review. Nothing is deleted." };
                    CheckScene(old, live[old.id], tracked, row);
                    rows.Add(row);
                }
            }
            catch (Exception exception) { errors.Add(exception.Message); }
            return new UIAuthoringChangePreviewResult
            {
                Rows = rows.AsReadOnly(), Errors = errors.AsReadOnly(), Notices = notices.AsReadOnly()
            };
        }

        /// <summary>Resolve a record's parent for the Editor window; does not open a missing scene.</summary>
        internal static RectTransform ResolveParent(UIAuthoringGenerationRecord record)
        {
            GameObject value = record != null ? UIAuthoringSceneFingerprint.Resolve(record.parentObjectId) as GameObject : null;
            return value != null ? value.GetComponent<RectTransform>() : null;
        }

        /// <summary>Report scene drift independently of the desired source operation.</summary>
        private static void CheckScene(UIAuthoringRecordedNode old, GameObject current,
            Dictionary<GameObject, string> tracked, UIAuthoringChangeRow row)
        {
            try
            {
                if (current == null) throw new InvalidOperationException("Recorded scene object is missing or its identity changed.");
                string parentId = UIAuthoringSceneFingerprint.Identity(current.transform.parent != null ? current.transform.parent.gameObject : null);
                if (parentId != old.parentObjectId || current.transform.GetSiblingIndex() != old.sceneOrder)
                    throw new InvalidOperationException("Scene parent or sibling order changed after baseline capture.");
                if (UIAuthoringSceneFingerprint.Node(current, tracked) != old.sceneHash)
                    throw new InvalidOperationException("Serialized UI state, prefab link, components or owned children changed after capture.");
            }
            catch (Exception exception) { Conflict(row, exception.Message); }
        }

        /// <summary>Detect desired sibling names already occupied by untracked scene objects.</summary>
        private static void CheckNameCollision(UIAuthoringNode node, RectTransform parent,
            Dictionary<string, GameObject> live, Dictionary<GameObject, string> tracked, UIAuthoringChangeRow row)
        {
            Transform target = parent;
            if (!string.IsNullOrEmpty(node.parentId))
            {
                GameObject owner;
                if (!live.TryGetValue(node.parentId, out owner) || owner == null) return;
                target = owner.transform;
            }
            for (int i = 0; i < target.childCount; i++)
            {
                GameObject child = target.GetChild(i).gameObject;
                if (child.name == node.name && !tracked.ContainsKey(child))
                    Conflict(row, "Desired sibling name is occupied by an untracked object: " + node.name);
            }
        }

        /// <summary>Append a conflict explanation without losing the source-side operation.</summary>
        private static void Conflict(UIAuthoringChangeRow row, string message)
        {
            row.HasConflict = true;
            row.Message += " Conflict: " + message;
        }

        /// <summary>Read canonical schema data; full validation is performed by the bridge.</summary>
        private static UIAuthoringDocument ReadDocument(UIAuthoringIntegrationRecipe recipe)
        {
            UIAuthoringDocument document;
            string error;
            if (recipe == null || recipe.schema == null) throw new InvalidOperationException("Recipe schema is missing.");
            if (!UIAuthoringSchemaParser.TryParse(recipe.schema.text, out document, out error))
                throw new InvalidOperationException(error);
            return document;
        }

        /// <summary>Canonical stylesheet digest; formatting-only JSON edits do not cause source review.</summary>
        private static string StylesheetHash(UIAuthoringIntegrationRecipe recipe)
        {
            if (recipe.stylesheet == null) return UIAuthoringSceneFingerprint.Hash(string.Empty);
            UIAuthoringStyleSheet sheet;
            string error;
            if (!UIAuthoringStyleSheetParser.TryParse(recipe.stylesheet.text, out sheet, out error))
                throw new InvalidOperationException(error);
            return UIAuthoringSceneFingerprint.Hash(JsonUtility.ToJson(sheet));
        }

        /// <summary>Profile identity and recursive asset dependency content hash.</summary>
        private static string ProfileHash(UIAuthoringIntegrationRecipe recipe)
        {
            if (recipe.profile == null || !AssetDatabase.Contains(recipe.profile))
                throw new InvalidOperationException("A saved Style Profile asset is required for generation records.");
            return UIAuthoringSceneFingerprint.Hash(UIAuthoringSceneFingerprint.Identity(recipe.profile) + "|" +
                AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(recipe.profile)).ToString());
        }

        /// <summary>Digest explicit Binder output configuration without reading or writing generated source.</summary>
        private static string BinderHash(UIAuthoringIntegrationRecipe recipe)
        {
            var builder = new StringBuilder();
            foreach (string value in new[] { recipe.bindingRootId, recipe.bindingClass, recipe.bindingNamespace, recipe.bindingOutputFolder })
                UIAuthoringSceneFingerprint.Token(builder, value);
            return UIAuthoringSceneFingerprint.Hash(builder.ToString());
        }

        /// <summary>Stamp all generation inputs so a baseline cannot silently attach to newly edited JSON.</summary>
        private static string InputStamp(UIAuthoringIntegrationRecipe recipe, UIAuthoringDocument document)
        {
            var builder = new StringBuilder();
            foreach (string value in new[] { JsonUtility.ToJson(document), StylesheetHash(recipe), ProfileHash(recipe), BinderHash(recipe) })
                UIAuthoringSceneFingerprint.Token(builder, value);
            foreach (UIAuthoringNode node in document.nodes)
            {
                UIAuthoringSceneFingerprint.Token(builder, TemplateId(recipe, node));
                UIAuthoringSceneFingerprint.Token(builder, MappingHash(recipe, node));
            }
            return UIAuthoringSceneFingerprint.Hash(builder.ToString());
        }

        /// <summary>Resolve the same explicit profile role/custom template used by the bridge.</summary>
        private static string TemplateId(UIAuthoringIntegrationRecipe recipe, UIAuthoringNode node)
        {
            if (IsContainer(node)) return "native:container";
            UIAuthoringPrefabMapping mapping = FindMapping(recipe, node.id);
            UIComponentKind kind;
            if (mapping != null) kind = mapping.kind;
            else if (string.Equals(node.kind, "button", StringComparison.OrdinalIgnoreCase)) kind = UIComponentKind.Button;
            else if (string.Equals(node.kind, "text", StringComparison.OrdinalIgnoreCase)) kind = UIComponentKind.TextLegacy;
            else if (string.Equals(node.kind, "image", StringComparison.OrdinalIgnoreCase)) kind = UIComponentKind.Image;
            else throw new InvalidOperationException("No template role for " + node.id);
            UIComponentTemplate template;
            string error;
            if (!AdvancedUIEditorIntegration.TryResolveTemplate(recipe.profile, kind,
                mapping != null ? mapping.customTemplateId : null, out template, out error) || template == null || template.Prefab == null)
                throw new InvalidOperationException(node.id + ": " + error);
            return UIAuthoringSceneFingerprint.Identity(template.Prefab);
        }

        /// <summary>Explicit mapping settings; source-label references use persistent prefab-component identity.</summary>
        private static string MappingHash(UIAuthoringIntegrationRecipe recipe, UIAuthoringNode node)
        {
            UIAuthoringPrefabMapping mapping = FindMapping(recipe, node.id);
            if (mapping == null) return UIAuthoringSceneFingerprint.Hash("default:" + node.kind.ToLowerInvariant());
            var builder = new StringBuilder();
            foreach (string value in new[] { mapping.kind.ToString(), mapping.customTemplateId, mapping.allowUnbound.ToString(),
                mapping.textMode.ToString(), mapping.textMode == UIAuthoringTextMode.ExplicitSource ?
                    UIAuthoringSceneFingerprint.Identity(mapping.sourceText) : string.Empty })
                UIAuthoringSceneFingerprint.Token(builder, value);
            return UIAuthoringSceneFingerprint.Hash(builder.ToString());
        }

        /// <summary>Find a node mapping; duplicate/stale mappings are rejected by bridge validation.</summary>
        private static UIAuthoringPrefabMapping FindMapping(UIAuthoringIntegrationRecipe recipe, string id)
        {
            if (recipe.mappings != null)
                foreach (UIAuthoringPrefabMapping mapping in recipe.mappings)
                    if (mapping != null && mapping.nodeId == id) return mapping;
            return null;
        }

        /// <summary>Case-insensitive built-in native container kind.</summary>
        private static bool IsContainer(UIAuthoringNode node)
        {
            return string.Equals(node.kind, "container", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Assign a stable relative sibling index from schema order.</summary>
        private static int NextOrder(Dictionary<string, int> orders, string parentId)
        {
            string key = parentId ?? string.Empty;
            int index;
            orders.TryGetValue(key, out index);
            orders[key] = index + 1;
            return index;
        }

        /// <summary>Rebuild the explicit pending ID/object map; no hierarchy-name adoption is performed.</summary>
        private static Dictionary<string, GameObject> PendingObjects(UIAuthoringPendingGeneration pending)
        {
            if (pending.ids == null || pending.objects == null || pending.ids.Count != pending.objects.Count)
                throw new InvalidOperationException("Pending generation references are incomplete.");
            var result = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int i = 0; i < pending.ids.Count; i++) result.Add(pending.ids[i], pending.objects[i]);
            return result;
        }

        /// <summary>Create only the fixed Editor record folders, without altering existing contents.</summary>
        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                string guid = AssetDatabase.CreateFolder(parent, name);
                if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Cannot create record folder: " + parent + "/" + name);
            }
        }

        /// <summary>Refuse recording/preview while Unity data is changing due to compilation, import or Play Mode.</summary>
        private static void EnsureEditorReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Wait until Unity finishes import/compilation and leaves Play Mode.");
        }
    }
}
