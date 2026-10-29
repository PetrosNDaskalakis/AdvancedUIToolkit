# Explicit Type Declarations — v1.1.2

This maintenance release replaces **120 inferred `var` declarations** across 16 production `.cs` files with concrete types. It includes `foreach`, `for`, `using`, and `out var` forms. Type inference was eliminated from the source; the string `"var"` in `AdvancedUIToolkitNaming.cs` is deliberately retained as a reserved C# keyword.

## Design convention

All new Advanced UI Toolkit C# source should use an explicit static type when declaring a variable. For example:

```csharp
UIEditorCreationResult creation = AdvancedUIEditorIntegration.Create(request);
foreach (UIComponentTemplate template in profile.GetEffectiveTemplates())
{
    // ...
}
```

Do not change public API types, prefab serialization, method overloads, or behavior merely to achieve a shorter declaration. The C# compiler may still use inference implicitly for lambdas and generic type arguments in expressions; this policy concerns local variable declarations.

## Verification

- Static scan: zero remaining `var` declarations in package `.cs` files.
- Existing reserved keyword string `"var"` kept intact.
- Original `.meta` files and GUIDs preserved.
- Static package validator and ZIP/patch equivalence checks performed.
- **Unity compilation and in-Editor regression verification must be completed by the developer.**

## Release planning

The previously proposed v1.1.2 batch-creation milestone will be renumbered to a later version. The integration API continues to have single-object creation and does not yet provide transactional batch Undo or headless Binder generation.
