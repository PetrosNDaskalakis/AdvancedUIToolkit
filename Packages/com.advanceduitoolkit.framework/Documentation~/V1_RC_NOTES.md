# Advanced UI Toolkit 1.0 Release Candidate (rc.1)

This release makes a focused regression fix in the simplified Workspace. When a user selects a child inside an independent nested `UIBindingRoot`, the Workspace now changes to that nested root rather than incorrectly retaining the outer screen. Ordinary child selections continue retaining the outer screen; selecting an unrelated non-UI scene object does not disrupt the workflow.

## Compatibility

- No modifications to `Runtime/` classes, UPM runtime assemblies, serialized manifest fields, Factory controls, automatic/manual generation modes, code template formats, or generated C# signatures.
- Previous script GUIDs are preserved; only the new EditMode regression test has a new GUID.
- No regeneration is required solely for installing this candidate.

## Verification status

Static package checks only. This environment cannot run the Unity Editor, Test Runner or player build. Use `V1_RELEASE_GATES.md` to decide whether to promote a candidate to stable 1.0.0.
