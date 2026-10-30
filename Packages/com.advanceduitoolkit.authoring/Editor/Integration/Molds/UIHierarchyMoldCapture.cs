using System;
using System.Collections.Generic;
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Read-only capture of a loaded scene hierarchy into portable mold data.</summary>
    public static class UIHierarchyMoldCapture
    {
        /// <summary>Use the existing root-selection eligibility rule on the exact source, never substituting an ancestor.</summary>
        public static bool IsValidSource(GameObject root)
        {
            return root != null && AdvancedUIEditorIntegration.IsValidCreationParent(root.GetComponent<RectTransform>()) &&
                !EditorSceneManager.IsPreviewScene(root.scene) && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>Capture without cloning, unpacking, editing, selecting or creating scene objects. The caller owns the returned transient asset.</summary>
        public static bool TryCapture(GameObject root, out UIHierarchyMold mold, out string error)
        {
            mold = null;
            error = null;
            if (!IsValidSource(root) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { error = "Choose a selectable scene RectTransform outside prefab instances and Prefab Mode, and wait for Unity to finish importing."; return false; }
            UIHierarchyMold snapshot = ScriptableObject.CreateInstance<UIHierarchyMold>();
            snapshot.hideFlags = HideFlags.HideAndDontSave;
            snapshot.name = root.name + " Mold";
            try
            {
                var transforms = new List<Transform>();
                Collect(root.transform, -1, snapshot, transforms);
                var references = new Dictionary<UnityEngine.Object, UIMoldProperty>();
                var components = new List<Component[]>();
                for (int i = 0; i < transforms.Count; i++)
                {
                    Transform current = transforms[i];
                    references.Add(current.gameObject, new UIMoldProperty { node = i, component = -1 });
                    references.Add(current, new UIMoldProperty { node = i, component = -2 });
                    var slots = new List<Component>();
                    foreach (Component component in current.GetComponents<Component>())
                    {
                        if (component == null) throw new InvalidOperationException("Missing script on " + PathOf(transforms, snapshot, i) + ". Repair it before capturing.");
                        if (component is Transform) continue;
                        references.Add(component, new UIMoldProperty { node = i, component = slots.Count });
                        slots.Add(component);
                    }
                    components.Add(slots.ToArray());
                }
                for (int i = 0; i < transforms.Count; i++)
                    foreach (Component component in components[i])
                    {
                        var stored = new UIMoldComponent { typeName = component.GetType().AssemblyQualifiedName };
                        snapshot.nodes[i].components.Add(stored);
                        string location = PathOf(transforms, snapshot, i) + " / " + component.GetType().Name;
                        // Binding identities and file ownership belong to Binder, never to a reusable structural mold.
                        if (component is UIBindingRoot)
                        { snapshot.omissions.Add(location + ": binding manifest and output configuration reset."); continue; }
                        ReadProperties(component, stored, references, snapshot.omissions, location);
                    }
                // Apply the same portable-data contract to new captures and loaded assets.
                if (!UIHierarchyMoldBuilder.ValidateMold(snapshot, out string validationError))
                    throw new InvalidOperationException(validationError);
                mold = snapshot;
                return true;
            }
            catch (Exception exception)
            {
                UnityEngine.Object.DestroyImmediate(snapshot);
                error = "Cannot capture mold: " + exception.Message;
                return false;
            }
        }

        /// <summary>Capture eligible containers in sibling order; prune each ineligible branch before reading its components or descendants.</summary>
        private static void Collect(Transform source, int parent, UIHierarchyMold mold, List<Transform> transforms, int depth = 0)
        {
            if (!IsValidSource(source.gameObject))
            {
                string location = parent < 0 ? source.name : PathOf(transforms, mold, parent) + "/" + source.name;
                mold.omissions.Add(location + ": excluded non-selectable branch, including all children.");
                return;
            }
            if (depth > UIHierarchyMold.MaximumDepth) throw new InvalidOperationException("This preview supports hierarchy depth up to 256.");
            int index = mold.nodes.Count;
            if (index >= UIHierarchyMold.MaximumNodeCount) throw new InvalidOperationException("This preview supports at most 10,000 objects per mold.");
            var node = new UIMoldNode
            {
                name = source.name, parent = parent, active = source.gameObject.activeSelf,
                layer = source.gameObject.layer, rectTransform = source is RectTransform,
                localPosition = source.localPosition, localRotation = source.localRotation, localScale = source.localScale
            };
            if (source is RectTransform rect)
            {
                node.anchorMin = rect.anchorMin; node.anchorMax = rect.anchorMax;
                node.pivot = rect.pivot; node.sizeDelta = rect.sizeDelta; node.anchoredPosition = rect.anchoredPosition3D;
            }
            mold.nodes.Add(node);
            transforms.Add(source);
            for (int i = 0; i < source.childCount; i++) Collect(source.GetChild(i), index, mold, transforms, depth + 1);
        }

        /// <summary>Capture parent-first property values and translate every Unity object reference into an internal slot or null.</summary>
        private static void ReadProperties(Component source, UIMoldComponent stored,
            Dictionary<UnityEngine.Object, UIMoldProperty> references, List<string> omissions, string location)
        {
            using (var serialized = new SerializedObject(source))
            {
                SerializedProperty property = serialized.GetIterator();
                bool enter = true;
                while (property.Next(enter))
                {
                    enter = false;
                    if (UIMoldPropertyCodec.IsInfrastructure(property.propertyPath)) continue;
                    var value = new UIMoldProperty { path = property.propertyPath, kind = (int)property.propertyType, valueType = property.type };
                    if (property.propertyType == SerializedPropertyType.Generic)
                    {
                        // Generic container markers also detect removed or renamed empty arrays/structs during restore.
                        enter = true;
                    }
                    else if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        UnityEngine.Object reference = property.objectReferenceValue;
                        if (reference != null && references.TryGetValue(reference, out UIMoldProperty target))
                        { value.node = target.node; value.component = target.component; }
                        else if (reference != null || property.objectReferenceInstanceIDValue != 0)
                            omissions.Add(location + " / " + property.propertyPath + ": reference outside the captured containers cleared" +
                                (reference != null ? " (" + reference.name + ")." : " (missing object)."));
                    }
                    else
                    {
                        try { value.value = UIMoldPropertyCodec.Read(property); }
                        catch (Exception exception) { throw new InvalidOperationException(location + ": " + exception.Message, exception); }
                    }
                    stored.properties.Add(value);
                }
            }
        }

        /// <summary>Produce a readable indexed location without using names as reference identity.</summary>
        private static string PathOf(List<Transform> transforms, UIHierarchyMold mold, int index)
        {
            string result = transforms[index].name + " [" + index + "]";
            for (int parent = mold.nodes[index].parent; parent >= 0; parent = mold.nodes[parent].parent)
                result = transforms[parent].name + "/" + result;
            return result;
        }
    }
}
