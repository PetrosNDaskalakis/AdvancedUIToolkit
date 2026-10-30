using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AdvancedUIAuthoring.Integration.Editor
{
    /// <summary>Loss-checked serialization for component value fields; object references are handled by the hierarchy service.</summary>
    internal static class UIMoldPropertyCodec
    {
        /// <summary>Concrete JSON wrapper for Integer values.</summary>
        [Serializable]
        private sealed class IntegerValue
        {
            /// <summary>Portable field value.</summary>
            public long value;
        }

        /// <summary>Concrete JSON wrapper for Boolean values.</summary>
        [Serializable]
        private sealed class BooleanValue
        {
            /// <summary>Portable field value.</summary>
            public bool value;
        }

        /// <summary>Concrete JSON wrapper for Float values.</summary>
        [Serializable]
        private sealed class FloatValue
        {
            /// <summary>Portable field value.</summary>
            public double value;
        }

        /// <summary>Concrete JSON wrapper for String values.</summary>
        [Serializable]
        private sealed class StringValue
        {
            /// <summary>Portable field value.</summary>
            public string value;
        }

        /// <summary>Concrete JSON wrapper for Color values.</summary>
        [Serializable]
        private sealed class ColorValue
        {
            /// <summary>Portable field value.</summary>
            public Color value;
        }

        /// <summary>Concrete JSON wrapper for LayerMask values.</summary>
        [Serializable]
        private sealed class LayerMaskValue
        {
            /// <summary>Portable field value.</summary>
            public int value;
        }

        /// <summary>Concrete JSON wrapper for Enum values.</summary>
        [Serializable]
        private sealed class EnumValue
        {
            /// <summary>Portable field value.</summary>
            public int value;
        }

        /// <summary>Concrete JSON wrapper for Vector2 values.</summary>
        [Serializable]
        private sealed class Vector2Value
        {
            /// <summary>Portable field value.</summary>
            public Vector2 value;
        }

        /// <summary>Concrete JSON wrapper for Vector3 values.</summary>
        [Serializable]
        private sealed class Vector3Value
        {
            /// <summary>Portable field value.</summary>
            public Vector3 value;
        }

        /// <summary>Concrete JSON wrapper for Vector4 values.</summary>
        [Serializable]
        private sealed class Vector4Value
        {
            /// <summary>Portable field value.</summary>
            public Vector4 value;
        }

        /// <summary>Concrete JSON wrapper for Rect values.</summary>
        [Serializable]
        private sealed class RectValue
        {
            /// <summary>Portable field value.</summary>
            public Rect value;
        }

        /// <summary>Concrete JSON wrapper for ArraySize values.</summary>
        [Serializable]
        private sealed class ArraySizeValue
        {
            /// <summary>Portable field value.</summary>
            public int value;
        }

        /// <summary>Concrete JSON wrapper for Character values.</summary>
        [Serializable]
        private sealed class CharacterValue
        {
            /// <summary>Portable field value.</summary>
            public int value;
        }

        /// <summary>Concrete JSON wrapper for AnimationCurve values.</summary>
        [Serializable]
        private sealed class AnimationCurveValue
        {
            /// <summary>Portable field value.</summary>
            public AnimationCurve value;
        }

        /// <summary>Concrete JSON wrapper for Bounds values.</summary>
        [Serializable]
        private sealed class BoundsValue
        {
            /// <summary>Portable field value.</summary>
            public Bounds value;
        }

        /// <summary>Concrete JSON wrapper for Gradient values.</summary>
        [Serializable]
        private sealed class GradientValue
        {
            /// <summary>Portable field value.</summary>
            public Gradient value;
        }

        /// <summary>Concrete JSON wrapper for Quaternion values.</summary>
        [Serializable]
        private sealed class QuaternionValue
        {
            /// <summary>Portable field value.</summary>
            public Quaternion value;
        }

        /// <summary>Concrete JSON wrapper for Vector2Int values.</summary>
        [Serializable]
        private sealed class Vector2IntValue
        {
            /// <summary>Portable field value.</summary>
            public Vector2Int value;
        }

        /// <summary>Concrete JSON wrapper for Vector3Int values.</summary>
        [Serializable]
        private sealed class Vector3IntValue
        {
            /// <summary>Portable field value.</summary>
            public Vector3Int value;
        }

        /// <summary>Concrete JSON wrapper for RectInt values.</summary>
        [Serializable]
        private sealed class RectIntValue
        {
            /// <summary>Portable field value.</summary>
            public RectInt value;
        }

        /// <summary>Concrete JSON wrapper for BoundsInt values.</summary>
        [Serializable]
        private sealed class BoundsIntValue
        {
            /// <summary>Portable field value.</summary>
            public BoundsInt value;
        }

        /// <summary>Concrete JSON wrapper for Hash128 values.</summary>
        [Serializable]
        private sealed class Hash128Value
        {
            /// <summary>Portable field value.</summary>
            public string value;
        }

        /// <summary>Concrete JSON wrapper for Unsigned values.</summary>
        [Serializable]
        private sealed class UnsignedValue
        {
            /// <summary>Portable field value.</summary>
            public ulong value;
        }

        /// <summary>Properties owned by Unity's object/prefab infrastructure, never portable component settings.</summary>
        internal static bool IsInfrastructure(string path)
        {
            return path == "m_ObjectHideFlags" || path == "m_CorrespondingSourceObject" ||
                path == "m_PrefabInstance" || path == "m_PrefabAsset" || path == "m_GameObject" ||
                path == "m_Script" || path == "m_EditorClassIdentifier" || path == "m_EditorHideFlags" || path == "m_Name";
        }

        /// <summary>Recognize only property forms implemented by this codec or the internal-reference mapper.</summary>
        internal static bool IsSupportedKind(int kind)
        {
            switch ((SerializedPropertyType)kind)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Hash128:
                    return true;
                default: return false;
            }
        }

        /// <summary>Capture a supported leaf; unsupported serialized forms block capture with a path-specific message.</summary>
        internal static string Read(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Integer && IsUnsigned64(property))
                return JsonUtility.ToJson(new UnsignedValue { value = property.ulongValue });
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return JsonUtility.ToJson(new IntegerValue { value = property.longValue });
                case SerializedPropertyType.Boolean: return JsonUtility.ToJson(new BooleanValue { value = property.boolValue });
                case SerializedPropertyType.Float: return JsonUtility.ToJson(new FloatValue { value = property.doubleValue });
                case SerializedPropertyType.String: return JsonUtility.ToJson(new StringValue { value = property.stringValue });
                case SerializedPropertyType.Color: return JsonUtility.ToJson(new ColorValue { value = property.colorValue });
                case SerializedPropertyType.LayerMask: return JsonUtility.ToJson(new LayerMaskValue { value = property.intValue });
                case SerializedPropertyType.Enum: return JsonUtility.ToJson(new EnumValue { value = property.intValue });
                case SerializedPropertyType.Vector2: return JsonUtility.ToJson(new Vector2Value { value = property.vector2Value });
                case SerializedPropertyType.Vector3: return JsonUtility.ToJson(new Vector3Value { value = property.vector3Value });
                case SerializedPropertyType.Vector4: return JsonUtility.ToJson(new Vector4Value { value = property.vector4Value });
                case SerializedPropertyType.Rect: return JsonUtility.ToJson(new RectValue { value = property.rectValue });
                case SerializedPropertyType.ArraySize: return JsonUtility.ToJson(new ArraySizeValue { value = property.intValue });
                case SerializedPropertyType.Character: return JsonUtility.ToJson(new CharacterValue { value = property.intValue });
                case SerializedPropertyType.AnimationCurve: return JsonUtility.ToJson(new AnimationCurveValue { value = property.animationCurveValue });
                case SerializedPropertyType.Bounds: return JsonUtility.ToJson(new BoundsValue { value = property.boundsValue });
                case SerializedPropertyType.Gradient: return JsonUtility.ToJson(new GradientValue { value = property.gradientValue });
                case SerializedPropertyType.Quaternion: return JsonUtility.ToJson(new QuaternionValue { value = property.quaternionValue });
                case SerializedPropertyType.Vector2Int: return JsonUtility.ToJson(new Vector2IntValue { value = property.vector2IntValue });
                case SerializedPropertyType.Vector3Int: return JsonUtility.ToJson(new Vector3IntValue { value = property.vector3IntValue });
                case SerializedPropertyType.RectInt: return JsonUtility.ToJson(new RectIntValue { value = property.rectIntValue });
                case SerializedPropertyType.BoundsInt: return JsonUtility.ToJson(new BoundsIntValue { value = property.boundsIntValue });
                case SerializedPropertyType.Hash128: return JsonUtility.ToJson(new Hash128Value { value = property.hash128Value.ToString() });
                default: throw new InvalidOperationException("Unsupported property " + property.propertyPath + " (" + property.propertyType + "). Capture is stopped; no fields were silently dropped.");
            }
        }

        /// <summary>Restore a supported leaf after checking the destination still has the recorded kind.</summary>
        internal static void Write(SerializedProperty property, UIMoldProperty stored)
        {
            if ((int)property.propertyType != stored.kind)
                throw new InvalidOperationException("Property type changed: " + stored.path + ". Capture a new mold with the installed scripts.");
            if (string.IsNullOrEmpty(stored.value)) throw new InvalidOperationException("Missing mold value at " + stored.path + ".");
            if (property.propertyType == SerializedPropertyType.Integer && IsUnsigned64(property))
            { property.ulongValue = JsonUtility.FromJson<UnsignedValue>(stored.value).value; return; }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: property.longValue = JsonUtility.FromJson<IntegerValue>(stored.value).value; break;
                case SerializedPropertyType.Boolean: property.boolValue = JsonUtility.FromJson<BooleanValue>(stored.value).value; break;
                case SerializedPropertyType.Float: property.doubleValue = JsonUtility.FromJson<FloatValue>(stored.value).value; break;
                case SerializedPropertyType.String: property.stringValue = JsonUtility.FromJson<StringValue>(stored.value).value; break;
                case SerializedPropertyType.Color: property.colorValue = JsonUtility.FromJson<ColorValue>(stored.value).value; break;
                case SerializedPropertyType.LayerMask: property.intValue = JsonUtility.FromJson<LayerMaskValue>(stored.value).value; break;
                case SerializedPropertyType.Enum: property.intValue = JsonUtility.FromJson<EnumValue>(stored.value).value; break;
                case SerializedPropertyType.Vector2: property.vector2Value = JsonUtility.FromJson<Vector2Value>(stored.value).value; break;
                case SerializedPropertyType.Vector3: property.vector3Value = JsonUtility.FromJson<Vector3Value>(stored.value).value; break;
                case SerializedPropertyType.Vector4: property.vector4Value = JsonUtility.FromJson<Vector4Value>(stored.value).value; break;
                case SerializedPropertyType.Rect: property.rectValue = JsonUtility.FromJson<RectValue>(stored.value).value; break;
                case SerializedPropertyType.ArraySize: property.intValue = JsonUtility.FromJson<ArraySizeValue>(stored.value).value; break;
                case SerializedPropertyType.Character: property.intValue = JsonUtility.FromJson<CharacterValue>(stored.value).value; break;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = JsonUtility.FromJson<AnimationCurveValue>(stored.value).value; break;
                case SerializedPropertyType.Bounds: property.boundsValue = JsonUtility.FromJson<BoundsValue>(stored.value).value; break;
                case SerializedPropertyType.Gradient: property.gradientValue = JsonUtility.FromJson<GradientValue>(stored.value).value; break;
                case SerializedPropertyType.Quaternion: property.quaternionValue = JsonUtility.FromJson<QuaternionValue>(stored.value).value; break;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = JsonUtility.FromJson<Vector2IntValue>(stored.value).value; break;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = JsonUtility.FromJson<Vector3IntValue>(stored.value).value; break;
                case SerializedPropertyType.RectInt: property.rectIntValue = JsonUtility.FromJson<RectIntValue>(stored.value).value; break;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = JsonUtility.FromJson<BoundsIntValue>(stored.value).value; break;
                case SerializedPropertyType.Hash128: property.hash128Value = Hash128.Parse(JsonUtility.FromJson<Hash128Value>(stored.value).value); break;
                default: throw new InvalidOperationException("Unsupported property " + stored.path + ".");
            }
        }

        /// <summary>Unity names the same unsigned 64-bit serialized field differently for some native/managed types.</summary>
        private static bool IsUnsigned64(SerializedProperty property)
        {
            return property.type == "ulong" || property.type == "UInt64" || property.type == "unsigned long long";
        }
    }
}
