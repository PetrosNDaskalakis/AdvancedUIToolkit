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

| Profile | BaseProfile |
| --- | --- |
| MainMenuProfile | None |
| BaseStationProfile | None |
| SonarStationProfile | BaseStationProfile |
| WeaponsStationProfile | BaseStationProfile |

Open **Tools > Advanced UI Toolkit > Library** and register component prefab templates per profile:

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

Rename one sonar button to `BTN_TrackTarget` and one weapons button to `BTN_Fire`. Use a `UIBindingRoot` on each screen, then Tools > Advanced UI Toolkit > Binder to generate **separate** classes. Both styles use standard Unity Button references.

## Step 5: Validate

Check prefab links, inherited templates, unique names and generated reference ownership. Library appearance is independent from the API consumed by gameplay scripts.

Style libraries do not inject game logic or provide runtime CSS-like styling. The optional Authoring package provides Layouts and JSON compatibility tools alongside the Framework library.
