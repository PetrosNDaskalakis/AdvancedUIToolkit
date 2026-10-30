# JSON schema guide

[Compatibility index](INDEX.md#json-compatibility) · [Installation](INSTALLATION.md)

This guide documents the retained JSON compatibility workflow in Authoring. For scene hierarchy molds, start with [Layouts getting started](GETTING_STARTED.md).

The schema file uses Unity `JsonUtility`. It must contain a plain JSON object with **`version: 1`** and a nonempty **`nodes`** array. Unknown JSON fields are ignored by Unity's serializer; never rely on arbitrary CSS declarations being interpreted.

## Nodes

A `node` has `id`, `parentId` (optional), `kind`, `name`, `text` (optional), `style` (optional), `styleClass` (optional), `styleClasses` (optional list), and `styleProperties` (optional ordered declarations).

- `id`: globally unique within the schema, ASCII letters/digits/underscore/hyphen and begins with a letter.
- `parentId`: references an **earlier** container node, or leave empty to parent under the chosen scene RectTransform. This explicit order allows single-pass generation and simple preflight.
- `kind`: `container`, `text`, `image`, `button`, or a kind registered by an external Editor extension.
- `name`: Unity GameObject name; sibling names must be distinct in the same schema parent.
- `text`: native `Text` content for `text` and `button` elements.
- `styleClass`: optional registered class name. A class must exist in the assigned stylesheet or the registered Editor extension registry.

Optional **Stylesheet (Optional)** and reusable classes are documented in [Stylesheets](STYLESHEETS.md). The original v0.1 flat style fields remain supported.

## Built-in styles

| Property | Supported value | Unity implementation |
| --- | --- | --- |
| `width`, `height` | nonnegative number; omit or zero to preserve native default | `RectTransform.sizeDelta` and `LayoutElement` |
| `background` | `#RRGGBB` / `#RRGGBBAA` | `Image` color |
| `textColor` | `#RRGGBB` / `#RRGGBBAA` | `Text.color` |
| `fontSize` | nonnegative integer (zero means unspecified) | `Text.fontSize` |
| `layout` | `horizontal`, `vertical`, or empty; containers only | Layout Group |
| `spacing` | nonnegative float; positive values require layout | Layout Group |
| `padding` | nonnegative integer; positive values require layout | Layout Group |

Defaults for unspecified numeric style fields use `0` to avoid Unity version-dependent JSON initializer behavior. Missing styles retain the built-in element defaults. The generator uses Unity's built-in `LegacyRuntime.ttf` font for legacy `Text` labels.

`style.textAlignment` additionally supports `left`, `center`, or `right`.

## Design constraints

- This is native uGUI. Advanced stylesheet declarations can configure Unity GridLayoutGroup, but browser CSS Grid, flex wrapping, filters, gradients and animation parsing are not supported.
- A `button` creates a GameObject with `Image`/`Button` and a nested `Text` label. It has no gameplay callbacks.
- Among built-in native kinds, only `container` nodes can have schema children. A custom registered element can opt into children via `CanContainChildren`.
- The target scene parent cannot be a prefab asset or prefab instance. The Authoring window follows selected regular RectTransforms, skipping prefab instances.
- The input is validated before modification. If a factory or style extension fails during generation, the tool attempts Undo rollback and cleanup; this is **not** a disk-level transaction.
- Generated objects do not carry runtime authoring metadata. `id` is an operation-local reference; incremental regeneration is not implemented.

## Minimal JSON

```json
{
  "version": 1,
  "name": "Example",
  "nodes": [
    { "id": "panel", "kind": "container", "name": "Panel", "style": { "layout": "vertical", "padding": 12, "spacing": 8 } },
    { "id": "title", "parentId": "panel", "kind": "text", "name": "Title", "text": "Hello" }
  ]
}
```

The full `Samples~/WeaponPanel.schema.json` creates a styled three-button panel.
