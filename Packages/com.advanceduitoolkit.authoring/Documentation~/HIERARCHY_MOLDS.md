# Hierarchy molds

[Documentation index](INDEX.md) · [API reference](API_REFERENCE.md) · [Migration](MIGRATION.md)

A hierarchy mold is a versioned ScriptableObject containing a portable snapshot of eligible scene containers. It stores structure and supported serialized values; creation builds ordinary scene objects with no live connection to the mold or source.

## Capture eligibility

The exact source must be a loaded-scene RectTransform outside prefab instances, Prefab Mode and Play Mode. Capture shares Framework's creation-parent eligibility policy and never substitutes an ancestor. Inactive eligible containers are included in sibling order.

Every ineligible branch is excluded with all descendants before its components are inspected. This excludes linked prefab roots, nested prefab members, added overrides inside those branches and non-RectTransform branches. UI descendants of an excluded non-UI object are not reparented into the result.

Eligibility does not mean Unity's `Selectable` component or the Hierarchy picking-lock flag. There is no Button/Toggle component blacklist: ordinary manually created or fully unpacked controls can be retained. Capture cannot infer former prefab provenance after unpacking.

## Stored data

| Data | Behavior |
| --- | --- |
| Hierarchy | Retains eligible nodes, sibling order, names and active states |
| Geometry | Retains local transforms, anchors, pivot, size and anchored position |
| Components | Retains supported components, serialized values and layer numbers |
| Internal references | Remaps links between retained GameObjects, transforms and components |
| External references | Clears references to assets, external scene objects and excluded branches; reports omissions |
| Binder configuration | Retains `UIBindingRoot` type but resets its manifest and generated-output configuration |
| Other flags | Does not copy tags, static flags or hide flags |

Layer numbers require the destination project's layer definitions to match your intent. Strings remain literal strings, including custom asset-address values. Asset references such as fonts, sprites and materials are not portable mold values and must be restored deliberately after creation.

Supported serialized values include scalar values, common Unity math types, curves, gradients, arrays and internal object references. Missing scripts and unsupported serialized fields on retained components block capture. Managed references, exposed references and fixed buffers are not supported. Components in excluded branches are never inspected.

## Inspection and persistence

Inspect the tree and omission report before creation. The tree preview shows the first 200 nodes; creation includes all captured nodes. Save creates a new asset and refuses existing paths. Loading a saved asset uses a detached working copy. Changing the source after capture requires explicit recapture.

Format 2 is the current data format. Format-1 assets are preserved but blocked because their original prefab boundaries cannot be reconstructed reliably. Recapture the original hierarchy with prefab links intact; do not edit the version field or expect recapturing flattened output to recover those boundaries.

## Creation and limits

- Maximum 10,000 nodes and hierarchy depth 256, with root depth zero.
- Loaded data is checked for supported format, valid parent/reference structure, component types, supported property metadata and finite geometry.
- The destination is held after target selection. Creation makes a new hierarchy and does not replace existing objects.
- Component scripts must be installed in the destination project. Script field or dependency changes can require recapture.
- Component restoration may still fail after structural preflight. The builder cleans up failed output and uses grouped scene Undo for successful creation.
- Custom component Editor callbacks can cause effects outside the created hierarchy; those effects cannot be universally rolled back.
- Automatic binding generation is suspended during the creation transaction. Configured owning roots can update afterward in Automatic mode. Scene Undo does not restore generated C# files.

Molds do not instantiate prefab slots, auto-fill controls, require Style Profiles or JSON schemas, track a baseline, or regenerate existing output in place. Use Library for prefab placement and Binder for generated references.
