# Authoring changelog

## 0.5.0-preview.8

- Matched the Framework dependency to `1.2.0-preview.3`.
- Retained format-2 molds, existing asset identities and the 17 optional EditMode tests.
- Added a complete Layouts guide set alongside Framework documentation and consolidated development notes into official references.
- Authoring C# is unchanged from `0.5.0-preview.7`; documentation cleanup does not change behavior or package versions.

## 0.5.0-preview.7

- Made Create New Layout open hierarchy capture and Load Layout select molds.
- Moved JSON creation/loading to Compatibility → JSON Layouts.
- Preserved setup Inspector entry points and routed integration opening to JSON Compatibility.
- Limited JSON draft creation to JSON workflow steps.

## 0.5.0-preview.6

- Added shared loaded/captured depth limits and finite-geometry checks.
- Validated supported property kinds, required payloads and null-reference metadata.
- Clarified format-1 migration and independent output/explicit recapture.
- Expanded the supplied tests to 17.

## 0.5.0-preview.5

- Applied Framework's exact-container eligibility policy to capture.
- Pruned ineligible prefab and non-RectTransform branches before inspecting components.
- Introduced format 2 and preserved old assets with an explicit recapture requirement.

## 0.5.0-preview.4

- Added hierarchy capture, saved molds, guided creation and internal reference remapping.
- Added component-value restoration, external-reference reporting, binding configuration reset and grouped Undo/cleanup.

## Earlier authoring milestones

- Introduced guided Layouts, saved setup copies and save-as behavior.
- Consolidated the former Integration package into Authoring while preserving assembly/asset identities.
- Added native JSON schemas, stylesheets, advanced layout, prefab bridge/root-layout APIs and read-only generation-record comparison.

See [Migration](Documentation~/MIGRATION.md) for format/package changes and [Testing](Documentation~/TESTING.md) for verification scope. Changelog entries do not imply Unity test execution.
