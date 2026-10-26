# Installation Guide

[← README](../README.md) · [Documentation Index](INDEX.md) · [Troubleshooting](TROUBLESHOOTING.md)

## Requirements

- **Unity 2022.3 or later** (declared minimum; verify compatibility with the exact Unity release your team uses).
- Unity's **uGUI** package (`com.unity.ugui`), declared as a package dependency.
- Unity Test Framework if you want to run the included EditMode and PlayMode tests.
- The Advanced UI Toolkit package folder containing `package.json`, `Runtime/`, and `Editor/`.

The public product name is **Advanced UI Toolkit**. For backward compatibility, the package identifier is **`com.uicore.framework`** and the C# namespace remains **`UICore`**.

## Option A — Install from disk (recommended for first-time testing)

1. Download or extract the package folder from your approved source.
2. Confirm that its **root** contains `package.json`.
3. In Unity, open **Window → Package Manager**.
4. Choose **+ → Add package from disk...**.
5. Select `package.json` inside the package folder.
6. Wait for the Editor to import and compile the package.
7. Confirm **Tools → Advanced UI Toolkit → Binder** and **Tools → Advanced UI Toolkit → Factory** are available.

Do not place the entire extracted package under `Assets/`; this release is distributed as a Unity Package Manager package.

## Option B — Embedded package for local development

Close Unity, then place the folder at:

```text
MyUnityProject/
├── Assets/
├── Packages/
│   ├── manifest.json
│   └── com.uicore.framework/
│       ├── package.json
│       ├── Runtime/
│       ├── Editor/
│       ├── Tests/
│       ├── Samples~/
│       └── Documentation~/
└── ProjectSettings/
```

Reopen Unity. The embedded package can be edited locally, but version control should track those modifications explicitly. Avoid keeping duplicate `UICore` runtime/editor scripts in `Assets/` at the same time.

## Option C — Git installation (once a repository is published)

After your team approves a repository and tags a package release, use **Package Manager → + → Add package from git URL...** with its real repository URL. For example, a *format illustration*, not a published link:

```text
https://github.com/OWNER/REPOSITORY.git#v1.0.0
```

Do not copy this placeholder as a real package URL. If the UPM package lives in a repository subfolder, use the supported `?path=` Git URL syntax for your Unity version.

## Verify the installation

1. Create an empty Canvas and a Panel in a test scene.
2. Add `UIBindingRoot` to the Panel.
3. Create a **Button** through **GameObject → Advanced UI Toolkit → Button**.
4. Rename its root GameObject `BTN_Test`.
5. Open **Binder**, select the Panel, and verify `Test` is discovered.
6. Preview and generate its binding class; check that the generated property exists.
7. Run a PlayMode test with a `UIView<TBindings>` component on the same GameObject.

**Expected result:** no compiler errors, a valid generated binding reference, and working UI click events.

## Importing samples

Open **Window → Package Manager**, select Advanced UI Toolkit, and look for **Samples**. Available examples include Weapon Panel, Settings Panel, Station Style Libraries, and Code Generation Template. Some samples are guides and scripts rather than complete scenes or artwork.

## Upgrade an existing project

1. **Back up or commit the project first.**
2. Close Unity.
3. Replace or patch files within `Packages/com.uicore.framework/` using the appropriate version's package or upgrade patch.
4. **Preserve existing `.meta` GUIDs** for C# scripts referenced by your scenes and prefabs.
5. Keep generated classes and project-specific UI prefabs/Style Profiles under `Assets/`.
6. Reopen Unity, check Console errors, and verify Binder/Factory and a representative panel.
7. Run EditMode tests, PlayMode tests, and a player build before promoting a release to production.

> **Migrating from an old `Assets/UICore` implementation?** Do not delete the old Runtime/Editor scripts and import new scripts with different `.meta` GUIDs. Migrate by preserving the original GUID-bearing files and removing duplicates only after backup and verification.

## Generated files and project folders

Default locations for **new** assets:

```text
Assets/AdvancedUIToolkit/
├── Generated/  # generated C# binding classes
└── Libraries/  # game-specific style profile assets
```

Existing paths, including `Assets/UICore/`, remain valid if already saved in binding generation profiles. Advanced UI Toolkit doesn't automatically move or delete those assets.

## Tests and sign-off

- Open **Window → General → Test Runner** for EditMode/PlayMode tests.
- If package tests aren't discovered, add `"testables": ["com.uicore.framework"]` to your project `Packages/manifest.json` while preserving its other entries.
- From package root, run `python3 Tools~/verify_package.py` to perform a static structural audit (this does not compile Unity C#).
- Check a player build for your target platform and the exact Unity version your team supports.

**Release note:** `1.0.0-rc.10` is a release candidate. It has not been certified production-ready by the package metadata alone.
