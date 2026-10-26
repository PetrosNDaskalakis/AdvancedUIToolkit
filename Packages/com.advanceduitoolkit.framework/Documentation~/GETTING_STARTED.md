# Getting started: first working UI

**Outcome:** a working `WeaponPanel` with three buttons; no manual Inspector button references.

## 1. Install the package

Open **Window > Package Manager**, use **+ > Add package from disk** and select Advanced UI Toolkit's `package.json`. In an embedded setup, the package should be at `YourProject/Packages/com.advanceduitoolkit.framework/` (not `Assets/AdvancedUIToolkit`). Verify the Console has no compile errors. `com.unity.ugui` must be present.

If you are migrating a pre-UPM install, use [Migration](MIGRATION_TO_ADVANCED_UI_TOOLKIT.md) first to retain Unity script `.meta` GUIDs. **Never delete an old script and add a new GUID-bearing script if existing scenes reference it.**

## 2. Build a hierarchy

Create a Canvas and a panel under it. Attach `UIBindingRoot` to the panel (the **screen root**). Create three Advanced UI Toolkit buttons under this root using **GameObject > Advanced UI Toolkit > Button**. Rename their root GameObjects precisely:

```text
Canvas
└── WeaponPanel  [UIBindingRoot]
    ├── BTN_Arm   [Button]
    ├── BTN_Fire  [Button]
    └── BTN_Load  [Button]
```

The prefixes identify component types. `BTN_` indicates `UnityEngine.UI.Button`. The component can live on the named GameObject or on **exactly one matching child** inside the same binding scope. For example, `IMG_Icon` may be a wrapper with a single `Image` child. If several matching children exist, name the intended child explicitly or move the component to the wrapper to remove ambiguity.

## 3. Scan and generate

Open **Tools > Advanced UI Toolkit > Binder** and select `WeaponPanel`. The component list and inline messages highlight naming or reference issues. Advanced UI Toolkit suggests `WeaponPanelBindings` by default for a new root. To customize its class name, namespace, output folder, or ExampleScript, expand **Show More > Generation settings & property names**. Click **Preview C#** for a read-only source preview, then **Generate Bindings** when ready.

**Compile note:** saving a new `.cs` file triggers a Unity import/compilation. To avoid multiple interruptions, set **Binder > Show More > Automation & batch generation > Manual** while authoring, and generate when you are finished.

## 4. Write a view

Create `Assets/Scripts/WeaponPanel.cs`:

```csharp
using AdvancedUIToolkit;
using UnityEngine;
using Game.UI.Generated;

/// <summary>Connects weapon buttons to gameplay actions.</summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    /// <summary>Registers listeners once; Advanced UI Toolkit owns their cleanup.</summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Arm, Arm);
        Events.BindClick(UI.Fire, Fire);
        Events.BindClick(UI.Load, Load);
    }

    /// <summary>Handles Arm input.</summary>
    private void Arm() => Debug.Log("Arm clicked", this);

    /// <summary>Handles Fire input.</summary>
    private void Fire() => Debug.Log("Fire clicked", this);

    /// <summary>Handles Load input.</summary>
    private void Load() => Debug.Log("Load clicked", this);
}
```

Attach `WeaponPanel` **to the same GameObject as** `UIBindingRoot`. No `Button` fields or manual button Inspector hookups are required. If `WeaponPanel.cs` belongs to your own asmdef assembly, that assembly must reference `AdvancedUIToolkit.Runtime`.

## 5. Verify

Enter Play Mode and click Arm, Fire, Load. Each should log once. Disable and re-enable the root GameObject: managed callbacks suspend and restore without being registered twice. `OnInitialize` is executed once; `OnViewEnabled` and `OnViewDisabled` can be overridden for repeated activation.

## 6. Optional imported source-only sample

Import **Weapon Panel — Buttons** from **Package Manager > Advanced UI Toolkit > Samples**. It contains self-contained C# classes (`WeaponPanelExample` and `ScreenBindingsSample`) that compile without first running code generation, **but require you to perform the scan above to populate their registry**. Those files are demonstrations; production UIs should use the generated class.

## Next steps

- [Developer handbook](DEVELOPER_HANDBOOK.md) — automation and diagnostics.
- [Style profiles](STYLE_PROFILES.md) — separate station appearance.
- [Custom code templates](CUSTOM_CODE_TEMPLATES.md) — code layout and XML comments.
- [Troubleshooting](TROUBLESHOOTING.md) — compiler errors and failed lookups.
