# Layouts installation

[Documentation index](INDEX.md) · [Migration](MIGRATION.md)

## Requirements

Authoring `0.5.0-preview.8` depends on Framework `1.2.0-preview.3` and uGUI. Both manifests declare Unity 2022.3 as the minimum. Authoring and its Integration assembly run only in the Editor.

## Install the package pair

1. Save the project and close Unity.
2. Place the complete Framework directory at `Packages/com.advanceduitoolkit.framework`.
3. Place the complete Authoring directory at `Packages/com.advanceduitoolkit.authoring`.
4. Keep their `.meta` files, and retain your project's own `manifest.json` and `packages-lock.json`.
5. Reopen Unity, wait for compilation, and open **Tools → Advanced UI Toolkit → Layouts**.

For local disk installation, add Framework first and then Authoring through Package Manager's **Add package from disk**, selecting each package's `package.json`. Keep both local directories available. Do not add duplicate embedded copies.

The old standalone Integration package is included in Authoring. Remove that older package when migrating, using the [migration guide](MIGRATION.md). Never copy package scripts into `Assets` alongside their installed package copies.

## First checks

Follow [Getting started](GETTING_STARTED.md) in a disposable scene. Save molds under your project's `Assets` directory. Keep custom component scripts installed in any destination project that will load the molds.

The optional test assembly is ignored under `Tests~` until explicitly installed. See [Testing](TESTING.md) for setup. Ordinary mold use needs no JSON file, saved setup or generation baseline.
