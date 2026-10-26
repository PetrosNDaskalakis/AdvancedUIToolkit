# Style profiles & reusable station libraries

A style profile selects **which prefab** the Advanced UI Toolkit Factory uses for a control. It does not change the public component-reference API. Screens can look different while both expose `UI.Fire`, `UI.Volume`, etc.

## Example hierarchy

```text
BaseStation (UIStyleProfile)
├── SonarStation (inherits BaseStation)
├── WeaponsStation (inherits BaseStation)
└── NavigationStation (inherits BaseStation)

MainMenu (independent UIStyleProfile)
```

`BaseStation` might define a shared Toggle and Panel prefab. `SonarStation` can override only Button. Its effective set uses its own Button and inherits the rest.

## Build your first profile

1. Build and save **real prefab assets** for your controls (e.g. `SonarButton.prefab`) under your game's `Assets/...` folder. Use Unity's Prefab workflow and normal uGUI components.
2. Create a profile with **Assets > Create > Advanced UI Toolkit > Style Profile**. Name it `SonarStationProfile`.
3. Open **Tools > Advanced UI Toolkit > Factory** and register your prefabs in the profile's component templates.
4. Assign `SonarStationProfile` to a `UIStyleContext` on your Sonar station root.
5. Select a `RectTransform` under that root and create a Button using the Factory or **GameObject > Advanced UI Toolkit > Button**.
6. Verify the created control is a **linked prefab instance** and its name follows `BTN_...`.
7. Change the prefab asset and verify expected Unity prefab overrides/propagation.

## Parent profile reuse

Create `BaseStationProfile`, add shared templates, then assign it as `BaseProfile` on `SonarStationProfile`. Derived template identities override base templates; custom templates are keyed by a custom ID, standard templates by component kind. Avoid inheritance cycles. The Editor should flag invalid profile configurations; test inherited results with the Factory.

## What stays separate

- **Advanced UI Toolkit package:** framework source and built-in tooling, shareable across projects.
- **Project Assets:** game-specific prefabs, styles, generated code, scene instances.
- **Binding manifests:** serialized with `UIBindingRoot` on each screen or prefab root.

You can reuse the **same prefab** in several profiles. Its instance GameObject name determines the generated accessor; the prefab asset name is only its design asset identity.

## Known limitations

- No runtime stylesheet or per-state reactive theming engine is included.
- Prefab variants and custom shaders are subject to normal Unity behavior; thumbnail previews are best-effort.
- A generated binding is scoped to the selected screen/root, not shared globally across every prefab instance.

For a step-by-step exercise import **Station Style Libraries** via Unity Package Manager. That sample is documentation-only and does not install station artwork or prefab assets.

## Composite prefab templates

You can register a prefab with a `RectTransform` container at its root and the role's actual component on a child. For example, an `Image` profile entry can use `IMG_Icon` as the prefab instance name even when its only `Image` lives under `Graphic`. The Factory preserves the prefab link; Binder resolves a unique matching child and produces a typed `Image` accessor.

If more than one matching component is present, Binder will ask you to clarify which one is intended. It does not silently choose the first child. The prefab must still contain a component matching its role somewhere in the hierarchy, and a nested independent `UIBindingRoot` is excluded from the parent's binding scope.
