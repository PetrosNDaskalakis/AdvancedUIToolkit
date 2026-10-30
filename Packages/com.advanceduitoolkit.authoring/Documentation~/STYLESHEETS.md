# Modular JSON stylesheets

[Compatibility index](INDEX.md#json-compatibility) · [Installation](INSTALLATION.md)

This guide documents the retained JSON compatibility workflow in Authoring. For scene hierarchy molds, start with [Layouts getting started](GETTING_STARTED.md).

A stylesheet is a **separate, optional `TextAsset`**. In the compact Authoring window select **Schema** and **Stylesheet (Optional)** before clicking **Validate** or **Generate UI**. You can also call `UIAuthoringValidator.Validate(document, parent, sheet)` and `UIAuthoringGenerator.Generate(document, parent, sheet)`.

The JSON stylesheet format is **version 1**, unrelated to the package's version. It has `tokens` (`{ "name": "--accent", "value": "#33788C" }`) and `classes` (`{ "name": "button", "extends": "", "properties": [{ "name": "background", "value": "var(--accent)" }] }`). All fields are parsed by Unity `JsonUtility`.

## Cascading and priority (lowest to highest)

1. Built-in native element defaults (the same as in v0.1).
2. Classes from `styleClass`, then `styleClasses` in their listed order. Each class applies `extends` ancestors first, then its own declarations.
3. Legacy `node.style` fields that are nonzero/nonempty (for v0.1 compatibility).
4. `node.styleProperties` (`[{ "name": "text-color", "value": "var(--text-primary)" }]`) in order. This lets a node intentionally override a declaration with `0`.
5. Optional registered `IUIAuthoringStyleClass` handlers, after native styles and custom property adapters.

The final declaration for a property wins. A name that is both a stylesheet class and a registered handler is rejected as ambiguous. Registered `IUIAuthoringStyleClass` names remain supported even without a stylesheet. **Stylesheets are not runtime styles**: generated components do not update when a stylesheet asset changes.

## Supported properties

| Name | Value | Native component |
| --- | --- | --- |
| `width`, `height` | nonnegative, finite canvas-unit numbers | `RectTransform` / `LayoutElement` |
| `background` | `#RRGGBB` or `#RRGGBBAA` | `Image.color` |
| `text-color` | same hex format | child/root legacy uGUI `Text.color` |
| `font-size` | nonnegative integer | legacy `Text.fontSize` |
| `text-align` | `left`, `center`, `right` | legacy `Text.alignment` (middle vertical) |
| `layout` | `horizontal`, `vertical`, `grid`, `none` | Layout Group on container-capable kinds |
| `spacing` | nonnegative finite number | Layout Group spacing |
| `padding` | nonnegative integer | Layout Group uniform padding |

Only container-capable elements accept layouts. `spacing`/`padding` require a layout. Text formatting requires a text/button or compatible custom element. A text root cannot have an Image background. Unknown properties fail validation unless an external package registered a property adapter.

**Token references** must use the complete value exactly, e.g. `"var(--surface)"`, optionally referencing another token. Missing tokens and cycles are errors. Arbitrary expressions (`calc`, `var(--a, fallback)`, percentages, px/rem) are intentionally unsupported. Class inheritance must remain acyclic; missing classes, duplicates and invalid property values are rejected.

**Important limitations:** A numeric `0` remains the native default/unset sentinel for some v0.1 built-in style behaviors (e.g. width/height). The `none` layout value clears an inherited declaration, but does not remove any pre-existing Layout Group on an object. Styles are applied only to **newly created** objects, not existing manual overrides. CSS cascade selectors and responsive rules are not supported.

## Example

See `Samples~/NavalTheme.styles.json` paired with `Samples~/WeaponPanel.styled.schema.json`. The original `WeaponPanel.schema.json` still works without a stylesheet.

## Advanced layout

Additional built-in properties cover anchors, pivots, offsets, minimum/flexible sizing, individual padding sides, child alignment and fixed-column grid layout. See [Advanced Layout](ADVANCED_LAYOUT.md) for the complete table, limitations and a sample.
