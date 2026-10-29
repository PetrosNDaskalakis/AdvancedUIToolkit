# Advanced UI Toolkit — v1.1.0 release gates

[← Documentation Index](INDEX.md) · [v1.1.0 package notes](V1_1_STABLE_RELEASE_NOTES.md) · [Final acceptance](V1_1_STABLE_ACCEPTANCE.md)

**Current status:** **v1.1.0 has been packaged for stable-version acceptance, but this exact artifact is not yet signed off**. The developer reported that Preview 11 Compact works in Unity; no clean-install matrix, player-build evidence, or Profiler captures have been provided for the v1.1.0 package. Record the results in [the final acceptance checklist](V1_1_STABLE_ACCEPTANCE.md). Never interpret a static audit as a Unity validation.

## Installation and source compatibility

- [ ] Fresh clean install of the v1.1.0 full UPM archive in Unity 2022.3 LTS; both Binder and Factory menus appear.
- [ ] v1.1.0 upgrade patch applied to the exact **1.1.0-rc.1** base; resulting tree matches the full v1.1.0 UPM package.
- [ ] Editor compilation succeeds, no missing scripts or duplicate assembly errors; repeat with and without TMP.
- [ ] Existing serialized bindings, prefab links, style profiles and pre-existing `.meta` GUIDs persist after upgrading a real project.

## Binder, generation and lifecycle

- [ ] Binder discovers named root/child/inactive controls and honors nested binding roots, rename, duplication and deletion.
- [ ] Manual and Automatic C# generation produce compiling bindings; custom templates and escaped property XML summaries work.
- [ ] No-op generation does not rewrite C#; failed generation restores prior source/manifest; Undo after deletion regenerates correctly.
- [ ] Multiple roots, event subscriptions, pooling and enable/disable cycles produce no duplicate listeners or incorrect callbacks.
- [ ] Manually verify Binder generation, active/inactive controls, nested roots, event lifecycle, scene save/reload, and Undo/Redo. Record Unity version and reproduction steps for any failures.

## Factory / prefab creation

- [ ] Style profile inheritance, context resolution and prefab/regular-container parent rules behave as documented.
- [ ] Source deletion and reorder after selecting a text target cannot redirect an override to a different label.
- [ ] Legacy Text, inactive children, duplicate names, variants and nested prefabs work with the intended target.
- [ ] Saved TMP prefab overrides survive scene save/reopen and project reload when TMP is installed; compile without TMP.
- [ ] Undo only removes the Factory creation; Redo restores exact name, parent, text and prefab link. Prior unrelated edits remain unaffected.
- [ ] Failed Factory creation leaves no stray instance or corrupted Undo history.

## Measurement, build and publication

- [ ] Record baseline and candidate Unity Profiler CPU time and GC allocated bytes for Factory idle/selection/creation and Binder scan/generation.
- [ ] Build and run at least one target-platform player; Editor assembly remains excluded.
- [ ] Verify team IP ownership, licensing, included assets, and publication permissions before any public repository publication.

Static package validation passes only the packaging gate, never Unity compilation, manual workflow, Profiler, player-build, publication or stable-release gates.
