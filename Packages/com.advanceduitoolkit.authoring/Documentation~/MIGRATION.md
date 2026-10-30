# Layouts migration

[Documentation index](INDEX.md) · [Installation](INSTALLATION.md)

## Package pair and preserved assets

The current pair is Framework `1.2.0-preview.3` and Authoring `0.5.0-preview.8`. Save scenes and drafts, close Unity and replace the complete package directories. Keep project assets, generated code and all supplied `.meta` files. Retain your project's manifest/lock configuration and check the Console after reopening.

Authoring includes the former standalone Integration implementation with its assembly identity preserved. Remove the old Integration package during consolidation to avoid duplicate classes/assemblies. Do not delete saved setups, recipes, records or user scenes merely because the package location changed.

## Menus and workflow

The primary tools are **Library**, **Layouts** and **Binder** under **Tools → Advanced UI Toolkit**. Older instructions may call Library "Factory" or Binder "Workspace". Layouts is supplied by Authoring and keeps its state independent of Library.

**Create New Layout** captures a hierarchy; **Load Layout** selects saved molds. Existing JSON creation/loading remains under **Compatibility → JSON Layouts**. Saved setup Inspectors open a working copy in that path. Native Authoring and Legacy Integration Preview remain compatibility tools; record preview is read-only.

## Mold formats

Format-2 molds remain compatible; this documentation update does not require recapture. Format-1 molds are preserved but cannot be generated because their prefab boundaries were not stored. Recapture from the original hierarchy with prefab links intact and save a new asset. Editing the version number or recapturing flattened output cannot recover lost provenance. Existing scene output is not converted or changed automatically.

## Source control and rollback

Keep package updates and project asset/generated-code changes reviewable. To roll back, restore the previous compatible package pair and corresponding project state from source control. Scene Undo does not restore generated C# files. Confirm serialized references, generated classes and a representative scene before continuing normal development.
