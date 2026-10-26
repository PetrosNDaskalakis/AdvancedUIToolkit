# Advanced UI Toolkit RC5: compact authoring workspace

The **Binder** and **UI Library** remain separate dockable Editor windows. This release changes only the Editor user interface.

## Binder

1. Select a UI root.
2. Review the compact component/change/error summary.
3. Expand **Details** only to read the full warning list.
4. Inspect at most seven visible component rows before scrolling locally.
5. Preview C# or **Generate Changes** directly underneath the list.
6. Use **Show More** for generation templates, automation, diagnostics, and benchmarks.

## UI Library

1. Choose a UI root and a library from the top of the window.
2. Select a prefab from the full-width list or adaptive tile grid.
3. Enter the instance name and click **Create** in the compact selected-component box.
4. Expand **Preview & prefab actions** for visual states and asset links.
5. Use **Show More** for profile editing, inheritance options, validation, and other uncommon actions.

## Unity verification checklist

- Dock each window at approximately 340 px, 550 px, and 900 px and confirm no important controls are clipped.
- Try zero, two, and fifty components; the Binder actions should remain close to the list.
- Remove a registered component: a concise warning should be visible; **Details** should expose the full messages.
- Select a library prefab; its selected details should be compact and the component list should use the available width.
- Switch between List and Tiles; thumbnail creation and linking should still work.
- Use Show More to edit or validate a profile, then collapse it without affecting components.
- Verify manual generation queues changes, and generated code remains identical to RC4 for the same inputs.
- Run existing EditMode/PlayMode tests and a Unity player build before considering a stable release.

## Compatibility

The upgrade changes only two Editor C# files and UPM metadata/documentation. It retains the original `.meta` files for both Editor scripts. No Runtime changes or binding regeneration are required.
