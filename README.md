[README.md](https://github.com/user-attachments/files/33971741/README.md)
# Advanced UI Toolkit

**Build Unity uGUI interfaces faster, with fewer manual references and more reusable components.**

Advanced UI Toolkit is a modular, editor-focused framework that helps Unity developers **create, organize, bind, and maintain uGUI interfaces**. It combines a reusable UI component library with automatic component discovery, strongly typed C# binding generation, and validation tools.

It **does not replace Unity's UI system**. Your Canvas, Buttons, Toggles, prefabs, and MonoBehaviours continue working as normal—it automates the repetitive setup around them.

**Current release:** `1.0.0-rc.10` (release candidate) · **Unity:** 2022.3+ · **UI:** uGUI · **Package ID:** `com.uicore.framework`

> **Project status:** This is a release candidate, not yet a production-verified stable release. Source publication and the final open-source license are subject to ownership/permission review.

<!-- Optional repository cover image: Documentation~/Images/00-cover.png -->

## The problem with the usual Unity UI workflow

Unity's built-in uGUI and Prefab tools are capable, but UI-heavy projects often involve a lot of repeated setup:

- Declaring dozens of `Button`, `Text`, `Toggle`, and other component fields in C#.
- Dragging every reference from the Hierarchy into the Inspector.
- Repeating the same component designs and setup across screens.
- Manually fixing references and event subscriptions as interfaces change.
- Keeping different developers' naming, organization, and code patterns consistent.

The effort increases in games and simulators with multiple menus, stations, and complex control panels. Unity already provides prefabs and layout systems; the missing piece addressed here is **a consistent, automated workflow for discovering and using those UI components in code**.

## How Advanced UI Toolkit helps

| Tool | What it does |
| --- | --- |
| **UI Library / Factory** | Creates named uGUI controls or instances of your team's reusable prefab designs. |
| **Binder** | Scans a selected UI root and recognizes components using naming conventions such as `BTN_Fire`. |
| **C# Generator** | Creates documented, strongly typed references such as `UI.Fire`—no manual Inspector field assignments. |
| **Style Profiles** | Organizes separate component libraries for menus, stations, and other visual styles, with inheritance. |
| **Validation & Rename Support** | Reports naming conflicts, missing references, and changes that could break generated code. |
| **Manual / Automatic Generation** | Lets you choose when C# files change and Unity recompiles. |

**The result:** less repetitive wiring, more consistent UI prefabs, and more time spent on game functionality and interface design.

## Quick start: build your first bound UI

Follow this flow once to understand the main workflow:

**Install → Create UI → Name controls → Bind → Generate → Use in C#**

### 1. Install the package

Add Advanced UI Toolkit using Unity's **Package Manager → + → Add package from disk**, selecting the package's `package.json`. Alternatively, use it as an embedded package at `Packages/com.uicore.framework/`.

Open **Tools → Advanced UI Toolkit → Binder** and **Tools → Advanced UI Toolkit → Factory**.

> **Screenshot 01 — Installation**  
> *Add your image here:* `Documentation~/Images/01-installation.png`  
> Capture: Unity Package Manager showing Advanced UI Toolkit and its available samples.

### 2. Create a panel and its controls

Create a Canvas and a panel called `WeaponPanel`. Add a `UIBindingRoot` component to that panel. Create buttons through **GameObject → Advanced UI Toolkit → Button** or through **Factory**.

Give the button GameObjects clear names following the prefix convention:

```text
Canvas
└── WeaponPanel  [UIBindingRoot]
    ├── BTN_Arm   [Button]
    ├── BTN_Fire  [Button]
    └── BTN_Load  [Button]
```

> **Screenshot 02 — Factory and Hierarchy**  
> *Add your image here:* `Documentation~/Images/02-factory-hierarchy.png`  
> Capture: Factory on one side and the `WeaponPanel` hierarchy with three named buttons.

### 3. Let Binder discover the references

Select `WeaponPanel`, then open **Tools → Advanced UI Toolkit → Binder**. Binder lists the recognized components, pending changes, and validation messages.

`BTN_Arm` becomes the C# property `Arm`; `BTN_Fire` becomes `Fire`.

> **Screenshot 03 — Binder**  
> *Add your image here:* `Documentation~/Images/03-binder-scan.png`  
> Capture: Binder with `WeaponPanel` selected, the three components listed, and no errors.

### 4. Preview and generate the C# class

Click **Preview C#** to inspect the proposed output. Then choose **Generate Bindings** (or **Generate Changes** for an existing root).

A generated `WeaponPanelBindings` class will expose cached references to your buttons. By default, generated files live under `Assets/AdvancedUIToolkit/Generated/`.

> **Screenshot 04 — Generated bindings**  
> *Add your image here:* `Documentation~/Images/04-generated-code.png`  
> Capture: the C# preview beside the generated `WeaponPanelBindings.cs` file in the Project window.

### 5. Use the generated references

Create a script on the same GameObject as `UIBindingRoot`:

```csharp
using UnityEngine;
using UICore;
using Game.UI.Generated;

/// <summary>Handles the weapon panel's three buttons.</summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    /// <summary>Registers managed button callbacks once.</summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Arm, OnArm);
        Events.BindClick(UI.Fire, OnFire);
        Events.BindClick(UI.Load, OnLoad);
    }

    /// <summary>Handles the Arm button.</summary>
    private void OnArm() => Debug.Log("Arm pressed");

    /// <summary>Handles the Fire button.</summary>
    private void OnFire() => Debug.Log("Fire pressed");

    /// <summary>Handles the Load button.</summary>
    private void OnLoad() => Debug.Log("Load pressed");
}
```

No serialized button fields. No manual reference assignments. Managed button listeners are cleaned up by the view lifecycle.

> **Screenshot 05 — Working result**  
> *Add your image here:* `Documentation~/Images/05-playmode-test.png`  
> Capture: the working panel in Play Mode with the corresponding Console log messages.

### 6. Reuse your designs with Style Profiles

Save a custom `SonarButton.prefab`, create a **Style Profile**, and register it in the **UI Library**. A Sonar Station can use green tactical controls, while the Main Menu uses an entirely different design. Both still use the same Binder workflow.

> **Screenshot 06 — Reusable library**  
> *Add your image here:* `Documentation~/Images/06-style-profiles.png`  
> Capture: two different Style Profiles or station designs using their own prefab controls.

For a complete walkthrough, see the [Developer Handbook](Documentation~/DEVELOPER_HANDBOOK.md).

## Documentation

| Guide | Contents |
| --- | --- |
| [Installation](Packages/com.advanceduitoolkit.framework/Documentation~/INSTALLATION.md) | Requirements, UPM installation, upgrades, and initial verification |
| [Developer Handbook](Packages/com.advanceduitoolkit.framework/Documentation~/DEVELOPER_HANDBOOK.md) | Daily workflows, naming rules, prefabs, Style Profiles, generation modes, and templates |
| [Architecture](Packages/com.advanceduitoolkit.framework/Documentation~/ARCHITECTURE.md) | Framework modules, Editor/runtime separation, binding manifests, and data flow |
| [Technical Overview](Packages/com.advanceduitoolkit.framework/Documentation~/TECHNICAL_OVERVIEW.md) | Design decisions, generation pipeline, performance considerations, and limitations |
| [API Reference](Packages/com.advanceduitoolkit.framework/Documentation~/API_REFERENCE.md) | `UIBindingRoot`, `UIView<T>`, `UIEventScope`, Style Profiles, and examples |
| [Troubleshooting](Packages/com.advanceduitoolkit.framework/Documentation~/TROUBLESHOOTING.md) | Common compilation, generation, prefab, Undo/Redo, and Editor issues |
| [Documentation Index](Packages/com.advanceduitoolkit.framework/Documentation~/INDEX.md) | All guides in one place |

## Important notes

- **Reference binding is not data binding.** The toolkit exposes UI components in code; it does not automatically synchronize game data with them.
- **Manual mode** is recommended for building large interfaces because it defers generated C# changes until you choose to apply them.
- The public product name is **Advanced UI Toolkit**. The existing C# namespace remains `UICore` and the UPM identifier remains `com.uicore.framework` to preserve compatibility.
- This repository/package does not currently include the proposed JSON UI Schema Builder, runtime MVVM framework, or universal UI system.

## License and contributions

A public open-source license has **not yet been selected or approved**. Before publishing the repository, confirm ownership rights and add an explicit `LICENSE` file. See the package's `PUBLICATION_CHECKLIST.md` for the review process.

---

**Advanced UI Toolkit — Create once. Bind automatically. Reuse everywhere.**
