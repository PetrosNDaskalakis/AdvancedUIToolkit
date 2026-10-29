# Developer Handbook

[← README](../README.md) · [Installation](INSTALLATION.md) · [API Reference](API_REFERENCE.md)

This is the everyday guide for developers and UI designers using **Advanced UI Toolkit**. Most work takes place in just two windows:

- **Binder:** select a UI root, inspect discovered controls, review changes, preview C#, generate.
- **Factory / UI Library:** select a profile, browse saved prefabs, and create named UI objects.

Additional settings, diagnostics, and profile editing are kept under **Show More**.

## 1. Create a screen

1. Create a Canvas and a dedicated panel or screen root.
2. Add a `UIBindingRoot` component to that root.
3. Create controls through **GameObject → Advanced UI Toolkit** or the **Factory** window.
4. Rename the root GameObjects according to their function, e.g. `BTN_Fire`, `TGL_Power`, `SLD_Volume`.
5. Open **Tools → Advanced UI Toolkit → Binder**. Select the root in the Hierarchy; Binder recognizes the closest owning binding root.
6. Review issues, preview the generated source, and generate bindings.

### Naming conventions

| Example | Expected component | Generated property |
| --- | --- | --- |
| `BTN_Fire` | `Button` | `UI.Fire` |
| `TGL_Power` | `Toggle` | `UI.Power` |
| `SLD_Volume` | `Slider` | `UI.Volume` |
| `TXT_Status` | Legacy `Text` | `UI.Status` |
| `IMG_Contact` | `Image` | `UI.Contact` |
| `INP_PlayerName` | Legacy `InputField` | `UI.PlayerName` |
| `SCR_Messages` | `ScrollRect` | `UI.Messages` |

Prefix mappings are defined by the Editor naming registry; check the current Factory if you need additional types. The matching Unity component may be on the **named object** or on **one unambiguous descendant** of it (inactive children are supported). A component owned by a nested `UIBindingRoot` does not count. If multiple matching children exist, Binder reports an ambiguity rather than choosing one. Names must be unique *within a binding root*; separate roots can reuse identifiers.

## 2. Generate and use a typed binding class

Given `BTN_Fire` and `BTN_Load`, Binder generates a class such as `WeaponPanelBindings` implementing `IUIBindings` with `Button Fire` and `Button Load` properties. Its initializer retrieves references from the serialized manifest once.

Write your view as a `UIView<TBindings>`:

```csharp
using UnityEngine;
using AdvancedUIToolkit;
using Game.UI.Generated;

/// <summary>Demonstrates button binding and lifecycle-safe event handling.</summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    /// <summary>Registers the managed UI callbacks.</summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Fire, OnFire);
        Events.BindClick(UI.Load, OnLoad);
    }

    /// <summary>Handles the Fire button.</summary>
    private void OnFire() => Debug.Log("Fire");

    /// <summary>Handles the Load button.</summary>
    private void OnLoad() => Debug.Log("Load");
}
```

Attach the view to the same GameObject as its `UIBindingRoot`. The generated class is project code, usually stored in `Assets/AdvancedUIToolkit/Generated/`.

## 3. Work in Manual or Automatic mode

**Manual:** Recommended for UI-heavy editing sessions. You may create, rename, or remove many controls without rewriting generated C# until you choose **Generate Changes**. Use **Preview C#** to inspect the next result before generation.

**Automatic:** Validated hierarchy changes may update generated source as they occur. Unity will import and may compile those changes automatically, which can interrupt a long authoring session. Automatic mode also watches validated delete/Undo/Redo changes.

Switch modes in **Binder → Show More → Automation**. This setting controls source generation, not Unity's global asset-refresh behavior.

### After deleting or renaming a control

- Binder can preserve the previous accessor name when a GameObject is renamed.
- Intentional accessor renames or deletion of controls can break gameplay code still using the old property.
- In Manual mode, review the pending change and generate when ready.
- In Automatic mode, safe changes are applied automatically; the toolkit does **not** rewrite your hand-authored gameplay methods.
- Generated C# is not part of Unity's Undo history; instead the toolkit rescans and can regenerate after a restored hierarchy change.

## 4. Organize reusable UI with Style Profiles

Style Profiles are **ScriptableObject libraries** that reference saved uGUI prefabs. For example:

```text
BaseStationProfile
├── SonarStationProfile     → overrides Button
├── WeaponsStationProfile   → overrides InputField
└── NavigationStationProfile → inherits the shared controls
```

