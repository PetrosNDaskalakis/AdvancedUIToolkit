# Advanced UI Toolkit RC7 — UI Library selection and previews

The UI Library follows the Unity Hierarchy selection automatically. Select a different panel, screen, or independently marked UI root to make it the current creation target; no **Use Selected** click is needed.

Selecting controls inside the current root does not redirect creation. If a selected nested object has its own `UIBindingRoot` or `UIStyleContext`, it becomes the active root. Selecting a clearly named panel (`Panel`, `Screen`, `PNL_*`, `*Panel`, or `*Screen`) also treats it as an intentional creation container.

Prefabs created through the Factory do not change the active root. The Root field remains available for explicit drag-and-drop overrides.

UI Library previews now show only the normal prefab appearance. The old Visual State selector and non-normal-state rendering have been removed; prefabs retain their own Unity Selectable transitions at runtime.

## Check in Unity

1. Open UI Library and select `WeaponsPanel`; confirm the Root field follows without clicking a button.
2. Select a button inside the same panel; confirm Root remains `WeaponsPanel`.
3. Select `SonarPanel`; confirm Root changes automatically.
4. Instantiate a Button; confirm its prefab is created under the current root and the Root field stays put.
5. Select a nested root with `UIBindingRoot` or `UIStyleContext`; confirm it becomes the target.
6. Select a component prefab; confirm the preview has no Visual State picker.
7. Run the `UIComponentLibrarySelectionTests` EditMode test class.
