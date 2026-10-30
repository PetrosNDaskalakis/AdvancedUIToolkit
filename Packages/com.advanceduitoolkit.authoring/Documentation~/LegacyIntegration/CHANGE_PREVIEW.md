# Generation records and change preview

[Compatibility index](../INDEX.md#json-compatibility) · [Installation](../INSTALLATION.md)

This reference documents the retained JSON compatibility workflow in Authoring. It does not describe hierarchy molds.

This is the preview stage of regeneration. It records which scene objects belong to schema IDs, compares later source edits, and flags detected scene drift. **It never applies changes, replaces objects, deletes objects, or writes binding C#.**

## First run

1. Install the matching Framework and Authoring package pair described in [Installation](../INSTALLATION.md).
2. Open **Tools → Advanced UI Toolkit → Compatibility → Legacy Integration Preview**. Use your working recipe with `WeaponPanel.prefab-layout.schema.json` and `NavalPrefabLayout.styles.json`.
3. Generate once under a fresh target parent. Existing hierarchies without a captured identity map are not adopted by name.
4. Save the scene. In **Change Preview (read-only)**, click **Capture Baseline**. The new asset appears under `Assets/Editor/UIAuthoringRecords` and is assigned to Generation Record.
5. Click **Preview Changes**. Expect five Unchanged nodes and no scene conflicts; enable Show unchanged nodes to list them.
6. Assign the supplied `WeaponPanel.preview-changes.schema.json` to the same recipe. Keep the stylesheet and profile unchanged. Click **Preview Changes** again.

If the recipe uses explicit per-node mappings, remove the mapping for `load` and add one for `safe` if it needs a custom template or label target. Stale mappings are rejected by validation. The table below assumes the sample's default Button mappings.

Expected source rows:

| Node | Change | Reason |
| --- | --- | --- |
| panel | Unchanged | Same definition |
| buttons | Unchanged | Same definition |
| arm | Update | Label changes to ARMED and source order shifts |
| fire | Update | Preferred width changes to 180 and source order shifts |
| safe | Add | New BTN_Safe schema node |
| load | Remove | Existing schema ID omitted |

The scene should still show the original LOAD / ARM / FIRE hierarchy. The report is a proposal, not a generation action.

## Baseline and identity

Capture saves the **current saved state** of the exact objects returned by the last successful generation in this window. Compatible visual/component edits made before capture become the accepted baseline. It cannot tell you what changed before that moment. Generated names and parents must still match, and recipe inputs must not have changed between Generate UI and Capture Baseline.

Save the scene yourself; capture refuses unsaved or dirty scenes. Capture does not save other scenes or overwrite an existing record. Keep the window open until capture; closing it can discard pending object references. A captured record is a durable Editor-only asset containing stable GlobalObjectIds rather than scene-object references or names. It has no runtime scene component and is placed in an Editor folder. After an ordinary domain reload or Unity restart, open the recorded scene, select the record asset and click **Open Change Preview**.

Scene renames/moves inside the project preserve the scene GUID. Moving individual objects into another scene changes their identity and is reported as missing/conflicting. A closed scene is reported as unavailable; the tool does not open scenes automatically.

## Source change versus scene conflict

- **Add / Remove:** an ID appears/disappears in the schema. Changing an ID is therefore removal plus addition, not a rename.
- **Update:** node definition, source sibling order or mapping/text policy changed.
- **Replace:** kind or resolved source prefab identity changed.
- **Review:** the whole stylesheet or Style Profile dependency hash changed. This release deliberately flags a broad review scope rather than claiming a precise property-level cascade diff.
- **Unchanged:** no source-side difference was found.
- **CONFLICT:** the recorded scene object is missing, its parent/order changed, serialized UI state differs, an untracked sibling occupies a desired name, or safe comparison is unavailable. A conflict is independent of the source change. A source-Unchanged row can still show CONFLICT.

Root/owned-descendant fingerprints include GameObject state, component types and supported serialized values, source-prefab identities and child ordering. Tracked child schema nodes are compared in their own rows. Adding/removing untracked descendants or components is detected. External target-parent LayoutGroup configuration is checked separately.

## Deliberate limits

- No Apply/regenerate action, safe-operation guarantee, property-level merge, automatic conflict resolution, external-reference migration or deletion approval is provided.
- Normal layout-driven RectTransform positions, sizes, anchors and pivots are excluded to avoid reporting layout recalculation as manual edits. Manual values on those driven fields are outside this conflict check. Other source declarations and LayoutElement/LayoutGroup settings remain compared.
- UIBindingRoot manifests are excluded because Binder owns them. Preview does not inspect generated files. Use **Inspect Binder** for binding state; removal protection remains in the existing Binder workflow.
- Unsupported serialized property types (for example context-dependent ExposedReference), missing scripts, unresolved identities or inspection limits block capture or produce conflicts. Common primitive/math values, object references, arrays, curves, gradients and managed-reference type/field traversal are supported.
- Referenced scene objects must have persistent identities and their scenes must be loaded. Custom component state that is not serialized, external material/texture contents not included in the profile dependency graph, and arbitrary runtime behavior are not a complete part of the scene comparison.
- Preview is a snapshot. Observed Inspector changes, Undo, hierarchy changes and project imports clear it; rerun it after any other edits. A no-conflict result is not authorization to overwrite anything.
- Registered extension validators must honor the existing non-mutating validation contract.

Verify persistence, drift detection and read-only behavior using the [testing guide](../TESTING.md).
