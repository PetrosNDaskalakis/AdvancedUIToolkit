# Migrating a Unity project to Advanced UI Toolkit

[← Documentation Index](INDEX.md) · [Installation](INSTALLATION.md) · [Troubleshooting](TROUBLESHOOTING.md)

**Version 1.0.0-rc.12 changes C# identifiers and the UPM package identity. This is a breaking migration.** A rename of the package folder alone is not sufficient.

## Recommended path

1. Back up the **entire Unity project** or commit it to version control. Close Unity.
2. Use the **AdvancedUIToolkit_ProjectMigration.py** utility from the companion migration archive, initially without `--apply` to inspect proposed changes. The tool is distributed separately from the runtime package.
3. With `--apply`, the utility backs up all files it will modify. It updates project `Assets` scripts (including your own gameplay scripts), folders, `.asmdef` references, generated C#, custom template markers, and project package references. It preserves `.meta` GUID content and renames paired metadata files with their sources.
4. Keep exactly **one** toolkit package installed at `Packages/com.advanceduitoolkit.framework` or via UPM. Avoid importing a duplicate under `Assets`.
5. Open Unity and fix the first compiler error; older script references may also require manually updating third-party packages, C# reflection strings, Editor preferences, or external build scripts not included in the project.
6. In **Tools → Advanced UI Toolkit → Binder**, inspect each `UIBindingRoot`, preview code changes, and regenerate bindings if needed. Confirm the generator output path still points to your intended Assets folder.
7. Complete the manual Unity workflow checklist and a player build before using this in a production project.

## New technical identity

| Item | Value |
| --- | --- |
| Unity Package Manager ID | `com.advanceduitoolkit.framework` |
| C# runtime namespace | `AdvancedUIToolkit` |
| Editor namespace | `AdvancedUIToolkit.Editor` |
| Runtime assembly | `AdvancedUIToolkit.Runtime` |
| Editor assembly | `AdvancedUIToolkit.Editor` |
| Custom C# template marker | `// <ADVANCEDUITOOLKIT:PROPERTIES>` |
| Default generated folder | `Assets/AdvancedUIToolkit/Generated` |

Renamed package files retain their original Unity `.meta` GUID values so scene and prefab component links can remain intact. GUID preservation alone does not guarantee that all C# code, serialized reflection names, or external integrations will migrate automatically.

**Scope:** The compact toolkit archive includes Runtime, Editor, documentation, and samples (no bundled tests or benchmarks). Your separate game's `Assets` were not included in that archive; migrate them locally with the supplied tool, or upload the complete project for a direct conversion.
