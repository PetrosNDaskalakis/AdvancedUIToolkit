# Advanced UI Toolkit v0.9.9 — Release candidate verification

**Release status: CANDIDATE, not a production-certified release.** This package has static packaging checks and Unity test source, but Unity Editor compilation, interactive UX testing and player builds must be performed in the target project.

## Scope and compatibility

- Does **not** modify Runtime or Editor C# functionality from v0.9.8.
- No change to `UIBindingRoot` serialized fields, stable IDs, generated binding contracts, lifecycle hooks, prefab libraries, generation settings, or menu destinations.
- Adds EditMode release-candidate tests, a Python standard-library package audit, and final-release documentation.
- New Editor UI features are intentionally deferred to **v0.9.10 UX Simplification**.
- Existing generated bindings, settings and profiles do **not** need regeneration.

## Static checks (outside Unity)

Run from the package root:

```sh
python3 Tools~/verify_package.py
```

It checks package metadata, the declared uGUI dependency, assembly definitions, sample descriptors, critical files, duplicate `.meta` GUIDs, and obvious game-assembly imports. **It does not compile C#, prove Unity compatibility, inspect proprietary asset ownership, or run runtime tests.**

## Clean-project release gate

1. Install this package into a fresh Unity 2022.3 LTS-or-later test project with uGUI installed.
2. Verify the package is recognized as `com.advanceduitoolkit.framework` **0.9.9**.
3. Verify `Tools > Advanced UI Toolkit > Binder` and `Tools > Advanced UI Toolkit > Factory` open without console errors.
4. Choose **Manual** generation; create a Canvas with `BTN_Arm`, `BTN_Fire`, `BTN_Load`.
5. Scan and generate `WeaponPanelBindings`, then compile a `UIView<WeaponPanelBindings>` consumer.
6. Press Play; test button clicks, toggle/slider callbacks, disabling/enabling the root, and pooled reactivation.
7. Create a parent `UIBindingRoot` and a nested `UIBindingRoot`; confirm the parent does not register nested controls.
8. Open a source prefab in Prefab Mode; verify safe generation and error handling for instances.
9. Test renamed GameObjects with API preservation; validate intentional API-renaming warnings.
10. Test Factory profile inheritance and create prefabs with expected naming conventions.
11. Confirm generated C# preview does **not** write files; batch changes and invoke **Generate Pending Changes** manually.
12. Run all package EditMode tests, including `AdvancedUIToolkitReleaseCandidateTests`, and all PlayMode tests.
13. Build a player and verify the Editor assembly is excluded.
14. Verify no missing scripts or orphaned references after upgrading an existing Advanced UI Toolkit project.

**Tests discovery:** If the embedded package's tests are not listed in Unity Test Runner, add `"testables": ["com.advanceduitoolkit.framework"]` to your project `Packages/manifest.json`, preserving any existing entries.

## Evidence to record

| Check | Result | Notes |
| --- | --- | --- |
| Unity version & OS | Not run | |
| UPM clean install | Not run | |
| Console compilation | Not run | |
| WeaponPanel interactions | Not run | |
| Factory + Style Profiles | Not run | |
| Prefab isolation + renames | Not run | |
| Manual generation + preview | Not run | |
| EditMode test count | Not run | |
| PlayMode test count | Not run | |
| Player build | Not run | |
| Migration regression | Not run | |
| Open-source permissions | Pending owner review | |

## Exit criteria for v1.0

No blocking compile errors, no missing references, no regressions in events or generated APIs, passing test suites, clean player build, and a completed owner-approved publication checklist. The proposed v0.9.10 UX simplification must also pass a separate usability review before v1.0.