Create a profile via **Assets → Create → Advanced UI Toolkit → Style Profile**. Open **Factory → Show More → Edit Profile** to register prefabs by component kind, set defaults, or assign a Base Profile. Attach `UIStyleContext` to a screen or ancestor and assign the profile. Factory uses the nearest matching context.

When Factory instantiates `SonarButton.prefab` as `BTN_TrackTarget`, it retains the prefab connection. This means updating the source prefab can propagate to instances, subject to normal Unity prefab overrides.

## 5. Use the component preview and root selection

The Factory shows a best-effort **normal-state** preview of registered UI prefabs. Select a panel or independent nested UI root in Unity's Hierarchy and the Factory updates its creation target; selecting an ordinary control within that panel keeps the current root. When creating several controls, confirm the root shown in Factory before proceeding.

Use **Show More** for profile editing, validation, and other secondary actions rather than switching to another management window.

## 6. Custom C# generation templates

Binder can generate classes using a developer-provided, compilable `ExampleScript.cs`. The tool recognizes two insertion markers:

```csharp
using AdvancedUIToolkit;
using System;

namespace Example.Template
{
    /// <summary>Example script layout for generating component references.</summary>
    public sealed class ExampleScript : IUIBindings
    {
        // <ADVANCEDUITOOLKIT:PROPERTIES>

        /// <summary>Initializes all generated component references.</summary>
        /// <param name="root">The UI root containing registered references.</param>
        public void Initialize(UIBindingRoot root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            // <ADVANCEDUITOOLKIT:ASSIGNMENTS>
        }
    }
}
```

Choose this template from **Binder → Show More → Generation settings**. The template's layout, comments, and surrounding helper code are preserved, while the component properties and assignments are inserted. Generated code receives XML documentation. The template is not modified by generation; use the optional default-template option to apply it to newly configured screens.

## 7. Nested UI roots and lifetime

Each `UIBindingRoot` owns a distinct binding scope. A reusable Torpedo Control prefab with its own binding root should not have its child controls collected into the parent's manifest.

`UIView<T>` inherits from `MonoBehaviour`, so your script still has `transform`, `gameObject`, `Start()`, `Update()`, and coroutines. Let the base class own `Awake`, `OnEnable`, `OnDisable`, and `OnDestroy`. Override these toolkit hooks instead:

- `OnInitialize()` — once; register managed events.
- `OnViewInitialized()` — once after initialization.
- `OnViewEnabled()` and `OnViewDisabled()` — activation changes, including pooled views.
- `OnViewDestroyed()` — destruction before listener disposal.
- `OnDispose()` — legacy optional cleanup hook.

The event scope suspends subscriptions while disabled and restores them when enabled, preventing duplicate managed callbacks through repeated activation cycles.

## 8. Review errors before generating

Binder exposes issues directly and more detailed diagnostics under **Show More**. Check: duplicate names, absent components, nested root boundaries, changed generated output paths, prefab-instance restrictions, and unexpected edits to generated files.

For larger projects, test a representative screen, a nested prefab, and a pooled view before applying framework upgrades broadly. See [Troubleshooting](TROUBLESHOOTING.md) and the [Technical Overview](TECHNICAL_OVERVIEW.md).

## Binder property documentation (v1.1 preview)

In **Binder → Public Properties & XML Comments**, expand the section and write an optional XML Summary above each public property name. Click **Preview C#** then **Generate** to save comments to the binding manifest and generated C#. Leave a summary blank to retain the default template description. For screenshots and examples, see [Binder XML Summaries](BINDER_XML_SUMMARIES.md).

## Preview 5: UI containers and prefab selections

Binder and UI Library treat normal RectTransform groups as authoring parents and prevent prefab instances from becoming parent targets. See [Container selection and creation](CONTAINER_SELECTION_PREVIEW5.md) for the complete hierarchy example and Factory workflow.

## Prefab Text Override (introduced Preview 8; stabilized Preview 9)

UI Library can optionally replace one serialized `Text` or TextMeshPro label when creating a registered prefab. Select the prefab, enable **Override Displayed Text**, and enter the desired value. When multiple text labels exist, choose the correct **Text Target**. The override is applied to the **new prefab instance only**; its original asset is not edited. Binder generation is unaffected. See [Prefab Text Override](PREFAB_TEXT_OVERRIDE.md).
