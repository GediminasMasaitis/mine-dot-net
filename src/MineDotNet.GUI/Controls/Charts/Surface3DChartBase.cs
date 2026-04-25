using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Shared infrastructure for any "rotatable 3D surface" chart:
    //   - Viewport3D + PerspectiveCamera + ModelVisual3D
    //   - Ambient + directional lighting
    //   - Trackball-ish mouse drag (yaw + clamped pitch) and wheel zoom
    //   - 3D → 2D screen projection helper (WorldToScreen)
    //   - 2D overlay child that calls DrawAxesOverlay on top of the 3D scene
    //   - Title strip rendered in OnRender
    //
    // Subclasses provide the mesh (BuildMesh, assigning to ModelVisual.Content)
    // and the axis overlay (DrawAxesOverlay, using the protected DrawLine3D /
    // DrawLabel3D helpers). Base class handles everything else, so adding a
    // new 3D chart type is ~100 lines of data-to-mesh plus axis drawing.
    internal abstract class Surface3DChartBase : ChartBase
    {
        private readonly Viewport3D _viewport = new Viewport3D();
        private readonly PerspectiveCamera _camera = new PerspectiveCamera { FieldOfView = 45 };
        private readonly OverlayElement _overlay;

        private double _yawDeg = 35;
        private double _pitchDeg = -30;
        private double _distance = 3.2;
        private Point? _dragAnchor;
        // Drag throttle: mouse move fires 120+ times a second, and each full
        // Viewport3D repaint is CPU-software-rendered. Skip camera updates
        // that land inside a ~16ms window (≈60 FPS) so we don't build a
        // backlog of render work and introduce perceptible input lag.
        private int _lastUpdateTick;
        private const int MinUpdateMs = 16;

        protected const double TitleStrip = 22;

        // Subclass-writable — subclasses set Content here during BuildMesh.
        protected ModelVisual3D ModelVisual { get; } = new ModelVisual3D();

        protected abstract string ChartTitle { get; }
        protected abstract void BuildMesh();
        protected abstract void DrawAxesOverlay(DrawingContext dc);

        protected Surface3DChartBase()
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
            _viewport.Children.Add(ModelVisual);

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
            var title = Label(ChartTitle, 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));
        }

        public override void SetRuns(IReadOnlyList<BenchmarkSolverRun> runs, IReadOnlyList<Color> colors)
        {
            base.SetRuns(runs, colors);
            BuildMesh();
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
            var now = Environment.TickCount;
            if (now - _lastUpdateTick >= MinUpdateMs)
            {
                UpdateCamera();
                _lastUpdateTick = now;
            }
        }

        private void OnDragEnd(object sender, MouseButtonEventArgs e)
        {
            _dragAnchor = null;
            ReleaseMouseCapture();
            // Flush a final render so the displayed angle matches the last
            // mouse position even if the previous move was throttle-dropped.
            UpdateCamera();
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

        // 3D → 2D. Math: project (world - cameraPos) onto camera's basis,
        // divide by depth, apply horizontal-FOV + aspect scaling, map NDC
        // [-1, 1] to the viewport rect. Returns null for points behind the
        // camera plane so callers skip drawing them instead of flipping to
        // a mirror position on screen.
        protected Point? WorldToScreen(Point3D world)
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

        protected static readonly Pen AxisPen3D = MakePen(Color.FromRgb(160, 160, 170), 1.2);
        protected static readonly Pen TickPen3D = MakePen(Color.FromRgb(130, 130, 140), 1.0);

        private static Pen MakePen(Color c, double t)
        {
            var brush = new SolidColorBrush(c); brush.Freeze();
            var pen = new Pen(brush, t); pen.Freeze();
            return pen;
        }

        protected void DrawLine3D(DrawingContext dc, Point3D a, Point3D b, Pen pen)
        {
            var sa = WorldToScreen(a);
            var sb = WorldToScreen(b);
            if (sa == null || sb == null) return;
            dc.DrawLine(pen, sa.Value, sb.Value);
        }

        protected void DrawLabel3D(DrawingContext dc, Point3D world, string text, TextAlignment align)
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

        // Shared gradient used by both surface variants so "tall peaks read
        // warm, valleys read cool" matches the user's mental map regardless
        // of which chart they're looking at.
        protected static LinearGradientBrush MakeHeightGradient()
        {
            var g = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(40,  80, 150), 0.0),
                    new GradientStop(Color.FromRgb(50, 160, 200), 0.35),
                    new GradientStop(Color.FromRgb(220, 190, 90), 0.7),
                    new GradientStop(Color.FromRgb(200,  60,  60), 1.0),
                },
                new Point(0, 1), new Point(0, 0));
            g.Freeze();
            return g;
        }

        protected static string FormatAxisValue(double v)
        {
            if (v > 0 && v < 1) return $"{v * 100:0.#}%";
            return Math.Abs(v - Math.Round(v)) < 1e-6 ? $"{v:0}" : $"{v:0.##}";
        }

        private sealed class OverlayElement : FrameworkElement
        {
            private readonly Surface3DChartBase _parent;
            public OverlayElement(Surface3DChartBase parent)
            {
                _parent = parent;
                IsHitTestVisible = false;
            }
            protected override void OnRender(DrawingContext dc)
            {
                _parent.DrawAxesOverlay(dc);
            }
        }
    }
}
