# Contributing (draft)

Advanced UI Toolkit is currently in research and requires publication approval. Before any public repository is opened, agree on ownership and licensing.

Suggested contributor workflow:
1. Create a feature branch.
2. Keep Runtime independent from UnityEditor and game-specific assemblies.
3. Add an EditMode test for bug fixes when feasible.
4. Run the Unity test checklist and document Unity version/results.
5. Avoid editing generated files manually; modify the generator.
6. Keep breaking API changes visible in the changelog.
