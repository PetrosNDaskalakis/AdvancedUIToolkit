using System;
using System.Collections.Generic;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Instantiate portable mold data as ordinary scene objects with grouped Undo and failure cleanup.</summary>
    public static class UIHierarchyMoldBuilder
    {
        /// <summary>Prevent nested transactions from component lifecycle callbacks.</summary>
        private static bool creating;

        /// <summary>Check data structure and installed component types without constructing components or changing scene state.</summary>
        public static bool ValidateMold(UIHierarchyMold mold, out string error)
        {
            error = null;
            try
            {
                if (mold != null && mold.version == 1)
                    throw new InvalidOperationException("This mold uses the older capture format (Preview 4). That format did not preserve prefab boundaries, so this tool cannot determine which saved nodes came from prefabs. Capture a new mold from the original hierarchy with its prefab links intact. The saved asset has not been changed.");
                if (mold == null || mold.version != 2 || mold.nodes == null || mold.nodes.Count == 0 || mold.nodes.Count > UIHierarchyMold.MaximumNodeCount)
                    throw new InvalidOperationException("Choose a nonempty version 2 container mold (at most 10,000 objects).");
                var depths = new int[mold.nodes.Count];
                for (int i = 0; i < mold.nodes.Count; i++)
                {
                    UIMoldNode node = mold.nodes[i];
                    if (node == null || !node.rectTransform || node.name == null || node.components == null || node.layer < 0 || node.layer > 31 ||
                        (i == 0 ? node.parent != -1 : node.parent < 0 || node.parent >= i))
                        throw new InvalidOperationException("Invalid hierarchy node at index " + i + ".");
                    depths[i] = i == 0 ? 0 : depths[node.parent] + 1;
                    if (depths[i] > UIHierarchyMold.MaximumDepth)
                        throw new InvalidOperationException("Mold hierarchy exceeds depth 256 at " + node.name + ". Reduce nesting and capture again.");
                    ValidateGeometry(node, i);
                    foreach (UIMoldComponent component in node.components)
                    {
                        if (component == null || component.properties == null) throw new InvalidOperationException("Missing component data on " + node.name + ".");
                        ResolveType(component.typeName);
                        var paths = new HashSet<string>(StringComparer.Ordinal);
                        foreach (UIMoldProperty property in component.properties)
                        {
                            if (property == null || string.IsNullOrEmpty(property.path) || string.IsNullOrEmpty(property.valueType) || !paths.Add(property.path) ||
                                UIMoldPropertyCodec.IsInfrastructure(property.path))
                                throw new InvalidOperationException("Invalid component property on " + node.name + ".");
                            if (!UIMoldPropertyCodec.IsSupportedKind(property.kind))
                                throw new InvalidOperationException("Unsupported property kind at " + node.name + " / " + property.path + ". Recapture with supported component fields.");
                            if (property.kind != (int)SerializedPropertyType.Generic && property.kind != (int)SerializedPropertyType.ObjectReference &&
                                string.IsNullOrWhiteSpace(property.value))
                                throw new InvalidOperationException("Missing mold value at " + node.name + " / " + property.path + ". Recapture the mold.");
                            if (property.kind != (int)SerializedPropertyType.ObjectReference) continue;
                            if (property.node < -1 || property.node >= mold.nodes.Count || property.component < -2 ||
                                (property.node == -1 && property.component != -1))
                                throw new InvalidOperationException("Invalid internal reference at " + property.path + ".");
                            if (property.node >= 0 && (mold.nodes[property.node] == null || mold.nodes[property.node].components == null ||
                                property.component >= mold.nodes[property.node].components.Count))
                                throw new InvalidOperationException("Missing internal component at " + property.path + ".");
                        }
                    }
                }
                return true;
            }
            catch (Exception exception) { error = exception.Message; return false; }
        }

        /// <summary>Reject non-finite geometry before it can be assigned to newly created scene transforms.</summary>
        private static void ValidateGeometry(UIMoldNode node, int index)
        {
            if (!IsFinite(node.localPosition) || !IsFinite(node.localScale) ||
                !IsFinite(new Vector3(node.localRotation.x, node.localRotation.y, node.localRotation.z)) || !IsFinite(node.localRotation.w) ||
                !IsFinite(node.anchorMin) || !IsFinite(node.anchorMax) || !IsFinite(node.pivot) ||
                !IsFinite(node.sizeDelta) || !IsFinite(node.anchoredPosition))
                throw new InvalidOperationException("Non-finite transform value on " + node.name + " [" + index + "]. Repair the source geometry and recapture.");
        }

        /// <summary>Check all vector coordinates without imposing layout ranges or clamping developer values.</summary>
        private static bool IsFinite(Vector3 value) { return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z); }

        /// <summary>Reject NaN and infinity while retaining finite negative sizes, offsets and scale.</summary>
        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        /// <summary>Check the held scene destination and root name in addition to portable mold data.</summary>
        public static bool Validate(UIHierarchyMold mold, RectTransform parent, string rootName, out string error)
        {
            if (!ValidateMold(mold, out error)) return false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { error = "Wait for Unity to finish importing/compiling and leave Play Mode."; return false; }
            if (!AdvancedUIEditorIntegration.IsValidCreationParent(parent) || EditorSceneManager.IsPreviewScene(parent.gameObject.scene))
            { error = "Choose an ordinary scene RectTransform outside prefab instances and Prefab Mode."; return false; }
            if (string.IsNullOrWhiteSpace(rootName) || rootName != rootName.Trim() || rootName.Contains("/"))
            { error = "Give the new root a nonblank name without slashes or surrounding spaces."; return false; }
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == rootName)
                { error = "The target already has a child named '" + rootName + "'. Choose another root name."; return false; }
            return true;
        }

        /// <summary>Create a fresh hierarchy, remap internal references and restore active states only after every component is populated.</summary>
        public static bool TryCreate(UIHierarchyMold mold, RectTransform parent, string rootName, out GameObject root, out string error)
        {
            root = null;
            error = null;
            if (creating) { error = "A mold creation is already in progress."; return false; }
            if (!Validate(mold, parent, rootName, out error)) return false;
            var objects = new List<GameObject>();
            var components = new List<List<Component>>();
            UnityEngine.Object[] selection = Selection.objects;
            creating = true;
            int group = -1;
            using (AdvancedUIBindingIntegration.SuspendAutomaticGeneration())
            try
            {
                Undo.IncrementCurrentGroup(); group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Create Layout from Mold");
                // Build inactive objects first. Scripts can still execute editor callbacks when added.
                for (int i = 0; i < mold.nodes.Count; i++)
                {
                    UIMoldNode node = mold.nodes[i];
                    var item = new GameObject(i == 0 ? rootName : node.name, node.rectTransform ? typeof(RectTransform) : typeof(Transform));
                    objects.Add(item);
                    item.SetActive(false);
                    Undo.RegisterCreatedObjectUndo(item, "Create Mold Object");
                    if (item.scene != parent.gameObject.scene) Undo.MoveGameObjectToScene(item, parent.gameObject.scene, "Place Mold in Scene");
                    Undo.SetTransformParent(item.transform, i == 0 ? parent : objects[node.parent].transform, "Parent Mold Object");
                    components.Add(new List<Component>());
                }
                for (int i = 0; i < mold.nodes.Count; i++)
                    foreach (UIMoldComponent stored in mold.nodes[i].components)
                    {
                        Type type = ResolveType(stored.typeName);
                        Component found = null;
                        foreach (Component candidate in objects[i].GetComponents<Component>())
                            if (candidate != null && candidate.GetType() == type && !components[i].Contains(candidate)) { found = candidate; break; }
                        // Reuse exact-type RequireComponent additions; never merge distinct slots of the same type.
                        if (found == null) found = Undo.AddComponent(objects[i], type);
                        if (found == null || found.GetType() != type || components[i].Contains(found))
                            throw new InvalidOperationException("Cannot add " + type.FullName + " to " + objects[i].name + ".");
                        components[i].Add(found);
                    }
                for (int i = 0; i < objects.Count; i++)
                {
                    // Changed script dependencies must not silently add uncaptured components.
                    if (objects[i].GetComponents<Component>().Length != components[i].Count + 1)
                        throw new InvalidOperationException("Component dependencies changed on " + objects[i].name + ". Capture a new mold.");
                    for (int c = 0; c < components[i].Count; c++)
                        Restore(components[i][c], mold.nodes[i].components[c], objects, components);
                }
                // Serialized component callbacks may affect transforms; apply the captured transform last.
                for (int i = 0; i < objects.Count; i++)
                {
                    UIMoldNode node = mold.nodes[i];
                    GameObject item = objects[i];
                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { item, item.transform }, "Restore Mold Layout");
                    item.layer = node.layer;
                    item.transform.localRotation = node.localRotation; item.transform.localScale = node.localScale;
                    if (item.transform is RectTransform rect)
                    {
                        rect.anchorMin = node.anchorMin; rect.anchorMax = node.anchorMax; rect.pivot = node.pivot;
                        rect.sizeDelta = node.sizeDelta; rect.anchoredPosition3D = node.anchoredPosition;
                    }
                    else item.transform.localPosition = node.localPosition;
                }
                // Root activation is last so descendants cannot become active while links are being restored.
                for (int i = objects.Count - 1; i >= 0; i--) objects[i].SetActive(mold.nodes[i].active);
                Undo.CollapseUndoOperations(group);
                root = objects[0];
                EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
                return true;
            }
            catch (Exception exception)
            {
                error = "Mold creation failed: " + exception.Message;
                if (group >= 0)
                    try { Undo.RevertAllDownToGroup(group); }
                    catch (Exception rollback) { error += " Undo rollback failed: " + rollback.Message; }
                // Also cover allocation failures before an object was registered with Undo.
                for (int i = objects.Count - 1; i >= 0; i--)
                    if (objects[i] != null)
                        try { UnityEngine.Object.DestroyImmediate(objects[i]); }
                        catch (Exception cleanup) { error += " Cleanup failed: " + cleanup.Message; }
                return false;
            }
            finally
            {
                try { Selection.objects = selection; }
                finally { creating = false; }
            }
        }

        /// <summary>Resolve an installed, concrete component type; missing scripts never become placeholder objects.</summary>
        private static Type ResolveType(string name)
        {
            Type type = string.IsNullOrEmpty(name) ? null : Type.GetType(name, false);
            if (type == null || !typeof(Component).IsAssignableFrom(type) || typeof(Transform).IsAssignableFrom(type) || type.IsAbstract || type.ContainsGenericParameters)
                throw new InvalidOperationException("Component type unavailable: " + name + ". Install its script/package or recapture the mold.");
            return type;
        }

        /// <summary>Restore a component in property order, resizing arrays before their elements and remapping references after all objects exist.</summary>
        private static void Restore(Component component, UIMoldComponent stored, List<GameObject> objects, List<List<Component>> components)
        {
            if (component is AdvancedUIToolkit.UIBindingRoot) return;
            Undo.RegisterCompleteObjectUndo(component, "Restore Mold Component");
            using (var serialized = new SerializedObject(component))
            {
                foreach (UIMoldProperty value in stored.properties)
                {
                    SerializedProperty property = serialized.FindProperty(value.path);
                    if (property == null || (int)property.propertyType != value.kind || property.type != value.valueType)
                        throw new InvalidOperationException(component.GetType().Name + " property changed: " + value.path + ". Recapture with the installed scripts.");
                    if (property.propertyType == SerializedPropertyType.Generic) continue;
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        UnityEngine.Object reference = value.node < 0 ? null : value.component == -1 ? (UnityEngine.Object)objects[value.node] :
                            value.component == -2 ? objects[value.node].transform : components[value.node][value.component];
                        property.objectReferenceValue = reference;
                        if (property.objectReferenceValue != reference)
                            throw new InvalidOperationException("Cannot restore internal reference at " + value.path + ".");
                    }
                    else UIMoldPropertyCodec.Write(property, value);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // Newly added script fields can carry new defaults and external references; require recapture instead.
                serialized.Update();
                var expected = new HashSet<string>(StringComparer.Ordinal);
                foreach (UIMoldProperty value in stored.properties) expected.Add(value.path);
                SerializedProperty iterator = serialized.GetIterator();
                bool enter = true;
                while (iterator.Next(enter))
                {
                    enter = false;
                    if (UIMoldPropertyCodec.IsInfrastructure(iterator.propertyPath)) continue;
                    if (!expected.Remove(iterator.propertyPath))
                        throw new InvalidOperationException(component.GetType().Name + " gained a field: " + iterator.propertyPath + ". Recapture the mold.");
                    enter = iterator.propertyType == SerializedPropertyType.Generic;
                }
                if (expected.Count != 0) throw new InvalidOperationException(component.GetType().Name + " changed its serialized fields during restore. Recapture the mold.");
            }
        }
    }
}
