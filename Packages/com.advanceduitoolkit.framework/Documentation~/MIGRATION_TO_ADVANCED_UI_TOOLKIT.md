# Migrating to Advanced UI Toolkit

[← Documentation Index](INDEX.md) · [Installation](INSTALLATION.md) · [Troubleshooting](TROUBLESHOOTING.md)

**Important:** Version `1.0.0-rc.11` changes the Unity package identifier, namespace, assembly names, Editor class names, and code-template markers. This is **not a drop-in upgrade** for projects already using an earlier package release. It is appropriate for a new repository or a deliberate migration.

## Migration checklist

1. **Commit or back up** your project and copy your generated binding files.
2. Remove the previous package reference from `Packages/manifest.json` and install `com.advanceduitoolkit.framework` instead. Do **not** keep both packages installed; duplicate component scripts will conflict.
3. If using an embedded package, move the package directory to `Packages/com.advanceduitoolkit.framework/`; keep the paired `.meta` files. In this source archive, script and assembly-definition `.meta` GUIDs are preserved across file renames.
4. Replace former runtime namespace imports with `using AdvancedUIToolkit;` and update custom generated binding scripts that refer to the former namespace.
5. Update project `.asmdef` references to `AdvancedUIToolkit.Runtime`. Tests that depend on internal Editor utilities must use `AdvancedUIToolkit.Editor`.
6. If using custom C# code-generation templates, update both insertion markers to `// <ADVANCEDUITOOLKIT:PROPERTIES>` and `// <ADVANCEDUITOOLKIT:ASSIGNMENTS>`; optional property documentation uses `// <ADVANCEDUITOOLKIT:PROPERTY_DOC>`.
7. Reopen Unity, fix the first Console error, regenerate bindings in **Tools → Advanced UI Toolkit → Binder**, and verify screen scripts compile.
8. Reassign any lost assembly references or Editor preferences; changing preference-key names may reset previous local defaults.
9. Run the EditMode and PlayMode tests and make a player build before replacing a production dependency.

The package still targets native Unity uGUI. Only the package identity and source symbols have been renamed; serialized `UIBindingRoot` manifests and `UIStyleProfile` structures are otherwise intended to remain compatible. **Unity migration testing has not been performed here.**
