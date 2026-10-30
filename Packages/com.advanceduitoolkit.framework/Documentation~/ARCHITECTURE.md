# Framework architecture

[Documentation index](INDEX.md) · [Technical overview](TECHNICAL_OVERVIEW.md)

## Package boundaries

| Assembly | Responsibility | Availability |
| --- | --- | --- |
| `AdvancedUIToolkit.Runtime` | Serialized binding roots, typed binding contract, view lifecycle, event scope and Style Profile data | Editor and player |
| `AdvancedUIToolkit.Editor` | Library, Binder, prefab creation, discovery, code generation and automation | Editor only |

Framework has no dependency on Authoring. The optional Authoring package consumes Framework's public Editor APIs to create layouts and integrate binding operations. Authoring includes its existing Integration assembly; it is not a third installable package.

## Ownership

| Area | Owner |
| --- | --- |
| Visual controls | Project prefab assets selected through Style Profiles |
| Container structure | Project scene hierarchy, optionally recreated from a Layouts mold |
| Component references | Serialized manifest on each `UIBindingRoot` |
| Typed access | Generated project class implementing `IUIBindings` |
| View behavior | Handwritten project class derived from `UIView<TBindings>` |
| Managed UI listeners | `UIEventScope` for the owning view |
| Generated source writes | Binder and its automation services |

## Editor flow

Library resolves profile entries, validates the exact destination and creates linked prefab instances. It does not own layout recipes or mold state. Binder scans a selected root, resolves named components within its scope, compares the result with the manifest and generated-file state, and presents review before writing safe changes.

Automation observes configured roots and either queues safe changes in Manual mode or generates them in Automatic mode. Invalid names, missing references, ambiguities and ownership conflicts require review. Nested binding roots define independent scopes.

## Runtime flow

`UIView<TBindings>` obtains its `UIBindingRoot`, creates and initializes typed bindings once and registers managed UI listeners. Activation resumes listeners; deactivation suspends them; destruction disposes them. Gameplay code uses component properties directly. Runtime access does not execute the Editor discovery or generation pipeline.

## Persistence and safety

Project assets own profiles, prefabs, scenes and generated files. Package `.meta` GUIDs and assembly names preserve serialized references across upgrades. Scene Undo and C# file writes have different lifetimes; a hierarchy Undo is not a source-file rollback.

Framework provides component-reference binding and authoring tools. Screen navigation, reactive data binding, runtime theming and arbitrary custom-component side-effect rollback are outside its scope.
