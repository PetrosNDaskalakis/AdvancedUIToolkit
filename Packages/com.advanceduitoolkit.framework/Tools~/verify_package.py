#!/usr/bin/env python3
"""Verify a Advanced UI Toolkit UPM package without launching Unity.

Usage: python3 Tools~/verify_package.py
Only Python's standard library is required. This is a static package audit,
not a replacement for Unity compilation, Test Runner, or a player build.
"""

import json
import re
import sys
from pathlib import Path


# Inspect the portable package and report any packaging errors.
def inspect_package(root: Path) -> list[str]:
    errors: list[str] = []
    manifest_path = root / "package.json"
    if not manifest_path.is_file():
        return ["Missing package.json"]
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (ValueError, UnicodeError) as exc:
        return [f"Invalid package.json: {exc}"]

    if manifest.get("name") != "com.advanceduitoolkit.framework":
        errors.append("Unexpected package name")
    if manifest.get("displayName") != "Advanced UI Toolkit":
        errors.append("Unexpected public package display name")
    version = manifest.get("version", "")
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?", version):
        errors.append(f"Invalid semver-style version: {version!r}")
    if manifest.get("unity") is None:
        errors.append("No minimum Unity version declared")
    if "com.unity.ugui" not in manifest.get("dependencies", {}):
        errors.append("Missing uGUI dependency")

    assemblies = {
        "Runtime/AdvancedUIToolkit.Runtime.asmdef": "AdvancedUIToolkit.Runtime",
        "Editor/AdvancedUIToolkit.Editor.asmdef": "AdvancedUIToolkit.Editor",
        "Tests/Editor/AdvancedUIToolkit.Tests.Editor.asmdef": "AdvancedUIToolkit.Tests.Editor",
        "Tests/PlayMode/AdvancedUIToolkit.Tests.PlayMode.asmdef": "AdvancedUIToolkit.Tests.PlayMode",
    }
    for relative, expected in assemblies.items():
        path = root / relative
        if not path.is_file():
            errors.append(f"Missing assembly definition: {relative}")
            continue
        try:
            definition = json.loads(path.read_text(encoding="utf-8"))
        except (ValueError, UnicodeError) as exc:
            errors.append(f"Invalid asmdef {relative}: {exc}")
            continue
        if definition.get("name") != expected:
            errors.append(f"Unexpected assembly name in {relative}")
        if relative.startswith("Editor/") and definition.get("includePlatforms") != ["Editor"]:
            errors.append("Editor assembly must target Editor only")
        if relative.startswith("Tests/") and "TestAssemblies" not in definition.get("optionalUnityReferences", []):
            errors.append(f"Missing TestAssemblies reference in {relative}")
        if relative.startswith("Runtime/") and definition.get("includePlatforms"):
            errors.append("Runtime assembly unexpectedly restricted to specific platforms")

    expected_assemblies = set(assemblies.values())
    if len(expected_assemblies) != len(assemblies):
        errors.append("Duplicate assembly names")

    samples = manifest.get("samples", [])
    if len(samples) < 4:
        errors.append("Expected all four documented UPM sample entries")
    for sample in samples:
        relative = sample.get("path", "")
        # Prevent malformed asset paths accidentally escaping the package root.
        if not isinstance(relative, str) or not relative.startswith("Samples~/") or ".." in Path(relative).parts:
            errors.append(f"Unsafe sample path: {relative!r}")
            continue
        sample_dir = root / relative
        if not sample_dir.is_dir() or not (sample_dir / "README.md").is_file():
            errors.append(f"Missing sample or README: {relative}")

    required = (
        "Runtime/UIBindingRoot.cs", "Runtime/UIView.cs", "Runtime/UIEventScope.cs",
        "Editor/AdvancedUIToolkitWorkspaceWindow.cs", "Editor/UIComponentLibraryWindow.cs",
        "Editor/AdvancedUIToolkitEditor.cs", "Editor/AdvancedUIToolkitAutomation.cs",
        "Tests/Editor/AdvancedUIToolkitUndoRestoreTests.cs",
        "Tests/Editor/AdvancedUIToolkitWorkspaceSelectionRegressionTests.cs",
        "Documentation~/V1_RELEASE_GATES.md", "README.md", "PUBLICATION_CHECKLIST.md",
    )
    for relative in required:
        if not (root / relative).is_file():
            errors.append(f"Missing essential file: {relative}")

    # Verify that Unity can preserve serialized object references after an update.
    ids: dict[str, Path] = {}
    for meta in root.rglob("*.meta"):
        match = re.search(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$", meta.read_text(encoding="utf-8"))
        if match is None:
            errors.append(f"Invalid GUID in {meta.relative_to(root)}")
            continue
        guid = match.group(1).lower()
        if guid in ids:
            errors.append(f"Duplicate Unity GUID: {ids[guid]} and {meta.relative_to(root)}")
        else:
            ids[guid] = meta.relative_to(root)
    for source in list((root / "Runtime").rglob("*.cs")) + list((root / "Editor").rglob("*.cs")) + list((root / "Tests").rglob("*.cs")):
        if not (Path(str(source) + ".meta")).is_file():
            errors.append(f"Missing .meta for {source.relative_to(root)}")

    for source in list((root / "Runtime").glob("*.cs")) + list((root / "Editor").glob("*.cs")):
        body = source.read_text(encoding="utf-8")
        if re.search(r"(?m)^\s*using\s+(?:mnw|MNW)(?:[.;])", body):
            errors.append(f"Project-specific dependency in {source.relative_to(root)}")

    # The current main user workflow must be reflected in top-level docs.
    binder_path = root / "Editor/AdvancedUIToolkitWorkspaceWindow.cs"
    library_path = root / "Editor/UIComponentLibraryWindow.cs"
    if binder_path.is_file() and '[MenuItem("Tools/Advanced UI Toolkit/Binder"' not in binder_path.read_text():
        errors.append("Expected Tools > Advanced UI Toolkit > Binder menu item")
    if library_path.is_file() and '[MenuItem("Tools/Advanced UI Toolkit/Factory"' not in library_path.read_text():
        errors.append("Expected Tools > Advanced UI Toolkit > Factory menu item")
    readme_path = root / "README.md"
    if readme_path.is_file():
        readme = readme_path.read_text(encoding="utf-8")
        if f"v{version}" not in readme:
            errors.append("README does not identify the current package version")
        if "Tools > Advanced UI Toolkit > Workspace" in readme or "Tools → Advanced UI Toolkit → Workspace" in readme:
            errors.append("README still references obsolete Workspace menu")
        if "Tools → Advanced UI Toolkit → Binder" not in readme:
            errors.append("README lacks the current Binder menu path")
    return errors


# Execute the static validator and print release status.
def main() -> int:
    root = Path(__file__).resolve().parent.parent
    issues = inspect_package(root)
    if issues:
        for issue in issues:
            print("FAIL:", issue)
        return 1
    manifest = json.loads((root / "package.json").read_text(encoding="utf-8"))
    print(f"PASS: Advanced UI Toolkit v{manifest['version']} static package audit")
    print("NOTE: Unity compilation, EditMode/PlayMode testing, player builds, and publication review still required.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
