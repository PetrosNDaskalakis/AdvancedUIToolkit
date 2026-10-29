# Advanced UI Toolkit: team onboarding checklist

## First 30 minutes

- [ ] Install the Advanced UI Toolkit UPM package in a clean **test** Unity project.
- [ ] Open **Tools > Advanced UI Toolkit > Binder** and **Tools > Advanced UI Toolkit > Factory**.
- [ ] Set **Binder > Show More > Automation & batch generation > Manual** to avoid repeated generated C# imports.
- [ ] Follow [Getting started](GETTING_STARTED.md) and build a three-button Weapon Panel.
- [ ] Confirm `UI.Arm`, `UI.Fire`, `UI.Load` compile and receive one callback each.
- [ ] Disable and re-enable the root; callbacks should suspend/restore exactly once.
- [ ] Import the **Settings Panel — Controls** sample and test its event types.
- [ ] Run Diagnostics and correct any missing/duplicate binding entries.

## Code-review expectations

- Generated bindings are **generated**. Review them; do not manually edit their members.
- New fields and methods in game scripts follow the team's XML-comment requirement.
- New style prefabs belong in the game's Assets folder; core framework sources do not embed game-specific art or logic.
- A rename that changes a **public generated property** is treated as an API change.
- Manually verify reference lookup, listener lifecycle, prefab hierarchy/layout and Editor UI using the checklist.

## Before moving this package into production

- [ ] Confirm no missing scripts in existing scenes/prefabs after GUID-safe migration.
- [ ] Complete the [manual verification checklist](TEST_PLAN.md) in a clean project.
- [ ] Build the game to verify Editor scripts are excluded.
- [ ] Record Unity version, render pipeline, operating system, manual verification results, and profiler captures.
- [ ] Review license/IP before publishing any company-related source or assets publicly.
