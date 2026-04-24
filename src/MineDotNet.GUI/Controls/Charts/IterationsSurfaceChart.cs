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
    // 3D surface of "how the iteration distribution shifts across the sweep
    // axis":
    //   X = sweep axis (low → high)
    //   Y = log-binned iteration count (1 → 1000)
    //   Z = count of games in that (axis, iters) bin
    // Useful when you're sweeping ONE parameter and want to see how game
    // length distributions shift across its range. For a genuine 2D sweep
    // use MetricSurface3DChart instead.
    internal sealed class IterationsSurfaceChart : Surface3DChartBase
    {
        public string AxisName { get; set; } = "Sweep axis";

        private double[] _axisValues = Array.Empty<double>();
        private int _maxCount;
        private const int IterBins = 24;
        private const double IterMin = 1;
        private const double IterMax = 1000;

        protected override string ChartTitle => "Iterations surface";

        protected override void BuildMesh()
        {
            ModelVisual.Content = null;
            _axisValues = Array.Empty<double>();
            _maxCount = 0;

            var sweepRuns = Runs.Where(r => r.AxisValue.HasValue).ToList();
            if (sweepRuns.Count < 2) return;

            var byAxis = sweepRuns
                .GroupBy(r => r.AxisValue.Value)
                .OrderBy(g => g.Key)
                .ToList();
            var xCount = byAxis.Count;
            if (xCount < 2) return;

            _axisValues = byAxis.Select(g => g.Key).ToArray();

            var logMin = Math.Log10(IterMin);
            var logMax = Math.Log10(IterMax);
            var logStep = (logMax - logMin) / IterBins;

            var grid = new int[xCount, IterBins];
            for (var x = 0; x < xCount; x++)
                foreach (var run in byAxis[x])
                    foreach (var game in run.Games)
                    {
                        var iters = Math.Max(1, game.Iterations);
                        var bin = (int)((Math.Log10(iters) - logMin) / logStep);
                        if (bin < 0) bin = 0;
                        if (bin >= IterBins) bin = IterBins - 1;
                        grid[x, bin]++;
                    }

            var maxCount = 0;
            for (var x = 0; x < xCount; x++)
                for (var y = 0; y < IterBins; y++)
                    if (grid[x, y] > maxCount) maxCount = grid[x, y];
            if (maxCount == 0) return;
            _maxCount = maxCount;

            var mesh = new MeshGeometry3D();
            for (var x = 0; x < xCount; x++)
            {
                var nx = NormalizeIdx(x, xCount);
                for (var y = 0; y < IterBins; y++)
                {
                    var ny = NormalizeIdx(y, IterBins);
                    var nz = (double)grid[x, y] / maxCount;
                    mesh.Positions.Add(new Point3D(nx, nz, ny));
                }
            }
            for (var x = 0; x < xCount - 1; x++)
                for (var y = 0; y < IterBins - 1; y++)
                {
                    var i00 = x * IterBins + y;
                    var i01 = x * IterBins + (y + 1);
                    var i10 = (x + 1) * IterBins + y;
                    var i11 = (x + 1) * IterBins + (y + 1);
                    mesh.TriangleIndices.Add(i00);
                    mesh.TriangleIndices.Add(i01);
                    mesh.TriangleIndices.Add(i11);
                    mesh.TriangleIndices.Add(i00);
                    mesh.TriangleIndices.Add(i11);
                    mesh.TriangleIndices.Add(i10);
                }

            for (var i = 0; i < mesh.Positions.Count; i++)
            {
                var z = mesh.Positions[i].Y;
                mesh.TextureCoordinates.Add(new Point(0.5, 1.0 - z));
            }

            var material = new DiffuseMaterial(MakeHeightGradient());
            ModelVisual.Content = new GeometryModel3D(mesh, material) { BackMaterial = material };
        }

        private static double NormalizeIdx(int i, int count) => (i / (double)(count - 1) - 0.5) * 2;

        protected override void DrawAxesOverlay(DrawingContext dc)
        {
            if (_axisValues.Length < 2 || _maxCount == 0) return;

            const double xMin = -1, xMax = 1, zMin = -1, zMax = 1, yMin = 0, yMax = 1;

            var origin = new Point3D(xMin, yMin, zMin);
            DrawLine3D(dc, origin, new Point3D(xMax, yMin, zMin), AxisPen3D);
            DrawLine3D(dc, origin, new Point3D(xMin, yMin, zMax), AxisPen3D);
            DrawLine3D(dc, origin, new Point3D(xMin, yMax, zMin), AxisPen3D);

            var xTickCount = Math.Min(6, _axisValues.Length);
            for (var i = 0; i < xTickCount; i++)
            {
                var idx = (int)Math.Round(i * (_axisValues.Length - 1) / (double)(xTickCount - 1));
                var wx = NormalizeIdx(idx, _axisValues.Length);
                DrawLine3D(dc, new Point3D(wx, yMin, zMin), new Point3D(wx, yMin, zMin - 0.08), TickPen3D);
                DrawLabel3D(dc, new Point3D(wx, yMin, zMin - 0.15), FormatAxisValue(_axisValues[idx]), TextAlignment.Center);
            }

            for (var d = (int)Math.Log10(IterMin); d <= (int)Math.Log10(IterMax); d++)
            {
                var iter = Math.Pow(10, d);
                var t = (Math.Log10(iter) - Math.Log10(IterMin)) / (Math.Log10(IterMax) - Math.Log10(IterMin));
                var wz = zMin + t * (zMax - zMin);
                DrawLine3D(dc, new Point3D(xMin, yMin, wz), new Point3D(xMin - 0.08, yMin, wz), TickPen3D);
                DrawLabel3D(dc, new Point3D(xMin - 0.15, yMin, wz), $"{iter:0}", TextAlignment.Right);
            }

            for (var i = 1; i <= 4; i++)
            {
                var t = i / 4.0;
                var wy = yMin + t * (yMax - yMin);
                DrawLine3D(dc, new Point3D(xMin, wy, zMin), new Point3D(xMin - 0.06, wy, zMin), TickPen3D);
                var count = (int)Math.Round(t * _maxCount);
                DrawLabel3D(dc, new Point3D(xMin - 0.12, wy, zMin), $"{count}", TextAlignment.Right);
            }

            DrawLabel3D(dc, new Point3D(xMax + 0.2, yMin, zMin), AxisName, TextAlignment.Left);
            DrawLabel3D(dc, new Point3D(xMin, yMin, zMax + 0.2), "iterations", TextAlignment.Center);
            DrawLabel3D(dc, new Point3D(xMin, yMax + 0.15, zMin), "games", TextAlignment.Right);
        }
    }
}
