# Component extensibility — v1.1.5

Advanced UI Toolkit can now accept user-defined **Editor-registered binding component prefixes** without changing its Runtime assembly or built-in naming rules. The API is in `AdvancedUIToolkit.Editor` and requires an Editor assembly reference to `AdvancedUIToolkit.Editor`. The custom component itself must be in a runtime-accessible assembly referenced by the generated code's assembly.

## Register a component

```csharp
using System;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CustomBindingRegistration
{
    static CustomBindingRegistration()
    {
        string error;
        bool accepted = AdvancedUIComponentRegistry.TryRegister(
            "RAD", typeof(RadarDisplay), out error);
        if (!accepted) Debug.LogError(error);
    }
}
```

`RadarDisplay` is an example of a **public Component in a runtime assembly**, not a type supplied by this package. A GameObject named `RAD_Contacts` containing exactly one `RadarDisplay` on itself or a uniquely matching descendant becomes a binding candidate. Its generated type is `RadarDisplay`'s fully qualified C# type and the generated property is `Contacts`. Existing name prefixes retain their mappings. External types must be public, concrete, nonnested and nongeneric so the generator can emit a valid C# type name.

Registration is in-memory, per Editor domain. Re-register in `[InitializeOnLoad]` or `[InitializeOnLoadMethod]`; the same mapping can be registered repeatedly. Prefixes are case-sensitive, 2–8 characters, ASCII uppercase letters/digits, beginning with a letter; collisions are rejected. There is **no unregistration method** in v1.1.5, to prevent accidental accessors disappearing in automatic generation.

The framework does not automatically add external component types or assembly references to the consuming gameplay assembly. If generated bindings compile in an assembly definition, that asmdef must reference the assembly defining the custom Component. Editor-only Components must not be registered. Binder, diagnostics, nested binding scopes and reserved property names still enforce the existing rules. If the external package is removed, its `RAD_` mapping is no longer available and existing generated C# must be migrated deliberately.

### Optional TextMeshPro binding

A consumer that already depends on TMP may register a component type with a prefix, e.g.:

```csharp
string error;
bool accepted = AdvancedUIComponentRegistry.TryRegister(
    "TMP", typeof(TMPro.TextMeshProUGUI), out error);
```

The Editor integration itself does **not** require TextMeshPro to be installed, and it does not add a TMP dependency. Registration of a TMP component is a consumer responsibility.

## Decorative, unbound prefabs

The public `UIEditorCreationRequest` and `UIEditorBatchStep` have `AllowUnbound` (default `false`). Set this to `true` for a **Custom** template and supply an ordinary nonblank `InstanceName` / batch `Name` without any **currently registered** `PREFIX_` prefix. An asset such as a saved frame/decorative image can then be created as `Divider`, `Top Border`, etc. using the same linked-prefab, Undo and Text Override paths. The same Style Profile Custom template can still be used in the normal bound mode with a valid prefixed name.

```csharp
UIEditorCreationRequest request = new UIEditorCreationRequest
{
    Parent = buttonsParent,
    Profile = profile,
    Kind = UIComponentKind.Custom,
    CustomTemplateId = "FrameDecor",
    InstanceName = "Top Border",
    AllowUnbound = true,
    UpdateSelection = false
};
UIEditorCreationResult result = AdvancedUIEditorIntegration.Create(request);
```

Unbound is **an explicit external API option**, not a property stored on Style Profile assets or a new checkbox in the compact UI Library. Existing registered Custom templates must still have a valid default name and prefab component when used through the UI Library. For example, a Custom template with `DefaultObjectName=RT_FrameDecor` and `RectTransform` on its prefab root satisfies existing Library validation; external calls can override this with `InstanceName=Top Border` and `AllowUnbound=true`.

Prefabs remain **non-selectable as creation parents**, including in batches; only ordinary containers can own generated children. A name with a recognized binding prefix is rejected in unbound mode, and any prefixed descendants in its source prefab also block unbound creation, so it cannot silently become a Binder accessor. Registration of an additional prefix later can change which object names Binder recognizes; choose a consistent convention for decorative objects.

## External consumer acceptance checks

In a separate Editor assembly: register `RAD`, create a prefab whose root name is `RAD_Contacts` and which has the correct Component, inspect/generate with `AdvancedUIBindingIntegration`, and confirm that a strongly typed `Contacts` property is emitted. Verify a duplicate `RAD` registration is idempotent and conflicting registration fails. Confirm an unbound decorative prefab creates with `Top Border` but `BTN_Unbound` is rejected. Confirm one Undo step reverts a batch containing both a container and decorative prefab. Confirm missing registration does not silently overwrite an existing generated file.

These checks are **not bundled as EditMode/PlayMode suites** in the compact package. Static verification and source review are not substitutes for compiling and running them in Unity.
