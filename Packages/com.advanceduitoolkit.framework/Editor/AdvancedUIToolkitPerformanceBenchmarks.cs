using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Executes opt-in, in-memory microbenchmarks for the Advanced UI Toolkit binding registry
    /// and event scope. Measurements are diagnostic rather than pass/fail thresholds.
    /// </summary>
    internal static class AdvancedUIToolkitPerformanceBenchmarks
    {
        /// <summary>Number of measured runs used to calculate each median.</summary>
        private const int SampleCount = 5;

        /// <summary>Small immutable result for one measured operation.</summary>
        internal sealed class Metric
        {
            /// <summary>Human-readable operation being measured.</summary>
            internal readonly string Name;

            /// <summary>Median microseconds per operation across measured samples.</summary>
            internal readonly double MicrosecondsPerOperation;

            /// <summary>Total number of operations performed in each sample.</summary>
            internal readonly int OperationsPerSample;

            /// <summary>Total generation-zero garbage collections across measured samples.</summary>
            internal readonly int Gen0Collections;

            /// <summary>Constructs an immutable benchmark measurement.</summary>
            /// <param name="name">Name to display in the Editor report.</param>
            /// <param name="microsecondsPerOperation">Median cost in microseconds.</param>
            /// <param name="operationsPerSample">Operations executed per sample.</param>
            /// <param name="gen0Collections">Observed generation-zero collections.</param>
            internal Metric(string name, double microsecondsPerOperation,
                int operationsPerSample, int gen0Collections)
            {
                Name = name;
                MicrosecondsPerOperation = microsecondsPerOperation;
                OperationsPerSample = operationsPerSample;
                Gen0Collections = gen0Collections;
            }
        }

        /// <summary>Snapshot of the Editor environment and measured operations.</summary>
        internal sealed class Report
        {
            /// <summary>Number of synthetic controls used for the lookup tests.</summary>
            internal readonly int ControlCount;

            /// <summary>Number of requested lookup operations per sample.</summary>
            internal readonly int LookupIterations;

            /// <summary>Unity version used to collect the measurements.</summary>
            internal readonly string UnityVersion;

            /// <summary>Operating system and hardware information reported by Unity.</summary>
            internal readonly string SystemInfo;

            /// <summary>UTC time when the benchmark was run.</summary>
            internal readonly string TimeUtc;

            /// <summary>Read-only list of measured operations.</summary>
            internal readonly IReadOnlyList<Metric> Metrics;

            /// <summary>Initializes a reproducible benchmark report.</summary>
            /// <param name="controlCount">Synthetic control count.</param>
            /// <param name="lookupIterations">Lookup count for each sample.</param>
            /// <param name="metrics">Measured costs.</param>
            internal Report(int controlCount, int lookupIterations, IReadOnlyList<Metric> metrics)
            {
                ControlCount = controlCount;
                LookupIterations = lookupIterations;
                UnityVersion = Application.unityVersion;
                SystemInfo = SystemInfoSafe();
                TimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
                Metrics = metrics;
            }

            /// <summary>Formats all benchmark results as copyable plain text.</summary>
            /// <returns>A textual diagnostic report with clear measurement limitations.</returns>
            internal string ToText()
            {
                var builder = new StringBuilder();
                builder.AppendLine("Advanced UI Toolkit performance measurements (v0.9.7)");
                builder.AppendLine("Date: " + TimeUtc);
                builder.AppendLine("Unity: " + UnityVersion);
                builder.AppendLine("Environment: " + SystemInfo);
                builder.AppendLine("Synthetic controls: " + ControlCount);
                builder.AppendLine("Lookup iterations per sample: " + LookupIterations);
                builder.AppendLine("Samples per metric: " + SampleCount);
                builder.AppendLine();
                foreach (Metric metric in Metrics)
                {
                    builder.Append(metric.Name).Append(": ")
                        .Append(metric.MicrosecondsPerOperation.ToString("F3", CultureInfo.InvariantCulture))
                        .Append(" us/op (n=").Append(metric.OperationsPerSample)
                        .Append(", Gen0 collections across samples=")
                        .Append(metric.Gen0Collections).AppendLine(")");
                }
                builder.AppendLine();
                builder.AppendLine("Synthetic microbenchmarks, not a frame-time or memory budget.");
                builder.AppendLine("Timings vary with machine, Editor load and JIT warmup.");
                builder.AppendLine("Gen0 count is a coarse GC indicator, not allocated bytes.");
                builder.AppendLine("The test creates only temporary hidden objects; it writes no assets.");
                return builder.ToString();
            }
        }

        /// <summary>Runs repeatable benchmarks without touching project prefabs or generated C#.</summary>
        /// <param name="controlCount">Power-of-two number of synthetic uGUI buttons.</param>
        /// <param name="lookupIterations">Number of cached lookups in each sample.</param>
        /// <returns>A report containing the measured median per-operation costs.</returns>
        internal static Report Run(int controlCount, int lookupIterations)
        {
            if (controlCount < 1 || (controlCount & (controlCount - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(controlCount), "Control count must be a power of two.");
            if (lookupIterations < 1)
                throw new ArgumentOutOfRangeException(nameof(lookupIterations));

            var owner = new GameObject("AdvancedUIToolkit_Benchmark_Transient", typeof(RectTransform), typeof(UIBindingRoot));
            owner.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var root = owner.GetComponent<UIBindingRoot>();
                var entries = new List<UIBindingRoot.Entry>(controlCount);
                var keys = new string[controlCount];
                var buttons = new Button[controlCount];
                for (int index = 0; index < controlCount; index++)
                {
                    GameObject child = new GameObject("BTN_Control" + index.ToString("D3", CultureInfo.InvariantCulture),
                        typeof(RectTransform), typeof(Image), typeof(Button));
                    child.transform.SetParent(owner.transform, false);
                    child.hideFlags = HideFlags.HideAndDontSave;
                    Button button = child.GetComponent<Button>();
                    string key = "Control" + index.ToString("D3", CultureInfo.InvariantCulture);
                    keys[index] = key;
                    buttons[index] = button;
                    entries.Add(new UIBindingRoot.Entry
                    {
                        Key = key,
                        HierarchyKey = key,
                        StableId = "benchmark-" + index,
                        ComponentType = typeof(Button).FullName,
                        Reference = button
                    });
                }
                root.EditorSetEntries(entries);
                Button first = root.Get<Button>(keys[0]);
                if (first == null)
                    throw new InvalidOperationException("Benchmark setup failed: component not found.");

                // Warm up the JIT and Unity event internals before measuring.
                for (int index = 0; index < 1000; index++)
                {
                    if (root.Get<Button>(keys[index & (controlCount - 1)]) == null)
                        throw new InvalidOperationException("A test binding was lost.");
                }
                var scope = new UIEventScope();
                scope.BindClick(buttons[0], NoOp);
                scope.Suspend();
                scope.Resume();

                try
                {
                    var metrics = new List<Metric>(3);
                    metrics.Add(Measure("Cached Get<Button>", lookupIterations, index =>
                    {
                        if (root.Get<Button>(keys[index & (controlCount - 1)]) == null)
                            throw new InvalidOperationException("A cached reference was lost.");
                    }));
                    metrics.Add(Measure("Manifest cache rebuild + Get", 100, index =>
                    {
                        root.OnAfterDeserialize();
                        if (root.Get<Button>(keys[index & (controlCount - 1)]) == null)
                            throw new InvalidOperationException("A serialized reference was lost.");
                    }));
                    metrics.Add(Measure("Event Suspend + Resume", 1000, index =>
                    {
                        scope.Suspend();
                        scope.Resume();
                    }));
                    return new Report(controlCount, lookupIterations, metrics);
                }
                finally
                {
                    scope.Dispose();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        /// <summary>Benchmarks an operation with warmup and median timing.</summary>
        /// <param name="name">Displayed metric label.</param>
        /// <param name="iterations">Operations executed in each measured sample.</param>
        /// <param name="operation">Operation to execute.</param>
        /// <returns>Median cost with coarse garbage collection data.</returns>
        private static Metric Measure(string name, int iterations, Action<int> operation)
        {
            for (int index = 0; index < Math.Min(iterations, 100); index++)
                operation(index);

            var samples = new double[SampleCount];
            int gen0Collections = 0;
            for (int sample = 0; sample < SampleCount; sample++)
            {
                int beforeGen0 = GC.CollectionCount(0);
                long start = Stopwatch.GetTimestamp();
                for (int index = 0; index < iterations; index++)
                    operation(index);
                long elapsed = Stopwatch.GetTimestamp() - start;
                samples[sample] = 1000000.0 * elapsed / Stopwatch.Frequency / iterations;
                gen0Collections += GC.CollectionCount(0) - beforeGen0;
            }
            Array.Sort(samples);
            return new Metric(name, samples[SampleCount / 2], iterations, gen0Collections);
        }

        /// <summary>Minimal Unity-safe callback for the synthetic event test.</summary>
        private static void NoOp() { }

        /// <summary>Returns environment information without collecting personal identifiers.</summary>
        /// <returns>Unity platform and device class.</returns>
        private static string SystemInfoSafe()
        {
            return Application.platform + ", " + UnityEngine.SystemInfo.deviceType;
        }
    }
}
