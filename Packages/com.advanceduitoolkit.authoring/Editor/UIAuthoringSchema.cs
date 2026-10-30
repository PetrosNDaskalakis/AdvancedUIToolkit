using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>A versioned, ordered description of a native uGUI hierarchy.</summary>
    [Serializable]
    public sealed class UIAuthoringDocument
    {
        /// <summary>Schema format version. The initial implementation supports version 1.</summary>
        public int version = 1;

        /// <summary>A human-readable label used for the Undo operation.</summary>
        public string name = "New UI";

        /// <summary>Elements in parent-before-child order. No Unity object references are serialized.</summary>
        public List<UIAuthoringNode> nodes = new List<UIAuthoringNode>();
    }

    /// <summary>A single UI element identified independently of its display name.</summary>
    [Serializable]
    public sealed class UIAuthoringNode
    {
        /// <summary>Unique element ID within its document.</summary>
        public string id = string.Empty;

        /// <summary>Previously declared parent ID, or empty to use the selected scene parent.</summary>
        public string parentId = string.Empty;

        /// <summary>Registered element kind (container, text, image, button, or an extension).</summary>
        public string kind = "container";

        /// <summary>Name of the created Unity GameObject.</summary>
        public string name = "New Element";

        /// <summary>Displayed text for built-in text and button elements.</summary>
        public string text = string.Empty;

        /// <summary>Optional built-in visual and layout properties.</summary>
        public UIAuthoringStyle style = new UIAuthoringStyle();

        /// <summary>Optional registered style-class key. No CSS parser is bundled.</summary>
        public string styleClass = string.Empty;

        /// <summary>Additional ordered stylesheet or registered style class names.</summary>
        public List<string> styleClasses = new List<string>();

        /// <summary>Optional inline declarations; these override stylesheet classes and legacy style fields.</summary>
        public List<UIAuthoringStyleDeclaration> styleProperties = new List<UIAuthoringStyleDeclaration>();
    }

    /// <summary>
    /// Small, intentionally explicit native-uGUI styling contract; missing fields are safe because
    /// numeric fields use zero as the unset/default value (compatible with older JsonUtility).
    /// </summary>
    [Serializable]
    public sealed class UIAuthoringStyle
    {
        /// <summary>Preferred width in canvas units, or zero for unspecified.</summary>
        public float width;

        /// <summary>Preferred height in canvas units, or zero for unspecified.</summary>
        public float height;

        /// <summary>Background color in #RRGGBB or #RRGGBBAA format.</summary>
        public string background = string.Empty;

        /// <summary>Text color in #RRGGBB or #RRGGBBAA format.</summary>
        public string textColor = string.Empty;

        /// <summary>Text font size, or zero for unspecified.</summary>
        public int fontSize;

        /// <summary>Empty, horizontal, or vertical. Layout is implemented with uGUI Layout Groups.</summary>
        public string layout = string.Empty;

        /// <summary>Nonnegative layout group spacing, defaulting to zero.</summary>
        public float spacing;

        /// <summary>Nonnegative uniform Layout Group padding, defaulting to zero.</summary>
        public int padding;

        /// <summary>Optional text alignment: left, center, or right.</summary>
        public string textAlignment = string.Empty;

        /// <summary>Named RectTransform anchor preset (including stretch variants).</summary>
        public string anchor = string.Empty;

        /// <summary>Explicit pivot X in [0,1], or empty for the existing pivot.</summary>
        public string pivotX = string.Empty;

        /// <summary>Explicit pivot Y in [0,1], or empty for the existing pivot.</summary>
        public string pivotY = string.Empty;

        /// <summary>Anchored local X offset; empty means preserve the native default.</summary>
        public string positionX = string.Empty;

        /// <summary>Anchored local Y offset; empty means preserve the native default.</summary>
        public string positionY = string.Empty;

        /// <summary>Optional minimum width for native LayoutElement.</summary>
        public string minWidth = string.Empty;

        /// <summary>Optional minimum height for native LayoutElement.</summary>
        public string minHeight = string.Empty;

        /// <summary>Optional flexible width for native LayoutElement.</summary>
        public string flexibleWidth = string.Empty;

        /// <summary>Optional flexible height for native LayoutElement.</summary>
        public string flexibleHeight = string.Empty;

        /// <summary>Child alignment for horizontal, vertical, or grid layout groups.</summary>
        public string childAlignment = string.Empty;

        /// <summary>Optional child-control-width flag for horizontal/vertical layouts.</summary>
        public string childControlWidth = string.Empty;

        /// <summary>Optional child-control-height flag for horizontal/vertical layouts.</summary>
        public string childControlHeight = string.Empty;

        /// <summary>Optional child-force-expand-width flag for horizontal/vertical layouts.</summary>
        public string childForceExpandWidth = string.Empty;

        /// <summary>Optional child-force-expand-height flag for horizontal/vertical layouts.</summary>
        public string childForceExpandHeight = string.Empty;

        /// <summary>Optional per-side padding overrides. Empty preserves uniform padding.</summary>
        public string paddingLeft = string.Empty;

        /// <summary>Optional right padding override.</summary>
        public string paddingRight = string.Empty;

        /// <summary>Optional top padding override.</summary>
        public string paddingTop = string.Empty;

        /// <summary>Optional bottom padding override.</summary>
        public string paddingBottom = string.Empty;

        /// <summary>Optional fixed grid column count, only for grid layout.</summary>
        public string gridColumns = string.Empty;

        /// <summary>Optional grid cell width in canvas units.</summary>
        public string cellWidth = string.Empty;

        /// <summary>Optional grid cell height in canvas units.</summary>
        public string cellHeight = string.Empty;
    }

    /// <summary>Parse JSON into a typed schema, without allocating or modifying scene objects.</summary>
    public static class UIAuthoringSchemaParser
    {
        /// <summary>Attempts to parse a document using Unity's built-in JSON serializer.</summary>
        public static bool TryParse(string json, out UIAuthoringDocument document, out string error)
        {
            document = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "The schema JSON is empty.";
                return false;
            }

            try
            {
                document = JsonUtility.FromJson<UIAuthoringDocument>(json);
                if (document == null)
                {
                    error = "Could not parse a schema document.";
                    return false;
                }
                if (document.nodes == null)
                {
                    error = "The schema is missing its nodes array.";
                    document = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = "Invalid JSON: " + exception.Message;
                document = null;
                return false;
            }
        }
    }
}
