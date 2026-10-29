using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Creates fully configured legacy uGUI controls via Unity's own
    /// DefaultControls templates. Only bindable public roots get a prefix;
    /// implementation children keep their normal names.
    /// </summary>
    public static class UIFactory
    {
        /// <summary>Creates text.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Text (Legacy)", false, 12)]
        private static void CreateText(MenuCommand command) =>
            Create(command, UIComponentKind.TextLegacy, "TXT_LegacyText", DefaultControls.CreateText);

        /// <summary>Creates button.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Button", false, 12)]
        private static void CreateButton(MenuCommand command) =>
            Create(command, UIComponentKind.Button, "BTN_Button", DefaultControls.CreateButton);

        /// <summary>Creates image.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Image", false, 12)]
        private static void CreateImage(MenuCommand command) =>
            Create(command, UIComponentKind.Image, "IMG_Image", DefaultControls.CreateImage);

        /// <summary>Creates raw image.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Raw Image", false, 12)]
        private static void CreateRawImage(MenuCommand command) =>
            Create(command, UIComponentKind.RawImage, "RAW_RawImage", DefaultControls.CreateRawImage);

        /// <summary>Creates panel.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Panel", false, 12)]
        private static void CreatePanel(MenuCommand command) =>
            Create(command, UIComponentKind.Panel, "PNL_Panel", DefaultControls.CreatePanel);

        /// <summary>Creates toggle.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Toggle", false, 12)]
        private static void CreateToggle(MenuCommand command) =>
            Create(command, UIComponentKind.Toggle, "TGL_Toggle", DefaultControls.CreateToggle);

        /// <summary>Creates slider.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Slider", false, 12)]
        private static void CreateSlider(MenuCommand command) =>
            Create(command, UIComponentKind.Slider, "SLD_Slider", DefaultControls.CreateSlider);

        /// <summary>Creates input.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Input Field (Legacy)", false, 12)]
        private static void CreateInput(MenuCommand command) =>
            Create(command, UIComponentKind.InputField, "INP_InputField", DefaultControls.CreateInputField);

        /// <summary>Creates scroll view.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Scroll View", false, 12)]
        private static void CreateScrollView(MenuCommand command) =>
            Create(command, UIComponentKind.ScrollView, "SCR_ScrollView", DefaultControls.CreateScrollView);

        /// <summary>Creates scrollbar.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Scrollbar", false, 12)]
        private static void CreateScrollbar(MenuCommand command) =>
            Create(command, UIComponentKind.Scrollbar, "SBR_Scrollbar", DefaultControls.CreateScrollbar);

        /// <summary>Creates dropdown.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Dropdown (Legacy)", false, 12)]
        private static void CreateDropdown(MenuCommand command) =>
            Create(command, UIComponentKind.Dropdown, "DDP_Dropdown", DefaultControls.CreateDropdown);

        /// <summary>Creates rect.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/RectTransform (Empty)", false, 12)]
        private static void CreateRect(MenuCommand command) =>
            UIStyleFactory.CreateEmptyContainer(UIStyleFactory.GetParent(command), "Container");

        /// <summary>Creates value.</summary>
        private static void Create(
            MenuCommand command,
            UIComponentKind kind,
            string baseName,
            Func<DefaultControls.Resources, GameObject> factory)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Advanced UI Toolkit Factory: Create UI elements in Edit Mode.");
                return;
            }

            // MenuCommand.context is used for Hierarchy right-click creation.
            // Fall back to current selection when invoked from the top menu.
            RectTransform parent = UIStyleFactory.GetParent(command);

            if (parent == null)
            {
                Debug.LogWarning(
                    "Advanced UI Toolkit Factory: Select a Canvas, Panel, or other RectTransform " +
                    "in the scene or Prefab Mode before creating a UI element.");
                return;
            }

            // When a style profile provides a prefab for this role, use it.
            // A missing role falls back to Unity's regular configured uGUI control.
            if (UIStyleFactory.TryCreateStyled(parent, kind, baseName, out GameObject styled))
                return;

            // A native Unity default control must have the same isolated Undo
            // behavior as a styled prefab or empty container. Starting a new group
            // AFTER creating the control could merge its creation with a previous edit.
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Advanced UI Toolkit: Create " + baseName);
            GameObject created = factory(BuildResources());
            if (created == null)
            {
                Debug.LogError("Advanced UI Toolkit Factory: Failed to create " + baseName);
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            // Keep parenting and naming in the same Undo step as creation.

            // Unity's recommended Editor parenting preserves layers/alignment.
            GameObjectUtility.SetParentAndAlign(created, parent.gameObject);
            created.name = UIStyleFactory.MakeUniqueName(parent, baseName);
            Undo.RegisterCreatedObjectUndo(created, "Advanced UI Toolkit: Create " + baseName);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = created;
            EditorGUIUtility.PingObject(created);
        }

        /// <summary>Builds resources.</summary>
        private static DefaultControls.Resources BuildResources()
        {
            // These are Unity's built-in legacy UI sprites. If unavailable in
            // a particular editor version, the native controls still receive
            // their components and hierarchy; their sprites may be blank.
            return new DefaultControls.Resources
            {
                standard = BuiltinSprite("UI/Skin/UISprite.psd"),
                background = BuiltinSprite("UI/Skin/Background.psd"),
                inputField = BuiltinSprite("UI/Skin/InputFieldBackground.psd"),
                knob = BuiltinSprite("UI/Skin/Knob.psd"),
                checkmark = BuiltinSprite("UI/Skin/Checkmark.psd"),
                dropdown = BuiltinSprite("UI/Skin/DropdownArrow.psd"),
                mask = BuiltinSprite("UI/Skin/UIMask.psd")
            };
        }

        /// <summary>Performs the builtin sprite operation.</summary>
        private static Sprite BuiltinSprite(string path) =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }
}
