# Migrating from Assets/AdvancedUIToolkit v0.8.2 to UPM v0.9

**Important:** You have tested older versions inside `Assets/AdvancedUIToolkit`. Moving scripts into a package changes asset paths. If you lose the original `.meta` GUIDs for `UIBindingRoot.cs`, `UIStyleContext.cs`, `UIStyleProfile.cs`, etc., scenes or prefabs may show **Missing Script**. Back up the entire project (or commit it) first.

## Safest migration for an existing project (embedded package)

1. **Close Unity and back up your project.**
2. Extract the v0.9 package folder into `<YourProject>/Packages/com.advanceduitoolkit.framework/`.
3. **Before reopening Unity**, copy the existing `.cs.meta` files from `Assets/AdvancedUIToolkit/Runtime/` onto the matching new source files in `Packages/com.advanceduitoolkit.framework/Runtime/`, **replacing** the `.meta` files shipped with v0.9. Keep Unity GUID values unchanged. Preserve any existing `.meta` IDs for Editor scripts if your project serialized references to them.
4. Delete **only** the now-duplicate old source folders `Assets/AdvancedUIToolkit/Runtime/` and `Assets/AdvancedUIToolkit/Editor/` and their `.meta` files. Do **not** delete `Assets/AdvancedUIToolkit/Generated`, style profiles, prefabs, or other project-created data.
5. Open Unity. Check for missing-script warnings, assembly compilation errors, and package import errors.
6. Keep your previously generated `ScreenBindings.cs` where it is; it will reference `AdvancedUIToolkit.Runtime` through `IUIBindings` and `UIBindingRoot`.
7. If your own gameplay assembly uses an `.asmdef`, add a reference to `AdvancedUIToolkit.Runtime` in that asmdef. Legacy `Assembly-CSharp` projects normally receive auto-references.
8. Reopen **Tools > Advanced UI Toolkit > AutoBinder**, regenerate one screen, and rerun the three-button test, Rename Assistant, Diagnostics, Automation, and Component Library checks.
9. Only after this migration succeeds, try **Add package from Git URL** in a separate clean validation project. Remote Git installations do not preserve old `Assets` script GUIDs automatically; a documented upgrade path must be tested.

### Alternative for a new/empty project

Simply import the UPM package. No old GUID migration is necessary.

### Do not run two copies

Never leave both `Assets/AdvancedUIToolkit/{Runtime,Editor}` and the `Packages/com.advanceduitoolkit.framework/{Runtime,Editor}` script directories active in the same project. Duplicate types and assembly boundaries will cause compilation failures.

### User data placement

- Runtime/editor framework code: `Packages/com.advanceduitoolkit.framework/`
- Generated bindings: `Assets/AdvancedUIToolkit/Generated/` (existing), or another project `Assets/...` output folder
- Library profiles and station prefabs: project `Assets/...` (never inside PackageCache)

### UPM vs copy-one-folder

A single package directory still fulfills the portability goal. For Git URL installations the package is managed by Unity and effectively read-only; **make changes in an embedded package or source clone**, then commit and tag them.
