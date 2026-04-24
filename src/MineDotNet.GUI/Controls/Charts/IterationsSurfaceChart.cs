using System;
using System.Collections.Generic;
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
    //   X = sweep axis value (low density → high density)
    //   Y = iteration count (log-binned, 1 → 1000)
    //   Z = count of games that fell into that (axis, iters) bin
    //
    // Built on native WPF 3D (Viewport3D + PerspectiveCamera). Left-drag
    // rotates the camera around the origin, mouse wheel zooms. No external
    // deps. Title is drawn in the top strip via OnRender; the viewport
    // occupies the rest of the chart's cell.
    internal sealed class IterationsSurfaceChart : ChartBase
    {
        private readonly Viewport3D _viewport = new Viewport3D();
        private readonly PerspectiveCamera _camera = new PerspectiveCamera { FieldOfView = 45 };
        private readonly ModelVisual3D _modelVisual = new ModelVisual3D();

        // Spherical camera coords orbiting the origin. Initial angle looks
        // "down and at an angle" so the surface's shape is obvious without
        // the user having to fiddle.
        private double _yawDeg = 35;
        private double _pitchDeg = -30;
        private double _distance = 3.2;
        private Point? _dragAnchor;

        private const double TitleStrip = 22;

        public IterationsSurfaceChart()
        {
            AddVisualChild(_viewport);
            AddLogicalChild(_viewport);

            _viewport.Camera = _camera;
            _viewport.ClipToBounds = true;

            // Lighting: one ambient term so no face is pitch black, plus a
            // directional "sun" from the upper-back so height ridges cast
            // subtle shading on the downslope side.
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

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _viewport;

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Reserve the top strip for the title; the viewport fills the rest.
            _viewport.Arrange(new Rect(0, TitleStrip, finalSize.Width, Math.Max(0, finalSize.Height - TitleStrip)));
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _viewport.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var title = Label("Iterations surface (drag to rotate)", 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));
        }

        public override void SetRuns(IReadOnlyList<BenchmarkSolverRun> runs, IReadOnlyList<Color> colors)
        {
            base.SetRuns(runs, colors);
            RebuildMesh();
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
            // Clamp pitch so the camera can't flip over the pole — avoids
            // up-vector degeneracy and the "upside-down suddenly" feel.
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
            var cy = -_distance * Math.Sin(pitch); // -sin so positive pitch looks down
            var cz = _distance * Math.Cos(pitch) * Math.Cos(yaw);
            _camera.Position = new Point3D(cx, cy, cz);
            _camera.LookDirection = new Vector3D(-cx, -cy, -cz);
            _camera.UpDirection = new Vector3D(0, 1, 0);
        }

        private void RebuildMesh()
        {
            _modelVisual.Content = null;

            // Only meaningful for sweep data (one run per axis value). For
            // non-sweep runs there's only one X column so the "surface"
            // collapses to a line — not worth rendering.
            var sweepRuns = Runs.Where(r => r.AxisValue.HasValue).ToList();
            if (sweepRuns.Count < 2) return;

            var byAxis = sweepRuns
                .GroupBy(r => r.AxisValue.Value)
                .OrderBy(g => g.Key)
                .ToList();
            var xCount = byAxis.Count;
            if (xCount < 2) return;

            // Log-binned iteration count. 20 bins from 1 to 1000 matches
            // realistic game lengths (MaxIterationsPerGame = 1000 in the
            // runner).
            const int yCount = 24;
            const double iterMin = 1;
            const double iterMax = 1000;
            var logMin = Math.Log10(iterMin);
            var logMax = Math.Log10(iterMax);
            var logStep = (logMax - logMin) / yCount;

            var grid = new int[xCount, yCount];
            for (var x = 0; x < xCount; x++)
            {
                foreach (var run in byAxis[x])
                {
                    foreach (var game in run.Games)
                    {
                        var iters = Math.Max(1, game.Iterations);
                        var bin = (int)((Math.Log10(iters) - logMin) / logStep);
                        if (bin < 0) bin = 0;
                        if (bin >= yCount) bin = yCount - 1;
                        grid[x, bin]++;
                    }
                }
            }

            var maxCount = 0;
            for (var x = 0; x < xCount; x++)
                for (var y = 0; y < yCount; y++)
                    if (grid[x, y] > maxCount) maxCount = grid[x, y];
            if (maxCount == 0) return;

            // Build vertex grid centred on the origin in [-1, 1]² with z in
            // [0, 1]. Triangles connect adjacent cells into a surface.
            var mesh = new MeshGeometry3D();
            for (var x = 0; x < xCount; x++)
            {
                var nx = (x / (double)(xCount - 1) - 0.5) * 2;
                for (var y = 0; y < yCount; y++)
                {
                    var ny = (y / (double)(yCount - 1) - 0.5) * 2;
                    var nz = (double)grid[x, y] / maxCount;
                    mesh.Positions.Add(new Point3D(nx, nz, ny));
                }
            }

            for (var x = 0; x < xCount - 1; x++)
            {
                for (var y = 0; y < yCount - 1; y++)
                {
                    var i00 = x * yCount + y;
                    var i01 = x * yCount + (y + 1);
                    var i10 = (x + 1) * yCount + y;
                    var i11 = (x + 1) * yCount + (y + 1);

                    mesh.TriangleIndices.Add(i00);
                    mesh.TriangleIndices.Add(i01);
                    mesh.TriangleIndices.Add(i11);

                    mesh.TriangleIndices.Add(i00);
                    mesh.TriangleIndices.Add(i11);
                    mesh.TriangleIndices.Add(i10);
                }
            }

            // Height-keyed colour ramp via a vertical gradient brush applied
            // as a VisualBrush on the material. Low ridges read cool, tall
            // peaks read warm — same at-a-glance convention as the outcome
            // colours.
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

            // For a gradient to map by height we need TextureCoordinates per
            // vertex: u = 0 (doesn't matter), v = 1 - (z/1). WPF3D samples
            // the material's brush at these coords.
            for (var i = 0; i < mesh.Positions.Count; i++)
            {
                var z = mesh.Positions[i].Y; // y coordinate in our space holds the height
                mesh.TextureCoordinates.Add(new Point(0.5, 1.0 - z));
            }

            var material = new DiffuseMaterial(heightBrush);
            var model = new GeometryModel3D(mesh, material)
            {
                BackMaterial = material
            };

            var group = new Model3DGroup();
            group.Children.Add(model);
            _modelVisual.Content = group;
        }
    }
}
