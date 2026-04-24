using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Rotatable 3D surface of "how the iteration distribution shifts across
    // the sweep axis." Axes:
    //   X = sweep axis value (low → high)
    //   Y = iteration count (log-binned, 1 → 1000)
    //   Z = count of games that fell into that (axis, iters) bin
    //
    // 3D mesh is drawn by a Viewport3D child. Axis lines, ticks, and labels
    // are drawn as a 2D overlay on top of the viewport: we project every 3D
    // world-space point through the camera's view+projection matrices into
    // screen space, then render 2D lines and FormattedText there. That way
    // lines stay crisp (no thin-quad-aliasing) and text is never pixelated
    // or backwards.
    internal sealed class IterationsSurfaceChart : ChartBase
    {
        private readonly Viewport3D _viewport = new Viewport3D();
        private readonly PerspectiveCamera _camera = new PerspectiveCamera { FieldOfView = 45 };
        private readonly ModelVisual3D _modelVisual = new ModelVisual3D();
        private readonly OverlayElement _overlay;

        // Spherical camera coords. Pitch clamped to [-85, -5] so the camera
        // can't flip past the pole and the up-vector stays stable.
        private double _yawDeg = 35;
        private double _pitchDeg = -30;
        private double _distance = 3.2;
        private Point? _dragAnchor;

        private const double TitleStrip = 22;

        // Mesh extents in world space — set once per RebuildMesh, read by
        // DrawAxes when it places tick labels. Keeps DrawAxes data-driven
        // rather than hardcoding the [-1, 1]² cube.
        private double[] _axisValues = Array.Empty<double>();
        private int _maxCount;
        private const int IterBins = 24;
        private const double IterMin = 1;
        private const double IterMax = 1000;

        // Set by the dialog before SetRuns so axis labels read "Density"
        // instead of "Sweep axis". Pattern matches SweepLineChart.AxisName.
        public string AxisName { get; set; } = "Sweep axis";

        public IterationsSurfaceChart()
        {
            _overlay = new OverlayElement(this);
            AddVisualChild(_viewport);
            AddVisualChild(_overlay);
            AddLogicalChild(_viewport);
            AddLogicalChild(_overlay);

            _viewport.Camera = _camera;
            _viewport.ClipToBounds = true;

            var lights = new Model3DGroup();
            lights.Children.Add(new AmbientLight(Color.FromRgb(60, 60, 70)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(220, 220, 220),
                new Vector3D(-0.4, -1, -0.6)));
            _viewport.Children.Add(new ModelVisual3D { Content = lights });
            _viewport.Children.Add(_modelVisual);

            UpdateCamera();

            MouseLeftButtonDown += OnDragBegin;
            MouseMove += OnDragMove;
            MouseLeftButtonUp += OnDragEnd;
            MouseWheel += OnWheel;
            Cursor = Cursors.Hand;
        }

        protected override int VisualChildrenCount => 2;
        protected override Visual GetVisualChild(int index) => index == 0 ? (Visual)_viewport : _overlay;

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Viewport + overlay share the same rect so the overlay's local
            // coordinates match screen coords produced by projection.
            var rect = new Rect(0, TitleStrip, finalSize.Width, Math.Max(0, finalSize.Height - TitleStrip));
            _viewport.Arrange(rect);
            _overlay.Arrange(rect);
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _viewport.Measure(availableSize);
            _overlay.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var title = Label("Iterations surface", 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));
        }

        public override void SetRuns(IReadOnlyList<BenchmarkSolverRun> runs, IReadOnlyList<Color> colors)
        {
            base.SetRuns(runs, colors);
            RebuildMesh();
            _overlay.InvalidateVisual();
        }

        private void OnDragBegin(object sender, MouseButtonEventArgs e)
        {
            _dragAnchor = e.GetPosition(this);
            CaptureMouse();
        }

        private void OnDragMove(object sender, MouseEventArgs e)
        {
            if (_dragAnchor == null) return;
            var p = e.GetPosition(this);
            var dx = p.X - _dragAnchor.Value.X;
            var dy = p.Y - _dragAnchor.Value.Y;
            _yawDeg += dx * 0.4;
            _pitchDeg = Math.Max(-85, Math.Min(-5, _pitchDeg + dy * 0.4));
            _dragAnchor = p;
            UpdateCamera();
        }

        private void OnDragEnd(object sender, MouseButtonEventArgs e)
        {
            _dragAnchor = null;
            ReleaseMouseCapture();
        }

        private void OnWheel(object sender, MouseWheelEventArgs e)
        {
            _distance *= e.Delta > 0 ? 0.9 : 1.1;
            _distance = Math.Max(1.5, Math.Min(10, _distance));
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            var yaw = _yawDeg * Math.PI / 180;
            var pitch = _pitchDeg * Math.PI / 180;
            var cx = _distance * Math.Cos(pitch) * Math.Sin(yaw);
            var cy = -_distance * Math.Sin(pitch);
            var cz = _distance * Math.Cos(pitch) * Math.Cos(yaw);
            _camera.Position = new Point3D(cx, cy, cz);
            _camera.LookDirection = new Vector3D(-cx, -cy, -cz);
            _camera.UpDirection = new Vector3D(0, 1, 0);
            _overlay.InvalidateVisual();
        }

        private void RebuildMesh()
        {
            _modelVisual.Content = null;
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
            {
                foreach (var run in byAxis[x])
                {
                    foreach (var game in run.Games)
                    {
                        var iters = Math.Max(1, game.Iterations);
                        var bin = (int)((Math.Log10(iters) - logMin) / logStep);
                        if (bin < 0) bin = 0;
                        if (bin >= IterBins) bin = IterBins - 1;
                        grid[x, bin]++;
                    }
                }
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
                var nx = NormalizeX(x, xCount);
                for (var y = 0; y < IterBins; y++)
                {
                    var ny = NormalizeZ(y, IterBins);
                    var nz = (double)grid[x, y] / maxCount;
                    mesh.Positions.Add(new Point3D(nx, nz, ny));
                }
            }
            for (var x = 0; x < xCount - 1; x++)
            {
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
            }

            var heightBrush = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(40,  80, 150), 0.0),
                    new GradientStop(Color.FromRgb(50, 160, 200), 0.35),
                    new GradientStop(Color.FromRgb(220, 190, 90), 0.7),
                    new GradientStop(Color.FromRgb(200,  60,  60), 1.0),
                },
                new Point(0, 1), new Point(0, 0));
            heightBrush.Freeze();

            for (var i = 0; i < mesh.Positions.Count; i++)
            {
                var z = mesh.Positions[i].Y;
                mesh.TextureCoordinates.Add(new Point(0.5, 1.0 - z));
            }

            var material = new DiffuseMaterial(heightBrush);
            var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
            _modelVisual.Content = model;
        }

        // X: axis index → world x in [-1, 1]. Z (iteration bin index) uses
        // the same mapping. Kept as helpers so the axis/tick code and the
        // mesh code can't disagree on where cells land.
        private static double NormalizeX(int x, int xCount) => (x / (double)(xCount - 1) - 0.5) * 2;
        private static double NormalizeZ(int y, int yCount) => (y / (double)(yCount - 1) - 0.5) * 2;

        // 3D → 2D screen coordinates inside the viewport/overlay rect.
        // Returns null if the point is behind the camera plane. Math:
        //  1. Express (world - cameraPos) in camera's (right, up, forward)
        //     orthonormal basis.
        //  2. If depth ≤ 0 the point is behind the lens, bail.
        //  3. Horizontal FOV gives tan(halfFov); NDC x = cx / (depth*tan).
        //     Aspect multiplies the y term because FOV is horizontal.
        //  4. NDC [-1,1] → screen [0, size].
        private Point? WorldToScreen(Point3D world)
        {
            var w = _viewport.ActualWidth;
            var h = _viewport.ActualHeight;
            if (w <= 0 || h <= 0) return null;

            var forward = _camera.LookDirection;
            forward.Normalize();
            var upHint = _camera.UpDirection;
            var right = Vector3D.CrossProduct(forward, upHint);
            if (right.Length < 1e-9) return null;
            right.Normalize();
            var upOrtho = Vector3D.CrossProduct(right, forward);

            var rel = world - _camera.Position;
            var cx = Vector3D.DotProduct(rel, right);
            var cy = Vector3D.DotProduct(rel, upOrtho);
            var depth = Vector3D.DotProduct(rel, forward);
            if (depth <= 0.001) return null;

            var aspect = w / h;
            var tanHalf = Math.Tan(_camera.FieldOfView * Math.PI / 360);

            var ndcX = cx / (depth * tanHalf);
            var ndcY = cy * aspect / (depth * tanHalf);

            var sx = (ndcX + 1) * 0.5 * w;
            var sy = (1 - ndcY) * 0.5 * h;
            return new Point(sx, sy);
        }

        private static readonly Pen AxisPen3D = MakePen(Color.FromRgb(160, 160, 170), 1.2);
        private static readonly Pen TickPen3D = MakePen(Color.FromRgb(130, 130, 140), 1.0);

        private static Pen MakePen(Color c, double t)
        {
            var brush = new SolidColorBrush(c); brush.Freeze();
            var pen = new Pen(brush, t); pen.Freeze();
            return pen;
        }

        // Called by the overlay element. Draws three axis lines (the "base
        // corner" triad at world x=-1, y=0, z=-1) with tick marks and 2D
        // labels positioned via WorldToScreen. Called after every camera
        // update so the labels follow the rotation.
        internal void DrawAxes(DrawingContext dc, Size size)
        {
            if (_axisValues.Length < 2 || _maxCount == 0) return;

            const double xMin = -1, xMax = 1;
            const double zMin = -1, zMax = 1;
            const double yMin = 0, yMax = 1;

            // Base corner sits at the low end of X and Z, floor height.
            var origin = new Point3D(xMin, yMin, zMin);
            var xEnd = new Point3D(xMax, yMin, zMin);
            var zEnd = new Point3D(xMin, yMin, zMax);
            var yEnd = new Point3D(xMin, yMax, zMin);

            DrawLine3D(dc, origin, xEnd, AxisPen3D);
            DrawLine3D(dc, origin, zEnd, AxisPen3D);
            DrawLine3D(dc, origin, yEnd, AxisPen3D);

            // X ticks: pick up to 6 evenly-spaced sweep-axis indices so 21
            // densities don't turn into 21 labels stacked on top of each
            // other.
            var xTickCount = Math.Min(6, _axisValues.Length);
            for (var i = 0; i < xTickCount; i++)
            {
                var idx = (int)Math.Round(i * (_axisValues.Length - 1) / (double)(xTickCount - 1));
                var wx = NormalizeX(idx, _axisValues.Length);
                var p0 = new Point3D(wx, yMin, zMin);
                var p1 = new Point3D(wx, yMin, zMin - 0.08);
                DrawLine3D(dc, p0, p1, TickPen3D);
                DrawLabel3D(dc, p1 with { Z = zMin - 0.15 }, FormatAxisValue(_axisValues[idx]), TextAlignment.Center);
            }

            // Z (iterations) ticks at log decades: 1, 10, 100, 1000.
            for (var d = (int)Math.Log10(IterMin); d <= (int)Math.Log10(IterMax); d++)
            {
                var iter = Math.Pow(10, d);
                var t = (Math.Log10(iter) - Math.Log10(IterMin)) / (Math.Log10(IterMax) - Math.Log10(IterMin));
                var wz = zMin + t * (zMax - zMin);
                var p0 = new Point3D(xMin, yMin, wz);
                var p1 = new Point3D(xMin - 0.08, yMin, wz);
                DrawLine3D(dc, p0, p1, TickPen3D);
                DrawLabel3D(dc, new Point3D(xMin - 0.15, yMin, wz), $"{iter:0}", TextAlignment.Right);
            }

            // Y (height = count) ticks at 25% steps of the max, labelled
            // with actual game counts so the user can read "this bin holds
            // N games" off the chart.
            for (var i = 1; i <= 4; i++)
            {
                var t = i / 4.0;
                var wy = yMin + t * (yMax - yMin);
                var p0 = new Point3D(xMin, wy, zMin);
                var p1 = new Point3D(xMin - 0.06, wy, zMin);
                DrawLine3D(dc, p0, p1, TickPen3D);
                var count = (int)Math.Round(t * _maxCount);
                DrawLabel3D(dc, new Point3D(xMin - 0.12, wy, zMin), $"{count}", TextAlignment.Right);
            }

            // Axis titles placed slightly beyond each axis end.
            DrawLabel3D(dc, new Point3D(xMax + 0.2, yMin, zMin), AxisName, TextAlignment.Left);
            DrawLabel3D(dc, new Point3D(xMin, yMin, zMax + 0.2), "iterations", TextAlignment.Center);
            DrawLabel3D(dc, new Point3D(xMin, yMax + 0.15, zMin), "games", TextAlignment.Right);
        }

        private void DrawLine3D(DrawingContext dc, Point3D a, Point3D b, Pen pen)
        {
            var sa = WorldToScreen(a);
            var sb = WorldToScreen(b);
            if (sa == null || sb == null) return;
            dc.DrawLine(pen, sa.Value, sb.Value);
        }

        private void DrawLabel3D(DrawingContext dc, Point3D world, string text, TextAlignment align)
        {
            var s = WorldToScreen(world);
            if (s == null) return;
            var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                LabelFace, 10.5, LabelBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var dx = align switch
            {
                TextAlignment.Right => -ft.Width,
                TextAlignment.Center => -ft.Width / 2,
                _ => 0
            };
            dc.DrawText(ft, new Point(s.Value.X + dx, s.Value.Y - ft.Height / 2));
        }

        private static string FormatAxisValue(double v)
        {
            // Density sweeps pass fractional values (0.10…0.30). Everything
            // else (width, height, solver params) is an integer or near-int.
            if (v > 0 && v < 1) return $"{v * 100:0.#}%";
            return Math.Abs(v - Math.Round(v)) < 1e-6 ? $"{v:0}" : $"{v:0.##}";
        }

        // Tiny element that relays its OnRender to the parent so the axis
        // overlay draws on top of the viewport. Hit-test-invisible so mouse
        // events still reach the chart for drag/rotate.
        private sealed class OverlayElement : FrameworkElement
        {
            private readonly IterationsSurfaceChart _parent;
            public OverlayElement(IterationsSurfaceChart parent)
            {
                _parent = parent;
                IsHitTestVisible = false;
            }
            protected override void OnRender(DrawingContext dc)
            {
                _parent.DrawAxes(dc, RenderSize);
            }
        }
    }
}
