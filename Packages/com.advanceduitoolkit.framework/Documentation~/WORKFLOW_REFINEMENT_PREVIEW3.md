# Advanced UI Toolkit — Preview 3 Workflow Refinement

> **Historical release note:** The compact Preview 11 package does not include the synthetic benchmark UI/scripts or bundled EditMode/PlayMode tests mentioned below. Use [Unity Profiler and manual verification](PERFORMANCE_AND_TESTING.md) instead.

[← Roadmap](FOUNDATION_ROADMAP.md) · [Developer Handbook](DEVELOPER_HANDBOOK.md) · [Troubleshooting](TROUBLESHOOTING.md)

This maintenance preview refines **existing Binder and Factory workflows**. It adds no new UI controls, rendering engine, runtime API, serialized manifest members, or source-generation modes.

## Binder

1. Select a screen or panel in Unity's Hierarchy. Binder follows its owning root automatically. The Root field remains available for manual selection, but the redundant **Use Selected** button has been removed.
2. The component search filters the regular component list and the expanded **Public Properties & XML Comments** editor using the same rule. It checks the GameObject name, generated accessor, and hierarchy-derived key; use **×** or **Clear Search** to reset.
3. The compact summary distinguishes pending changes from blocking issues. Full issues remain available through **Details**, with no duplicated warning under the Generate button.
4. Switching roots clears stale generation messages and restores top-of-page browsing, so information from the previous screen is not mistaken for current status.
5. If no valid controls exist, Binder explains why Preview/Generate cannot run instead of silently disabling them.

## UI Library / Factory

1. Selecting a prefab card or list row reveals the compact instance-name editor.
2. When that same prefab is selected, **Create** on its card/list row now uses the edited instance name. Clicking **Create** on another prefab uses that other prefab's registered default, never the previous prefab's edited name.
3. If the current custom name is invalid, its Create actions are disabled and the validation reason is displayed below the input.
4. The Library's list rows fill the available dock width, and the search/filter empty state offers **Clear Filters**.
5. Opening the Library from Binder does not overwrite the manually chosen Style Profile when the target has no explicit `UIStyleContext`. With a context, the target profile is still used.

## What to verify in Unity

- Open Binder and UI Library side-by-side with the Hierarchy; choose two roots and confirm both follow selection without stale messages.
- Create a Weapon Panel from a Button prefab. Rename the selected instance `BTN_Load`, then use the card's Create button; verify the new object is actually named `BTN_Load`.
- Switch to another template and Create; verify its own default name is used. Try an invalid selected name (`BTN Load`) and confirm creation is blocked with a readable reason.
- Search `Arm`, expand XML comments, and confirm only matching entries appear. Clear the search and verify all entries return, without losing staged summary edits.
- Generate, duplicate a button (Ctrl+D), edit its name and XML summary, then preview and regenerate. Confirm the changes are detected without unsafe source writes.
- Dock each tool at 320px, 450px and 900px widths; verify there are no clipped essential controls.

The four new EditMode regression tests verify search semantics and quick-create naming/guards. They are **written but not executed** here. The package static validator does not compile Unity code or replace actual Editor tests.
