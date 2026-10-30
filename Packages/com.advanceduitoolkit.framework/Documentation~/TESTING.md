# Framework testing

[Documentation index](INDEX.md) · [Troubleshooting](TROUBLESHOOTING.md)

## Static package audit

From the package directory, run `python3 Tools~/verify_package.py`. Python's standard library is sufficient. The audit checks package metadata, required documentation, samples, assembly boundaries, source metadata GUIDs and current menu/README contracts.

The audit does not compile C#, run Unity or build a player. Framework's compact package contains no bundled Unity test assembly or synthetic benchmark suite.

## Unity verification

Use a disposable project or scene with your intended Unity version and render pipeline. Record package versions and results.

1. Import the package cleanly and resolve compilation errors. Confirm Library and Binder menus.
2. Register a prefab profile, including inherited/custom entries. Check search, kind filters, Show Inherited, empty-state recovery and list/tile state after domain reload.
3. Test follow-selection and pinned targets, Locate, invalid prefab/non-UI targets and compilation/import/Play Mode creation guards.
4. Create linked controls and confirm names, text overrides, source prefab preservation and Undo/Redo.
5. Discover controls on a binding root, including inactive/nested components and an independent child binding root. Confirm ambiguous/duplicate names are reported.
6. Preview and generate a configured binding class. Exercise rename, duplicate, deletion, missing references, output collision and edited-generated-file handling.
7. Repeat safe change detection in Manual and Automatic modes. Confirm Manual mode does not write until requested.
8. Run a view through activation, deactivation, component disable, reactivation and destruction; check one managed callback per event and no stale listeners.
9. Save/reopen scenes and prefabs. Build and run a representative player, confirming Editor assemblies are excluded.

For public Editor APIs, run the usage checks in the creation, batch, Binder and component-extensibility references. Measure scan/generation time and allocations on representative screens if performance is a release criterion.

## Verification status

This source distribution supplies static checks and documented manual procedures. It does not include a Unity CI result, player-build report or certified compatibility matrix. Record actual Unity results separately; do not interpret the preview package version or a static audit pass as production certification.
