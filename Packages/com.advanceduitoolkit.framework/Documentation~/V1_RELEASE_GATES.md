# Advanced UI Toolkit 1.0 release gates — RC9

**Package:** `com.advanceduitoolkit.framework` · **Candidate version:** `1.0.0-rc.11` · **Status:** Awaiting Unity verification.

> Passing static checks or individual gameplay tests does not certify a Unity package as production-ready. Record evidence for each required gate before updating `package.json` to `1.0.0`.

## A. Clean-project installation (blocking)

- [ ] In a new Unity **2022.3 LTS** project (or the company's supported version), install the extracted package through Package Manager from disk or as an embedded package.
- [ ] Check for compile errors, missing assembly references, missing packages, duplicate menu items, and import warnings.
- [ ] Open **Tools → Advanced UI Toolkit → Binder** and **Tools → Advanced UI Toolkit → Factory**. Check the windows docked narrowly beside Inspector and expanded wide.
- [ ] Confirm four sample entries appear in Package Manager. Import the Weapon Panel example and follow its README.

## B. Main workflow (blocking)

- [ ] Create a UI root, use Factory to add three buttons and two toggles from a Style Profile; verify prefab links, names, and root selection.
- [ ] Select controls inside that root; Factory/Binder continue targeting the owning root. Select a sibling panel and independent nested root; verify selection switches correctly.
- [ ] Use **Manual** generation: add, rename, and delete controls; no generated C# file changes before pressing Generate. Preview C# must not write any files.
- [ ] Generate a `WeaponPanelBindings` class with documented properties and use it from a `UIView<WeaponPanelBindings>` script.
- [ ] Run in Play Mode; all click/toggle callbacks work. Disable and re-enable the panel repeatedly; callbacks should not duplicate.
- [ ] Edit `ExampleScript.cs` as a custom template and generate another class. Validate required markers and XML comments.

## C. Prefab and Undo safety (blocking)

- [ ] Generate bindings on a prefab in Prefab Mode and confirm linked instances remain intact.
- [ ] Confirm separate nested `UIBindingRoot` scopes do not cross-register children.
- [ ] In Automatic mode, delete a bound control. Verify accessor and manifest removal. Press **Ctrl+Z once**; verify the restored control triggers the accessor's return. Test Redo too.
- [ ] Repeat with deletion of the last bound control: an empty bindings class is valid and can later be repopulated.
- [ ] In Manual mode, Undo/Redo must update the pending-change state without automatically writing source.
- [ ] Confirm unexpected edits to generated C# block automatic overwrite and clearly present a review path.
- [ ] Confirm two roots cannot silently generate to the same owned output file.

## D. Performance / stability (blocking)

- [ ] Run the complete **EditMode and PlayMode** Test Runner suites; record the results and any ignored tests.
- [ ] Run the Binder's performance diagnostics on a representative large UI. Capture results but do not mistake them for runtime FPS benchmarks.
- [ ] Confirm there are no excessive C# regeneration cycles or ongoing Editor stalls while adding many prefabs in Manual mode.
- [ ] Build a **player executable** for the target platform to verify runtime assembly compilation.
- [ ] Confirm the same scenarios in the second Unity version your team supports, if applicable.

## E. Publication / legal (blocking for public release)

- [ ] Obtain project-owner approval to publish sources and any media. Check for proprietary assets, company IP, fonts, or sensitive details.
- [ ] Choose an explicit SPDX-recognized open-source license with the copyright owner and add its complete license text and required notices.
- [ ] Agree on GitHub owner/repository visibility and contribution policy.

## Static audit

From package root run:

```sh
python3 Tools~/verify_package.py
```

The validator checks package metadata, expected assemblies and samples, Unity `.meta` GUID uniqueness, source references, and current tool entrypoints. It **cannot** compile C# or run the Unity Editor.

## Sign-off evidence

Record the Unity Editor version, target platform, Test Runner result (counts and failures), player build status, before/after Undo screenshots, Binder/Factory screenshots, code generation output, and static audit output.

**Promotion rule:** Rename to `1.0.0` and create a Git release/tag **only after all applicable blocking checks have passed**. No automatic promotion is performed by this package.
