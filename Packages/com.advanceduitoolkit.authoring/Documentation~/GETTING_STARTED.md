# Layouts getting started

[Documentation index](INDEX.md) · [Developer handbook](DEVELOPER_HANDBOOK.md)

## Prepare a source

In a loaded scene, create an ordinary panel with ordinary child RectTransforms such as Header, Body, Buttons and Footer. Add Layout Groups or Layout Elements where needed. The source root must be outside prefab instances and Prefab Mode.

You can capture a panel that already contains linked prefab controls. Those entire prefab branches are omitted, leaving the eligible parent containers. A manually created Button is an ordinary scene object and is retained if otherwise eligible; capture does not classify controls by component type. See [Capture rules](HIERARCHY_MOLDS.md).

## Capture and create

1. Open **Tools → Advanced UI Toolkit → Layouts**.
2. Choose **Create New Layout** and assign the exact source container.
3. Choose **Capture and Continue**.
4. Inspect the captured tree and the excluded-branch and cleared-reference report. Check that the remaining structure matches your intent.
5. Optionally save a new mold asset under `Assets`.
6. Choose a destination RectTransform and a new root name.
7. Review and choose **Create Layout**.

The source remains unchanged. The new layout is an ordinary, independent hierarchy; later source or mold edits do not update it. Undo the scene creation as one operation when needed.

## Populate and bind

Open **Library**, choose a Style Profile and add saved prefab controls to the new containers. Then configure `UIBindingRoot` and generate typed references in **Binder** using the [Framework quick start](../../com.advanceduitoolkit.framework/Documentation~/GETTING_STARTED.md).

External asset references on captured components are cleared, including fonts. Reassign the required assets on the result, or use Library's complete prefab controls when their visuals should be preserved.

## Reuse a saved layout

Choose **Load Layout**, select a format-2 mold and continue through inspection and target selection. A mold's Inspector also offers **Open in Layouts**. Loading works on a detached copy. To reuse revisions made to a scene hierarchy, capture it again and save a new mold; saving refuses to overwrite existing assets.

Existing JSON setup workflows are available separately under **Compatibility → JSON Layouts**. They are documented in the [compatibility reference](INDEX.md#json-compatibility).
