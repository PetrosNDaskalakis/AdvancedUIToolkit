# Contributing

## Project boundaries

Keep player-safe code in `AdvancedUIToolkit.Runtime` and Editor code in the Editor assemblies. Framework must remain usable without Authoring. Library owns prefab placement, Layouts owns reusable container structures and Binder owns binding manifests/generated source. Preserve existing serialized field and assembly identities unless a documented migration is intentional.

## Making changes

Use a branch and a small reproducible example. Preserve `.meta` GUIDs, keep project-specific content outside package source and update the relevant user/API documentation alongside behavior changes. Record user-facing changes in the appropriate package changelog. Do not add development diaries, temporary acceptance reports or speculative roadmaps to the published documentation tree.

Run the Framework static package audit and the Unity checks appropriate to the change. For mold changes, run the optional Authoring EditMode suite and affected manual workflows. Report checks actually performed and any unexecuted Unity/player validation. Avoid changing package versions solely for documentation formatting.

## Reporting issues

Include Unity version, package versions, target platform where relevant, reproduction steps, Console errors and a minimal scene/component arrangement. For Binder include generation mode and root/output settings; for Layouts include mold format and the capture/creation error or omission report. Remove private project data from shared examples.

The distribution currently has no LICENSE file. Do not add a license or third-party assets without the project's owner choosing their terms.
