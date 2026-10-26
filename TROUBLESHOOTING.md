# Troubleshooting

[← README](../README.md) · [Installation](INSTALLATION.md) · [API Reference](API_REFERENCE.md)

These instructions apply to **Advanced UI Toolkit `1.0.0-rc.10`**. Start with the first Unity Console error. Most reference problems can be inspected from **Tools → Advanced UI Toolkit → Binder**, then **Show More → Project diagnostics**.

## Compilation errors after installation

**Check:** You installed the actual UPM package (`package.json` at its root), `com.unity.ugui` is resolved, and you do not have duplicate Runtime/Editor scripts left under `Assets/`.

If game code uses its own `.asmdef`, add a `UICore.Runtime` reference. Existing code must still use `using UICore;`—the public product rename did not change the namespace.

## `CS0311` or `CS0310` with `UIView<MyBindings>`

Your binding class must implement `IUIBindings`, expose a public parameterless constructor, and provide `Initialize(UIBindingRoot root)`. Older generated classes with a constructor taking the root need to be regenerated through Binder.

## A generated component reference is missing

**Example:** `UI.Fire` fails to initialize.

Check all of the following:

1. `UIBindingRoot` and your `UIView<...>` subclass are on the **same** GameObject.
2. The target GameObject name follows the expected prefix, e.g. `BTN_Fire`.
3. Its `Button` component exists **on the named GameObject**, not only on a child label.
4. The control is inside the correct root, not inside an independently bound nested root.
5. Binder shows a valid component and you have saved/generated its manifest.

## Two controls produce duplicate keys

Rename objects uniquely within the same root, such as `BTN_FirePrimary` and `BTN_FireSecondary`. Separate binding roots can use the same property name without conflict.

## My prefab is not using the correct Style Profile

Check that the root or ancestor has `UIStyleContext` with the intended profile; confirm a saved prefab is registered under the required component kind; inspect whether an inherited base profile supplies an unexpected component. Select the intended UI root in the Hierarchy and verify Factory shows the correct Library and Root fields.

## I still get frequent C# recompilations while designing

Switch to **Binder → Show More → Automation → Manual**. Manual mode defers **toolkit-generated source writes** until you click Generate. Unity's independent asset imports, user C# edits, or other tools can still trigger compilation. Its own Auto Refresh preference is configured separately in **Edit → Preferences → Asset Pipeline**.

## Renaming a control changed my generated API

GameObject renames can preserve the previous generated property name, but an intentional API rename or deleted control means game code using the old `UI.SomeProperty` must be updated. Use **Preview C#** to review the difference, then generate when ready. Advanced UI Toolkit does not rewrite your handwritten gameplay scripts.

## Delete → Ctrl+Z does not restore an accessor

1. Confirm generation mode is **Automatic** if you expect immediate regeneration.
2. Confirm Unity restored the UI object to the correct owning root.
3. Allow Unity to finish its hierarchy update; reselect the root and check Binder's pending changes.
4. In **Manual** mode, restoration only queues the change—click Generate.
5. If the file was edited manually or ownership validation failed, review the warning and resolve it before regenerating.

The C# text file itself is not Undo-managed by Unity. The expected behavior is re-scanning the restored UI and regenerating the corresponding accessor.

## Automatic generation is blocked

Check for invalid names, duplicate properties, missing expected components, unsafe scene prefab-instance modifications, an unconfigured output path, two screens targeting the same file, or unexpected manual edits to the generated source. Resolve the warning, open the original prefab in Prefab Mode where appropriate, and regenerate through Binder.

## UI buttons don't respond after re-enabling a view

Use `Events.BindClick(...)` from `OnInitialize()`; ensure the view and control are enabled/interactable and the scene contains an EventSystem. The toolkit suspends managed listeners on disable and restores them on re-enable. Do not duplicate registrations in `OnViewEnabled()`.

## A click fires twice

Check whether you also configured the same action in the Button Inspector's `On Click()` list or added it directly through `onClick.AddListener`. Toolkit-managed listeners and separately authored listeners can both fire.

## Prefab preview looks blurry, blank, or different from the game

Select **Refresh Previews** in the Factory. The Preview renderer is best-effort and can be affected by shaders, fonts, render pipelines, or unusual Canvas configurations. Confirm the real prefab in Scene/Game view before assuming the runtime UI is broken. The component browser intentionally shows normal-state previews only.

## Generated class cannot be found

Verify the file was actually generated and compiled, the namespace in your `using` statement matches the chosen generation profile (for example `Game.UI.Generated`), and the class name matches the generic type argument. Check for the first compiler error in the Console.

## Missing scripts after moving from `Assets/UICore`

This normally indicates lost `.meta` GUIDs or duplicate copies of the same class. Restore your project backup and migrate while preserving original script GUIDs. Avoid removing the old scripts and creating brand-new GUID-bearing replacements.

## Package tests do not appear

Install Unity Test Framework. If necessary, include the package in the project manifest's `testables` list: `"testables": ["com.uicore.framework"]` without removing other entries. Open **Window → General → Test Runner** and inspect EditMode and PlayMode tests separately.

## Report an issue

Include: Unity Editor version, toolkit version, whether the project uses an embedded or disk package, full first Console error, expected/actual behavior, reproduction steps, prefab-instance vs prefab-asset context, and (if applicable) generated C# diff or Binder screenshot. Never attach proprietary project assets to a public issue without permission.
