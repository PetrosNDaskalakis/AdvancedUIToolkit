# Layouts developer handbook

[Documentation index](INDEX.md) · [Hierarchy molds](HIERARCHY_MOLDS.md)

## Everyday workflow

Use Layouts to reuse a screen's container structure. Capture an eligible hierarchy, inspect omissions, optionally save a mold, select a destination and create. Add complete controls through Library and generate references through Binder. The three tools own separate tasks and keep their own authoring state.

Keep structural containers as ordinary scene RectTransforms. Put reusable visual controls in prefabs. Capturing their parent includes the containers and skips the linked control branches. Ordinary non-prefab controls remain eligible, so inspect the captured tree rather than assuming every Button is excluded.

## Asset organization

Store molds, control prefabs and Style Profiles under project-owned folders in `Assets`. Keep them outside package source directories so package upgrades preserve your content. A mold is a snapshot, not a dependency on the original hierarchy. Keep the required component scripts installed wherever it is reused.

Save a new mold after intentional structural changes. Loading a saved mold works on a copy; it does not mutate the source asset. The save operation creates a new asset and prevents overwrite. Choose descriptive names that identify the structure or revision.

## Review before creating

Read the omission report for excluded branches and cleared references. External Unity assets are cleared even when a component itself is retained. Reassign required fonts/materials/sprites afterward, or leave visual prefab construction to Library. Check layer mappings in the destination project and confirm that custom components are compatible.

The destination remains held after the target step. Confirm its scene and hierarchy before creation. Choose a root name that does not collide at the destination. Creation pauses while Unity is compiling, importing or entering Play Mode.

## Undo and binding

Creation groups its scene operations for Undo/Redo and cleans up failed output. Arbitrary component callbacks may affect assets or objects outside that transaction. Test unfamiliar custom components in a disposable scene.

Captured `UIBindingRoot` components receive reset binding/output configuration. Configure each new screen in Binder. Generated source files are separate from scene Undo; review both hierarchy changes and generated files when reverting work.

## Existing JSON projects

Open **Tools → Advanced UI Toolkit → Compatibility → JSON Layouts** to create from JSON, load a saved setup or resume a JSON draft. Saved setup Inspectors route to a working copy in that workflow. **Native Authoring** and **Legacy Integration Preview** remain under Compatibility.

JSON generation uses schemas, stylesheets and optionally prefab mappings. Generation records support a read-only change/conflict preview; they do not implement Apply or incremental synchronization. See the [compatibility index](INDEX.md#json-compatibility).

## Extending Layouts

Use the public capture and builder services from Editor code on Unity's main thread. Own and dispose transient captures, check returned errors and validate before creation. See [API reference](API_REFERENCE.md). Use [Testing](TESTING.md) when changing eligibility, serialization, restoration, navigation or compatibility behavior.
