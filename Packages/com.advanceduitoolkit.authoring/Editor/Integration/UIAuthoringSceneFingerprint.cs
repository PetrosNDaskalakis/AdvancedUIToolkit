using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AdvancedUIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Conservative serialized-state fingerprints for saved scene objects; no writes or layout rebuilds.</summary>
    internal static class UIAuthoringSceneFingerprint
    {
        /// <summary>Culture used for stable numeric formatting.</summary>
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <summary>SHA-256 over UTF-8 input; record format fixes the serialization algorithm version.</summary>
        internal static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>Append a length-prefixed token so field separators cannot create ambiguous input.</summary>
        internal static void Token(StringBuilder builder, string value)
        {
            string text = value ?? string.Empty;
            builder.Append(text.Length.ToString(Invariant)).Append(':').Append(text);
        }

        /// <summary>Get a persistent identity and verify that it resolves to the same currently loaded object.</summary>
        internal static string Identity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(value);
            if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) != value)
                throw new InvalidOperationException("Cannot resolve a stable identity for '" + value.name +
                    "'. Save its scene/asset and keep referenced scenes loaded.");
            return id.ToString();
        }

        /// <summary>Resolve a stored identifier without opening scenes or selecting objects.</summary>
        internal static UnityEngine.Object Resolve(string value)
        {
            GlobalObjectId id;
            return GlobalObjectId.TryParse(value, out id) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) : null;
        }

        /// <summary>Hash one schema node and prefab internals, stopping at other explicitly tracked schema roots.</summary>
        internal static string Node(GameObject root, Dictionary<GameObject, string> tracked)
        {
            if (root == null) throw new InvalidOperationException("The recorded scene object is missing.");
            var builder = new StringBuilder();
            AppendObject(builder, root, root, tracked, 0);
            return Hash(builder.ToString());
        }

        /// <summary>Track the external parent's layout configuration, excluding unrelated scene siblings and canvas size.</summary>
        internal static string ParentLayout(RectTransform parent)
        {
            var builder = new StringBuilder();
            foreach (LayoutGroup group in parent.GetComponents<LayoutGroup>()) AppendComponent(builder, group);
            return Hash(builder.ToString());
        }

        /// <summary>Walk owned descendants, retaining ordering and detecting added/removed components or children.</summary>
        private static void AppendObject(StringBuilder builder, GameObject current, GameObject root,
            Dictionary<GameObject, string> tracked, int depth)
        {
            if (depth > 256) throw new InvalidOperationException("Hierarchy inspection is limited to 256 levels.");
            string id;
            if (current != root && tracked.TryGetValue(current, out id))
            {
                Token(builder, "schema-child");
                Token(builder, id);
                return;
            }
            Token(builder, "object");
            Token(builder, current.name);
            Token(builder, current.activeSelf.ToString());
            Token(builder, current.layer.ToString(Invariant));
            Token(builder, current.tag);
            Token(builder, Identity(PrefabUtility.GetCorrespondingObjectFromSource(current)));
            foreach (Component component in current.GetComponents<Component>())
            {
                if (component == null) throw new InvalidOperationException("Missing script on '" + current.name + "'.");
                // Binder owns its manifest. Syncing it must not appear to modify authoring-owned UI state.
                if (component is UIBindingRoot) continue;
                AppendComponent(builder, component);
            }
            Token(builder, current.transform.childCount.ToString(Invariant));
            for (int index = 0; index < current.transform.childCount; index++)
                AppendObject(builder, current.transform.GetChild(index).gameObject, root, tracked, depth + 1);
        }

        /// <summary>Serialize component properties with persistent references instead of transient instance IDs.</summary>
        private static void AppendComponent(StringBuilder builder, Component component)
        {
            Token(builder, component.GetType().AssemblyQualifiedName);
            Token(builder, Identity(PrefabUtility.GetCorrespondingObjectFromSource(component)));
            RectTransform rect = component as RectTransform;
            LayoutGroup parentGroup = rect != null && rect.parent != null ? rect.parent.GetComponent<LayoutGroup>() : null;
            bool drivenRect = rect != null && (rect.drivenByObject != null || (parentGroup != null && parentGroup.enabled));
            if (rect != null) Token(builder, drivenRect ? "layout-driven" : "authored-rect");
            if (rect != null) Token(builder, rect.localPosition.z.ToString("R", Invariant));
            using (var serialized = new SerializedObject(component))
            {
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                int inspected = 0;
                while (property.Next(enterChildren))
                {
                    if (++inspected > 50000) throw new InvalidOperationException("Serialized inspection exceeded 50000 properties on " + component.GetType().Name);
                    enterChildren = true;
                    string path = property.propertyPath;
                    if (Skip(path, rect != null, drivenRect)) { enterChildren = false; continue; }
                    Token(builder, path);
                    Token(builder, property.type);
                    Token(builder, ((int)property.propertyType).ToString(Invariant));
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Generic:
                            break;
                        case SerializedPropertyType.Integer:
                            Token(builder, property.longValue.ToString(Invariant)); enterChildren = false; break;
                        case SerializedPropertyType.Boolean:
                            Token(builder, property.boolValue.ToString()); enterChildren = false; break;
                        case SerializedPropertyType.Float:
                            Token(builder, property.doubleValue.ToString("R", Invariant)); enterChildren = false; break;
                        case SerializedPropertyType.String:
                            Token(builder, property.stringValue); enterChildren = false; break;
                        case SerializedPropertyType.ObjectReference:
                            if (property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                throw new InvalidOperationException("Missing reference at " + component.GetType().Name + "." + path);
                            Token(builder, Identity(property.objectReferenceValue)); enterChildren = false; break;
                        case SerializedPropertyType.Enum:
                        case SerializedPropertyType.LayerMask:
                        case SerializedPropertyType.Character:
                        case SerializedPropertyType.ArraySize:
                            Token(builder, property.intValue.ToString(Invariant)); enterChildren = false; break;
                        case SerializedPropertyType.Color:
                            Token(builder, JsonUtility.ToJson(property.colorValue)); enterChildren = false; break;
                        case SerializedPropertyType.Vector2:
                            Token(builder, JsonUtility.ToJson(property.vector2Value)); enterChildren = false; break;
                        case SerializedPropertyType.Vector3:
                            Token(builder, JsonUtility.ToJson(property.vector3Value)); enterChildren = false; break;
                        case SerializedPropertyType.Vector4:
                            Token(builder, JsonUtility.ToJson(property.vector4Value)); enterChildren = false; break;
                        case SerializedPropertyType.Rect:
                            Token(builder, JsonUtility.ToJson(property.rectValue)); enterChildren = false; break;
                        case SerializedPropertyType.Bounds:
                            Token(builder, JsonUtility.ToJson(property.boundsValue)); enterChildren = false; break;
                        case SerializedPropertyType.Quaternion:
                            Token(builder, JsonUtility.ToJson(property.quaternionValue)); enterChildren = false; break;
                        case SerializedPropertyType.Vector2Int:
                            Token(builder, JsonUtility.ToJson(property.vector2IntValue)); enterChildren = false; break;
                        case SerializedPropertyType.Vector3Int:
                            Token(builder, JsonUtility.ToJson(property.vector3IntValue)); enterChildren = false; break;
                        case SerializedPropertyType.RectInt:
                            Token(builder, JsonUtility.ToJson(property.rectIntValue)); enterChildren = false; break;
                        case SerializedPropertyType.BoundsInt:
                            Token(builder, JsonUtility.ToJson(property.boundsIntValue)); enterChildren = false; break;
                        case SerializedPropertyType.AnimationCurve:
                            AppendCurve(builder, property.animationCurveValue); enterChildren = false; break;
                        case SerializedPropertyType.FixedBufferSize:
                            Token(builder, property.fixedBufferSize.ToString(Invariant)); enterChildren = false; break;
                        case SerializedPropertyType.ManagedReference:
                            Token(builder, property.managedReferenceFullTypename); break;
                        case SerializedPropertyType.Gradient:
                            // Gradients contain value data, not scene-object references.
                            Token(builder, property.contentHash.ToString()); enterChildren = false; break;
                        case SerializedPropertyType.Hash128:
                            Token(builder, property.hash128Value.ToString()); enterChildren = false; break;
                        default:
                            throw new InvalidOperationException("Unsupported serialized property " + property.propertyType +
                                " at " + component.GetType().Name + "." + path + "; no safe baseline comparison is available.");
                    }
                }
            }
        }

        /// <summary>Exclude internal linkage and derived RectTransform geometry; structure is tracked separately.</summary>
        private static bool Skip(string path, bool rect, bool driven)
        {
            if (path == "m_ObjectHideFlags" || path == "m_CorrespondingSourceObject" ||
                path == "m_PrefabInstance" || path == "m_PrefabAsset" || path == "m_GameObject" ||
                path == "m_Children" || path == "m_Father" || path == "m_RootOrder") return true;
            if (rect && path == "m_LocalPosition") return true;
            if (rect && driven && (path == "m_AnchoredPosition" || path == "m_SizeDelta" ||
                path == "m_AnchorMin" || path == "m_AnchorMax" || path == "m_Pivot")) return true;
            return false;
        }

        /// <summary>Canonical animation-curve values without runtime object identities.</summary>
        private static void AppendCurve(StringBuilder builder, AnimationCurve curve)
        {
            if (curve == null) { Token(builder, "null"); return; }
            Token(builder, ((int)curve.preWrapMode).ToString(Invariant));
            Token(builder, ((int)curve.postWrapMode).ToString(Invariant));
            foreach (Keyframe key in curve.keys)
            {
                Token(builder, key.time.ToString("R", Invariant));
                Token(builder, key.value.ToString("R", Invariant));
                Token(builder, key.inTangent.ToString("R", Invariant));
                Token(builder, key.outTangent.ToString("R", Invariant));
                Token(builder, key.inWeight.ToString("R", Invariant));
                Token(builder, key.outWeight.ToString("R", Invariant));
                Token(builder, ((int)key.weightedMode).ToString(Invariant));
            }
        }
    }
}
