# Preview 5 — UI Containers and Prefab Selection

[← README](../README.md) · [Developer Handbook](DEVELOPER_HANDBOOK.md) · [Troubleshooting](TROUBLESHOOTING.md)

## Selection policy

**Advanced UI Toolkit** distinguishes normal **UI containers** from **prefab instances**. When working in Binder and UI Library, a selectable container is a scene GameObject with a `RectTransform` that is **not part of a prefab instance**.

```text
Canvas                       selectable
└── Panel                    selectable
    ├── Header               selectable
    ├── Body                 selectable
    │   ├── Buttons parent   selectable
    │   │   ├── BTN_Load     prefab: not selectable as parent
    │   │   ├── BTN_Arm      prefab: not selectable as parent
    │   │   └── BTN_Fire     prefab: not selectable as parent
    │   └── Toggles parent   selectable
    └── Footer               selectable
```

- Selecting **Canvas**, **Panel**, **Header**, **Body**, or **Buttons parent** makes the object the current Factory creation parent.
- Selecting a **prefab instance**, or even one of its nested child GameObjects, selects the **nearest non-prefab ancestor** instead. For example, `BTN_Arm` resolves to `Buttons parent`, not to `BTN_Arm` or to the previously selected Footer.
- Selecting an unrelated non-UI object or a prefab asset in the Project window preserves the last valid scene parent.
- Binder shows the same effective container. It still uses the owning `UIBindingRoot` to scan and generate code; prefab controls remain registered and can generate `UI.Arm`, `UI.Fire`, etc.
- If no eligible scene parent exists, prefab creation is unavailable until a valid container is selected.

## Create a layout container from UI Library

1. Select a normal Canvas/Panel/container in Unity Hierarchy.
2. Open **Tools → Advanced UI Toolkit → Factory**.
3. Enter a name in the **Container** field (for example `Buttons parent`).
4. Click **+ Create**. The toolkit adds a regular `RectTransform` GameObject below the selected parent, stretches it to the parent by default, and selects it.
5. Select a button or toggle template in the Library and create instances under your new container.

These groups **do not** become new prefab assets, are not added to a Style Profile, and do not create binding accessors merely because they have a name. They support Unity Undo and remain fully editable in the Hierarchy. The **GameObject → Advanced UI Toolkit → RectTransform (Empty)** menu offers the same ordinary container creation.

## Compatibility and scope

This is an Editor authoring rule, not a restriction on Unity's Hierarchy selection or prefab editing tools. You can still select any object in the Unity Hierarchy itself; the Binder/Library parent context deliberately resolves to a container. It does not remove prefabs from the binding scanner or change `UIBindingRoot` serialized data.

Test scene prefab instances and nested prefabs in your Unity project. This package has not been compiled or tested in Unity by the automated static audit.
