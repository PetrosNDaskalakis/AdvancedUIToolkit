# Advanced UI Toolkit v1.1.1 — External Editor API

**Status:** Source-level release candidate; run Unity compilation and manual acceptance before deployment. This contract is Editor-only (`AdvancedUIToolkit.Editor`), available to external Editor assemblies with an explicit assembly reference to `AdvancedUIToolkit.Editor` (and `AdvancedUIToolkit.Runtime` where necessary).

## What is exposed

- `AdvancedUIEditorIntegration.TryResolveTemplate(profile, kind, customId, out template, out error)`: reads an explicit profile and inherited entries, never Library global selection.
- `AdvancedUIEditorIntegration.GetTextTargets(prefabAsset)`: yields source-component identities and text for legacy Text or optional TMP, including nested/inactive children.
- `AdvancedUIEditorIntegration.Validate(request)`: checks the same parent/prefab/naming/type/source-text restrictions as the Factory before changing the scene.
- `AdvancedUIEditorIntegration.Create(request)`: linked prefab instance and optional one-label instance-only override. Does not select or ping by default.
- `AdvancedUIEditorIntegration.ValidateContainer(parent, name)` and `CreateContainer(parent, name)`: ordinary RectTransform creation with nonblank names.

Existing menu and window creation methods preserve their original selection and per-operation Undo behavior. Each individual call still uses a separate Undo group. To create a whole hierarchy transactionally in v1.1.3, use `AdvancedUIEditorIntegration.ValidateBatch` and `CreateBatch` rather than chaining individual calls. The new batch API is documented in [BATCH_CREATION_API.md](BATCH_CREATION_API.md).

## Example consumer (Editor-only)

```csharp
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEngine;

public static class WeaponPanelIntegrationExample
{
    public static void AddLoadButton(UIStyleProfile profile, RectTransform buttonsParent)
    {
        if (!AdvancedUIEditorIntegration.TryResolveTemplate(profile, UIComponentKind.Button,
            null, out UIComponentTemplate template, out string error))
        {
            Debug.LogError(error);
            return;
        }
        IReadOnlyList<UIEditorTextTarget> labels = AdvancedUIEditorIntegration.GetTextTargets(template.Prefab);
        UIEditorCreationRequest request = new UIEditorCreationRequest
        {
            Profile = profile,
            Parent = buttonsParent,
            Kind = UIComponentKind.Button,
            InstanceName = "BTN_Load",
            TextOverride = labels.Count == 0 ? null : new UIEditorTextOverride
            {
                Enabled = true,
                SourceComponent = labels[0].SourceComponent,
                Text = "LOAD"
            }
        };
        var validation = AdvancedUIEditorIntegration.Validate(request);
        if (!validation.Succeeded) { Debug.LogError(validation.Error); return; }
        UIEditorCreationResult result = AdvancedUIEditorIntegration.Create(request);
        if (!result.Succeeded) Debug.LogError(result.Error);
    }
}
```

## Boundaries and safeguards

- The profile must be explicit; no fallback to `EditorPrefs`, active selection, or `UIStyleContext`.
- A parent must be a non-prefab scene `RectTransform`. Source-prefab contents remain protected, including children of prefab instances. Selection behavior in Binder/Library is unchanged.
- Existing `PREFIX_Identifier` naming rules remain mandatory for bindable prefabs. Decorative/unbound prefabs are not yet exposed by this bridge.
- Text labels are associated with a **source Component reference**, not just a position. If reordered, the correct label is still selected; if removed, validation fails. Empty enabled text clears the selected label. Source prefab is not edited.
- Unsupported components and invalid role/type combinations fail preflight. Custom template IDs use `UIComponentKind.Custom` plus `CustomTemplateId`.
- Individual creation uses Unity's existing Factory Undo group. v1.1.3 additionally offers `ValidateBatch` / `CreateBatch`, with a single grouped Undo operation and rollback on failed creation. v1.1.4 adds a separate `AdvancedUIBindingIntegration` facade for noninteractive inspection, initial setup, safe generation, and automation suspension. See [Headless Binder API](HEADLESS_BINDER_API.md).
- Creation can still fail after preflight due to Unity serialization, prefab linkage, or unexpected Editor errors. Check each result and do not assume preflight means guaranteed execution.
- Unity compilation, an independent consumer assembly compile, prefab variant behavior and real Undo/Redo must be verified inside a Unity 2022.3+ project.

## Manual verification checklist

1. Install the v1.1.1 embedded UPM package; check Unity Console is free of compile errors.
2. Compile the example through an **external** `Editor` assembly definition referencing `AdvancedUIToolkit.Editor` explicitly, with `AdvancedUIToolkit.Runtime` where required.
3. With a saved Style Profile and prefab, validate and create a button under a regular `RectTransform`; inspect its prefab link.
4. Verify global Unity selection stays unchanged when `UpdateSelection` is false, and changes when true.
5. Override a label on a nested inactive child and confirm the original prefab's text stays unchanged.
6. Reorder two label components and confirm the same source component is targeted; remove the selected component and expect a validation error.
7. Verify `CreateContainer` accepts arbitrary names, but fails if parent belongs to a prefab instance.
8. Verify normal Binder and UI Library buttons still select the newly created object and that their standard creation and Undo behave as before.
9. Verify expected naming, template inheritance, optional TMP behavior and custom-template errors.

