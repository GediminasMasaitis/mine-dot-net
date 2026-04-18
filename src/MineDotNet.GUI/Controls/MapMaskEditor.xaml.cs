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
        private TextBox _mapBox;
        // Tracks the column index offset inside ColumnsPanel: 0 is the map column, then
        // masks. Removing a mask at index N means removing Children at N+1.

        private const int MinMaskCount = 7;
        private const double ColumnWidth = 128;

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
            var dock = new DockPanel { Margin = new Thickness(0, 0, 8, 0), LastChildFill = true, Width = ColumnWidth };
            var header = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Foreground = headerColor,
                Margin = new Thickness(2, 0, 0, 4)
            };
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
            DockPanel.SetDock(header, Dock.Top);
            dock.Children.Add(header);
            dock.Children.Add(box);
            return (dock, box);
        }

        private void SetMaskCount(int count)
        {
            count = Math.Max(count, MinMaskCount);
            while (_maskBoxes.Count < count)
            {
                var idx = _maskBoxes.Count;
                var color = new SolidColorBrush(_palette.MaskColors[idx]);
                color.Freeze();
                var (panel, box) = CreateColumn($"MASK {idx}", color);
                ColumnsPanel.Children.Add(panel);
                _maskBoxes.Add(box);
            }
            while (_maskBoxes.Count > count)
            {
                var idx = _maskBoxes.Count - 1;
                ColumnsPanel.Children.RemoveAt(idx + 1); // +1 for the map column
                _maskBoxes.RemoveAt(idx);
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
