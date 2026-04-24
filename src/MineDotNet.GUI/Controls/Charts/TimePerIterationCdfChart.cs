using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Distribution of per-iteration solve time (ElapsedMs / Iterations per
    // game). Decouples "solver is slow" from "solver happens to iterate a
    // lot" — both land the same way in the plain time CDF but have very
    // different tuning implications.
    internal sealed class TimePerIterationCdfChart : ValueCdfChart
    {
        protected override string ChartTitle => "Time per iteration CDF";
        protected override double MinValue => 0.01;
        protected override double ExtractValue(BenchmarkGameResult game)
            => game.Iterations > 0 ? game.ElapsedMs / game.Iterations : 0;
        protected override string FormatTick(double value) => TimeCdfChart.FormatMsTick(value);
    }
}
