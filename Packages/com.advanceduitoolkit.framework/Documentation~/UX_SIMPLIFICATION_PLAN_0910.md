# Advanced UI Toolkit v0.9.10 — UX Simplification plan

**Goal:** A developer should build and bind a simple UI without hunting through four tabs. No new runtime capabilities.

## Default workflow

1. **Choose UI Root** (current selection, auto-detected).
2. **Create** (open Factory with inherited style profile preselected).
3. **Review** (inline list of recognized components and actionable warnings).
4. **Generate Changes** (one primary button; manual by default, no background C# writes).

## Window plan

- **Workspace** opens to one primary **Build & Bind** page instead of a tab strip.
- Status bar: `Root: WeaponPanel • 5 bound • 2 pending • 0 errors`.
- Component registry and naming results appear in one compact searchable list.
- One persistent primary action: `Generate 2 Pending Changes` (or `Up to date`).
- Warnings shown next to the affected component, with `Select`/`Fix` when safely possible.
- **Advanced** foldout (closed by default) holds: Automation mode, Diagnostics, Performance, Code Templates, output locations, rename safety and developer options.
- **Factory** remains a separate window for creating prefabs/styles, but prioritizes selecting a library, searching, previewing and creating; rarely used settings move to a collapsible panel.
- A command toolbar and back link replace repeated inter-window navigation buttons.

## Usability gates

- No more than one primary call to action visible in each daily-use area.
- At common Inspector dock widths (320–450 px), no clipped labels or hidden primary buttons.
- Can create, scan, review and generate a three-button panel without visiting Advanced.
- Generation preview, manual mode, safe rename and diagnostic capabilities remain accessible.
- No automatic C# writing in Manual mode, and no unexpected changes to stored settings.
- Test with a new developer and a designer; record their number of steps, errors and confusing points.

Implementation is **planned for v0.9.10**, not part of v0.9.9.
