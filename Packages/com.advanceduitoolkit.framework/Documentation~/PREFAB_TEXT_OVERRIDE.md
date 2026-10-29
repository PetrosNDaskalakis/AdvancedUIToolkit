# Prefab Text Override — Advanced UI Toolkit

[← README](../README.md) · [Developer Handbook](DEVELOPER_HANDBOOK.md) · [Troubleshooting](TROUBLESHOOTING.md)

**Introduced in `1.1.0-preview.8`; selection safety improved in `1.1.0-preview.9`.** This is an Editor-only UI Library refinement. It does not change Binder, generated bindings, or runtime gameplay text behavior.

## Quick tutorial: LOAD, ARM, FIRE

1. Save a regular uGUI Button prefab with a `Text` or `TextMeshProUGUI` label at its root or inside a child. Register it once as the Button entry of a Style Profile.
2. In **Tools → Advanced UI Toolkit → Factory**, select an ordinary non-prefab `Buttons parent` RectTransform in your Unity Hierarchy.
3. Select the Button entry under **Selected Component**.
4. Enter **Object Name**: `BTN_Load`.
5. Enable **Override Displayed Text** and enter **Displayed Text**: `LOAD`.
6. Click **Create**. The new instance retains its prefab link and displays `LOAD`; the saved prefab source still has its original text.
7. Repeat with `BTN_Arm` / `ARM` and `BTN_Fire` / `FIRE`.
8. Open **Tools → Advanced UI Toolkit → Binder** and generate your existing `UI.Load`, `UI.Arm`, and `UI.Fire` C# references as normal.

You can create additional buttons from the gallery's quick **Create** action. The edited label is used **only for the currently selected prefab entry**. Other entries still use their own original prefab text.

## Supported targets

- Legacy Unity `UnityEngine.UI.Text` on the root or any child.
- `TMPro.TMP_Text` subclasses (including `TextMeshProUGUI`) on the root or child **if TMP is installed**. Advanced UI Toolkit has no compile-time TMP assembly dependency.
- Inactive child GameObjects.
- More than one text component: the **Text Target** popup lets you choose exactly one label by its hierarchy path. Only that label changes.
- Prefabs containing no supported label are still usable; the override controls are not shown.

## Empty vs. unchanged

| UI Library state | Result |
| --- | --- |
| **Override Displayed Text** disabled | Preserve the source prefab's current text |
| Enabled with `LOAD` | Create the new instance with `LOAD` |
| Enabled with an empty value | Create the new instance with an intentionally blank label |

Changing the label of a new instance uses Unity's Undo and prefab-instance modification APIs. **The source prefab asset and already-created sibling instances are never edited.** If the selected label has been deleted from the source prefab before creation, Factory refuses that override rather than modifying an unrelated component.

## Deliberate limitations

- Editor-only, at creation time. No runtime reactive or data binding.
- Only one text target can be changed per created instance.
- No localization database, fonts, font sizes, colors, rich-text tools, or automatic layout corrections.
- Dynamically generated runtime labels cannot be discovered at edit time.
- The Factory keeps a reference to the selected source component. If a saved prefab is reordered, the same selected label remains the target. If it is removed, replaced or cannot be resolved after an asset refresh, the Factory refuses creation with the pending override until you explicitly choose another target (or turn the override off).
- Index-only override requests are supported for legacy internal callers, but do not protect against source edits before creation. The Library UI always requests component-identity validation.
- Unity's Inspector remains the tool for advanced label layout and styling.

## Unity verification

The package includes `AdvancedUIToolkitPrefabTextOverrideTests` covering legacy and optional TMP text, inactive nested labels, disambiguation, source deletion/reordering with duplicate names, saved variants, nested prefab targets, Undo separation, and source preservation. Run the tests inside Unity; their presence is not evidence that they pass.

The test code is included, but **Unity compilation and test execution must be performed in a Unity 2022.3+ Editor**. Check generated Button instances in a scene, Prefab Mode if relevant, and your project's render pipeline before adopting the preview in production.
