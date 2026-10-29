# Proposed next feature — Prefab Text Override

[← README](../README.md) · [Foundation roadmap](FOUNDATION_ROADMAP.md) · [UI Library](DEVELOPER_HANDBOOK.md)

**Historical design proposal. Implemented in Preview 8.** See the [current usage guide](PREFAB_TEXT_OVERRIDE.md).

## Goal

Let a developer choose a prefab in **Tools → Advanced UI Toolkit → Factory**, enter the text it should display, and click **Create**. The created prefab instance uses that text, while the source prefab asset remains unchanged.

The control's existing **Object Name** remains separate from the displayed text.

## Target flow

1. Register `WeaponButton.prefab` once in a Style Profile.
2. Select `Buttons parent` (an ordinary RectTransform container).
3. Select the registered Button in the UI Library.
4. Set **Object Name** to `BTN_Load` and **Displayed Text** to `LOAD`.
5. Click **Create**. The new prefab instance displays `LOAD`.
6. Repeat with `BTN_Arm` / `ARM` and `BTN_Fire` / `FIRE`.
7. Binder still generates `UI.Load`, `UI.Arm`, `UI.Fire` based on the GameObject names; the text is merely an instance-specific visual override.

## Proposed compact UI

```text
Selected Component: WeaponButton
Object Name      [ BTN_Load         ]
Displayed Text   [ LOAD             ]
                            [ Create ]
```

The text input appears only if the chosen prefab contains a supported label component. No additional panel or tab should be required.

## Text discovery and ambiguity

- Support a legacy `UnityEngine.UI.Text` on the prefab's root **or any descendant**, including inactive children.
- Support TextMeshPro `TMP_Text`/`TextMeshProUGUI` when TMP is installed, without introducing a mandatory assembly dependency for projects that use only uGUI legacy text.
- If there is **exactly one** supported text component, preselect it automatically.
- If there are **several** labels (for example `Title`, `Description`, `Counter`), show a small **Text Target** dropdown with child hierarchy paths; never silently overwrite all text labels or guess the wrong one.
- If there is no supported text component, hide the Displayed Text editor and preserve normal creation.
- Distinguish **no override** from an intentional empty string: leaving the override disabled uses the source prefab text; explicitly clearing a field can create an empty label.
- Treat input placeholders, dynamic counters, and other auxiliary labels as separate selectable targets rather than assuming all text should be changed.

## Creation safety

- Instantiate the existing prefab using the current Factory action.
- Apply the selected text only to the **newly created instance**, on the correct nested `Text`/TMP component. Never edit the original prefab asset or other scene instances.
- Record the change using Unity Undo and prefab-instance property modification tracking. Undo should behave predictably even when the prefab contains nested children.
- Don't change binding discovery, public accessor names, `UIBindingRoot` manifests, XML summaries, generated code, or Runtime APIs.
- When changing selected prefabs, reset the text override draft to avoid accidentally giving an unrelated prefab an old label.
- Keep the current prefab-selection restriction: clicking a prefab in the Hierarchy does not make that prefab the creation parent.

## Acceptance tests

1. Root-level legacy Text is overridden only on the new instance.
2. Nested, inactive legacy Text is discovered and overridden.
3. Nested TMP label is overridden when TMP is installed; packages without TMP still compile.
4. Multiple labels require an explicit choice, and only that target changes.
5. Empty text and unchanged text have distinct behavior.
6. Undo/Redo restores or reapplies the expected instance override without modifying the prefab asset.
7. Three instances can display LOAD / ARM / FIRE while using one registered prefab template.
8. Binder and automatic/manual C# generation produce the same accessors as before.

## Scope and sequencing

Keep the feature out of the present performance and reliability cycle. After those milestones pass Unity verification, implement it as a **small Editor-only UI Library refinement**, not a separate text-editing subsystem or runtime text-binding framework.
