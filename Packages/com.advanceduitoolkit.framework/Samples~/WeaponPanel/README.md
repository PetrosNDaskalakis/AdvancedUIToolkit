# Weapon Panel — Buttons (importable source sample)

**What you get:** two C# files that compile after import (`WeaponPanelExample.cs`, `ScreenBindingsSample.cs`) and a quick setup guide. This sample intentionally contains **no `.unity` scene or `.prefab` file**. It demonstrates API usage independently of generated application code.

1. Import this sample via **Window > Package Manager > Advanced UI Toolkit > Samples > Weapon Panel — Buttons > Import**.
2. Create a Canvas and Panel. Add `UIBindingRoot` to the Panel.
3. Create three children under the Panel named **exactly** `BTN_Arm`, `BTN_Fire`, `BTN_Load`; each named object must contain its own `UnityEngine.UI.Button`.
4. Open **Tools > Advanced UI Toolkit > Binder**, select the Panel, scan and validate. Generate/save the manifest. You may generate any production class; the imported demo uses `ScreenBindingsSample` instead.
5. Attach **WeaponPanelExample** to the **same** Panel GameObject as `UIBindingRoot`.
6. Play; clicking each button logs one corresponding line in the Console.
7. Disable/re-enable the Panel and confirm events restore without duplicates.

The hand-written `ScreenBindingsSample` is purely for a self-contained learning example. **Use generated bindings in actual project screens**, and avoid importing two copies of this sample under conflicting namespaces.
