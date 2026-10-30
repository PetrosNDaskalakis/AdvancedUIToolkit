# Root layout plans

[Compatibility index](INDEX.md#json-compatibility) · [Installation](INSTALLATION.md)

This guide documents the retained JSON compatibility workflow in Authoring. For scene hierarchy molds, start with [Layouts getting started](GETTING_STARTED.md).

This Editor-only API gives independent consumers access to the existing stylesheet cascade and native root-layout mapping. This API belongs to AdvancedUIAuthoring.Editor; the current Authoring package depends on Framework and also includes its Integration assembly.

```csharp
UIAuthoringRootLayoutPlan plan;
string error;
if (!UIAuthoringRootLayoutPlan.TryCreate(node, sheet, out plan, out error))
    throw new System.InvalidOperationException(error);

// For already created objects; this does not mutate them.
if (!plan.ValidateTarget(createdObject, createdObject.transform.parent, out error))
    throw new System.InvalidOperationException(error);

// Run inside the caller's Undo transaction. The caller owns cleanup/rollback.
plan.Apply(createdObject);
```

For a source prefab whose parent container has not been created, call `ValidateSource(prefab, schemaParent, sheet, existingParent, out error)`. Supply the actual schema parent or null for a top-level node. This lets preflight account for an upcoming native LayoutGroup. The full schema must still pass `UIAuthoringValidator.Validate`; this API does not validate node order, sibling names, registry element kinds or entire hierarchy structure.

`PropertyNames` is an immutable view of the explicit declarations after cascading. `HasChanges` is false for an empty plan. The resolved mutable style DTO is kept private. JSON and stylesheet objects are not modified. Each call creates a snapshot; later JSON changes require a new plan.

## Supported properties

`width`, `height`, `anchor`, `pivot-x`, `pivot-y`, `position-x`, `position-y`, `min-width`, `min-height`, `flexible-width`, `flexible-height`.

Classes, inherited classes, tokens, legacy style fields and inline styleProperties resolve with existing Authoring precedence. Explicit zero works for positions, pivots, minimums and flexible sizes. Explicit declaration width/height zero is rejected; omit dimensions to preserve the source. Legacy numeric width/height zero continues to mean unspecified.

No background, text, font, padding, spacing, child alignment, grid, or layout-group declarations are allowed, even when their values are zero or `none`. Registered style-class callbacks and property adapters are not applied through this API. Container generation continues to support its existing styles and extensions.

## Native behavior

- Only the target root's RectTransform and LayoutElement are modified. Width/height update the rect size and preferred size; the parent's LayoutGroup still has final authority. A grid uses its own cell sizes and does not use the child's preferred width as a grid-track definition.
- Reuses one enabled root LayoutElement, preserving unspecified fields. Adds one using Undo when needed. Duplicate, disabled or ignoreLayout elements block size overrides. A preserved source minimum larger than an explicit fixed size also blocks it.
- Anchors/pivots/positions conflict with a parent LayoutGroup and fail. Explicit fixed size plus stretch on the same axis also fails. An implicit stretched source requires a compatible explicit anchor or a layout parent to own sizing.
- Enabled ContentSizeFitter dimensions, enabled AspectRatioFitter modes and other active RectTransform drivers are checked. Arbitrary custom drivers that only start after generation or do not declare their ownership cannot be detected in advance.
- Source validation is read-only. Apply accepts editable scene objects only, refuses persistent assets and busy Editor states, and validates the live target again. It records Undo and prefab property modifications but does not open, collapse or roll back an Undo group.
- The caller must run on the Editor main thread, own the target and manage a transaction. Apply can throw on Unity failures; callers must handle rollback. The Integration assembly in Authoring supplies that transaction for newly generated controls.

No Runtime assembly or renderer is added. Validate in a real Unity project before relying on Undo/Redo and prefab serialization.
