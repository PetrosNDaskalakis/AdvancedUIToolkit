# Advanced UI Toolkit v1.0 Release Verification

[← Documentation Index](INDEX.md) · [Installation](INSTALLATION.md)

This is a **release candidate**, not a Unity-verified stable build. Before a v1.0 stable tag:

- [ ] Install the package in a clean supported Unity project with `com.unity.ugui`.
- [ ] Verify `Tools → Advanced UI Toolkit → Binder` and `Tools → Advanced UI Toolkit → Factory`.
- [ ] Build a UI root with button, toggle and slider; preview and generate bindings.
- [ ] Compile a gameplay script using `UIView<TBindings>` and managed `Events`.
- [ ] Create a prefab from the UI Library and verify naming and profile inheritance.
- [ ] In Automatic mode delete a bound prefab, Undo, and Redo; verify binding regeneration.
- [ ] Confirm Manual mode queues changes until generation is explicitly requested.
- [ ] Test nested `UIBindingRoot` boundaries and multiple independent roots.
- [ ] Verify custom templates use `ADVANCEDUITOOLKIT` insertion markers.
- [ ] Verify authoring workflows in Edit Mode and lifecycle behavior in Play Mode without errors.
- [ ] Build a player and confirm the Editor assembly is excluded.
- [ ] Test migration from an existing project with `.meta` GUIDs preserved.
- [ ] Confirm company approval and release license before any public GitHub publication.

`Tools~/verify_package.py` validates static structure and references, **not** Unity C# compilation or runtime behavior.
