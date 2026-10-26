# Station Style Libraries — guided exercise

This is a **documentation-only** importable sample. No artwork or prefabs are included: your team owns its own project UI assets. The goal is to create three independent styles using one shared framework.

## Step 1: Create prefabs

In your game project's `Assets/UI/Prefabs`, create and save:

- `BaseStationToggle.prefab` — common station toggle.
- `BaseStationPanel.prefab` — shared panel background.
- `SonarButton.prefab` — compact green sonar button.
- `WeaponsButton.prefab` — an alternative button style.
- `MenuButton.prefab` — main menu design.

Each must contain the expected Unity uGUI component on its root GameObject (e.g. `Button` for button templates).

## Step 2: Build profiles

Use **Assets > Create > Advanced UI Toolkit > Style Profile**, creating:

```text
MainMenuProfile  (no parent)
BaseStationProfile
├── SonarStationProfile   (BaseProfile = BaseStationProfile)
└── WeaponsStationProfile (BaseProfile = BaseStationProfile)
```

Open **Tools > Advanced UI Toolkit > Factory** and register component prefab templates per profile:

| Profile | Button | Toggle | Panel |
| --- | --- | --- | --- |
| BaseStationProfile | — | BaseStationToggle | BaseStationPanel |
| SonarStationProfile | SonarButton | inherits | inherits |
| WeaponsStationProfile | WeaponsButton | inherits | inherits |
| MainMenuProfile | MenuButton | as needed | as needed |

## Step 3: Attach style context

Create `SonarScreen` and `WeaponsScreen` under a Canvas. Add `UIStyleContext` to each, referencing its style profile. Select a RectTransform inside each and use **GameObject > Advanced UI Toolkit > Button**.

Expected result: the Sonar screen creates linked SonarButton instances while Weapons creates linked WeaponsButton instances, with valid auto-generated `BTN_...` names. A Toggle under either screen resolves the shared BaseStationToggle.

## Step 4: Bind by role, not visual style

Rename one sonar button to `BTN_TrackTarget` and one weapons button to `BTN_Fire`. Use a `UIBindingRoot` on each screen, then Workspace > Bindings to generate **separate** classes. Both styles use standard Unity Button references.

## Step 5: Validate

Check prefab links, inherited templates, unique names and generated reference ownership. Factory appearance is independent from the API consumed by gameplay scripts.

**Important:** The style libraries do not inject game logic, and Advanced UI Toolkit does not yet support JSON schema generation or runtime CSS-like styling.
