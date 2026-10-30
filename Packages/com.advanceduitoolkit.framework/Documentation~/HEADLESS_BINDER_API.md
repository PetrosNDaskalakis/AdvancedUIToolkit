# Headless Binder Integration

[Documentation index](INDEX.md)

`AdvancedUIBindingIntegration` is an **Editor-only public API** for consumer packages. It uses the exact same Binder scanning, code-template generation, file safety, and serialized manifest implementation already used by the standalone Binder. It does not open a window or select objects.

## Build a hierarchy, then generate its first bindings

A consumer Editor assembly must reference `AdvancedUIToolkit.Editor` and `AdvancedUIToolkit.Runtime` where required.

```csharp
using AdvancedUIToolkit;
using AdvancedUIToolkit.Editor;
using UnityEngine;

// `request` is an existing valid UIEditorBatchRequest.
using (AdvancedUIBindingIntegration.SuspendAutomaticGeneration())
{
    UIEditorBatchResult batch = AdvancedUIEditorIntegration.CreateBatch(request);
    if (!batch.Succeeded)
        Debug.LogError(batch.Error);
}

// Once the scope ends, the hierarchy is complete and automation may rescan.
UIEditorBindingResult initial = AdvancedUIBindingIntegration.InitializeBindings(
    new UIEditorBindingSetup
    {
        Target = weaponPanel,
        ClassName = "WeaponPanelBindings",
        NamespaceName = "Game.UI.Generated",
        OutputFolder = "Assets/Generated",
        TemplateScript = null
    });
if (initial.Status == UIEditorBindingStatus.Blocked)
    Debug.LogError(initial.Message);
```

**Important:** `InitializeBindings` is only for unregistered roots. A new root must contain at least one legal bindable component; otherwise Binder reports a block. Generated files go under `Assets/`, not the UPM package. Save the scene/prefab to persist the manifest. A null `TemplateScript` selects the built-in template, regardless of user EditorPrefs.

## Inspect and safely regenerate

```csharp
UIEditorBindingResult inspection = AdvancedUIBindingIntegration.InspectBindings(root);
if (inspection.Status == UIEditorBindingStatus.PendingChanges)
{
    UIEditorBindingResult generated = AdvancedUIBindingIntegration.GenerateBindings(root);
    Debug.Log(generated.Message);
}
else if (inspection.Status == UIEditorBindingStatus.PendingRemoval)
{
    // Removing an accessor is a breaking API change. Ask for user approval in
    // your own Editor UI, then call GenerateBindings(root, true) if approved.
    Debug.LogWarning(inspection.Message);
}
```

Public statuses: `UpToDate`, `PendingChanges`, `PendingRemoval`, `Generated`, `SetupRequired`, and `Blocked`. `Succeeded` is false for `Blocked` and `SetupRequired`; a `PendingRemoval` result is not proof that changes were written. Check the **Status** to determine what to do next.

## Behavior and boundaries

- **No dialogs** are displayed. Removal of existing properties is denied by default. Passing `allowRemovedAccessors: true` acknowledges the breaking change and still requires all Binder safety checks to pass.
- Inspection does **not** rename objects, create a manifest, or write source. Unsupported roots or files edited outside Binder are blocked.
- For initial generation, provide explicit class, namespace, folder and an optional custom template. Invalid names, target ownership, unsafe paths, and occupied destinations are blocked by Binder's normal validation.
- `SuspendAutomaticGeneration()` is a **nestable** `IDisposable` scope. Dispose from the Unity main thread. It pauses scheduled automation scans and writes, not manual Unity editing. The outermost scope schedules one debounced rescan when released.
- `AdvancedUIEditorIntegration.CreateBatch` automatically suspends automation during its Undo operation and rollback. External clients can also wrap additional hierarchy edits in a suspension. **Call headless generation only after releasing that scope**; it refuses generation while suspended.
- No C# generation is included in Unity's hierarchy Undo transaction. Generated files and scene Undo must be treated as separate stages.
- For registered roots, `GenerateBindings` works only with a saved generation profile and existing, unmodified generated source. It never takes over a hand-written script.
- Avoid invoking from background threads or during Unity compilation/import/Play Mode transitions.
- Custom Binder component types are registered separately through the [component extensibility API](COMPONENT_EXTENSIBILITY_API.md).
- This package does **not** ship Editor test or benchmark panels. Test in Unity 2022.3+ and use the native Unity Profiler.

## Acceptance checks in Unity

1. Compile this package and an external Editor asmdef referencing `AdvancedUIToolkit.Editor` and `AdvancedUIToolkit.Runtime`.
2. Create a Canvas → WeaponPanel → ButtonsParent hierarchy with three linked prefabs in one batch. Confirm a single Ctrl+Z removes the hierarchy and restores it through Redo.
3. Call `InitializeBindings` **after** the batch scope ends; check source and manifest, generated C# compilation, and XML summaries.
4. Add a button; `InspectBindings` should return `PendingChanges`, and `GenerateBindings` should update the source without opening Binder.
5. Delete a button; `GenerateBindings` without explicit approval should return `PendingRemoval`, leave source and manifest untouched, and only update after calling it again with `allowRemovedAccessors: true`.
6. During a long `SuspendAutomaticGeneration()` scope with Automatic mode enabled, confirm automatic updates do not run early; after releasing the scope, changes are detected.
7. Modify the generated source by hand; subsequent external generation must refuse to overwrite it.
8. Confirm existing Binder/Library UI and manual/automatic modes are unchanged.
