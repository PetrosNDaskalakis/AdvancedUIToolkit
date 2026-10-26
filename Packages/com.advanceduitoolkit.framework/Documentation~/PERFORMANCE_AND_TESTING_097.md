# Advanced UI Toolkit v0.9.7 — Performance & Testing

This release adds **optional** diagnostics and regression tests; runtime behavior and generation settings are unchanged.

## Performance window

Open **Tools > Advanced UI Toolkit > Binder > Performance**. Choose a synthetic hierarchy size and an iteration count, then click **Run Benchmarks**. Copy Report provides the results, Unity version and configuration. The benchmark uses transient hidden objects, cleans them up in a `finally` block, and does not write assets or code.

Measured operations: cached `Get<Button>`, cache rebuild + Get, `UIEventScope.Suspend` and `Resume`. Each metric uses five samples and reports the median microseconds/operation. Generation-zero collections are a rough signal, not allocated byte counts. These synthetic values are **not** representative of complete UI frame times or guaranteed performance.

## Run tests

1. Open **Window > General > Test Runner** (install Unity Test Framework if necessary).
2. Run all **EditMode** tests and **PlayMode** tests.
3. Embedded packages normally expose tests automatically; if not, add `"testables": ["com.advanceduitoolkit.framework"]` to the **project** `Packages/manifest.json` while preserving other entries, and reopen Unity. Advanced UI Toolkit does not edit your project manifest itself.
4. Verify your existing WeaponPanel and Prefab/Factory scenes manually after upgrading.

## Manual verification

- Existing WeaponPanel receives a single callback per interaction.
- Repeated disable/enable cycles keep listeners suspended or active appropriately.
- AutoBinder respects nested roots and Prefab Mode rules.
- Manual generation still queues updates without writing C# until requested.
- Running Performance creates no saved scene or prefab and does not compile scripts.
- All EditMode and PlayMode tests pass in the installed Unity Editor.

The Test Runner checks correctness, **not** performance thresholds. Benchmark numbers will differ by hardware, Unity version and Editor load. Compare on the same machine and do not assume zero allocations from a zero Gen0 count.

For a current release checklist, see [TEST_PLAN.md](TEST_PLAN.md) and [ONBOARDING.md](ONBOARDING.md).
