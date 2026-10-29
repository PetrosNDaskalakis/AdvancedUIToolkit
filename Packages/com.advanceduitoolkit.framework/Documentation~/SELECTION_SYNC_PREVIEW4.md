# Preview 4 — Hierarchy selection synchronization

[← README](../README.md) · [Developer Handbook](DEVELOPER_HANDBOOK.md) · [Foundation Roadmap](FOUNDATION_ROADMAP.md)

## What changed

Both Advanced UI Toolkit windows now follow ordinary UI GameObject selection, not only named panels or objects with `UIBindingRoot`.

### UI Library / Factory

The **Parent** field is the exact `RectTransform` you most recently selected in the Unity Hierarchy. Selecting `WeaponPanel/ControlsGroup/BTN_Fire` makes `BTN_Fire` the parent of the next prefab you create. Selecting the sibling `ControlsGroup` makes that group the parent instead. This works with arbitrarily named UI containers.

A non-UI scene object or asset selected in the Project window cannot be used as a uGUI creation parent, so the previous valid parent is retained. When Factory itself creates a prefab, Unity may select that new instance; this particular selection is ignored so repeated **Create** clicks produce siblings instead of nesting each new prefab inside the previous one.

### Binder

Binder shows two distinct fields:

- **Selected:** the actual GameObject in Unity's Hierarchy, including ordinary controls and child containers.
- **Binding Root:** the UI scope whose existing manifest and generated C# class are being inspected. A normal child remains in its owning root; an independently nested `UIBindingRoot` switches scope.

The selected component is emphasized within the scanned component list. A prefab wrapper with a matching control on an unambiguous child is also highlighted. If you deliberately want to create a separate binding scope from a child container, choose it explicitly in **Binding Root** and use the usual generation workflow.

### Cross-window navigation

- Binder → Factory opens at the selected UI object (or falls back to its binding root).
- Factory → Binder opens the owning binding root of the current parent.

## Acceptance checks in Unity

1. Select `WeaponPanel`, then an ordinary child `ControlsGroup` with a `RectTransform`. Factory's **Parent** becomes `ControlsGroup` automatically.
2. Select `BTN_Load`; Factory's **Parent** becomes `BTN_Load`, even though the object is not a binding root.
3. Create a new prefab in Factory and confirm it is a child of `BTN_Load`. Create again and confirm the next prefab is a sibling of the first created instance.
4. Select `BTN_Fire` in the Hierarchy and confirm Binder's **Selected** shows `BTN_Fire` while **Binding Root** remains `WeaponPanel`.
5. Verify Binder highlights the selected control, previews/generates the correct screen class, and does not silently create a separate manifest on `BTN_Fire`.
6. Check a nested independently bound prefab and switch back to its parent. Confirm each scope remains independent.
7. Select a non-UI scene GameObject or a prefab asset in the Project window. Factory should retain its previous valid UI parent.
8. Run EditMode tests and verify a player build.

This update does **not** change generated code formats, binding serialization, or Runtime APIs. Visual and functional verification must occur in the Unity Editor; static audits alone are not sufficient.
