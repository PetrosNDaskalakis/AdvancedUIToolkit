# Advanced UI Toolkit v1.1.0 — Stable-version package notes

[← Documentation Index](INDEX.md) · [Final acceptance checklist](V1_1_STABLE_ACCEPTANCE.md) · [RC1 historical notes](RC1_RELEASE_NOTES.md)

**Package version:** `1.1.0` · **Base:** `1.1.0-rc.1` Compact · **Release state:** packaged for final Unity acceptance; **not yet certified or approved for publication**.

## Scope

This is a **code-frozen promotion of RC1**. There are no new UI features, runtime APIs, serialized fields, prefab-creation rules, or Binder generation changes. The compact package deliberately excludes bundled EditMode/PlayMode test assemblies, synthetic benchmarks, and test/benchmark controls in the Editor windows. Profile performance with **Window → Analysis → Profiler**.

The release keeps:

- **Binder:** native non-prefab container selection, strongly typed reference generation, XML property summaries, Automatic/Manual generation and safety checks.
- **UI Library / Factory:** Style Profiles, prefab creation, regular RectTransform containers, and instance-only Prefab Text Override supporting root/child legacy Text and optional TMP.
- **Undo/Redo:** isolated Factory creation operations and regeneration workflow.
- **UPM boundaries:** Editor-only tools separated from the Runtime assembly.

## Install and upgrade

**Full package:** Back up the Unity project, close the Editor, and replace the embedded package at `Packages/com.advanceduitoolkit.framework` with the provided complete UPM archive. Or add the extracted `package.json` through Unity Package Manager. Do **not** keep duplicate copies of the same scripts under `Assets/`.

**Incremental patch:** Apply the upgrade patch **only** to `1.1.0-rc.1` Compact. It contains release metadata and documentation only; do not apply it to Preview 11 or any earlier package. The patch includes instructions and should reproduce the exact full v1.1.0 package tree.

**Version history:** Prior release candidates and preview notes are intentionally retained in `CHANGELOG.md` and the historical documentation.

## What is verified and what remains open

| Gate | State |
| --- | --- |
| Manifest and UPM source structure audit | Completed outside Unity |
| Full and patch archive integrity and equivalence | Completed outside Unity |
| Production `.cs` and retained Unity `.meta` parity with RC1 | Completed outside Unity |
| Clean Unity Editor compilation on this exact package | **Not yet documented** |
| Binder and Factory real-scene workflows | **Not yet documented** |
| Optional TMP and prefab override persistence | **Not yet documented** |
| Play Mode interactions and target player build | **Not yet documented** |
| Unity Profiler observations | **Not yet documented** |
| Public redistribution rights and license approval | **Not yet documented** |

Use the [Final Acceptance Checklist](V1_1_STABLE_ACCEPTANCE.md) to collect this evidence before calling the release validated. A version number does not substitute for the Unity-side checks.

## Next phase

Once acceptance is recorded, the v1.1.0 release can be finalized. After that, **Phase 3 (v1.2)** begins with small JSON UI schema support that reuses the existing Factory and Binder infrastructure. No schema authoring is included in this package.
