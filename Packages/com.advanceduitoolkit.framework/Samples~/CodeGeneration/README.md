# Code Generation Template sample

This sample contains `ExampleScript.cs`, a **compilable example for Advanced UI Toolkit's binding generator**. It is a template, not a component that needs to be attached to a GameObject.

1. Import this sample through **Window > Package Manager > Advanced UI Toolkit > Samples**.
2. Open **Tools > Advanced UI Toolkit > Binder**, select a configured UI root, then open **Bindings**.
3. Drag the imported `ExampleScript.cs` into the **Example Script** field.
4. Preview the proposed generated C# before saving. The two `// <ADVANCEDUITOOLKIT:...>` markers are replaced with discovered properties and assignments.
5. Use **Save Manifest + Generate C#** (or the Manual generation queue) when ready for Unity to compile.

You may customize formatting, namespaces, helper methods, and XML documentation. **Keep one** `// <ADVANCEDUITOOLKIT:PROPERTIES>` marker and **one** `// <ADVANCEDUITOOLKIT:ASSIGNMENTS>` marker, and keep a public `Initialize(UIBindingRoot root)` method. Do not modify generated output directly.

This sample intentionally does not include a scene or prefab. Generated files belong under your project `Assets` directory, not inside the package.
