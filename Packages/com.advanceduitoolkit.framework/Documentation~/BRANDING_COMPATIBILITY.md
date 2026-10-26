# Advanced UI Toolkit — branding and compatibility

This release changes the public product name from **Advanced UI Toolkit** to **Advanced UI Toolkit**.

## Updated public names

- Unity Package Manager: **Advanced UI Toolkit**
- `Tools > Advanced UI Toolkit > Binder`
- `Tools > Advanced UI Toolkit > Factory`
- `GameObject > Advanced UI Toolkit > ...`
- `Assets > Create > Advanced UI Toolkit > Style Profile`
- Editor tooltips, validation messages and documentation

The docked tool names remain **Binder** and **UI Library** for a compact Editor layout.

## Names intentionally preserved

This is a backward-compatible brand refresh, **not** a breaking API migration.
To avoid breaking saved prefabs, generated classes and user scripts, the following
identifiers remain unchanged:

- `using AdvancedUIToolkit;` and `namespace AdvancedUIToolkit.Editor`
- `AdvancedUIToolkit.Runtime`, `AdvancedUIToolkit.Editor` and test assembly names
- `com.advanceduitoolkit.framework` (Unity package identifier and embedded folder name)
- Existing `.meta` file GUIDs and serialization keys
- Existing `<ADVANCEDUITOOLKIT:...>` markers used by code-generation templates
- Existing `EditorPrefs` keys that store generation preferences

Do not manually rename the namespace, assembly definitions or package folder
when installing this release.

## Generated assets and existing projects

For **new** UI roots and libraries, default locations now use:

- `Assets/AdvancedUIToolkit/Generated`
- `Assets/AdvancedUIToolkit/Libraries`

Previously generated files and prefab libraries under `Assets/AdvancedUIToolkit/` remain
valid and are neither moved nor deleted. Existing configured UI roots keep
their saved output path. Avoid moving or renaming those assets without also
updating each root's saved generation settings.

## Upgrade

Back up your project. With Unity closed, apply the upgrade patch over
`Packages/com.advanceduitoolkit.framework/`, preserving existing metadata. Reopen Unity,
check the new menu paths, and rerun Binder/Factory and PlayMode tests.

This package is a release candidate; it has passed static package checks but
still requires Unity compilation and project-specific testing before a stable
release can be declared.
