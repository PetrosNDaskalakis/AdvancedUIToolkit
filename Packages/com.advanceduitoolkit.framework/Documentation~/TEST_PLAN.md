# Advanced UI Toolkit v1.1.0 RC1 — manual verification plan

This is a **manual verification procedure**, not proof that the RC1 package passed in Unity. The compact distribution includes **no EditMode or PlayMode test assemblies, no Test Runner shortcut, and no synthetic benchmarks**. Record hands-on results for each supported Editor version; profile with Unity Profiler. See [RC1 release notes](RC1_RELEASE_NOTES.md).

## 1. Clean-package installation

- [ ] Create a new Unity project with uGUI available.
- [ ] Install `com.advanceduitoolkit.framework` from `package.json` through Package Manager.
- [ ] Confirm package version displays **1.1.0-rc.1**.
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
- [ ] Ordinary UI containers can be used as parents; selecting a prefab resolves to its nearest regular container.
- [ ] Prefab Text Override applies to one chosen root/child/inactive label, preserves source asset, and remains safe when source targets are removed/reordered.
- [ ] Native fallback control, prefab instance, and empty container creation each undo independently of unrelated edits.
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

## 6. Manual verification and Unity Profiler

1. Perform the workflows in sections 1–5 in a clean project and an upgraded project. Record failures and Unity Console messages.
2. Enter Play Mode and verify button callbacks, lifecycle enable/disable behavior, and re-instantiation without duplicate listeners.
3. Open **Window > Analysis > Profiler** and capture representative CPU Usage and GC Alloc measurements for Binder/Factory operations in your own UI scenes.
4. Build a player for the target platform and confirm the Editor assembly is excluded.
5. The compact UPM package intentionally does not include Test Runner suites or synthetic benchmarks.

## 7. Release evidence to record

| Field | Value |
| --- | --- |
| Unity version | Fill after test |
| OS | Fill after test |
| Advanced UI Toolkit version | 1.1.0-rc.1 |
| Package install | Pass / Fail |
| Weapon Panel | Pass / Fail |
| Settings Panel | Pass / Fail |
| Manual Editor workflows | Pass / Fail |
| Play Mode lifecycle | Pass / Fail |
| Player build | Pass / Fail |
| Known issues | Notes |

**Release gate:** do not call this version Unity-tested or production-ready until the checklist is actually executed in the target Editor and recorded.

See the [v1.1 release gates](V1_1_RELEASE_GATES.md) and [RC1 evidence checklist](RC1_EVIDENCE_CHECKLIST.md). After RC1, verify the manual gates, actual Unity builds, and publication requirements before tagging v1.1.0 Stable.
