# Advanced UI Toolkit 1.0.0-rc.8 — Undo/Redo Binding Synchronization

## Why the fix was needed

In earlier versions, generating C# changed two separate things: an on-disk `.cs` file and the serialized `UIBindingRoot` manifest. Only the manifest was registered with Unity Undo. This meant Ctrl+Z could undo the manifest update instead of a preceding UI object deletion, leaving source and manifest inconsistent.

The derived manifest is now persisted without creating a Unity Undo step. Unity's scene is marked dirty so it can be saved, while the generated `.cs` remains a normal asset write. Automatic Undo/Redo monitoring also schedules a deferred rescan.

## Expected behavior

1. Set Binder → Show More → Automation to **Automatic** and enable watching.
2. In an editable scene or Prefab Stage with an existing registered `UIBindingRoot`, create a named UI control and generate bindings.
3. Delete that control from the Hierarchy. The generated C# accessor should disappear.
4. Press Ctrl+Z once. The deleted control should reappear in the Hierarchy.
5. Automatic mode should detect it and regenerate the C# accessor after its debounce interval and Unity's import/compile cycle.
6. The Binder should report no pending changes after successful regeneration.

## Important limitations

- If the generated script has been manually modified, or no longer matches the trusted saved manifest, automatic overwriting remains blocked to protect custom code. Review and regenerate from Binder.
- Deleting a prefab **asset** in the Project window is different from deleting a prefab **instance** under a UI root. This workflow monitors editable hierarchy roots and the active Prefab Stage.
- If a removed accessor was still used by gameplay code, temporary compile errors may appear after deletion. Undo can restore the accessor when the UI control is restored.
- A fresh deletion/restoration test is necessary after upgrading; older Undo steps created by previous versions may already have desynchronized manifest and source.

## Test coverage

`AdvancedUIToolkitUndoRestoreTests` verifies that an automatic manifest update does not steal the previous delete Undo and that a restored control becomes eligible for a new binding.
