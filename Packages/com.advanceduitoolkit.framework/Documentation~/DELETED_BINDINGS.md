# Deleting UI Components and Updating Bindings

The AutoBinder follows each `UIBindingRoot` manifest and monitors named controls in its hierarchy. Deleting a bound prefab instance or named uGUI component creates a **pending removal**.

- **Manual mode (recommended):** continue editing the UI without recompiles; the Workspace displays the removed binding and offers **Generate Changes**. The Automation advanced section offers **Generate Pending Changes** for multiple roots. Confirm to update the generated `.cs` and serialized manifest.
- **Automatic mode:** valid removals are generated after the editor watcher detects the changed hierarchy. This may immediately trigger script compilation.
- **Delete the final control:** generate an empty bindings class; a deleted root itself has no remaining manifest to update.
- **Prefab instances:** edit the source prefab in Prefab Mode; the framework deliberately does not auto-generate overrides on scene prefab instances.
- **Project assets:** deleting a prefab *asset* is not the same as removing a component from a UI hierarchy. Edit the owning UI root if its manifest still contains a missing reference.

Advanced UI Toolkit refuses automatic updates if the generated file has been manually modified, if the destination is shared/owned by another root, if validation fails, or when a generation template is missing. It does not automatically edit application logic: `UI.Fire` will produce a compiler error if you deleted the corresponding `Fire` binding and still reference it in another script. Update those usages when confirming the removal.

## Verification

1. Create `BTN_Test`, generate its binding, save the UI root.
2. Set **Manual** mode and delete `BTN_Test`.
3. Select the original UI root in Workspace: see one pending removal.
4. Click **Preview C#** to check that the accessor is absent.
5. Click **Generate Changes**, confirm removal, and save the scene/prefab.
6. Check the generated class and serialized manifest no longer include `Test`.
7. Repeat with a root whose last control was removed, then in Automatic mode.
8. Confirm unrelated roots and generated files remain untouched.
