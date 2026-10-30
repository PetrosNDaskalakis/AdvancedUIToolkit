# Layouts technical overview

[Documentation index](INDEX.md) · [Architecture](ARCHITECTURE.md) · [API reference](API_REFERENCE.md)

## Capture pipeline

Capture applies the same exact-container eligibility policy used by Framework. It walks eligible RectTransforms in sibling order and prunes each ineligible branch before reading its components. It records hierarchy and geometry, builds an identity map for retained GameObjects/transforms/component slots, then serializes supported component properties against that map.

References inside the map become node/component links. Outside references become null links and omissions. This preserves internal relationships without copying project asset dependencies or silently flattening linked prefab branches. Literal strings remain values, not resolved asset references.

## Portable representation

Format 2 stores a parent-before-child node list, serialized component type information and property payloads. Internal reference identity uses node/component slots, not GameObject names, so duplicate names do not define identity. The root has depth zero. Node count is limited to 10,000 and depth to 256.

Names, active states, layer numbers, geometry and supported component values are retained. Tags, static flags and hide flags are not copied. `UIBindingRoot` configuration is reset. See the [complete behavior reference](HIERARCHY_MOLDS.md).

## Validation and restoration

Preflight checks format, parent ordering, depth, geometry, type availability, property metadata and internal reference bounds. The destination and requested root name are validated separately. During creation, the builder constructs ordinary objects/components, restores values and resolves references with grouped Undo and failure cleanup.

Structural validation cannot prove that every custom component can restore successfully. Changing a script's serialized fields or required components may require recapture. Editor callbacks can produce effects beyond the created subtree.

## Snapshot semantics

Capture does not alter the source. Loading uses a copy. Saving makes a new asset. Creation produces an independent hierarchy. There is no background synchronization among these objects; recapture explicitly when revisions should become reusable.

## Performance and verification

The preview tree displays at most 200 nodes, while creation uses the full validated snapshot. Measure capture, validation, restoration and Undo on representative project hierarchies, especially those with custom components. Limits bound data size; they are not performance guarantees. See [Testing](TESTING.md).

## JSON authoring

The retained JSON path resolves schema and stylesheet input, validates supported native/prefab layout rules and creates a fresh hierarchy. Its generation records provide read-only comparison against a saved baseline. They do not update existing output or apply conflicts. See the [compatibility documentation](INDEX.md#json-compatibility).
