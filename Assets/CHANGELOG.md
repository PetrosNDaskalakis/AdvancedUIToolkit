# Changelog

## 0.9.1 — Pre-1.0 polish

- Consolidated AutoBinder, change automation and diagnostics into one **Workspace** window.
- Kept Component Library / Factory as a separate Editor window.
- Added XML summaries to runtime, Editor, sample and test members; generated bindings now include XML documentation.
- Added optional, per-root `ExampleScript.cs` templates with two explicit insertion markers.
- Added template validation, safe source preservation and generation tests.
- Kept serializable manifests and existing `UIView<T>` APIs unchanged.
- Automatic generation requires manual review when the example template changes.

## [0.9.0] - 2026-10-23
- Reorganized v0.8.2 framework into a Unity Package Manager package.
- Added Runtime/Editor assembly definitions and explicit uGUI dependency.
- Added Editor tests and a sample import folder.
- Kept generated binding scripts and application style profiles under project Assets.
- Added output-directory guard to prevent writing generated code into Packages or external paths.
- Documented safe migration, code architecture, known limitations, and IP review.

## [0.8.2] - 2026-10-23
- Component preview renderer and preview caching (carried over unchanged).

## [0.8.1] - 2026-10-23
- Responsive Component Library window (carried over unchanged).

## [0.7.0] - 2026-10-22
- Component libraries and style profiles (carried over unchanged).
