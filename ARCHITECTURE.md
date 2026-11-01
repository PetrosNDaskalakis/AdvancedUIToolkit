# Architecture

This page describes the architecture observed in the **uploaded source snapshot**, not a proposed future design.

## Package boundaries

```text
com.advanceduitoolkit.framework
  Runtime/    UIBindingRoot, IUIBindings, UIView<TBindings>, UIEventScope,
              UIStyleProfile and related runtime types
  Editor/     Binder, Library, code generation, component registry,
              editor integration and batch APIs

com.advanceduitoolkit.authoring
  Editor/     Layouts wizard, hierarchy mold capture/build, optional
              JSON authoring and compatibility tools
```

The Framework can be installed independently. Authoring declares a dependency on Framework version `1.2.0-preview.4` and contains Editor-only authoring tools. Both packages declare Unity 2022.3 and `com.unity.ugui` in their manifests.

## Binder flow

```text
Scene UI hierarchy (BTN_Fire, etc.)
      ↓ Editor scan / validation
UIBindingRoot serialized manifest
      + generated C# class implementing IUIBindings
      ↓ Unity script compilation
UIView<TBindings>.Awake()
      ↓ TBindings.Initialize(root)
Typed references (UI.Fire)
      ↓ UIEventScope
Managed event listeners and view hooks
```

The generated bindings resolve serialized component references via `UIBindingRoot`, rather than repeatedly searching the scene at runtime. `UIView<TBindings>` initializes once and manages suspension, resumption and disposal of registered listeners.

## Library flow

```text
Saved UI prefabs + UIStyleProfile
      ↓ Library Editor window
Prefab instance under selected UI container
      ↓ Binder (optional)
Typed C# accessor
```

The Library is the visual prefab placement workflow. It is distinct from layout molds.

## Layouts flow

```text
Eligible RectTransform container hierarchy
      ↓ Mold capture + validation
Saved hierarchy mold
      ↓ Mold builder / Layouts wizard
New independent container hierarchy
      ↓ Library
Prefab controls added separately
```

The mold capture excludes prefab branches and non-eligible branches. The implementation includes validation, reference remapping and safeguards for invalid structures. Newly built hierarchies are not live-linked to their source mold. This is a structural reuse mechanism, not a prefab replacement or a full scene serializer.

## Compatibility boundary

The Authoring package also includes JSON authoring and integration tooling under **Tools → Advanced UI Toolkit → Compatibility**. These remain separate from the recommended Library → Layouts → Binder workflow.

## Verification boundary

This architecture was checked against package manifests, runtime source, editor entry points and mold test names. The uploaded archive was **not compiled or executed in Unity in this environment**. A production-readiness claim requires Unity compilation, automated tests, manual authoring tests and validation in a real target project.

Detailed historical documents are archived in [`Document~/`](Document~/).
