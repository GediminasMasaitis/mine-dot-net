using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Rotatable 3D surface for a two-axis sweep. Axes:
    //   X = sweep axis A (first parameter)
    //   Z = sweep axis B (second parameter)
    //   Y = extracted metric (win rate / avg time / avg iters) — height
    // Uses the first solver's runs only. Needs both axes configured; with
    // only one axis, renders empty — the heatmap's sibling, same data.
    //
    // Subclasses pick the metric extractor, the formatter for the Y axis
    // label, and optional fixed [min, max] for that metric (e.g. 0–100%
    // for win rate).
    internal abstract class MetricSurface3DChart : Surface3DChartBase
    {
        public string AxisNameA { get; set; } = "Axis A";
        public string AxisNameB { get; set; } = "Axis B";

        protected abstract double ExtractValue(BenchmarkSolverRun run);
        protected abstract string FormatValue(double value);
        protected abstract double? FixedMin { get; }
        protected abstract double? FixedMax { get; }
        protected abstract string YAxisName { get; }

        private double[] _axisAValues = Array.Empty<double>();
        private double[] _axisBValues = Array.Empty<double>();
        private double _vMin, _vMax;
        private bool _hasData;

        protected override void BuildMesh()
        {
            ModelVisual.Content = null;
            _axisAValues = Array.Empty<double>();
            _axisBValues = Array.Empty<double>();
            _hasData = false;

            // First solver only — heatmap semantics. Keeping it consistent
            // so the user switches between the two views without surprises.
            var cells = Runs
                .Where(r => r.SolverIndex == 0 && r.AxisValue.HasValue && r.AxisValueB.HasValue && r.GamesPlayed > 0)
                .GroupBy(r => (a: r.AxisValue.Value, b: r.AxisValueB.Value))
                .ToDictionary(g => g.Key, g => g.First());
            if (cells.Count == 0) return;

            var axisA = cells.Keys.Select(k => k.a).Distinct().OrderBy(x => x).ToArray();
            var axisB = cells.Keys.Select(k => k.b).Distinct().OrderBy(x => x).ToArray();
            if (axisA.Length < 2 || axisB.Length < 2) return;

            _axisAValues = axisA;
            _axisBValues = axisB;

            var vMin = FixedMin ?? double.PositiveInfinity;
            var vMax = FixedMax ?? double.NegativeInfinity;
            if (FixedMin == null || FixedMax == null)
            {
                foreach (var r in cells.Values)
                {
                    var v = ExtractValue(r);
                    if (FixedMin == null && v < vMin) vMin = v;
                    if (FixedMax == null && v > vMax) vMax = v;
                }
            }
            if (!double.IsFinite(vMin)) vMin = 0;
            if (!double.IsFinite(vMax)) vMax = vMin + 1;
            if (Math.Abs(vMax - vMin) < 1e-9) vMax = vMin + 1e-9;
            _vMin = vMin;
            _vMax = vMax;
            _hasData = true;

            var mesh = new MeshGeometry3D();
            for (var xi = 0; xi < axisA.Length; xi++)
            {
                var nx = NormalizeIdx(xi, axisA.Length);
                for (var zi = 0; zi < axisB.Length; zi++)
                {
                    var nz = NormalizeIdx(zi, axisB.Length);
                    var value = cells.TryGetValue((axisA[xi], axisB[zi]), out var run)
                        ? ExtractValue(run)
                        : vMin;
                    var ny = (value - vMin) / (vMax - vMin);
                    ny = Math.Max(0, Math.Min(1, ny));
                    mesh.Positions.Add(new Point3D(nx, ny, nz));
                }
            }

            for (var xi = 0; xi < axisA.Length - 1; xi++)
                for (var zi = 0; zi < axisB.Length - 1; zi++)
                {
                    var i00 = xi * axisB.Length + zi;
                    var i01 = xi * axisB.Length + (zi + 1);
                    var i10 = (xi + 1) * axisB.Length + zi;
                    var i11 = (xi + 1) * axisB.Length + (zi + 1);
                    mesh.TriangleIndices.Add(i00);
                    mesh.TriangleIndices.Add(i01);
                    mesh.TriangleIndices.Add(i11);
                    mesh.TriangleIndices.Add(i00);
                    mesh.TriangleIndices.Add(i11);
                    mesh.TriangleIndices.Add(i10);
                }

            for (var i = 0; i < mesh.Positions.Count; i++)
            {
                var h = mesh.Positions[i].Y;
                mesh.TextureCoordinates.Add(new Point(0.5, 1.0 - h));
            }

            var material = new DiffuseMaterial(MakeHeightGradient());
            ModelVisual.Content = new GeometryModel3D(mesh, material) { BackMaterial = material };
        }

        private static double NormalizeIdx(int i, int count) => (i / (double)(count - 1) - 0.5) * 2;

        protected override void DrawAxesOverlay(DrawingContext dc)
        {
            if (!_hasData) return;

            const double xMin = -1, xMax = 1, zMin = -1, zMax = 1, yMin = 0, yMax = 1;
            var origin = new Point3D(xMin, yMin, zMin);

            DrawLine3D(dc, origin, new Point3D(xMax, yMin, zMin), AxisPen3D);
            DrawLine3D(dc, origin, new Point3D(xMin, yMin, zMax), AxisPen3D);
            DrawLine3D(dc, origin, new Point3D(xMin, yMax, zMin), AxisPen3D);

            DrawAxisTicks(dc, _axisAValues, axis: 0, offsetDir: new Vector3D(0, 0, -1));
            DrawAxisTicks(dc, _axisBValues, axis: 2, offsetDir: new Vector3D(-1, 0, 0));

            for (var i = 0; i <= 4; i++)
            {
                var t = i / 4.0;
                var wy = yMin + t * (yMax - yMin);
                DrawLine3D(dc, new Point3D(xMin, wy, zMin), new Point3D(xMin - 0.06, wy, zMin), TickPen3D);
                var value = _vMin + t * (_vMax - _vMin);
                DrawLabel3D(dc, new Point3D(xMin - 0.12, wy, zMin), FormatValue(value), TextAlignment.Right);
            }

            DrawLabel3D(dc, new Point3D(xMax + 0.2, yMin, zMin), AxisNameA, TextAlignment.Left);
            DrawLabel3D(dc, new Point3D(xMin, yMin, zMax + 0.2), AxisNameB, TextAlignment.Center);
            DrawLabel3D(dc, new Point3D(xMin, yMax + 0.15, zMin), YAxisName, TextAlignment.Right);
        }

        // Draw up to 6 ticks along one of the two horizontal axes. `axis`
        // selects which world axis (0=X, 2=Z) to move the tick along;
        // `offsetDir` is where the tick mark points (outward from the base
        // corner) and where the label sits relative to it.
        private void DrawAxisTicks(DrawingContext dc, double[] values, int axis, Vector3D offsetDir)
        {
            if (values.Length < 2) return;
            var count = Math.Min(6, values.Length);
            for (var i = 0; i < count; i++)
            {
                var idx = (int)Math.Round(i * (values.Length - 1) / (double)(count - 1));
                var w = NormalizeIdx(idx, values.Length);
                var base0 = axis == 0
                    ? new Point3D(w, 0, -1)
                    : new Point3D(-1, 0, w);
                var tickEnd = base0 + offsetDir * 0.08;
                var labelAt = base0 + offsetDir * 0.15;
                DrawLine3D(dc, base0, tickEnd, TickPen3D);
                DrawLabel3D(dc, labelAt, FormatAxisValue(values[idx]),
                    axis == 0 ? TextAlignment.Center : TextAlignment.Right);
            }
        }
    }

    internal sealed class WinRateSurface3DChart : MetricSurface3DChart
    {
        protected override string ChartTitle => "Win rate surface (2D sweep)";
        protected override string YAxisName => "win rate";
        protected override double ExtractValue(BenchmarkSolverRun run) => run.WinRate * 100;
        protected override string FormatValue(double v) => $"{v:0}%";
        protected override double? FixedMin => 0;
        protected override double? FixedMax => 100;
    }

    internal sealed class AvgTimeSurface3DChart : MetricSurface3DChart
    {
        protected override string ChartTitle => "Avg time surface (2D sweep)";
        protected override string YAxisName => "avg ms";
        protected override double ExtractValue(BenchmarkSolverRun run) => run.AvgMs;
        protected override string FormatValue(double v) => $"{v:0} ms";
        protected override double? FixedMin => null;
        protected override double? FixedMax => null;
    }

    internal sealed class AvgIterationsSurface3DChart : MetricSurface3DChart
    {
        protected override string ChartTitle => "Avg iterations surface (2D sweep)";
        protected override string YAxisName => "avg iters";
        protected override double ExtractValue(BenchmarkSolverRun run) => run.AvgIterations;
        protected override string FormatValue(double v) => $"{v:0.#}";
        protected override double? FixedMin => null;
        protected override double? FixedMax => null;
    }
}
