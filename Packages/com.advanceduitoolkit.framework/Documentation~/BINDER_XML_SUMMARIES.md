# Binder: per-property XML summaries

[← Documentation Index](INDEX.md) · [Developer Handbook](DEVELOPER_HANDBOOK.md)

Binder stores per-property XML summaries and includes them in generated C# documentation.

## Quick workflow

1. Select your UI root in Unity's Hierarchy, then open **Tools → Advanced UI Toolkit → Binder**.
2. Find **Public Properties & XML Comments**, just below the recognized components, and expand it.
3. For each component, enter a short **XML Summary** above its **Public Property** name.
4. Click **Preview C#** to review the generated documentation and references.
5. Click **Generate Bindings** or **Generate Changes** to save the manifest and updated C#.

Example values for a panel containing `BTN_Load`, `BTN_Arm`, and `BTN_Fire`:

| UI object | XML Summary | Public Property |
| --- | --- | --- |
| `BTN_Load` | Loads the selected weapon. | `Load` |
| `BTN_Arm` | Arms the loaded weapon. | `Arm` |
| `BTN_Fire` | Fires the armed weapon. | `Fire` |

Result:

```csharp
/// <summary>Loads the selected weapon.</summary>
public UnityEngine.UI.Button Load { get; private set; }

/// <summary>Arms the loaded weapon.</summary>
public UnityEngine.UI.Button Arm { get; private set; }

/// <summary>Fires the armed weapon.</summary>
public UnityEngine.UI.Button Fire { get; private set; }
```

## Behavior and compatibility

- **One description per binding.** The optional comment belongs to its saved manifest entry, not to a particular row position, and survives a recognized hierarchy rename.
- **Empty is valid.** Leaving the field empty uses the existing default comment or the global `ADVANCEDUITOOLKIT:PROPERTY_DOC` template directive.
- **Explicit text takes priority.** A nonempty per-property summary overrides the global template's property-summary pattern, but doesn't change the template's class layout or initialization code.
- **Safe XML.** Special characters such as `&`, `<`, and `>` are escaped in generated source.
- **No recompilation while typing.** Edits are staged in Binder. Only generation saves the manifest and writes C#.
- **Documentation-only changes are tracked.** Binder shows a pending change and enables generation even when the property name and hierarchy didn't change.
- **Older roots remain compatible.** Existing entries without `XmlSummary` continue to generate their previous default documentation.
- **Automatic mode** continues to handle safe hierarchy changes; handwritten comment edits from Binder still require Generate to commit them. This avoids recompiling at every keystroke.

## Testing

With a representative panel, verify that comments appear in the C# preview, persist after saving/reopening, survive a rename, and remain intact after deleting and undoing another control. Verify both the default generator and a custom code-generation template.

## Duplicating components safely

When Unity creates a duplicate named `BTN_Fire (1)` with **Ctrl+D**, opening or updating Binder normalizes that name to `BTN_Fire_2` (or the next available number). This prevents the invalid generated property name `Fire (1)` from blocking **Preview C#** and **Generate Changes**.

If you edit an XML summary or public property name before duplicating or adding a control, Binder preserves the unpublished text through the hierarchy rescan. **Refresh** also keeps those edits. They are saved to the manifest only when you generate.

A genuine conflict such as two unrelated GameObjects named `BTN_Fire` still requires an intentional rename. Binder displays the blocking issue beside the disabled actions. Background automation remains read-only during inspection; the interactive Binder performs the safe Unity duplicate-name correction.
