# Prefab root layout

[Compatibility index](../INDEX.md#json-compatibility) · [Installation](../INSTALLATION.md)

This reference documents the retained JSON compatibility workflow in Authoring. It does not describe hierarchy molds.

Apply size rules to linked controls without changing the source prefab or recursively styling its labels.

```json
{
  "id": "fire",
  "parentId": "buttons",
  "kind": "button",
  "name": "BTN_Fire",
  "text": "FIRE",
  "styleClass": "flexible-control",
  "styleProperties": [
    { "name": "flexible-width", "value": "0" },
    { "name": "width", "value": "160" }
  ]
}
```

| Property | Application |
| --- | --- |
| width, height | Root rect dimension plus LayoutElement preferred size; positive overrides only |
| min-width, min-height | Root LayoutElement minimums, including zero |
| flexible-width, flexible-height | Root LayoutElement flexibility, including zero |
| anchor | Existing Authoring presets; stretch and fixed sizes cannot conflict |
| pivot-x, pivot-y | Root pivot in [0,1] |
| position-x, position-y | Root anchored position, including zero and negative offsets |

Existing class/token/inline precedence applies. The new sample combines an inherited base rule, height/min-width tokens, a flexible class and FIRE's zero-valued inline override.

## Keep native layout ownership clear

A parent horizontal/vertical group determines final sizing according to its child-control and expansion flags. Width/height are preferred-size inputs when the group controls those dimensions. A GridLayoutGroup determines final cell size from the container's cell-width/cell-height; child preferred sizes do not create custom grid tracks. Anchor, pivot and position declarations on layout-controlled children are blocked.

A prefab's unspecified root fields are preserved. If it already has one enabled LayoutElement, it is reused. When needed, a missing LayoutElement is added to the instance with Undo. Multiple root LayoutElements, a disabled/ignoreLayout element or an inherited minimum larger than the requested fixed size cause validation errors. Override the minimum explicitly or increase the requested size.

Root ContentSizeFitter/AspectRatioFitter settings and active RectTransform drivers can conflict with explicit geometry. Preflight rejects known conflicts, and the live target is checked again during creation. Custom drivers that only activate later still require project testing.

## Appearance remains prefab-owned

Background, text-color, font-size, text-align, layout-group, padding, spacing and grid declarations on prefab nodes are unsupported. They fail before creation, including explicit zero/none values. Put those rules on native containers where applicable, or edit the source prefab's visual design deliberately. This release does not change child Images, fonts, label colors or nested prefab structure. Schema text still follows the existing single-label/source-identity mapping.

## Unity acceptance focus

- Compare source prefab and generated controls: only the intended root overrides and selected text override should differ.
- Undo once after Generate UI; Redo restores hierarchy, links, root sizes and text. Test before running Binder, which has separate manifest operations.
- Save/reopen the scene and inspect prefab overrides, including an added LayoutElement.
- Confirm invalid declarations or known source conflicts leave no generated objects.
- Existing recipes with no prefab styles must still generate as before.

No incremental regeneration is included. The compatibility tools include Editor-only generation records and a read-only change/conflict preview; see [Change preview](CHANGE_PREVIEW.md).
