# Layouts API reference

[Documentation index](INDEX.md) · [Architecture](ARCHITECTURE.md)

The primary mold APIs use namespace and assembly `AdvancedUIAuthoring.Integration.Editor`. Call them only from Editor code on Unity's main thread. Reference the installed Authoring and Framework Editor assemblies in your Editor asmdef as required by the types you use.

## `UIHierarchyMoldCapture`

| Method | Result |
| --- | --- |
| `IsValidSource(GameObject root)` | Tests exact source eligibility; does not substitute an ancestor |
| `TryCapture(GameObject root, out UIHierarchyMold mold, out string error)` | Returns a transient mold on success or an error on failure |

The caller owns the transient mold and must destroy it when finished unless deliberately transferring ownership into an asset workflow. Capture leaves source scene objects unchanged. Eligibility excludes prefab and non-RectTransform branches; see [Hierarchy molds](HIERARCHY_MOLDS.md).

## `UIHierarchyMoldBuilder`

| Method | Purpose |
| --- | --- |
| `ValidateMold(UIHierarchyMold mold, out string error)` | Validate stored data and installed component types |
| `Validate(UIHierarchyMold mold, RectTransform parent, string rootName, out string error)` | Validate mold, target and requested name |
| `TryCreate(UIHierarchyMold mold, RectTransform parent, string rootName, out GameObject root, out string error)` | Create an independent hierarchy with grouped scene Undo and cleanup on failure |

Check the boolean and error result of every operation. Preflight does not guarantee that arbitrary custom component callbacks or changed field payloads can be restored. Generated C# writes are separate from the scene transaction.

## Capture and create example

Place this helper in an Editor assembly. Pass an eligible source and destination; it returns the new hierarchy and disposes its temporary capture.

```csharp
using System;
using AdvancedUIAuthoring.Integration.Editor;
using UnityEngine;

public static class LayoutExample
{
    public static GameObject CopyLayout(GameObject source, RectTransform target, string name)
    {
        if (!UIHierarchyMoldCapture.TryCapture(source, out UIHierarchyMold mold, out string error))
            throw new InvalidOperationException(error);

        try
        {
            if (!UIHierarchyMoldBuilder.Validate(mold, target, name, out error))
                throw new InvalidOperationException(error);
            if (!UIHierarchyMoldBuilder.TryCreate(mold, target, name, out GameObject result, out error))
                throw new InvalidOperationException(error);
            return result;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mold);
        }
    }
}
```

For user-facing tools, expose the captured omission report before creation so cleared references and excluded branches are reviewable.

## Data and window entry points

`UIHierarchyMold` stores the format version, nodes and omissions. Current format is 2; `MaximumNodeCount` is 10,000 and `MaximumDepth` is 256. Treat loaded assets as source data and use a working copy when editing.

`UIAuthoringLayoutsWindow.Open()` opens Layouts. `OpenMold` opens a saved mold. `OpenSetup` routes a saved JSON setup to its compatibility workflow, and `OpenCompatibility()` opens JSON Compatibility. See the [window source](../Editor/Integration/UIAuthoringLayoutsWindow.cs) for overloads and argument types.

## Compatibility APIs

- [JSON/native extension API](EXTENSIONS.md)
- [Root layout planning API](ROOT_LAYOUT_API.md)
- [Framework bridge and Binder integration](LegacyIntegration/API.md)
- [Generation records and read-only change preview](LegacyIntegration/CHANGE_PREVIEW.md)

These APIs do not turn molds into schema-driven layouts or provide an Apply operation for existing hierarchies.
