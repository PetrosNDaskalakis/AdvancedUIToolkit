# Binding generation preview (v0.9.6)

Advanced UI Toolkit now has a **Preview Generated C#** button in **Tools > Advanced UI Toolkit > Binder > Bindings**.

1. Select a UI root and **Scan & Preview Changes**.
2. Choose the class name, namespace, output folder and optional `ExampleScript.cs`.
3. Click **Preview Generated C#**. This inspects the in-memory result without writing files or altering Unity assets.
4. Switch between **Proposed**, **Changes**, and **Current**. Use **Copy** to copy the current tab.
5. Close the preview and click **Save Manifest + Generate C#** when you explicitly want to write code.

Preview validates the current hierarchy fingerprint and basic generation settings. A preview is **not** permission to overwrite existing code: final generation retains the same ownership, overwrite, and breaking-API safeguards as v0.9.5. It does not alter the chosen Manual/Automatic generation mode.

The comparison uses line-based differences. For files over 500 lines, use the Proposed/Current tabs rather than a calculated diff.

## XML documentation customization

Add this optional line to a compilable custom template:

```csharp
// <ADVANCEDUITOOLKIT:PROPERTY_DOC>UI binding {name}: {type} from {hierarchy}.
```

It must appear no more than once. The template remains a normal C# class. Advanced UI Toolkit removes the configuration comment from generated output and creates XML summaries for each generated property. Template edits require manual regeneration to keep file-safety checks effective.

## Limitations

The preview does not compile C# and does not validate gameplay logic. Unity compilation and Play/Edit Mode tests must still be performed in a Unity project. A preview does not update any saved binding manifest or generation profile.
