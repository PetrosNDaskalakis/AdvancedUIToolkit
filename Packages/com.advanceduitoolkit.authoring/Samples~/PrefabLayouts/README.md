# JSON prefab-layout samples

These examples exercise the retained JSON compatibility workflow. They are not hierarchy molds and do not include finished scene assets or control artwork.

Open **Tools → Advanced UI Toolkit → Compatibility → JSON Layouts**. Provide a destination, a Style Profile containing your own compatible prefabs and the appropriate schema/stylesheet inputs. Validate before generating.

| Files | Purpose |
| --- | --- |
| `WeaponPanel.integration.schema.json`, `NavalIntegration.styles.json` | Prefab-mapped screen structure and container styling |
| `WeaponPanel.prefab-layout.schema.json`, `NavalPrefabLayout.styles.json` | Supported layout overrides on prefab roots |
| `WeaponPanel.preview-changes.schema.json` | Changed schema input for comparing a tracked generation record |

Read [Schema guide](../../Documentation~/SCHEMA_GUIDE.md), [Prefab layout](../../Documentation~/LegacyIntegration/PREFAB_LAYOUT.md) and [Change preview](../../Documentation~/LegacyIntegration/CHANGE_PREVIEW.md) for setup, restrictions and expected behavior. Generation-record comparison is read-only; there is no Apply operation.

For the primary scene-capture workflow, follow [Layouts getting started](../../Documentation~/GETTING_STARTED.md).
