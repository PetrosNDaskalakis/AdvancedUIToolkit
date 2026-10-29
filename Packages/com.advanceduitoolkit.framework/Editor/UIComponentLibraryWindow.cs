using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Responsive, native IMGUI editor for Advanced UI Toolkit component libraries.
    /// Presents a single, straightforward component browser with optional
    /// advanced tools tucked behind a Show More / Show Less workflow.
    /// </summary>
    public sealed class UIComponentLibraryWindow : EditorWindow
    {
        /// <summary>Minimum gallery tile width before the layout adds another column.</summary>
        private const float CardWidth = 152f;


        /// <summary>Stores the profiles reference or configuration value.</summary>
        private readonly List<UIStyleProfile> profiles = new List<UIStyleProfile>();
        /// <summary>Stores the selected profile reference or configuration value.</summary>
        private UIStyleProfile selectedProfile;
        /// <summary>
        /// Effective inherited prefab list shared by the header, component browser,
        /// and selected component details during one OnGUI pass. Recomputed each pass,
        /// so external Profile edits and Undo cannot leave a stale cross-frame cache.
        /// </summary>
        private List<UIComponentTemplate> effectiveTemplatesForGui;
        /// <summary>Stores the search component reference or configuration value.</summary>
        private string searchComponent = string.Empty;
        /// <summary>Stores the selected identity reference or configuration value.</summary>
        private string selectedIdentity;
        /// <summary>Stores the instance name reference or configuration value.</summary>
        private string instanceName = string.Empty;
        /// <summary>Stores the kind filter reference or configuration value.</summary>
        private UIComponentKind kindFilter = UIComponentKind.Custom;
        /// <summary>Stores the filter all reference or configuration value.</summary>
        private bool filterAll = true;
        /// <summary>Stores the list view reference or configuration value.</summary>
        private bool listView = true;
        /// <summary>Whether component discovery includes inherited profile templates.</summary>
        [SerializeField] private bool showInherited = true;

        /// <summary>Scrollable position of the unified UI Library page.</summary>
        private Vector2 mainScroll;
        /// <summary>Whether secondary profile-management options are visible.</summary>
        [SerializeField] private bool showAdvancedOptions;
        /// <summary>Whether the profile-editing section is visible.</summary>
        [SerializeField] private bool showProfileEditor;
        /// <summary>Whether the validation section is visible.</summary>
        [SerializeField] private bool showValidation;
        /// <summary>Whether the help section is visible.</summary>
        [SerializeField] private bool showHelp;
        /// <summary>Whether complete profile-validation messages are expanded.</summary>
        [SerializeField] private bool showValidationDetails;
        /// <summary>Whether optional prefab actions are visible.</summary>
        [SerializeField] private bool showPrefabActions;
        /// <summary>Whether selected component details are expanded.</summary>
        [SerializeField] private bool selectedDetailsExpanded = true;
        /// <summary>The RectTransform under which new prefab instances are created.</summary>
        [SerializeField] private RectTransform targetRoot;
        /// <summary>True while the Factory is instantiating a component through its own UI.</summary>
        private bool isCreatingComponent;
        /// <summary>Last Factory-created prefab, whose automatic selection should not change the parent.</summary>
        private GameObject lastCreatedComponent;
        /// <summary>Editable name for the next non-prefab layout container.</summary>
        [SerializeField] private string newContainerName = "Container";
        /// <summary>Whether the next instance should override its original displayed text.</summary>
        [SerializeField] private bool overrideDisplayedText;
        /// <summary>Which discovered text component on the selected prefab to override.</summary>
        [SerializeField] private int selectedTextTargetIndex;
        /// <summary>Actual selected source label, unaffected by label reordering.</summary>
        [SerializeField] private Component selectedTextSourceComponent;
        /// <summary>Text to apply to the new instance; an empty string is intentional.</summary>
        [SerializeField] private string displayedText = string.Empty;

        /// <summary>Opens the UI Library window.</summary>
        [MenuItem("Tools/Advanced UI Toolkit/Factory", false, 2)]
        public static void Open() => GetWindow<UIComponentLibraryWindow>("UI Library");

        /// <summary>Opens the Factory without losing the explicitly selected creation parent.</summary>
        /// <param name="root">The owning UI root, or null to use the current selection.</param>
        internal static void OpenFor(RectTransform root)
        {
            UIComponentLibraryWindow window = GetWindow<UIComponentLibraryWindow>("UI Library");
            if (root != null)
            {
                // SetTargetRoot already follows a UIStyleContext when present.
                // Without an explicit context, preserve the developer's chosen library.
                window.SetTargetRoot(root);
            }
            window.Show();
            window.Focus();
        }

        /// <summary>Opens the component library from the hierarchy context.</summary>
        [MenuItem("GameObject/Advanced UI Toolkit/Component Library...", false, 50)]
        private static void OpenFromHierarchy(MenuCommand command)
        {
            if (command != null && command.context is GameObject go)
                Selection.activeGameObject = go;
            Open();
        }

        /// <summary>Handles the Unity OnEnable callback.</summary>
        private void OnEnable()
        {
            titleContent = new GUIContent("UI Library", EditorGUIUtility.IconContent("Prefab Icon").image);
            minSize = new Vector2(300, 310);
            RectTransform previousRoot = targetRoot != null ? targetRoot : UIStyleFactory.GetParent(null);
            selectedProfile = UIStyleFactory.ActiveProfile;
            ReloadProfiles();
            targetRoot = null;
            SetTargetRoot(ResolveSelectionRoot(previousRoot, Selection.activeGameObject));
            EditorApplication.projectChanged += OnProjectChanged;
            UIPrefabPreviewCache.Updated += Repaint;
        }

        /// <summary>Handles the Unity OnDisable callback.</summary>
        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            UIPrefabPreviewCache.Updated -= Repaint;
        }

        /// <summary>Handles asset-database changes.</summary>
        private void OnProjectChanged()
        {
            ReloadProfiles();
            Repaint();
        }

        /// <summary>
        /// Follows non-prefab UI containers. Selecting a prefab or one of its children
        /// resolves to the nearest non-prefab ancestor. Factory-created prefabs never
        /// become the next creation parent.
        /// </summary>
        private void OnSelectionChange()
        {
            GameObject selected = Selection.activeGameObject;
            if (isCreatingComponent || (lastCreatedComponent != null && selected == lastCreatedComponent))
            {
                lastCreatedComponent = null;
                Repaint();
                return;
            }
            lastCreatedComponent = null;
            SetTargetRoot(ResolveSelectionRoot(targetRoot, selected));
            Repaint();
        }

        /// <summary>
        /// Selects the closest non-prefab RectTransform. Prefab instance selections
        /// resolve to their parent container, never to the prefab itself.
        /// </summary>
        /// <param name="current">Previously selected creation parent.</param>
        /// <param name="selected">Current Hierarchy selection.</param>
        /// <returns>The closest non-prefab UI parent, or the last valid parent.</returns>
        internal static RectTransform ResolveSelectionRoot(RectTransform current, GameObject selected) =>
            UIHierarchySelection.ResolveParent(current, selected);

        /// <summary>
        /// Updates the validated creation parent and resolves any inherited style context.
        /// Does not erase a manually selected library when the parent has no style profile.
        /// </summary>
        /// <param name="root">The new creation root.</param>
        private void SetTargetRoot(RectTransform root)
        {
            RectTransform current = targetRoot == null ? null :
                UIHierarchySelection.ResolveParent(null, targetRoot.gameObject);
            RectTransform resolved = root == null ? null :
                UIHierarchySelection.ResolveParent(current, root.gameObject);
            if (targetRoot == resolved) return;
            targetRoot = resolved;
            if (targetRoot == null) return;
            root = targetRoot;
            for (Transform node = root; node != null; node = node.parent)
            {
                UIStyleContext context = node.GetComponent<UIStyleContext>();
                if (context == null || context.Profile == null) continue;
                if (selectedProfile != context.Profile) SelectProfile(context.Profile);
                break;
            }
        }

        /// <summary>Reloads all available style profiles from the project.</summary>
        private void ReloadProfiles()
        {
            profiles.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:UIStyleProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UIStyleProfile profile = AssetDatabase.LoadAssetAtPath<UIStyleProfile>(path);
                if (profile != null && !profiles.Contains(profile)) profiles.Add(profile);
            }
            profiles.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            if (selectedProfile == null || !profiles.Contains(selectedProfile))
                selectedProfile = UIStyleFactory.ActiveProfile != null
                    ? UIStyleFactory.ActiveProfile : profiles.FirstOrDefault();
        }

        /// <summary>
        /// Draws one full-width UI Library page. The library selector stays in the
        /// toolbar; no permanent side column consumes horizontal space.
        /// </summary>
        private void OnGUI()
        {
            RectTransform parent = targetRoot;
            DrawToolbar();
            DrawTargetBar(ref parent);
            DrawCompactLibraryPicker();
            DrawMain(parent);
            DrawFooter(parent);
        }

        /// <summary>Draws the top toolbar.</summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Create Components", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Binder", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    AdvancedUIToolkitWorkspaceWindow.OpenForRoot(targetRoot != null
                        ? AdvancedUIToolkitWorkspaceWindow.ResolveRoot(targetRoot.gameObject) : null);
                if (GUILayout.Button(showAdvancedOptions ? "Show Less" : "Show More",
                    EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    showAdvancedOptions = !showAdvancedOptions;
            }
        }

        /// <summary>Displays the exact prefab creation parent from the Hierarchy.</summary>
        private void DrawTargetBar(ref RectTransform parent)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(new GUIContent("Parent", "Follows non-prefab UI containers. Selecting a prefab uses its nearest valid parent."),
                    GUILayout.Width(47));
                RectTransform newRoot = (RectTransform)EditorGUILayout.ObjectField(
                    targetRoot, typeof(RectTransform), true, GUILayout.MinWidth(75));
                if (newRoot != targetRoot) SetTargetRoot(newRoot);
            }
            parent = targetRoot;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(new GUIContent("Container", "Create a regular, non-prefab RectTransform under the active parent."),
                    GUILayout.Width(61f));
                newContainerName = GUILayout.TextField(newContainerName, GUILayout.MinWidth(60f),
                    GUILayout.ExpandWidth(true));
                using (new EditorGUI.DisabledScope(parent == null ||
                    string.IsNullOrWhiteSpace(newContainerName) ||
                    EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("+ Create", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    {
                        GameObject created = UIStyleFactory.CreateEmptyContainer(parent, newContainerName);
                        if (created != null)
                        {
                            // Unlike prefab instances, newly created empty containers
                            // are valid parents and should become active immediately.
                            lastCreatedComponent = null;
                            SetTargetRoot((RectTransform)created.transform);
                        }
                    }
                }
            }
        }

        /// <summary>Draws a compact library selector for narrow windows.</summary>
        private void DrawCompactLibraryPicker()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Library", GUILayout.Width(47));
                if (profiles.Count == 0)
                {
                    GUILayout.Label("No profiles", EditorStyles.miniLabel);
                }
                else
                {
                    string[] names = profiles.Select(p => p.name).ToArray();
                    int index = Mathf.Max(0, profiles.IndexOf(selectedProfile));
                    int next = EditorGUILayout.Popup(index, names, EditorStyles.toolbarPopup);
                    if (next != index && next >= 0 && next < profiles.Count)
                        SelectProfile(profiles[next]);
                }
                using (new EditorGUI.DisabledScope(selectedProfile == null))
                {
                    if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(37)))
                        EditorGUIUtility.PingObject(selectedProfile);
                }
            }
        }

        /// <summary>Selects a style profile and clears context-specific UI state.</summary>
        /// <param name="profile">The profile to select.</param>
        private void SelectProfile(UIStyleProfile profile)
        {
            effectiveTemplatesForGui = null;
            selectedProfile = profile;
            selectedIdentity = null;
            instanceName = string.Empty;
            overrideDisplayedText = false;
            selectedTextTargetIndex = 0;
            selectedTextSourceComponent = null;
            displayedText = string.Empty;
            mainScroll = Vector2.zero;
        }

        /// <summary>Draws the compact component browser and optional settings.</summary>
        /// <param name="parent">The current creation root.</param>
        private void DrawMain(RectTransform parent)
        {
            // Cache only for this GUI event, never across frames or external profile edits.
            effectiveTemplatesForGui = null;
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll, GUILayout.ExpandHeight(true));
            if (selectedProfile == null)
            {
                EditorGUILayout.HelpBox("Create a UI Style Profile to register your reusable prefabs.",
                    MessageType.Info);
                if (GUILayout.Button("Create Style Profile")) CreateProfileAsset();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawHeaderRow(parent);
            if (showAdvancedOptions) DrawAdvancedOptions(parent);

            if (!string.IsNullOrEmpty(selectedIdentity))
            {
                selectedDetailsExpanded = EditorGUILayout.Foldout(selectedDetailsExpanded,
                    "Selected Component", true);
                if (selectedDetailsExpanded)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        DrawSelectedTemplateDetails(parent);
                }
            }

            DrawGallery(parent);
            EditorGUILayout.EndScrollView();
            effectiveTemplatesForGui = null;
        }

        /// <summary>Draws a single short profile header without fixed-width actions.</summary>
        /// <param name="parent">The current creation root.</param>
        private void DrawHeaderRow(RectTransform parent)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(selectedProfile.name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(VisibleTemplates().Count + " components", EditorStyles.miniLabel);
            }
        }

        /// <summary>Draws optional library-management tools with compact, wrapping rows.</summary>
        /// <param name="parent">The current creation root.</param>
        private void DrawAdvancedOptions(RectTransform parent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Library Options", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(UIStyleFactory.ActiveProfile == selectedProfile))
                    {
                        if (GUILayout.Button("Set Default", EditorStyles.miniButton, GUILayout.Width(86f)))
                            UIStyleFactory.ActiveProfile = selectedProfile;
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("New Profile", EditorStyles.miniButton)) CreateProfileAsset();
                    if (GUILayout.Button("Refresh", EditorStyles.miniButton)) ReloadProfiles();
                    if (GUILayout.Button("Ping", EditorStyles.miniButton)) EditorGUIUtility.PingObject(selectedProfile);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    showInherited = EditorGUILayout.ToggleLeft("Inherited", showInherited, GUILayout.Width(90f));
                    if (GUILayout.Button("Refresh Previews", EditorStyles.miniButton))
                        UIPrefabPreviewCache.Clear();
                    using (new EditorGUI.DisabledScope(parent == null ||
                                                       EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        if (GUILayout.Button(new GUIContent("Apply Profile", "Assign this style profile to the selected parent."),
                            EditorStyles.miniButton))
                            AttachContext(parent, selectedProfile);
                    }
                }

                showProfileEditor = EditorGUILayout.Foldout(showProfileEditor, "Edit Profile", true);
                if (showProfileEditor)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        DrawProfileSetup();
                }
                showValidation = EditorGUILayout.Foldout(showValidation, "Validation", true);
                if (showValidation)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        DrawValidationPage();
                }
                showHelp = EditorGUILayout.Foldout(showHelp, "Help", true);
                if (showHelp)
                    EditorGUILayout.HelpBox("Select any UI GameObject in the Hierarchy to choose the creation parent, " +
                        "pick a saved prefab, then Create. " +
                        "Source prefab links and AutoBinder-compatible names are preserved.", MessageType.Info);
            }
        }

        /// <summary>
        /// Resolves inherited prefab templates at most once per Library GUI event.
        /// No persistent cache is kept: profile assets may be edited in other Inspectors.
        /// </summary>
        /// <returns>Effective templates for the selected profile.</returns>
        private List<UIComponentTemplate> EffectiveTemplatesForGui()
        {
            if (effectiveTemplatesForGui == null)
                effectiveTemplatesForGui = selectedProfile.GetEffectiveTemplates();
            return effectiveTemplatesForGui;
        }

        /// <summary>Returns the currently visible component templates.</summary>
        /// <returns>A filtered list of component templates.</returns>
        private List<UIComponentTemplate> VisibleTemplates()
        {
            List<UIComponentTemplate> entries = showInherited
                ? EffectiveTemplatesForGui()
                : selectedProfile.Templates.Where(x => x != null && x.Prefab != null).ToList();
            return entries.Where(t => t != null && t.Prefab != null &&
                    (filterAll || t.Kind == kindFilter) &&
                    (string.IsNullOrEmpty(searchComponent) ||
                     (t.Kind + " " + t.Id + " " + t.DefaultObjectName + " " + t.Prefab.name)
                     .IndexOf(searchComponent, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>Draws a searchable gallery that uses the full available dock width.</summary>
        /// <param name="parent">The current creation root.</param>
        private void DrawGallery(RectTransform parent)
        {
            List<UIComponentTemplate> templates = VisibleTemplates();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                searchComponent = GUILayout.TextField(searchComponent,
                    GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField,
                    GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(searchComponent) &&
                    GUILayout.Button(new GUIContent("×", "Clear component search"),
                        EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    searchComponent = string.Empty;
                    GUI.FocusControl(null);
                }
                filterAll = GUILayout.Toggle(filterAll, "All", EditorStyles.toolbarButton, GUILayout.Width(33f));
                if (!filterAll)
                    kindFilter = (UIComponentKind)EditorGUILayout.EnumPopup(kindFilter,
                        EditorStyles.toolbarPopup, GUILayout.Width(90f));
                listView = GUILayout.Toggle(listView, listView ? "List" : "Tiles",
                    EditorStyles.toolbarButton, GUILayout.Width(45f));
            }

            if (templates.Count == 0)
            {
                GUILayout.Label(string.IsNullOrWhiteSpace(searchComponent) && filterAll
                    ? "No prefab templates registered. Open Show More > Edit Profile to add one."
                    : "No components match the current search/filter.",
                    EditorStyles.wordWrappedMiniLabel);
                if ((!string.IsNullOrWhiteSpace(searchComponent) || !filterAll) &&
                    GUILayout.Button("Clear Filters", EditorStyles.miniButton))
                {
                    searchComponent = string.Empty;
                    filterAll = true;
                }
            }
            else if (listView)
            {
                foreach (UIComponentTemplate entry in templates)
                    DrawTemplateRow(entry, parent);
            }
            else
            {
                float available = Mathf.Max(260f, position.width - 34f);
                int columns = Mathf.Max(1, Mathf.FloorToInt((available + 8f) / (CardWidth + 8f)));
                float cardWidth = Mathf.Max(CardWidth, (available - (columns - 1) * 8f) / columns);
                for (int first = 0; first < templates.Count; first += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int i = first; i < Mathf.Min(templates.Count, first + columns); i++)
                            DrawTemplateCard(templates[i], parent, cardWidth);
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        /// <summary>Draws a component card.</summary>
        /// <param name="template">The component template to display.</param>
        /// <param name="parent">The current creation root.</param>
        private void DrawTemplateCard(UIComponentTemplate template, RectTransform parent, float cardWidth)
        {
            bool selected = selectedIdentity == template.Identity;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(cardWidth)))
            {
                Rect preview = GUILayoutUtility.GetRect(cardWidth - 16f, 77f, GUILayout.Height(77f));
                DrawPreview(preview, template.Prefab);
                if (GUI.Button(preview, GUIContent.none, GUIStyle.none)) SelectTemplate(template);
                if (GUILayout.Button(TemplateLabel(template),
                    selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton))
                    SelectTemplate(template);
                GUILayout.Label(template.Kind.ToString(), EditorStyles.miniLabel);
                using (new EditorGUI.DisabledScope(!CanCreateQuickInstance(template, parent,
                           selectedIdentity, instanceName, EditorApplication.isPlayingOrWillChangePlaymode)))
                    if (GUILayout.Button(new GUIContent("Create", "Create using the selected name, or the prefab default."),
                        GUILayout.Height(20)))
                        CreateTemplate(parent, template, RequestedQuickInstanceName(template,
                            selectedIdentity, instanceName), selectedIdentity == template.Identity);
            }
        }

        /// <summary>Draws a full-width component row.</summary>
        /// <param name="template">The component template to display.</param>
        /// <param name="parent">The current creation root.</param>
        private void DrawTemplateRow(UIComponentTemplate template, RectTransform parent)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
            {
                Rect preview = GUILayoutUtility.GetRect(39f, 38f, GUILayout.Width(39f), GUILayout.Height(38f));
                DrawPreview(preview, template.Prefab);
                if (GUI.Button(preview, GUIContent.none, GUIStyle.none)) SelectTemplate(template);
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    if (GUILayout.Button(TemplateLabel(template),
                        selectedIdentity == template.Identity ? EditorStyles.miniButtonMid : EditorStyles.miniButton,
                        GUILayout.ExpandWidth(true)))
                        SelectTemplate(template);
                    GUILayout.Label(template.Kind.ToString(), EditorStyles.miniLabel);
                }
                using (new EditorGUI.DisabledScope(!CanCreateQuickInstance(template, parent,
                           selectedIdentity, instanceName, EditorApplication.isPlayingOrWillChangePlaymode)))
                    if (GUILayout.Button(new GUIContent("Create", "Create using the selected name, or the prefab default."),
                        GUILayout.Width(58), GUILayout.Height(23)))
                        CreateTemplate(parent, template, RequestedQuickInstanceName(template,
                            selectedIdentity, instanceName), selectedIdentity == template.Identity);
            }
        }

        /// <summary>
        /// Uses the edited instance name only for the currently selected prefab.
        /// The other gallery entries keep their own registered default names.
        /// </summary>
        /// <param name="template">Prefab entry being created.</param>
        /// <param name="selectedTemplateId">Currently selected entry identity.</param>
        /// <param name="editedName">Current name editor text.</param>
        /// <returns>An explicit name or null to use the template default.</returns>
        internal static string RequestedQuickInstanceName(UIComponentTemplate template,
            string selectedTemplateId, string editedName)
        {
            return template != null &&
                   string.Equals(template.Identity, selectedTemplateId, StringComparison.Ordinal)
                ? editedName : null;
        }

        /// <summary>
        /// Prevents quick-create buttons from bypassing invalid names entered in the
        /// selected component editor. Unselected prefabs use their registered defaults.
        /// </summary>
        /// <param name="template">Prefab entry being created.</param>
        /// <param name="parent">Target root for instantiation.</param>
        /// <param name="selectedTemplateId">Selected component identity.</param>
        /// <param name="editedName">The edited instance name.</param>
        /// <param name="isPlaying">Whether Unity is entering or in Play Mode.</param>
        /// <returns>True if creating this entry is allowed.</returns>
        internal static bool CanCreateQuickInstance(UIComponentTemplate template, RectTransform parent,
            string selectedTemplateId, string editedName, bool isPlaying)
        {
            if (parent == null || template == null || template.Prefab == null || isPlaying)
                return false;
            string requested = RequestedQuickInstanceName(template, selectedTemplateId, editedName);
            return requested == null || UIStyleFactory.IsValidDefaultName(requested,
                template.Kind, out _);
        }

        /// <summary>Selects a component template.</summary>
        /// <param name="template">The selected template.</param>
        private void SelectTemplate(UIComponentTemplate template)
        {
            if (template == null) return;
            if (selectedIdentity != template.Identity)
            {
                selectedIdentity = template.Identity;
                selectedDetailsExpanded = true;
                overrideDisplayedText = false;
                selectedTextTargetIndex = 0;
                selectedTextSourceComponent = null;
                displayedText = string.Empty;
                instanceName = !string.IsNullOrWhiteSpace(template.DefaultObjectName)
                    ? template.DefaultObjectName
                    : template.Kind == UIComponentKind.Custom ? template.Prefab.name :
                      UIStyleFactory.DefaultName(template.Kind);
            }
        }

        /// <summary>Returns a display label for a template.</summary>
        /// <param name="template">The template to label.</param>
        /// <returns>The display label.</returns>
        private static string TemplateLabel(UIComponentTemplate template) =>
            template.Kind == UIComponentKind.Custom ? template.Id : template.Kind.ToString();

        /// <summary>Draws a preview image or fallback for a prefab.</summary>
        /// <param name="rect">The target rect.</param>
        /// <param name="prefab">The prefab to preview.</param>
        private void DrawPreview(Rect rect, GameObject prefab)
        {
            if (prefab == null) return;
            Texture thumbnail = UIPrefabPreviewCache.Get(prefab);
            Rect area = new Rect(rect.x + 2, rect.y + 2,
                Mathf.Max(1f, rect.width - 4), Mathf.Max(1f, rect.height - 4));
            if (thumbnail != null)
                GUI.DrawTexture(area, thumbnail, ScaleMode.ScaleToFit);
            else
                DrawGraphicFallback(area, prefab);
        }

        /// <summary>
        /// Lightweight UI-shaped placeholder while preview capture is queued or when
        /// an SRP/custom shader cannot be captured. Avoids the blue prefab cube.
        /// This is not a pixel-accurate rendering of the original prefab.
        /// </summary>
        private static void DrawGraphicFallback(Rect rect, GameObject prefab)
        {
            EditorGUI.DrawRect(rect, new Color(.15f, .17f, .19f, 1f));
            Rect content = new Rect(rect.x + 8, rect.y + rect.height * .18f,
                Mathf.Max(4f, rect.width - 16), Mathf.Max(4f, rect.height * .64f));

            Image image = prefab.GetComponent<Image>();
            if (image == null) image = prefab.GetComponentInChildren<Image>(true);
            RawImage raw = prefab.GetComponent<RawImage>();
            if (raw == null) raw = prefab.GetComponentInChildren<RawImage>(true);
            if (image != null)
            {
                EditorGUI.DrawRect(content, image.color);
                Sprite sprite = image.sprite;
                if (sprite != null && sprite.texture != null)
                {
                    Color before = GUI.color;
                    GUI.color = image.color;
                    try
                    {
                        Rect atlas = sprite.textureRect;
                        Rect texCoords = new Rect(atlas.x / sprite.texture.width,
                            atlas.y / sprite.texture.height, atlas.width / sprite.texture.width,
                            atlas.height / sprite.texture.height);
                        GUI.DrawTextureWithTexCoords(content, sprite.texture, texCoords);
                    }
                    catch (UnityException)
                    {
                        GUI.DrawTexture(content, sprite.texture, ScaleMode.ScaleToFit);
                    }
                    finally { GUI.color = before; }
                }
            }
            else if (raw != null)
            {
                EditorGUI.DrawRect(content, raw.color);
                if (raw.texture != null) GUI.DrawTexture(content, raw.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(content, new Color(.22f, .25f, .28f));
            }

            Text legacy = prefab.GetComponentInChildren<Text>(true);
            string label = legacy != null ? legacy.text : string.Empty;
            if (string.IsNullOrEmpty(label))
            {
                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component.GetType().FullName != "TMPro.TextMeshProUGUI")
                        continue;
                    System.Reflection.PropertyInfo textProperty = component.GetType().GetProperty("text");
                    if (textProperty != null) label = textProperty.GetValue(component, null) as string;
                    break;
                }
            }
            if (string.IsNullOrEmpty(label)) label = prefab.name;
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.Clamp((int)(rect.height * .14f), 9, 15)
            };
            style.normal.textColor = legacy != null ? legacy.color : Color.white;
            GUI.Label(content, label, style);
        }

        /// <summary>
        /// Offers one explicit label override for a prefab that contains supported
        /// root or nested text. Multiple labels require an explicit target choice.
        /// </summary>
        /// <param name="prefab">Currently selected source prefab asset.</param>
        private bool DrawTextOverrideEditor(GameObject prefab)
        {
            // Disabled entries are drawn on every GUI event. A cheap availability
            // query avoids allocating paths, Target objects and original text when
            // the user is not editing a displayed-text override.
            if (!overrideDisplayedText && !UIPrefabTextOverride.HasTargets(prefab))
                return true;

            bool nextEnabled = EditorGUILayout.ToggleLeft("Override Displayed Text", overrideDisplayedText);
            if (!nextEnabled)
            {
                overrideDisplayedText = false;
                return true;
            }

            List<UIPrefabTextOverride.Target> targets = UIPrefabTextOverride.FindTargets(prefab);
            if (!overrideDisplayedText && targets.Count > 0)
            {
                selectedTextTargetIndex = Mathf.Clamp(selectedTextTargetIndex, 0, targets.Count - 1);
                selectedTextSourceComponent = targets[selectedTextTargetIndex].Component;
                displayedText = targets[selectedTextTargetIndex].CurrentText;
            }
            overrideDisplayedText = true;

            // Resolve the specific component, not its previous position in the list.
            int currentIndex = targets.FindIndex(t => selectedTextSourceComponent != null &&
                                                        t.Component == selectedTextSourceComponent);
            if (currentIndex >= 0)
            {
                selectedTextTargetIndex = currentIndex;
            }
            else
            {
                EditorGUILayout.HelpBox("The selected prefab text component has changed or was removed. " +
                    "Choose a new text target before creating another instance.", MessageType.Warning);
                if (targets.Count > 0)
                {
                    string[] candidates = new[] { "Choose a new label..." }
                        .Concat(targets.Select(t => t.Label)).ToArray();
                    int newSelection = EditorGUILayout.Popup("New Text Target", 0, candidates);
                    if (newSelection > 0 && newSelection <= targets.Count)
                    {
                        int resolvedIndex = newSelection - 1;
                        selectedTextTargetIndex = resolvedIndex;
                        selectedTextSourceComponent = targets[resolvedIndex].Component;
                        displayedText = targets[resolvedIndex].CurrentText;
                        currentIndex = resolvedIndex;
                    }
                }
                if (currentIndex < 0) return false;
            }
            if (targets.Count > 1)
            {
                string[] targetNames = targets.Select(t => t.Label).ToArray();
                int next = EditorGUILayout.Popup("Text Target", selectedTextTargetIndex, targetNames);
                if (next != selectedTextTargetIndex)
                {
                    selectedTextTargetIndex = next;
                    selectedTextSourceComponent = targets[next].Component;
                    displayedText = targets[next].CurrentText;
                }
            }
            else
                EditorGUILayout.LabelField("Text Target", targets[0].Label, EditorStyles.miniLabel);

            displayedText = EditorGUILayout.TextField("Displayed Text", displayedText);
            return true;
        }

        /// <summary>
        /// Builds a text override for the selected template only. An unselected
        /// card always uses the source prefab text, even if another entry is edited.
        /// </summary>
        /// <param name="applyOverride">Whether this is the selected template.</param>
        /// <returns>A disabled request or the text chosen in the detail editor.</returns>
        private UIPrefabTextOverrideRequest BuildTextOverride(bool applyOverride)
        {
            return new UIPrefabTextOverrideRequest
            {
                Enabled = applyOverride && overrideDisplayedText,
                TargetIndex = selectedTextTargetIndex,
                Text = displayedText,
                RequireSourceComponent = applyOverride && overrideDisplayedText,
                SourceComponent = selectedTextSourceComponent
            };
        }

        /// <summary>
        /// Shows the selected prefab in a compact horizontal summary. Extra state
        /// previews and asset actions are visible only if requested.
        /// </summary>
        /// <param name="parent">The current creation root.</param>
        private void DrawSelectedTemplateDetails(RectTransform parent)
        {
            UIComponentTemplate template = EffectiveTemplatesForGui()
                .FirstOrDefault(t => t != null && t.Identity == selectedIdentity);
            if (template == null || template.Prefab == null)
            {
                GUILayout.Label("Select a valid prefab in the list below.", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                Rect preview = GUILayoutUtility.GetRect(86f, 70f,
                    GUILayout.Width(86f), GUILayout.Height(70f));
                DrawPreview(preview, template.Prefab);
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    GUILayout.Label(TemplateLabel(template), EditorStyles.boldLabel);
                    GUILayout.Label(template.Kind.ToString(), EditorStyles.miniLabel);
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(template.Prefab, typeof(GameObject), false,
                            GUILayout.ExpandWidth(true));
                }
            }

            bool textTargetValid = DrawTextOverrideEditor(template.Prefab);

            // Validate after the name field is edited, and retain the result beyond
            // the horizontal layout scope for the inline error message below.
            bool valid;
            string reason;
            using (new EditorGUILayout.HorizontalScope())
            {
                instanceName = EditorGUILayout.TextField(instanceName,
                    GUILayout.MinWidth(90f), GUILayout.ExpandWidth(true));
                valid = UIStyleFactory.IsValidDefaultName(instanceName, template.Kind, out reason);
                using (new EditorGUI.DisabledScope(!valid || !textTargetValid || parent == null ||
                                                   EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Create", GUILayout.Width(74f), GUILayout.Height(22f)))
                        CreateTemplate(parent, template, instanceName, true);
                }
                if (!valid) GUILayout.Label(new GUIContent("!", reason), EditorStyles.miniBoldLabel,
                    GUILayout.Width(12f));
            }

            if (!valid)
                GUILayout.Label(reason, EditorStyles.wordWrappedMiniLabel);
            if (parent == null)
                GUILayout.Label("Select a UI GameObject as the parent to enable creation.", EditorStyles.wordWrappedMiniLabel);

            showPrefabActions = EditorGUILayout.Foldout(showPrefabActions, "Prefab Actions", true);
            if (showPrefabActions)
            {
                bool inherited = !selectedProfile.Templates.Any(t => t != null && t.Identity == template.Identity);
                EditorGUILayout.LabelField("Defined In", inherited ? "Inherited profile" : "Current profile");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Prefab", EditorStyles.miniButton))
                        AssetDatabase.OpenAsset(template.Prefab);
                    if (GUILayout.Button("Ping Prefab", EditorStyles.miniButton))
                        EditorGUIUtility.PingObject(template.Prefab);
                }
            }
        }

        /// <summary>Creates a prefab instance from the chosen template.</summary>
        /// <param name="parent">The creation parent.</param>
        /// <param name="template">The template to instantiate.</param>
        /// <param name="name">An optional override name.</param>
        private void CreateTemplate(RectTransform parent, UIComponentTemplate template, string name = null,
            bool useSelectedOverride = false)
        {
            if (parent == null || template == null) return;
            string fallback = template.Kind == UIComponentKind.Custom
                ? template.Prefab != null ? template.Prefab.name : "RT_Component"
                : UIStyleFactory.DefaultName(template.Kind);
            isCreatingComponent = true;
            try
            {
                lastCreatedComponent = UIStyleFactory.CreatePrefab(parent, template, fallback, name,
                    BuildTextOverride(useSelectedOverride));
            }
            finally
            {
                isCreatingComponent = false;
            }
        }

        /// <summary>Draws the editable profile section.</summary>
        private void DrawProfileSetup()
        {
            GUILayout.Label("Edit Profile", EditorStyles.boldLabel);
            GUILayout.Label("Edit saved prefab assets. Existing instances retain their overrides.",
                EditorStyles.wordWrappedMiniLabel);
            SerializedObject serialized = new SerializedObject(selectedProfile);
            serialized.Update();
            SerializedProperty baseProperty = serialized.FindProperty("baseProfile");
            SerializedProperty entries = serialized.FindProperty("templates");
            EditorGUILayout.PropertyField(baseProperty, new GUIContent("Base Profile"));
            GUILayout.Space(7);
            EditorGUILayout.LabelField("Local Components", EditorStyles.boldLabel);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty item = entries.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Component " + (i + 1), EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Up", GUILayout.Width(34)) && i > 0)
                        {
                            entries.MoveArrayElement(i, i - 1);
                            break;
                        }
                        if (GUILayout.Button("Down", GUILayout.Width(48)) && i < entries.arraySize - 1)
                        {
                            entries.MoveArrayElement(i, i + 1);
                            break;
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            entries.DeleteArrayElementAtIndex(i);
                            selectedIdentity = null;
                            break;
                        }
                    }
                    SerializedProperty kind = item.FindPropertyRelative("Kind");
                    EditorGUILayout.PropertyField(kind);
                    if ((UIComponentKind)kind.enumValueIndex == UIComponentKind.Custom)
                        EditorGUILayout.PropertyField(item.FindPropertyRelative("Id"), new GUIContent("Library ID"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("DefaultObjectName"), new GUIContent("Default Name"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("Prefab"));
                }
            }
            if (GUILayout.Button("+ Add Component"))
            {
                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                SerializedProperty item = entries.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("Kind").enumValueIndex = (int)UIComponentKind.Button;
                item.FindPropertyRelative("Id").stringValue = string.Empty;
                item.FindPropertyRelative("DefaultObjectName").stringValue = "BTN_Button";
                item.FindPropertyRelative("Prefab").objectReferenceValue = null;
            }
            if (serialized.ApplyModifiedProperties())
            {
                // The currently rendered profile changed; do not reuse its inherited list.
                effectiveTemplatesForGui = null;
                EditorUtility.SetDirty(selectedProfile);
                Repaint();
            }
        }

        /// <summary>Displays a compact profile-validation summary and optional full issues.</summary>
        private void DrawValidationPage()
        {
            List<string> problems = ValidateProfile(selectedProfile);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(problems.Count == 0 ? "Profile valid" :
                    problems.Count + " configuration issue(s)", EditorStyles.miniBoldLabel,
                    GUILayout.ExpandWidth(true));
                if (problems.Count > 0 && GUILayout.Button(showValidationDetails ? "Hide" : "Details",
                    EditorStyles.miniButton, GUILayout.Width(65f)))
                    showValidationDetails = !showValidationDetails;
            }
            if (problems.Count > 0 && showValidationDetails)
            {
                foreach (string problem in problems)
                    EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }
        }

        /// <summary>Validates the selected profile and all inherited templates.</summary>
        /// <param name="profile">The profile to validate.</param>
        /// <returns>A list of discovered problems.</returns>
        private static List<string> ValidateProfile(UIStyleProfile profile)
        {
            List<string> errors = new List<string>();
            HashSet<UIStyleProfile> seenProfiles = new HashSet<UIStyleProfile>();
            for (UIStyleProfile current = profile; current != null; current = current.BaseProfile)
            {
                if (!seenProfiles.Add(current))
                {
                    errors.Add("Inheritance cycle detected at '" + current.name + "'.");
                    break;
                }
                HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
                foreach (UIComponentTemplate entry in current.Templates)
                {
                    if (entry == null) continue;
                    if (entry.Kind == UIComponentKind.Custom && string.IsNullOrWhiteSpace(entry.Id))
                        errors.Add(current.name + ": Custom entry has an empty ID.");
                    if (!identities.Add(entry.Identity))
                        errors.Add(current.name + ": Duplicate identity '" + entry.Identity + "'.");
                    if (entry.Prefab == null)
                    {
                        errors.Add(current.name + ": Missing prefab for '" + entry.Identity + "'.");
                        continue;
                    }
                    if (!AssetDatabase.Contains(entry.Prefab))
                        errors.Add(current.name + ": Use a saved prefab for '" + entry.Identity + "'.");
                    if (entry.Prefab.GetComponent<RectTransform>() == null)
                        errors.Add(current.name + ": Missing RectTransform on '" + entry.Prefab.name + "'.");
                    Type expected = UIStyleFactory.GetRequiredType(entry.Kind);
                    string proposedName = string.IsNullOrWhiteSpace(entry.DefaultObjectName)
                        ? entry.Kind == UIComponentKind.Custom ? entry.Prefab.name : UIStyleFactory.DefaultName(entry.Kind)
                        : entry.DefaultObjectName.Trim();
                    if (!UIStyleFactory.IsValidDefaultName(proposedName, entry.Kind, out string error))
                        errors.Add(current.name + ": Invalid instance name '" + proposedName + "': " + error);
                    else if (entry.Kind == UIComponentKind.Custom)
                    {
                        int cut = proposedName.IndexOf('_');
                        expected = AdvancedUIToolkitNaming.TypeByPrefix[proposedName.Substring(0, cut)];
                    }
                    if (expected != null && AdvancedUIToolkitNaming.ResolveNamedComponent(
                        entry.Prefab.transform, expected, out string resolutionProblem) == null)
                        errors.Add(current.name + ": Prefab '" + entry.Prefab.name +
                            "' " + resolutionProblem);
                }
            }
            return errors;
        }

        /// <summary>Draws a simple status footer.</summary>
        /// <param name="parent">The current creation root.</param>
        private void DrawFooter(RectTransform parent)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(parent != null ? "Parent: " + parent.name : "No UI parent selected", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(selectedProfile != null ? "Library: " + selectedProfile.name : "No library selected", EditorStyles.miniLabel);
            }
        }

        /// <summary>Attaches a style context to the current UI root.</summary>
        /// <param name="parent">The root to attach to.</param>
        /// <param name="profile">The profile to assign.</param>
        private static void AttachContext(RectTransform parent, UIStyleProfile profile)
        {
            if (parent == null || profile == null) return;
            UIStyleContext context = parent.GetComponent<UIStyleContext>();
            if (context == null) context = Undo.AddComponent<UIStyleContext>(parent.gameObject);
            SerializedObject obj = new SerializedObject(context);
            obj.FindProperty("profile").objectReferenceValue = profile;
            obj.ApplyModifiedProperties();
            EditorUtility.SetDirty(context);
        }

        /// <summary>Creates a new UI style profile asset.</summary>
        private void CreateProfileAsset()
        {
            const string defaultFolder = "Assets/AdvancedUIToolkit/Libraries";
            EnsureFolders(defaultFolder);
            string path = EditorUtility.SaveFilePanelInProject("Create UI Style Profile", "NewUIStyleProfile", "asset",
                "Select where to save the reusable library.", defaultFolder);
            if (string.IsNullOrEmpty(path)) return;
            UIStyleProfile profile = CreateInstance<UIStyleProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            ReloadProfiles();
            SelectProfile(profile);
            UIStyleFactory.ActiveProfile = profile;
            Selection.activeObject = profile;
        }

        /// <summary>Ensures all folders in a path exist before asset creation.</summary>
        /// <param name="path">The target folder path.</param>
        private static void EnsureFolders(string path)
        {
            string[] parts = path.Split('/');
            string folder = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = folder + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(folder, parts[i]);
                folder = next;
            }
        }
    }
}
