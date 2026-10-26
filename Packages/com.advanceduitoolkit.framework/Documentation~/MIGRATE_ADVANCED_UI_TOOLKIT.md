# Advanced UI Toolkit v0.9.1 — migration for your AdvancedUIToolkit project

This package was prepared specifically from the `Assets` archive you uploaded. Every existing Runtime, Editor, and EditMode Test C# script has the **same source content** as v0.9.1, so the code is not being replaced. Their original `.cs.meta` GUIDs have been copied into this package to preserve Unity references.

## Do this with Unity closed

1. Make a complete backup of the project. You uploaded only `Assets`, not `Packages` or `ProjectSettings`, so the whole-project migration has not been tested.
2. Extract the zip at the **Unity project root**, next to `Assets/`, `Packages/` and `ProjectSettings/`. The included package should be at `Packages/com.advanceduitoolkit.framework/`, NOT `Assets/Packages/`.
3. Once extraction completes, **before opening Unity**, remove these now-duplicate framework folders from `Assets/`: `Runtime/`, `Editor/`, `Tests/`, `Samples/`, and `Documentation/`. They are part of Advanced UI Toolkit in the uploaded archive. If you've added unrelated files to these folders since uploading, move those unrelated files elsewhere before deleting. Remove each matching folder `.meta` file too (if present).
4. **Do not delete or move** `Assets/AdvancedUIToolkit/Generated/`, `Assets/AdvancedUIToolkit/Libraries/`, `Assets/Prefabs/`, `Assets/Scenes/`, `Assets/Scripts/` or `Assets/Settings/`.
5. Open Unity; allow assembly compilation. Look for Missing Script components and errors in the Console. Unity should discover the embedded UPM package from the `Packages/` directory.
6. Open `Tools > Advanced UI Toolkit > Binder` and `Tools > Advanced UI Toolkit > Factory`, scan and validate your existing weapon panel. Regenerate its binding class only after verifying it uses your saved project output path `Assets/AdvancedUIToolkit/Generated`.
7. Play your existing `WeaponPanel` test and verify button clicks and saved style-profile prefab links. Run `Tools > Advanced UI Toolkit > Binder > Show More > Project diagnostics` and Automation verification.
8. If it fails, revert the backup; do not mix and match old and new script copies.

## What stays in Assets

```text
Assets/
├── AdvancedUIToolkit/
│   ├── Generated/        # ScreenBindings.cs - project specific
│   └── Libraries/        # WeaponControl.asset - project specific
├── Prefabs/              # your test button/image prefabs
├── Scenes/               # your test scene
├── Scripts/              # WeaponPanel.cs and game logic
└── Settings/             # render pipeline and project assets
```

## What moves into Packages

```text
Packages/
└── com.advanceduitoolkit.framework/
    ├── package.json
    ├── Runtime/           # core runtime scripts, GUIDs preserved
    ├── Editor/            # core editor scripts, GUIDs preserved
    ├── Tests/             # editmode tests, GUIDs preserved
    ├── Samples~/          # package examples (Unity does not auto-import)
    ├── Documentation~/    # package documentation
    └── README.md
```

## Notes

- Unity `.cs.meta` GUIDs from the uploaded archive were kept for all 6 Runtime, 11 Editor, and 3 Editor test scripts. Existing Unity Editor script references should therefore continue to point to the same script assets after migration.
- The new UPM `asmdef` files mean that core scripts now compile into `AdvancedUIToolkit.Runtime` and `AdvancedUIToolkit.Editor` rather than the project's `Assembly-CSharp` assemblies. Your `Assets/Scripts/TestingScripts/WeaponPanel.cs` remains outside an assembly definition and can normally reference `AdvancedUIToolkit.Runtime`. Add an explicit `AdvancedUIToolkit.Runtime` reference if you later create a separate gameplay asmdef.
- `Assets/AdvancedUIToolkit/Libraries/WeaponControl.asset` refers to `UIStyleProfile` via its old script GUID. That GUID is retained by this package.
- Package code still needs Unity compilation and runtime validation. Static checks do not guarantee scenes with serialized MonoBehaviours or ScriptableObjects will behave identically.
- Do not publish this repository until you have reviewed ownership, public license terms, and all code/assets for confidential material.
