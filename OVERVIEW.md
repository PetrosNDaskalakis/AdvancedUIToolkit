# Overview

Advanced UI Toolkit (A.U.T.) is a **Unity uGUI editor and runtime toolkit** for reducing repetitive screen construction. It targets human Unity developers first; AI-generated code is optional, not a prerequisite.

## The problem

UI work often repeats the same steps: place prefabs, recreate similar container hierarchies, locate components from gameplay scripts, and maintain event listeners. A.U.T. offers explicit tools for those operations rather than replacing Unity's native UI authoring.

## The four parts

**Library (Framework editor)** places registered prefab-based controls from a selected style profile. It is intended to preserve the project's visual conventions rather than invent new UI designs.

**Layouts (Authoring editor)** captures eligible RectTransform container structures into reusable *hierarchy molds*. A mold can rebuild the container skeleton independently. It does **not** serialize prefab-control branches into the mold; populate those using Library after reconstruction.

**Binder (Framework editor)** scans naming-convention UI elements and produces a serialized binding manifest plus a generated C# binding class. `BTN_Fire`, for example, can become `UI.Fire`.

**Runtime (Framework)** provides `UIBindingRoot`, `IUIBindings`, `UIView<TBindings>` and `UIEventScope` to resolve references and manage listeners through the screen lifecycle.

## Typical workflow

1. Build or recreate the layout skeleton.
2. Add saved prefabs with Library.
3. Add `UIBindingRoot`, name bindable controls and generate bindings with Binder.
4. Implement screen behavior in a `UIView<TBindings>` subclass.
5. Test the result in Play Mode and in the target build.

## What A.U.T. is not

It is not a UIElements/UI Toolkit replacement, not a general data-reactivity system, and not an autonomous AI code generator. Optional JSON authoring compatibility features exist but are not the recommended first workflow.

## Read next

[Quick Start](QUICKSTART.md) · [API Reference](API.md) · [Architecture](ARCHITECTURE.md)
