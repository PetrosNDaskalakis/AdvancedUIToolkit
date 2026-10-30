# Framework developer handbook

[Documentation index](INDEX.md) · [API reference](API_REFERENCE.md)

## Responsibilities

Library owns reusable prefab placement. Binder owns component discovery, manifests and generated references. `UIView<TBindings>` owns view initialization and `UIEventScope` owns listeners registered through its helpers. Authoring's Layouts tool provides reusable container structures independently of the prefab library.

## Library workflow

Create saved prefab assets and register them in a `UIStyleProfile`. Standard templates use component kinds; custom templates use IDs. A `BaseProfile` shares templates across libraries, with local identities overriding inherited ones. A `UIStyleContext` can associate a profile with an ancestor container. See [Style Profiles](STYLE_PROFILES.md).

Choose a profile, search or filter its entries, select a template, confirm the target and create. The visible count reflects the selected kind, query and inherited-entry scope. Search, kind and list/tile choices survive ordinary domain reload. Filtered-out selected entries hide their details until visible again.

Targets must be eligible ordinary scene RectTransforms outside prefab instances and Prefab Mode. Manual target assignment pins the exact container; **Locate** helps find it. Creation pauses during compilation, import and Play Mode. Library preserves prefab links; it does not edit source prefab assets to customize a new instance. [Text overrides](PREFAB_TEXT_OVERRIDE.md) can change a chosen label on that instance.

## Naming and binding scope

| Object name | Component | Generated accessor |
| --- | --- | --- |
| `BTN_Fire` | Button | `UI.Fire` |
| `TGL_Power` | Toggle | `UI.Power` |
| `SLD_Volume` | Slider | `UI.Volume` |
| `TXT_Status` | Legacy Text | `UI.Status` |
| `IMG_Contact` | Image | `UI.Contact` |
| `INP_PlayerName` | Legacy InputField | `UI.PlayerName` |
| `SCR_Messages` | ScrollRect | `UI.Messages` |

Binder resolves the component on the named object or one unambiguous descendant. Inactive children participate; children owned by a nested `UIBindingRoot` do not. Duplicate or invalid identifiers and ambiguous matches need review before generation. Names are scoped to a binding root.

Select the exact root you intend to configure. Set class name, namespace and output path, preview C#, then generate. Generated classes implement `IUIBindings` and resolve the manifest once. Keep handwritten gameplay logic in the view, outside generated files.

## Change review and generation

Review renames, missing references and removals in Binder. Update consuming game code when removing generated properties. Generated-file ownership and output collision checks protect files from unintended replacement; resolve reported conflicts rather than bypassing them.

Manual mode detects changes without writing C# automatically. Automatic mode writes validated safe changes for configured roots. Both modes still require manual handling of unsafe or ambiguous changes. See [Generation modes](GENERATION_MODES.md), [Custom templates](CUSTOM_CODE_TEMPLATES.md) and [XML summaries](BINDER_XML_SUMMARIES.md).

## Runtime code

Use `UIView<TBindings>` and register managed events once in `OnInitialize`. Use `OnViewEnabled`, `OnViewDisabled` and other [lifecycle hooks](LIFECYCLE.md) for repeated activation. Manual UnityEvent listeners, global subscriptions and coroutines need your own lifetime management. Generated references are component references; the framework does not provide reactive game-data binding or screen navigation.

## Prefabs, Undo and source control

Preserve `.meta` files and inspect prefab overrides after authoring. Scene Undo and generated C# writes are separate operations: undoing hierarchy edits does not restore a previously generated source file. Review generated changes in source control and regenerate deliberately when restoring an earlier hierarchy.

Keep reusable game assets and generated code under `Assets`; keep package code in its package directory. Use [Migration](MIGRATION.md) for upgrades and [Testing](TESTING.md) for validation.

## Editor extensions

Use the public [creation API](EXTERNAL_EDITOR_API.md), [batch API](BATCH_CREATION_API.md), [headless Binder API](HEADLESS_BINDER_API.md) and [component registration API](COMPONENT_EXTENSIBILITY_API.md). Run these from Editor code on Unity's main thread. Use batch creation when operations need one grouped Undo and failure cleanup.
