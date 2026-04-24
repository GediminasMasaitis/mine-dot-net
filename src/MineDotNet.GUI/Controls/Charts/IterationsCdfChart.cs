using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Distribution of per-game iteration counts. Mirrors the time CDF but on
    // game length rather than cost — tells you how far games get before they
    // end. Weak configs hug the left (die fast); strong configs run long.
    internal sealed class IterationsCdfChart : ValueCdfChart
    {
        protected override string ChartTitle => "Iterations CDF";
        protected override double MinValue => 1;
        protected override double ExtractValue(BenchmarkGameResult game) => game.Iterations;
        protected override string FormatTick(double value) => $"{value:0}";
    }
}
