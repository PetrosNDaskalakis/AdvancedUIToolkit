# Framework installation

[Documentation index](INDEX.md) · [Migration](MIGRATION.md)

## Requirements

The package declares Unity 2022.3 as its minimum and `com.unity.ugui` as a dependency. Check the exact Unity version, render pipeline and player targets used by your project; the minimum declaration is not a tested-version matrix. TextMeshPro integration is optional and does not add a mandatory package dependency.

## Embedded package

1. Save the project and close Unity.
2. Place the complete `com.advanceduitoolkit.framework` directory under the project's `Packages` directory. The package's `package.json` must be directly inside that directory.
3. Preserve every supplied `.meta` file. Keep your project's dependency manifest and lock file.
4. Open Unity, wait for import and compilation, and check the Console.
5. Open **Tools → Advanced UI Toolkit → Library** and **Tools → Advanced UI Toolkit → Binder**.

## Install from disk

Keep the package folder at a durable location outside `Assets`. In **Window → Package Manager**, choose **+ → Add package from disk** and select the Framework `package.json`. Keep that folder available: the project references it. Do not install both a local and embedded copy.

## Add Layouts

Install the matching [Authoring package](../../com.advanceduitoolkit.authoring/Documentation~/INSTALLATION.md) to enable **Layouts**. Framework also works independently.

## Project code and samples

Use namespace `AdvancedUIToolkit`. Runtime game assemblies reference `AdvancedUIToolkit.Runtime`; Editor extensions additionally reference `AdvancedUIToolkit.Editor`. Never reference an Editor assembly from a player assembly.

Save project prefabs, Style Profiles, generated bindings and views under `Assets`, outside the installed package. Package Manager exposes four documented samples. Start with [Getting started](GETTING_STARTED.md), then follow the [testing guide](TESTING.md).

## Upgrade

Replace the complete package directory rather than merging it over an older installation. Keep project assets and generated code. Remove obsolete duplicate framework sources only after identifying them; preserve project scripts that use the API. Older `UICore` installations need the [source migration](MIGRATION.md).
