# Advanced UI Toolkit v1.1.3 — Batch Creation API

**Scope:** Editor-only public integration contract, intended for external packages that depend on `AdvancedUIToolkit.Editor` (and `AdvancedUIToolkit.Runtime` for profile types). Does not add any UI controls, tests or benchmark tools to the package.

## Why it exists

The ordinary UI Library creates one component per click and selects it. That behavior is preserved. An external UI generator, however, needs to create a complete hierarchy under one existing, ordinary `RectTransform` with **one Ctrl+Z**, without changing the active selection, and with rollback if a step cannot be completed.

## API types

- `UIEditorBatchRequest.RootParent`: existing scene non-prefab RectTransform.
- `UIEditorBatchRequest.Steps`: ordered `UIEditorBatchStep` elements.
- `UIEditorBatchStep.Id`: unique stable key *within one batch*; not a serialized scene identifier.
- `UIEditorBatchStep.ParentId`: ID of an **earlier container step**; blank uses `RootParent`. Prefab instances cannot be parents.
- `UIEditorBatchStep.StepKind`: `Container` or `Prefab`.
- `UIEditorBatchStep.Profile`, `ComponentKind`, `CustomTemplateId`, `Name`, `TextOverride`: same explicit profile/validation conventions as the single-item API.
- `AdvancedUIEditorIntegration.ValidateBatch(request)`: checks the complete definition without creating scene objects.
- `AdvancedUIEditorIntegration.CreateBatch(request)`: creates scene objects and returns an `UIEditorBatchResult` with `Succeeded`, `Error`, `FailedStepIndex` and `CreatedObjects` keyed by IDs.

## Example: Panel → ButtonsParent → Button

The following is Editor-only code for a separate consumer assembly referencing the two toolkit assemblies. The example expects the consumer to provide a valid `UIStyleProfile` with a registered Button prefab.

```csharp
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEngine;

public static class WeaponBatchExample
{
    public static UIEditorBatchResult Build(RectTransform canvas, UIStyleProfile profile)
    {
        UIEditorBatchRequest batch = new UIEditorBatchRequest
        {
            RootParent = canvas,
            UndoName = "Generate Weapon Controls"
        };
        batch.Steps.Add(new UIEditorBatchStep
        {
            Id = "panel", Name = "WeaponPanel", StepKind = UIEditorBatchStepKind.Container
        });
        batch.Steps.Add(new UIEditorBatchStep
        {
            Id = "buttons", ParentId = "panel", Name = "ButtonsParent",
            StepKind = UIEditorBatchStepKind.Container
        });
        batch.Steps.Add(new UIEditorBatchStep
        {
            Id = "load", ParentId = "buttons", Name = "BTN_Load",
            StepKind = UIEditorBatchStepKind.Prefab,
            Profile = profile, ComponentKind = UIComponentKind.Button
        });

        UIEditorValidationResult validation = AdvancedUIEditorIntegration.ValidateBatch(batch);
        if (!validation.Succeeded)
            Debug.LogError(validation.Error);
        return AdvancedUIEditorIntegration.CreateBatch(batch);
    }
}
```

To override text in a batch prefab step, call `TryResolveTemplate(...)`, then `GetTextTargets(template.Prefab)`, choose a source component, and assign `new UIEditorTextOverride { Enabled = true, SourceComponent = target.SourceComponent, Text = "LOAD" }` to that step before validation. A deleted label causes preflight to fail closed.

## Undo and failure behavior

1. Preflight checks that every ID is unique; parents refer to preceding containers; each prefab resolves against an explicit Style Profile; names and single text targets satisfy the existing Factory validation.
2. If preflight fails, `CreateBatch` returns immediately without creating anything or opening an Undo group.
3. The batch starts its **own** Undo group and creates each element through the existing Factory using a `joinUndoGroup` internal option.
4. On success, the Undo group is collapsed into **one Ctrl+Z**, and the result includes created GameObjects keyed by their step IDs.
5. On failure, `Undo.RevertAllDownToGroup` attempts to revert the batch; a secondary cleanup removes already-tracked created objects if Unity's rollback throws. In that exceptional case, Undo history repair cannot be guaranteed; inspect the scene and Console.
6. The previous Unity selection is preserved, including on failure.

Batch creation changes native scene objects only. It **does not** write C# generated binding files, suppress automatic Binder scans, alter prefab assets, or modify existing binding manifest serialization. Headless Binder generation and automation coordination are planned separately.

## Limits

- Containers must be ordinary non-prefab `RectTransform` objects; prefab instances and their descendants cannot act as parents.
- Parent steps must appear before children. Invalid/missing/circular references are rejected during preflight.
- The batch does not create an entire Unity Scene or root Canvas. Supply an existing editable Canvas/panel parent.
- There is no nested transaction support or cross-process rollback guarantee. The API is intended for synchronous calls on Unity's Editor thread.
- Factory may still generate a unique name if another sibling already uses the requested name; do not rely on names as persistent authoring IDs.
- Only one source-identity-checked text component can be overridden per created prefab instance.
- For this release, Editor automation may schedule a later scan after the hierarchy changes; headless C# generation is not part of the batch operation.

## Manual Unity acceptance (no bundled test panels)

1. Install the v1.1.3 package and compile an external Editor assembly using `AdvancedUIEditorIntegration.CreateBatch`.
2. Build `Canvas → WeaponPanel → ButtonsParent → BTN_Load / BTN_Arm / BTN_Fire` with linked prefabs; confirm three separate text overrides remain correct and source prefabs unchanged.
3. Preserve the selected object while generating; Ctrl+Z once should remove the entire generated hierarchy; Ctrl+Shift+Z should restore it.
4. Attempt a missing template, deleted text source, duplicate step ID, forward reference and prefab-as-parent; preflight should refuse without changing scene or Undo.
5. Force a runtime Factory failure after a successfully created container, and verify rollback leaves no generated hierarchy behind.
6. Use UI Library manually to create a container and prefab; each should still have its own Undo and normal selection behavior.
7. Verify Automatic and Manual Binder generation continue to work after creating and undoing a batch.

**Verification status:** package static audit and archive checks are available in this release; real Unity compilation and Undo/Redo behavior must be confirmed in the target Unity project.
