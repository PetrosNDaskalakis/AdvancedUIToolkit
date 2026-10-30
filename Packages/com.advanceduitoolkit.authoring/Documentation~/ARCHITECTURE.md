# Layouts architecture

[Documentation index](INDEX.md) · [Technical overview](TECHNICAL_OVERVIEW.md)

## Package and assembly boundaries

| Assembly | Responsibility |
| --- | --- |
| `AdvancedUIAuthoring.Editor` | JSON schema parsing, stylesheets, validation, native generation and root layout planning |
| `AdvancedUIAuthoring.Integration.Editor` | Layouts window, hierarchy molds, Framework bridge, saved setups and generation records |

Both assemblies are Editor-only and ship in `com.advanceduitoolkit.authoring`. Framework remains independently usable. The Integration assembly name is preserved for compatibility even though the former Integration package is consolidated into Authoring.

## Primary mold workflow

`UIAuthoringLayoutsWindow` owns wizard state. `UIHierarchyMoldCapture` reads an eligible hierarchy without modifying it, records portable data in a transient `UIHierarchyMold` and reports omissions. The user can save a new asset or load a detached copy of an existing asset. `UIHierarchyMoldBuilder` validates the data and destination, creates the hierarchy, restores values and reconnects internal references.

Library remains responsible for placing linked control prefabs. Binder remains responsible for manifests and generated bindings. Molds do not contain live prefab slots, schema mappings or regeneration baselines.

## Persistence

| Object | Lifetime |
| --- | --- |
| Source hierarchy | Existing scene content; capture leaves it unchanged |
| Working mold | Detached transient capture or loaded copy |
| Saved mold | Project ScriptableObject asset with format version and portable node data |
| Created hierarchy | Independent ordinary scene objects; no live link to source or saved mold |
| JSON setup / generation record | Compatibility assets retaining their existing identities and behavior |

## Compatibility boundary

JSON creation and setup loading are explicit Compatibility entry points. The bridge converts validated schema intent into Framework creation operations and applies supported root layout plans. Records support read-only change/conflict preview. These paths remain available without entering the normal mold flow.

## Failure boundary

Validation rejects known structural, property and target errors before creation. Restoration can still fail when component fields or dependencies have changed. Grouped scene Undo and cleanup cover the new hierarchy; arbitrary custom component callbacks can affect other state and are outside a universal rollback guarantee.
