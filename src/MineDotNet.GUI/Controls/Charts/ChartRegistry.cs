using System;
using System.Collections.Generic;

namespace MineDotNet.GUI.Controls.Charts
{
    // Metadata for a chart the user can pick from the benchmark dialog's
    // chart-selection popup. Acts as a factory so the dialog can spin up
    // a fresh instance each time a chart is added to the display grid.
    internal sealed class ChartDescriptor
    {
        public ChartDescriptor(string displayName, Func<ChartBase> factory)
        {
            DisplayName = displayName;
            Factory = factory;
        }

        public string DisplayName { get; }
        public Func<ChartBase> Factory { get; }
    }

    // Enumerates every chart type the dialog can show. Adding a new chart =
    // drop it into the All list with a display name; the picker, grid, and
    // progress plumbing pick it up automatically.
    internal static class ChartRegistry
    {
        public static IReadOnlyList<ChartDescriptor> All { get; } = new List<ChartDescriptor>
        {
            new ChartDescriptor("Outcomes",                     () => new OutcomeBarChart()),
            new ChartDescriptor("Solve time CDF",               () => new TimeCdfChart()),
            new ChartDescriptor("Iterations CDF",               () => new IterationsCdfChart()),
            new ChartDescriptor("Time per iteration CDF",       () => new TimePerIterationCdfChart()),
            new ChartDescriptor("Iterations by outcome",        () => new OutcomeIterationsCdfChart()),
            new ChartDescriptor("Iterations surface (3D)",      () => new IterationsSurfaceChart()),
            new ChartDescriptor("Win rate heatmap (2D sweep)",  () => new WinRateHeatmap()),
            new ChartDescriptor("Avg time heatmap (2D sweep)",  () => new AvgTimeHeatmap()),
            new ChartDescriptor("Avg iterations heatmap (2D sweep)", () => new AvgIterationsHeatmap()),
            new ChartDescriptor("Win rate vs avg time",         () => new WinRateVsTimeScatter()),
            new ChartDescriptor("Win rate vs sweep axis",       () => new WinRateSweepChart()),
            new ChartDescriptor("Avg time vs sweep axis",       () => new AvgTimeSweepChart()),
            new ChartDescriptor("Avg iterations vs sweep axis", () => new AvgIterationsSweepChart()),
        };
    }
}
