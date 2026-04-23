using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;
using MineDotNet.IO;

namespace MineDotNet.GUI.Controls
{
    public partial class MapMaskEditor : UserControl
    {
        private IStringMapParser _parser;
        private IStringMapVisualizer _visualizer;
        private IMaskConverter _maskConverter;
        private IPaletteProvider _palette;

        private readonly List<TextBox> _maskBoxes = new List<TextBox>();
        // Parallel to _maskBoxes. Drives whether the mask renders on the board
        // (GetVisibleMasks filters on it). Default checked — matches old
        // behaviour of "all masks visible". Toggling fires VisibilityChanged
        // so MainWindow can repaint.
        private readonly List<CheckBox> _maskVisibleChecks = new List<CheckBox>();
        private TextBox _mapBox;
        // Tracks the column index offset inside ColumnsPanel: 0 is the map column, then
        // masks. Removing a mask at index N means removing Children at N+1.

        private const int MinMaskCount = 7;
        private const double ColumnWidth = 128;

        // Fires when any mask's visibility checkbox toggles. Listeners should
        // re-render whatever surface consumes GetVisibleMasks().
        public event EventHandler VisibilityChanged;

        public MapMaskEditor()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _parser = IOCC.GetService<IStringMapParser>();
                _visualizer = IOCC.GetService<IStringMapVisualizer>();
                _maskConverter = IOCC.GetService<IMaskConverter>();
                _palette = IOCC.GetService<IPaletteProvider>();
                BuildMapColumn();
                SetMaskCount(MinMaskCount);
            }
        }

        private void BuildMapColumn()
        {
            var mapColor = new SolidColorBrush(Color.FromRgb(230, 230, 235));
            mapColor.Freeze();
            var (panel, box) = CreateColumn("MAP", mapColor);
            _mapBox = box;
            ColumnsPanel.Children.Add(panel);
        }

        private (FrameworkElement panel, TextBox box) CreateColumn(string title, Brush headerColor)
        {
            var (panel, box, _) = CreateColumnInternal(title, headerColor, includeVisibilityCheck: false);
            return (panel, box);
        }

        private (FrameworkElement panel, TextBox box, CheckBox check) CreateMaskColumn(string title, Brush headerColor)
            => CreateColumnInternal(title, headerColor, includeVisibilityCheck: true);

        private (FrameworkElement panel, TextBox box, CheckBox check) CreateColumnInternal(string title, Brush headerColor, bool includeVisibilityCheck)
        {
            var dock = new DockPanel { Margin = new Thickness(0, 0, 8, 0), LastChildFill = true, Width = ColumnWidth };

            var headerRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var header = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Foreground = headerColor,
                Margin = new Thickness(2, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(header, 0);
            headerRow.Children.Add(header);

            CheckBox check = null;
            if (includeVisibilityCheck)
            {
                check = new CheckBox
                {
                    IsChecked = true,
                    ToolTip = "Show on board",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 2, 0)
                };
                check.Checked += OnMaskVisibilityToggled;
                check.Unchecked += OnMaskVisibilityToggled;
                Grid.SetColumn(check, 1);
                headerRow.Children.Add(check);
            }

            var box = new TextBox
            {
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontSize = 12,
                AcceptsReturn = true,
                AcceptsTab = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Foreground = headerColor
            };
            DockPanel.SetDock(headerRow, Dock.Top);
            dock.Children.Add(headerRow);
            dock.Children.Add(box);
            return (dock, box, check);
        }

        private void OnMaskVisibilityToggled(object sender, RoutedEventArgs e)
        {
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetMaskCount(int count)
        {
            count = Math.Max(count, MinMaskCount);
            while (_maskBoxes.Count < count)
            {
                var idx = _maskBoxes.Count;
                var color = new SolidColorBrush(_palette.MaskColors[idx]);
                color.Freeze();
                var (panel, box, check) = CreateMaskColumn($"MASK {idx}", color);
                ColumnsPanel.Children.Add(panel);
                _maskBoxes.Add(box);
                _maskVisibleChecks.Add(check);
            }
            while (_maskBoxes.Count > count)
            {
                var idx = _maskBoxes.Count - 1;
                ColumnsPanel.Children.RemoveAt(idx + 1); // +1 for the map column
                _maskBoxes.RemoveAt(idx);
                _maskVisibleChecks.RemoveAt(idx);
            }
        }

        public void SetMap(Map map) => _mapBox.Text = map == null ? string.Empty : _visualizer.VisualizeToString(map);

        public void SetMasks(IList<Mask> masks)
        {
            if (masks == null)
            {
                foreach (var b in _maskBoxes) b.Text = string.Empty;
                return;
            }
            SetMaskCount(masks.Count);
            var maps = _maskConverter.ConvertToMaps(masks).ToList();
            for (var i = 0; i < masks.Count; i++)
            {
                _maskBoxes[i].Text = _visualizer.VisualizeToString(maps[i]);
            }
        }

        public void SetMask(int index, Map map)
        {
            if (index < 0 || index >= _maskBoxes.Count) return;
            _maskBoxes[index].Text = _visualizer.VisualizeToString(map);
        }

        public void ClearMask(int index)
        {
            if (index < 0 || index >= _maskBoxes.Count) return;
            _maskBoxes[index].Text = string.Empty;
        }

        public Map GetMap()
        {
            var text = _mapBox.Text.Replace(";", Environment.NewLine);
            return string.IsNullOrWhiteSpace(text) ? null : _parser.Parse(text);
        }

        public Mask GetMask(int index)
        {
            if (index < 0 || index >= _maskBoxes.Count) return null;
            var text = _maskBoxes[index].Text.Replace(";", Environment.NewLine);
            if (string.IsNullOrWhiteSpace(text)) return null;
            return _maskConverter.ConvertToMask(_parser.Parse(text));
        }

        public IList<Mask> GetMasks()
        {
            var result = new List<Mask>();
            foreach (var box in _maskBoxes)
            {
                var text = box.Text.Replace(";", Environment.NewLine);
                if (string.IsNullOrWhiteSpace(text)) continue;
                result.Add(_maskConverter.ConvertToMask(_parser.Parse(text)));
            }
            return result;
        }

        // Like GetMasks, but drops masks whose per-column visibility checkbox
        // is unchecked. Used by the board render path so "hide" really hides.
        // Engine/solver callers should keep using GetMasks / GetMask to see
        // everything regardless of what the user is currently displaying.
        public IList<Mask> GetVisibleMasks()
        {
            var result = new List<Mask>();
            for (var i = 0; i < _maskBoxes.Count; i++)
            {
                if (_maskVisibleChecks[i].IsChecked != true) continue;
                var text = _maskBoxes[i].Text.Replace(";", Environment.NewLine);
                if (string.IsNullOrWhiteSpace(text)) continue;
                result.Add(_maskConverter.ConvertToMask(_parser.Parse(text)));
            }
            return result;
        }

        public void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            SetMaskCount(3);
            if (results == null) return;
            var mines = _maskConverter.ConvertToMask(results, true, map.Width, map.Height);
            _maskBoxes[1].Text = _visualizer.VisualizeToString(_maskConverter.ConvertToMap(mines));
            var safe = _maskConverter.ConvertToMask(results, false, map.Width, map.Height);
            _maskBoxes[2].Text = _visualizer.VisualizeToString(_maskConverter.ConvertToMap(safe));
        }
    }
}
