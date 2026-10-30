# Layouts troubleshooting

[Documentation index](INDEX.md) · [Hierarchy molds](HIERARCHY_MOLDS.md)

| Symptom | Check and action |
| --- | --- |
| Layouts menu missing | Confirm both matching packages are installed and Console compilation errors are resolved |
| Source cannot be captured | Use the exact ordinary loaded-scene RectTransform outside prefab instances/Prefab Mode; leave Play Mode and wait for import/compilation |
| Prefab controls absent | Entire linked prefab branches are intentionally excluded; populate the new containers with Library |
| Ordinary Button retained | Eligibility is based on scene/prefab status, not component type; manually authored or unpacked controls remain eligible |
| UI children under a non-UI object absent | Non-RectTransform branches are pruned in full; place reusable UI containers under eligible RectTransforms |
| Font, sprite, material or scene link missing | External references are cleared and reported; reassign them or use complete Library prefabs |
| Missing script or unsupported property | Repair the retained component/script or use supported serialized fields, then recapture |
| Old mold blocked | Format 1 lacks prefab provenance; recapture the original hierarchy with links intact and save a new format-2 mold |
| Source edits did not appear | Capture is a snapshot; explicitly recapture after changes |
| Mold edits did not update created output | Created hierarchies are independent; edit the scene normally and capture revisions for later reuse |
| Save refuses an existing path | Choose a new asset path; saving creates a new mold rather than overwriting |
| Invalid depth, geometry or property metadata | Correct the source and recapture; do not hand-edit version/property payloads to bypass validation |
| Creation fails after preflight | Check changed component fields/dependencies and custom callbacks; verify cleanup and recapture compatible data |
| Binding configuration absent | Captured UIBindingRoot manifests/output settings are reset; configure the new screen in Binder |
| JSON setup not listed by Load Layout | Use Compatibility → JSON Layouts or the setup Inspector; Load Layout selects molds |
| No Apply button in change preview | Generation-record preview is read-only and does not implement incremental regeneration |

When reporting an issue, include Unity/package versions, mold format, source/destination eligibility, the omission/error report, component types involved and minimal reproduction steps. Test custom components in a disposable scene because their Editor callbacks may affect state beyond the created hierarchy.
