# Quick Start

This walkthrough creates one button and a typed view script using the **Framework** package. The **Authoring** package is optional.

## 1. Install

Copy `Packages/com.advanceduitoolkit.framework` into the Unity project's `Packages/` directory (including `.meta` files). Open Unity **2022.3+** and wait for compilation. To use Layouts, also copy `com.advanceduitoolkit.authoring`.

## 2. Create a screen

Create a Canvas and a Panel beneath it. Add `UIBindingRoot` to the Panel. Create or select a **Style Profile** from **Assets → Create → Advanced UI Toolkit → Style Profile**, register a compatible Button prefab, and open **Tools → Advanced UI Toolkit → Library**. Place a Button under the Panel and name its GameObject `BTN_Fire`.

You can alternatively place an ordinary uGUI Button and give it the same name. Library is useful when the project has saved templates.

## 3. Generate bindings

Open **Tools → Advanced UI Toolkit → Binder**. Select the Panel's `UIBindingRoot`, configure class name `WeaponPanelBindings`, namespace `Game.UI.Generated`, and an output folder under `Assets/`. Generate, then wait for Unity to compile the generated script. Check that the generated class exposes `Fire`.

## 4. Add behavior

Add this script to the same GameObject as `UIBindingRoot`:

```csharp
using AdvancedUIToolkit;
using Game.UI.Generated;
using UnityEngine;

public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Fire, OnFire);
    }

    private void OnFire()
    {
        Debug.Log("Fire clicked");
    }
}
```

Enter Play Mode and click the button. `UIView<TBindings>` resolves references on initialization and suspends/restarts registered listeners when the view is disabled/enabled. Do not override or hide its Unity lifecycle methods; use its view hooks.

## 5. Optional: reusable layouts

With Authoring installed, open **Tools → Advanced UI Toolkit → Layouts**. Choose **Create New Layout** to capture a suitable container skeleton, or **Load Layout** to reuse a saved mold. Prefab branches are omitted by design: use Library to populate the reconstructed containers.

## Troubleshooting

If `UI.Fire` does not exist, verify the `BTN_` prefix, the binding root selection, the generated output and Unity compilation. If a view reports a missing root, put `UIBindingRoot` on the same GameObject as the view. For advanced cases see [`Document~/`](Document~/).
