# Advanced UI Toolkit: Code Generation Modes

Open **Tools > Advanced UI Toolkit > Binder > Show More > Automation**.

## Manual (recommended for large UI authoring sessions)

- Advanced UI Toolkit watches and detects safe pending additions/renames but **does not write generated `.cs` files** automatically.
- You may create or rename many prefab controls before generating.
- Click **Refresh Now** to update the pending list, then **Generate Pending Changes (N)** to write all approved-safe binding files in a single batch.
- Each row also supports **Generate Safe Changes**, which immediately writes that individual binding class.
- Missing references, unsafe prefabs, invalid names, conflicting output paths, or edited generated files still require manual review in **Binder**. Valid deletions are supported in both modes; consuming code that uses deleted accessors must be updated.
- A UI root must have a saved generation profile from a previous manual generation. Unconfigured roots cannot be generated in bulk.

## Automatic

- After a hierarchy change, Advanced UI Toolkit checks the registered roots and regenerates only validated safe changes.
- Unity may import and compile updated C# after each generated change. Choose Manual when editing many UI elements at once.

## Important: Unity's own compilation settings

These options control **Advanced UI Toolkit's generation**, not Unity's global script compilation. The Unity Editor may still compile when you edit ordinary C# files or when other tools write scripts.

To also stop Unity from auto-refreshing unrelated changed files, configure **Edit > Preferences > Asset Pipeline > Auto Refresh** (the exact options vary by Unity version). You can manually refresh through **Assets > Refresh** when ready. Avoid changing global Unity preferences from Advanced UI Toolkit, because this can affect other workflows and package imports.

## Compatibility

- Runtime API and generated C# class formats are unchanged.
- Existing UIBindingRoot manifests and style profiles are preserved.
- No regeneration is needed merely to install this release.
- Mode is stored in the developer's Unity Editor preferences, not project assets.
