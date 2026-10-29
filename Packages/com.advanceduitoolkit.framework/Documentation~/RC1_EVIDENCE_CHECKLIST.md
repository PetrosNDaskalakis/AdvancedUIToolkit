# Advanced UI Toolkit v1.1.0 — RC1 evidence checklist

[← Documentation Index](INDEX.md) · [RC1 release notes](RC1_RELEASE_NOTES.md)

> This is a **blank manual release-evidence template**, not a claim that RC1 passed all Unity checks. **Preview 11 Compact was reported working by the developer**; the RC1 archive still requires its own installation and player-build verification before promotion to v1.1.0 Stable. No bundled Unity Test Runner or benchmark features are required.

## Environment

- Candidate package hash (SHA-256):
- Unity Editor version and target platform:
- OS and architecture:
- Clean-install project path (redact personal paths from public logs):
- Upgrade project / baseline version:
- TextMeshPro absent/present:

## Required validation

| Gate | Result (Pass/Fail/Blocked) | Evidence path / notes |
| --- | --- | --- |
| Clean UPM install + scripts compile | | |
| Binder / UI Library opens without errors | | |
| Manual Binder / Factory workflow checks | | |
| Play Mode event and lifecycle verification | | |
| Native fallback Undo / Redo isolation | | |
| Saved prefab Text / TMP override and reload | | |
| Manual vs Automatic generation + Undo/Redo | | |
| Nested / inactive prefab binding discovery | | |
| Generation transaction rollback | | |
| No unnecessary source writes after no-op generation | | |
| Performance baseline and candidate profiler capture | | |
| Import all samples | | |
| Player build with Editor assembly excluded | | |
| Existing scene/prefab links and GUID preservation | | |
| Documentation, license, and publication review | | |

## Findings and disposition

| ID | Severity | Repro steps / link | Resolution and verified version |
| --- | --- | --- | --- |
| | | | |

## Decision

- [ ] All required gates passed; RC1 may proceed to **v1.1.0 Stable**, subject to publication authorization.
- [ ] One or more gates blocked; fix issues and repeat the affected **manual checks**.

Verified by:

Date:
