# JSON bridge Editor API

[Compatibility index](../INDEX.md#json-compatibility) · [Installation](../INSTALLATION.md)

This reference documents the retained JSON compatibility workflow in Authoring. It does not describe hierarchy molds.

Namespace: `AdvancedUIAuthoring.Integration.Editor`.
Assembly: `AdvancedUIAuthoring.Integration.Editor` (Editor platform only).
A custom consumer asmdef must explicitly reference that assembly and the Authoring/Toolkit assemblies for types it uses.

```csharp
UIAuthoringBridgeResult check = UIAuthoringToolkitBridge.Validate(recipe, canvasRect);
if (!check.Succeeded) { Debug.LogError(check.Error); return; }

UIAuthoringBridgeResult build = UIAuthoringToolkitBridge.Generate(recipe, canvasRect);
if (!build.Succeeded) { Debug.LogError(build.Error); return; }

// A separate operation after hierarchy creation. It may write C# and trigger compilation.
UIEditorBindingResult bindings = UIAuthoringToolkitBridge.SyncBindings(recipe, build.BindingRoot);
Debug.Log(bindings.Status + ": " + bindings.Message);
```

Use `UIAuthoringToolkitBridge.InspectBindings(rootGameObject)` for read-only Binder status. Check `Status`, not only `Succeeded`: `PendingRemoval` is not evidence that source was updated. Sync never passes accessor-removal approval to Toolkit.

`CreatedObjects` maps exact schema IDs to generated GameObjects for this call only. It is empty on failure and during validation. `BindingRoot` is the generated native container identified by `recipe.bindingRootId`, or null when that field is empty. Do not use this temporary map as a persistent regeneration identity database.

All methods must run synchronously on the Unity Editor main thread. Generation refuses Play Mode, compilation and import, and blocks reentrant bridge generation. Generation uses public Toolkit batch operations for linked prefabs and Authoring's native generator for containers, under an outer Undo group and nestable automation suspension. No private Toolkit APIs or internal type reflection are used. Prefab sizing uses public UIAuthoringRootLayoutPlan snapshots; no style DTO reflection remains. Source and live target conflicts are validated, and layout application is included inside the outer hierarchy transaction.

No generated C# file transaction is included in scene rollback. No methods edit Style Profiles, source prefabs or the portable JSON assets.

The bridge does not expose root-layout overrides for already generated hierarchies. Generate creates new controls; the public Authoring plan API is available for independent consumers that own their targets and transactions.

## Generation records and read-only previews

```csharp
// Immediately after a successful generation:
UIAuthoringPendingGeneration pending = UIAuthoringChangePreview.BeginTracking(recipe, parent, build);

// Later, after the user saves the scene and before the window's pending state is discarded:
UIAuthoringGenerationRecord record;
string error;
if (!UIAuthoringChangePreview.CaptureBaseline(pending, out record, out error))
    Debug.LogError(error);

// This reads current recipe + loaded scene state. It does not generate, delete or write bindings.
UIAuthoringChangePreviewResult report = UIAuthoringChangePreview.Preview(record);
```

Capture requires an unchanged recipe input stamp, live generated references, a saved clean scene and resolvable object references. It creates a new asset at a unique path under Assets/Editor/UIAuthoringRecords; it never replaces an older record. All generated nodes must remain in the original scene with the generated names and parents. Baseline capture intentionally adopts the current saved visual/component state, including any compatible edits made before capture.

Preview uses `record.Recipe`, not another recipe currently selected elsewhere. `Rows` contain the proposed source change and an independent `HasConflict` flag. `Errors` are blocking record/scene/source problems; `Notices` report shared dependency changes. `HasConflicts == false` is not permission or proof of safe regeneration. There is no Apply API.

Both operations run synchronously on the Unity Editor main thread. The service does not open missing scenes or rebuild layout. Unsupported serialized properties fail capture or produce a conflict rather than being silently omitted. Binder manifests and computed layout geometry are deliberately outside UI fingerprint ownership; use Inspect Binder separately. Custom extension validators must continue to obey the non-mutating validation contract.
