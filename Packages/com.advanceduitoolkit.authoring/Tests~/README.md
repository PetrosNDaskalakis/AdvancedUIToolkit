# Optional hierarchy mold EditMode tests

The suite contains 17 tests and is supplied as source. Unity ignores `Tests~` in the package by default; these tests are not part of the production assemblies.

1. Use a disposable Unity project with both supplied packages and Unity Test Framework installed.
2. Copy the entire `Tests~/Editor` directory to `Assets/AdvancedUIToolkitMoldTests/Editor`.
3. Wait for compilation, open Unity Test Runner, select EditMode and run `AdvancedUIAuthoring.MoldTests.HierarchyMoldTests`.
4. Record Unity/package versions and all results. Remove the copied test directory after validation if no longer needed.

Coverage includes nested/inactive geometry, component identity, internal reference remapping, external reference clearing, prefab exclusions, binding configuration reset, unsupported fields, failure cleanup, collision protection, grouped Undo/Redo, selection-policy agreement, non-UI pruning, legacy format rejection, depth/geometry/property validation and snapshot independence.

Fixtures use an additive empty scene and a uniquely named temporary Assets folder, removing those during teardown. Start in Manual binding mode, then perform the Automatic-mode and UI workflow checks in [Testing](../Documentation~/TESTING.md).

No Unity compilation or test execution report is bundled. Passing this suite does not establish support for every custom component, Editor version or player target.
