# Advanced UI Toolkit v0.9.10 — Simplified workflow

## The two Editor tools

- **Tools > Advanced UI Toolkit > Binder** — scan a UI root, review its named components, preview source, and generate bindings. This is the only everyday binding screen.
- **Tools > Advanced UI Toolkit > Factory** — choose a style library and create reusable uGUI prefab controls under the selected root.

The old Bindings, Automation, Diagnostics, Performance, Profile, Details, and Validation page **functions are retained** but no longer compete as top-level navigation tabs. Generation, registry, and runtime APIs are unchanged.

## First UI in four steps

1. Select a panel in the Hierarchy and open **Workspace**. It will use the selected RectTransform as the UI root, including before a `UIBindingRoot` has been added. A panel called `WeaponPanel` automatically suggests the generated class `WeaponPanelBindings`.
2. Click **Factory**, select your library, and create the buttons/toggles you need. The creation target remains on the selected UI root even when Unity selects a new child. Use names such as `BTN_Arm` and `TGL_On`.
3. Return to **Workspace**. Its status line and searchable component list show new or changed controls. Fix any displayed naming or reference errors.
4. Click **Preview C#** to inspect without writing. Click **Generate Bindings** or **Generate N Changes** when ready. Unity may import/compile the generated source at that point.

## Narrow docking

At Inspector-width docks, the Workspace has a single-column root field, compact summary, searchable component list, and an always-available Generate action. It does not require four tab labels to fit beside the Inspector.

Factory uses a component gallery by default. A selected component's details are collapsed in a narrow dock; larger docks show details on the right. Profile editing/validation are reached through **Manage**.

## Advanced settings

Click **Advanced** in Workspace only when needed:

- **Generation settings & property names** — output class, namespace, folder, ExampleScript template, rename behavior, and manual property-name overrides.
- **Automation & batch generation** — Manual or Automatic C# generation, change monitoring, and generating safe changes across several roots. Existing preferences are preserved.
- **Project diagnostics** — on-demand read-only scene/prefab validation.
- **Performance benchmarks** — opt-in synthetic measurements, not frame timings.

Click **Hide options** to return to the usual create → review → generate workflow.

## Safety and backwards compatibility

- Advanced UI Toolkit **does not change** Unity's global Auto Refresh preference.
- In Manual mode, creating controls and scanning the hierarchy does not write C# files; clicking Generate does.
- Automatically preserved API names still report a pending manifest change when the GameObject was renamed.
- Repeated screens get a suggested class name derived from their respective root (e.g. `SonarPanelBindings`) rather than recycling `ScreenBindings`.
- Generation retains the previous output-ownership, protected prefab, deleted accessor, and unexpected-edits safeguards.
- Existing generated classes, manifests, Unity script GUIDs, style profiles, event scopes, and runtime `UIView<T>` classes do not require regeneration or migration for this version.

## Manual acceptance test

1. Open Workspace docked beside the Inspector; no top-level tabs should appear.
2. Select `WeaponPanel`, create `BTN_Arm` using Factory, and return to Workspace.
3. Confirm the previous selected UI root did not jump to the new button.
4. Generate once; check `WeaponPanelBindings` exists and existing UI scripts compile.
5. Rename `BTN_Arm` to `BTN_ArmWeapon`; confirm the old C# property can be preserved while the rename is listed as a change.
6. Confirm Manual mode doesn't write generated C# until you click Generate.
7. Open Advanced and verify templates, diagnostics, automation and performance remain accessible.
8. Open Factory at 350px and 800px, browse a style library, and verify creating a component preserves its prefab connection.
9. Run EditMode and PlayMode tests and try opening and closing the panel to verify event lifecycle.

This version changes only Editor UX and test/documentation code. Unity compilation, layout inspection and runtime tests still need to be performed in a Unity project.
