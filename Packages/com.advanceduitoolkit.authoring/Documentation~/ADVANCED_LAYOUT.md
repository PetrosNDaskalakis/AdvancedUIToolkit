# Advanced Layout

[Compatibility index](INDEX.md#json-compatibility) · [Installation](INSTALLATION.md)

This guide documents the retained JSON compatibility workflow in Authoring. For scene hierarchy molds, start with [Layouts getting started](GETTING_STARTED.md).

The JSON compatibility tools map stylesheet declarations to **native uGUI layout components** on ordinary scene GameObjects and RectTransforms. Styles apply during Editor generation; there is no stylesheet runtime. The current Authoring package depends on Framework.

## Layout properties

These declarations work in stylesheet classes or per-node `styleProperties`; fields with the corresponding camel-case names also work inside a node's `style` object. Values are invariant-culture strings unless noted.

| Name | Values | Native mapping |
| --- | --- | --- |
| `anchor` | `top-left`, `top`, `top-right`, `left`, `center`, `right`, `bottom-left`, `bottom`, `bottom-right`, `stretch`, `stretch-horizontal`, `stretch-vertical` | `RectTransform.anchorMin/anchorMax` |
| `pivot-x`, `pivot-y` | `0`–`1` | `RectTransform.pivot` |
| `position-x`, `position-y` | any finite number (negative allowed) | `RectTransform.anchoredPosition` |
| `min-width`, `min-height` | nonnegative finite number | `LayoutElement.minWidth/minHeight` |
| `flexible-width`, `flexible-height` | nonnegative finite number | `LayoutElement.flexibleWidth/flexibleHeight` |
| `width`, `height` | existing positive fixed/preferred sizes | `RectTransform.sizeDelta` and `LayoutElement.preferredWidth/preferredHeight` |
| `layout` | `horizontal`, `vertical`, `grid`, `none` | `HorizontalLayoutGroup`, `VerticalLayoutGroup`, `GridLayoutGroup` |
| `child-control-width`, `child-control-height`, `child-force-expand-width`, `child-force-expand-height` | `true` or `false` | Native horizontal/vertical Layout Group child-sizing and expansion flags |
| `child-alignment` | `upper-left`, `upper-center`, `upper-right`, `middle-left`, `middle-center`, `middle-right`, `lower-left`, `lower-center`, `lower-right` | `LayoutGroup.childAlignment` |
| `padding-left`, `padding-right`, `padding-top`, `padding-bottom` | nonnegative integers | Individual `LayoutGroup.padding` sides (override `padding`) |
| `spacing` | nonnegative finite number | Horizontal/vertical spacing or equal x/y grid spacing |
| `grid-columns` | integer 1–1024 | Grid constraint fixed-column count |
| `cell-width`, `cell-height` | positive finite number | `GridLayoutGroup.cellSize` |

The legacy v0.1 and v0.2 style fields still work. Named styles and tokens resolve with the same precedence as v0.2. An inline declaration can override a class's value, including positions equal to **zero** (unlike the old numeric width/height default sentinel).

### Example

```json
{
  "name": "naval-panel",
  "properties": [
    { "name": "width", "value": "560" },
    { "name": "height", "value": "270" },
    { "name": "anchor", "value": "center" },
    { "name": "layout", "value": "vertical" },
    { "name": "child-alignment", "value": "upper-left" },
    { "name": "child-control-width", "value": "true" },
    { "name": "padding", "value": "20" },
    { "name": "padding-bottom", "value": "12" }
  ]
}
```

For a grid:

```json
{
  "name": "weapons-grid",
  "properties": [
    { "name": "layout", "value": "grid" },
    { "name": "grid-columns", "value": "3" },
    { "name": "cell-width", "value": "160" },
    { "name": "cell-height", "value": "48" },
    { "name": "spacing", "value": "9" }
  ]
}
```

## Important validation rules

- The target hierarchy must be an ordinary scene `RectTransform`. Children can be assigned only to schema elements whose registered factory supports child elements.
- Layout/child-alignment/padding properties require a parent-capable element and a layout. Child-control and child-force-expand flags require horizontal or vertical layouts. Grid-only properties require `layout: grid`.
- **A child inside a schema-generated Layout Group cannot use manual anchors, pivots or positions.** Layout Groups control their children's placement; mixing both usually creates surprising results.
- `min-width` / `min-height` cannot exceed a specified fixed width/height.
- `width` conflicts with `stretch` or `stretch-horizontal`; `height` conflicts with `stretch` or `stretch-vertical`. Stretch instead determines that dimension from its parent.
- Invalid/non-finite property values, unsupported names and incomplete token references fail validation **before creating scene objects**.

## Native-engine limits

- Native uGUI grid uses **fixed cell sizes** and optional fixed column counts. This is **not CSS Grid** and doesn't offer automatic fractional tracks or intrinsic content sizing.
- Anchors are chosen from documented presets; raw anchor min/max vectors and responsive media queries are not available yet.
- Layout Group positions override child `RectTransform` placement. The validation rule covers groups declared in the same schema; it cannot fully inspect arbitrary custom layout adapters or runtime-added layout drivers.
- Unity's `CanvasScaler` and anchored transforms handle ordinary resizing, but breakpoint-driven reflow, dynamic viewport classes, max-size constraints, `%`, `px`, `rem`, aspect ratio, and `calc()` are not included.
- Layout styles are **baked into native Unity components on generation**. Modifying a JSON stylesheet later doesn't live-update an existing scene.
- Linked prefab creation is available through the [Framework bridge](LegacyIntegration/API.md), with [prefab root layout rules](LegacyIntegration/PREFAB_LAYOUT.md). Incremental regeneration is not implemented; generation-record comparison remains read-only.

## Manual Unity acceptance

1. Import `Samples~/WeaponPanel.layout.schema.json` and `Samples~/NavalLayout.styles.json` into `Assets`.
2. Select a scene Canvas; open **Tools → Advanced UI Toolkit → Compatibility → Native Authoring**, assign both assets, Validate, then Generate UI.
3. Check `WeaponPanel` has a `VerticalLayoutGroup`, its `ButtonsGrid` has a `GridLayoutGroup` with **3 fixed columns**, and the three buttons display LOAD, ARM, FIRE.
4. Check `CornerMarker` is separately anchored at the **top right** of the selected Canvas with negative 30 offsets.
5. Change `padding-left` and `cell-width`; re-generate to inspect the newly generated UI.
6. Temporarily add `position-x` to `BTN_Load`'s style: validation **must block generation** because its parent uses a GridLayoutGroup.
7. Temporarily set `grid-columns` on `naval-panel`: validation **must fail** because it is a vertical layout.
8. Confirm old v0.1 and v0.2 schemas still generate with the stylesheet picker empty or with the previous `NavalTheme.styles.json`.
9. Confirm one Ctrl+Z removes the newly created hierarchy and the current Unity selection stays unchanged.
10. Save and reload the scene, and make a player build; no custom Authoring runtime code should be needed.

No EditMode, PlayMode or benchmark panels are bundled. Unity Profiler remains the performance tool.
