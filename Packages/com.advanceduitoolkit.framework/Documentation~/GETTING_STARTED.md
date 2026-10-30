# Framework getting started

[Documentation index](INDEX.md) · [Developer handbook](DEVELOPER_HANDBOOK.md)

## Create a reusable control library

1. Create a Canvas and an ordinary screen panel with a child RectTransform for controls.
2. Build a uGUI Button and save it as a prefab under `Assets`.
3. Choose **Assets → Create → Advanced UI Toolkit → Style Profile** and save a profile.
4. Register the Button prefab in the profile's templates. Open **Tools → Advanced UI Toolkit → Library** and choose that profile.
5. Set the target to your ordinary scene container. Use the Button template to create `BTN_Fire`.

Library keeps the source prefab link. Search and kind filters affect the displayed entries; **Show Inherited** controls whether inherited templates participate. Clear the filters if the expected entry is hidden. A manually assigned target stays pinned; use hierarchy following when you want selection changes to drive the target. Library does not silently substitute an ancestor for an invalid selected container.

## Generate typed references

1. Add `UIBindingRoot` to the screen root.
2. Open **Tools → Advanced UI Toolkit → Binder** and select that root.
3. Review discovery. `BTN_Fire` should resolve to a Button with generated property `Fire`.
4. Configure class name `WeaponPanelBindings`, namespace `Game.UI.Generated` and an output directory under `Assets`.
5. Preview C#, resolve reported issues and generate. Wait for Unity compilation.
6. Create `WeaponPanel.cs` under `Assets` and attach the view to the same root:

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

    private void OnFire() => Debug.Log("Fire");
}
```

In Play Mode, clicking Fire should log once. Disable and re-enable the view, then click again to check that listeners resume without duplication. See [Lifecycle](LIFECYCLE.md) for all hooks.

## Reuse the screen structure

Install Authoring and follow the [Layouts quick start](../../com.advanceduitoolkit.authoring/Documentation~/GETTING_STARTED.md). A mold captures eligible containers; add complete prefab controls with Library after creating the layout, then configure Binder for the new screen.

Use [Manual generation](GENERATION_MODES.md) for long authoring sessions so you can review and write binding changes together.
