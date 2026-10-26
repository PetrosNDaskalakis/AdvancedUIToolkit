# Advanced UI Toolkit v0.9.9 release candidate verification plan

This document is a **test procedure**, not evidence that the package has passed in Unity. Record your results for every Unity version you support. v0.9.8 intentionally changes documentation and samples only; runtime behavior is inherited from v0.9.7.

## 1. Clean-package installation

- [ ] Create a new Unity project with uGUI available.
- [ ] Install `com.advanceduitoolkit.framework` from `package.json` through Package Manager.
- [ ] Confirm package version displays **0.9.9**.
- [ ] Confirm **Tools > Advanced UI Toolkit > Binder** and **Tools > Advanced UI Toolkit > Factory** open.
- [ ] Confirm no Editor code is referenced by player runtime builds.
- [ ] If your game uses asmdefs, verify a game asmdef can reference `AdvancedUIToolkit.Runtime`.

## 2. Essential regression checks

- [ ] Create `BTN_Arm`, `BTN_Fire`, `BTN_Load` and generate class + root manifest.
- [ ] A `UIView<TBindings>` script reads all three generated properties.
- [ ] Button events fire exactly once per click.
- [ ] Disabling a panel suspends Advanced UI Toolkit-managed listeners; re-enabling resumes them once.
- [ ] A nested `UIBindingRoot` owns its own entries and is not included in its parent's manifest.
- [ ] Rename a GameObject while preserving its accessor; verify existing code still compiles.
- [ ] Intentionally rename an accessor; review generated diffs and update consuming code.
- [ ] Open a prefab in Prefab Mode and perform safe generation; instance generation protection works.

## 3. Factory and style libraries

- [ ] Create a profile and assign a linked Button prefab.
- [ ] Create controls under a `UIStyleContext` and confirm the correct style resolves.
- [ ] Add inherited base profile, override one component, and verify inheritance.
- [ ] Factory names duplicate controls uniquely.
- [ ] Preview shows image or a documented fallback for unsupported UI shaders.

## 4. Generator and automation

- [ ] Choose **Manual**; add several controls without triggering Advanced UI Toolkit-generated `.cs` updates.
- [ ] Click **Generate Pending Changes**; verify output and manifest.
- [ ] Preview generated C# without writing files.
- [ ] Import `Samples~/CodeGeneration/ExampleScript.cs` and test a custom template.
- [ ] Confirm generated members include XML summaries and the template is not overwritten.
- [ ] Attempt an output collision and confirm the generator blocks unsafe overwrite.

## 5. Samples

- [ ] Import **Weapon Panel — Buttons**; the sample source compiles without a generated game binding type.
- [ ] Set up its required hierarchy and regenerate the manifest; clicks reach the Console.
- [ ] Import **Settings Panel — Controls**; source compiles with legacy uGUI types.
- [ ] Create named Slider/Toggle/InputField/Dropdown/Text under a root and generate its manifest.
- [ ] Volume, Mute, PlayerName and Quality callbacks update the demo Status label.
- [ ] Import **Station Style Libraries**; follow the guide with project-owned prefab assets.

## 6. Unity Test Runner

1. Open **Window > General > Test Runner**. Install Unity Test Framework if absent.
2. If package tests do not appear, add `"testables": ["com.advanceduitoolkit.framework"]` to the project's `Packages/manifest.json` (preserve other entries).
3. Run all package **EditMode** tests and record outcomes.
4. Run all package **PlayMode** tests and record outcomes.
5. Run **Workspace > Performance** and copy the report as a baseline; don't conflate synthetic timings with frame times.

## 7. Release evidence to record

| Field | Value |
| --- | --- |
| Unity version | Fill after test |
| OS | Fill after test |
| Advanced UI Toolkit version | 0.9.9 |
| Package install | Pass / Fail |
| Weapon Panel | Pass / Fail |
| Settings Panel | Pass / Fail |
| EditMode tests | Pass / Fail + count |
| PlayMode tests | Pass / Fail + count |
| Player build | Pass / Fail |
| Known issues | Notes |

**Release gate:** do not call this version Unity-tested or production-ready until the checklist is actually executed in the target Editor and recorded.

See [RELEASE_CANDIDATE_099.md](RELEASE_CANDIDATE_099.md) for current release gates and the required UX simplification stage before v1.0.
