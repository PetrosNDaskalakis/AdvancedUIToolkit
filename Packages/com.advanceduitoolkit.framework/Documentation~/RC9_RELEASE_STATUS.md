# Advanced UI Toolkit v1.0.0-rc.9 — Release status

**Candidate status:** Static package audit passed. Unity compilation, EditMode/PlayMode tests, UI workflow tests, and player build are not verifiable in this environment and are therefore pending.

## What changed from RC8?

- `package.json`: candidate version bumped to `1.0.0-rc.9` and description updated.
- `README.md`: rewritten for the two-window **Binder / Factory** workflow, with direct installation and lifecycle examples.
- Primary onboarding, troubleshooting and sample guides: corrected legacy menu paths and clarified current generated-binding behavior.
- `Documentation~/V1_RELEASE_GATES.md`: complete clean-install, prefab/Undo, test and publication acceptance criteria.
- `Tools~/verify_package.py`: now audits menu entrypoints, sample README files, `.meta` presence/GUID collisions, assembly definitions and version consistency.
- `CHANGELOG.md`: recorded changes to the release candidate.

## What did not change?

- Runtime `*.cs` files: **all unchanged**.
- Editor `*.cs` files: **all unchanged**.
- Automated test `*.cs` files: **all unchanged**.
- Unity `.meta` files and GUIDs: **all unchanged**.
- Generated binding structure, existing project manifests, prefab library assets and profile inheritance: **unchanged**.

## Verification instructions

1. In a clean Unity LTS project, install `com.advanceduitoolkit.framework` through Package Manager.
2. Test **Tools > Advanced UI Toolkit > Binder** and **Tools > Advanced UI Toolkit > Factory**, narrow and wide.
3. Run **Window > General > Test Runner**, EditMode and PlayMode; record results.
4. Exercise Automatic mode delete → generate → Ctrl+Z → regenerate and subsequent Redo.
5. Generate a binding class from a weapon panel, verify callbacks, disable/re-enable cycles, and player build.
6. Confirm asset ownership and license before any public GitHub publication.

See `V1_RELEASE_GATES.md` for the expanded verification matrix.

**Release policy:** Keep version `1.0.0-rc.9` until verified in Unity. Change it to `1.0.0` only once every applicable gate passes.
