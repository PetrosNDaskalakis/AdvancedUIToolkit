# Framework troubleshooting

[Documentation index](INDEX.md) · [Testing](TESTING.md)

| Symptom | Check and action |
| --- | --- |
| Library or Binder menu missing | Wait for compilation; resolve Console errors; verify package location and remove duplicate installations |
| Layouts menu missing | Install the matching Authoring package; Layouts is optional and is not shipped in Framework alone |
| Library has no entries | Choose a saved Style Profile, confirm registered prefabs, clear search/kind filters and check Show Inherited |
| Selected entry details disappeared | The selected entry may be hidden by the current filter; reset it to reveal the details |
| Wrong creation destination | Check pinned versus follow-selection mode, inspect the scene/hierarchy context and use Locate |
| Target rejected | Select the exact ordinary scene RectTransform outside prefab instances and Prefab Mode; do not rely on ancestor fallback |
| Creation disabled | Wait for import/compilation and leave Play Mode; check the selected profile, template and target |
| Binding not discovered | Check naming prefix, valid identifier, matching component and whether a nested binding root owns it |
| Ambiguous match | Ensure the named object has one intended matching component in its binding scope |
| Generated property missing | Confirm the correct root and output settings; review pending changes and generate in Manual mode |
| Generation refused | Read Binder's issue details for duplicate names, missing references, output collisions or edited generated files |
| Missing namespace after migration | Replace old UICore namespace/assembly references according to Migration, then regenerate deliberately |
| Duplicate callbacks | Register managed events once in OnInitialize; remove repeated or unmanaged subscriptions |
| UI stops receiving managed events | Confirm the view is enabled and initialized; inspect initialization exceptions and avoid hiding base lifecycle methods |
| Text override rejected | Re-select a valid source label if the prefab changed, or disable the override |

Scene Undo does not restore generated C# files. When reverting a hierarchy edit, inspect generated files and consuming code as well. Do not delete or overwrite an unrelated file to bypass generation ownership checks.

For prefab visual or layout differences, inspect ordinary Unity prefab overrides, fonts, layout components and render-pipeline behavior. Library previews are an Editor aid, not a guarantee of player rendering.

Report the Unity version, both package versions when applicable, Console error, reproduction steps, generation mode and a minimal scene/prefab configuration. See [Contributing](../CONTRIBUTING.md).
