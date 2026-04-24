using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Cumulative distribution of per-game solve time, one step line per solver.
    // Reads as "fraction of games ≤ X ms."
    internal sealed class TimeCdfChart : ValueCdfChart
    {
        protected override string ChartTitle => "Solve time CDF";
        protected override double MinValue => 0.1;
        protected override double ExtractValue(BenchmarkGameResult game) => game.ElapsedMs;
        protected override string FormatTick(double value) => FormatMsTick(value);

        internal static string FormatMsTick(double ms)
        {
            if (ms < 1) return $"{ms:0.##} ms";
            if (ms < 1000) return $"{ms:0} ms";
            var s = ms / 1000.0;
            return s < 10 ? $"{s:0.#} s" : $"{s:0} s";
        }
    }
}
