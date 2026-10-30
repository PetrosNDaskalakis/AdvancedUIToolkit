# Layouts testing

[Documentation index](INDEX.md) · [Test installation](../Tests~/README.md)

## Automated coverage

The optional EditMode suite contains 17 behavioral tests for capture/creation, internal and external references, eligible container structure, prefab/non-UI branch exclusion, format rejection, limits, metadata validation, snapshot independence, cleanup and Undo/Redo. Install it following [Tests README](../Tests~/README.md).

The suite is supplied as source and is not automatically enabled from `Tests~`. No Unity execution result is bundled with this distribution. Static source/package checks cannot replace Unity compilation or test execution.

## Manual workflow checks

1. Import the matching package pair and confirm Layouts opens. Create New Layout should open capture directly; Load Layout should list molds.
2. Capture ordinary Header/Body/Footer containers with linked prefab controls under child containers. Confirm eligible containers remain and each prefab branch is absent without modifying the source.
3. Include inactive containers, ordinary manually authored controls and a non-RectTransform branch. Confirm eligibility follows the documented rule rather than a component blacklist.
4. Review omitted external assets and references into excluded branches. Confirm internal retained references reconnect and external references are cleared in output.
5. Save a new mold, reload it, reopen Unity and create under a different valid destination. Confirm source/saved asset independence and refusal to overwrite an existing save path.
6. Change the source after capture. Confirm output stays based on the snapshot until explicit recapture.
7. Exercise target/name validation, Undo/Redo, scene save/reopen and failure cleanup. Test multiple scenes and custom components in disposable scenes.
8. Check old format-1 rejection leaves the asset unchanged. Confirm valid format-2 molds still load.
9. Populate output through Library and configure Binder. Test Manual and Automatic modes; inspect generated source separately from scene Undo.
10. Open Compatibility → JSON Layouts, create from JSON, load a setup and resume a draft. Confirm setup Inspectors open the compatibility workflow and the normal mold path needs no JSON draft.
11. Exercise existing native JSON generation and Legacy Integration Preview with saved records. Confirm preview remains read-only and reports conflicts without Apply.

## Failure and performance checks

Test missing scripts, unsupported fields, changed component definitions, invalid loaded metadata, non-finite geometry and depth/node limits. Confirm clear errors and no partial new hierarchy. Custom Editor callbacks can produce external effects that cannot be universally undone.

Measure large representative captures and creations in your intended Unity version. The 200-node preview display cap does not reduce the number of nodes created. Retain Unity/package versions, errors and test results when reporting a failure.
