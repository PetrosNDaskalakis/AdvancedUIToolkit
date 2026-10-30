using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIAuthoring.Editor
{
    /// <summary>Creates native uGUI components; this package never needs a custom runtime renderer.</summary>
    internal sealed class UIBuiltInElementFactory : IUIAuthoringElementFactory
    {
        /// <summary>Built-in kind owned by this factory.</summary>
        private readonly string kind;

        /// <summary>Only ordinary containers accept child schema nodes in v0.1.</summary>
        public bool CanContainChildren { get { return kind == "container"; } }

        /// <summary>Configure a native kind.</summary>
        public UIBuiltInElementFactory(string kind)
        {
            this.kind = kind;
        }

        /// <summary>Create a new, unparented RectTransform-based native uGUI object.</summary>
        public GameObject Create(UIAuthoringNode node)
        {
            GameObject element;
            if (kind == "text")
            {
                element = new GameObject(node.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                RectTransform rectangle = element.GetComponent<RectTransform>();
                rectangle.sizeDelta = new Vector2(200f, 36f);
                Text label = element.GetComponent<Text>();
                ConfigureText(label, node.text);
            }
            else if (kind == "image")
            {
                element = new GameObject(node.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                element.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);
                element.GetComponent<Image>().raycastTarget = false;
            }
            else if (kind == "button")
            {
                element = new GameObject(node.name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
                element.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 44f);
                GameObject labelObject = new GameObject("Label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                labelObject.transform.SetParent(element.transform, false);
                RectTransform labelRectangle = labelObject.GetComponent<RectTransform>();
                labelRectangle.anchorMin = Vector2.zero;
                labelRectangle.anchorMax = Vector2.one;
                labelRectangle.offsetMin = Vector2.zero;
                labelRectangle.offsetMax = Vector2.zero;
                ConfigureText(labelObject.GetComponent<Text>(), node.text);
            }
            else
            {
                element = new GameObject(node.name, typeof(RectTransform));
                element.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 180f);
            }
            return element;
        }

        /// <summary>Initialize a readable, non-intercepting legacy uGUI label.</summary>
        private static void ConfigureText(Text label, string value)
        {
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value ?? string.Empty;
            label.color = Color.white;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }
    }

    /// <summary>Applies the small built-in visual style subset to freshly created native UI objects.</summary>
    internal static class UIBuiltInStyleApplier
    {
        /// <summary>Apply only explicit style values; defaults remain in the native components.</summary>
        public static void Apply(GameObject element, UIAuthoringNode node)
        {
            Apply(element, node, node.style);
        }

        /// <summary>Apply a resolved stylesheet/inline style without changing the schema node.</summary>
        public static void Apply(GameObject element, UIAuthoringNode node, UIAuthoringStyle style)
        {
            if (style == null) return;
            RectTransform rectangle = element.GetComponent<RectTransform>();
            if (rectangle == null) return;
            Vector2 size = rectangle.sizeDelta;
            if (style.width > 0f) size.x = style.width;
            if (style.height > 0f) size.y = style.height;
            rectangle.sizeDelta = size;

            if (style.width > 0f || style.height > 0f)
            {
                LayoutElement layout = element.GetComponent<LayoutElement>();
                if (layout == null) layout = element.AddComponent<LayoutElement>();
                if (style.width > 0f) layout.preferredWidth = style.width;
                if (style.height > 0f) layout.preferredHeight = style.height;
            }

            if (!string.IsNullOrEmpty(style.background))
            {
                Image image = element.GetComponent<Image>();
                if (image == null) image = element.AddComponent<Image>();
                Color background;
                if (ColorUtility.TryParseHtmlString(style.background, out background))
                {
                    image.color = background;
                }
                image.raycastTarget = string.Equals(node.kind, "button", System.StringComparison.OrdinalIgnoreCase);
            }

            Text[] labels = element.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
            {
                if (style.fontSize > 0) label.fontSize = style.fontSize;
                if (!string.IsNullOrEmpty(style.textAlignment))
                {
                    label.alignment = style.textAlignment == "left" ? TextAnchor.MiddleLeft :
                        style.textAlignment == "right" ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
                }
                if (!string.IsNullOrEmpty(style.textColor))
                {
                    Color color;
                    if (ColorUtility.TryParseHtmlString(style.textColor, out color)) label.color = color;
                }
            }

            UIAuthoringLayoutProperties.ApplyRectTransform(rectangle, style);
            UIAuthoringLayoutProperties.ApplyLayoutElement(element, style);

            string layoutName = style.layout ?? string.Empty;
            if (layoutName == "horizontal" || layoutName == "vertical")
            {
                HorizontalOrVerticalLayoutGroup group = layoutName == "horizontal"
                    ? (HorizontalOrVerticalLayoutGroup)element.AddComponent<HorizontalLayoutGroup>()
                    : element.AddComponent<VerticalLayoutGroup>();
                group.spacing = style.spacing;
                group.padding = UIAuthoringLayoutProperties.MakePadding(style);
                group.childAlignment = UIAuthoringLayoutProperties.ResolveAlignment(style.childAlignment);
                group.childControlWidth = style.childControlWidth == "true";
                group.childControlHeight = style.childControlHeight == "true";
                group.childForceExpandWidth = style.childForceExpandWidth == "true";
                group.childForceExpandHeight = style.childForceExpandHeight == "true";
            }
            else if (layoutName == "grid")
            {
                GridLayoutGroup group = element.AddComponent<GridLayoutGroup>();
                group.padding = UIAuthoringLayoutProperties.MakePadding(style);
                group.childAlignment = UIAuthoringLayoutProperties.ResolveAlignment(style.childAlignment);
                group.spacing = new Vector2(style.spacing, style.spacing);
                if (!string.IsNullOrEmpty(style.cellWidth) || !string.IsNullOrEmpty(style.cellHeight))
                {
                    Vector2 cellSize = group.cellSize;
                    if (!string.IsNullOrEmpty(style.cellWidth)) cellSize.x = UIAuthoringLayoutProperties.ReadFloat(style.cellWidth);
                    if (!string.IsNullOrEmpty(style.cellHeight)) cellSize.y = UIAuthoringLayoutProperties.ReadFloat(style.cellHeight);
                    group.cellSize = cellSize;
                }
                if (!string.IsNullOrEmpty(style.gridColumns))
                {
                    group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    group.constraintCount = UIAuthoringLayoutProperties.ReadInt(style.gridColumns);
                }
            }
        }
    }
}
