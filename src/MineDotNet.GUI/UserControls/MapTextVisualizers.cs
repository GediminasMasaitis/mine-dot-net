using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;
using MineDotNet.IO;

namespace MineDotNet.GUI.UserControls
{
    internal sealed partial class MapTextVisualizers : UserControl
    {
        private readonly IStringMapParser _parser;
        private readonly IStringMapVisualizer _visualizer;
        private readonly IMaskConverter _maskConverter;
        
        private readonly IBrushProvider _brushes;
        private readonly IList<TextBox> _maskTextBoxes;
        private readonly IList<Label> _maskLabels;

        private const int MinMaskCount = 5;

        public MapTextVisualizers()
        {
            InitializeComponent();

            Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Bold);
            MapTextBox.Font = Font;

            _parser = IOCC.GetService<IStringMapParser>();
            _visualizer = IOCC.GetService<IStringMapVisualizer>();
            _brushes = IOCC.GetService<IBrushProvider>();
            _maskConverter = IOCC.GetService<IMaskConverter>();

            _maskTextBoxes = new List<TextBox>();
            _maskLabels = new List<Label>();
            SetMaskCount(MinMaskCount);
        }

        private void SetMaskCount(int maskCount)
        {
            var difference = maskCount - _maskTextBoxes.Count;
            if (difference > 0)
            {
                AddMasks(difference);
            }
            else if (difference < 0)
            {
                RemoveMasks(-difference);
            }
        }

        private void AddMasks(int maskCount)
        {
            const int offsetX = 170;
            const int offsetY = 0;

            var totalMasks = _maskTextBoxes.Count + maskCount;
            for (var i = _maskTextBoxes.Count; i < totalMasks; i++)
            {
                var newTextBox = new TextBox();
                newTextBox.Parent = this;
                newTextBox.Location = new Point(MapTextBox.Location.X + offsetX * i, MapTextBox.Location.Y + offsetY * i);
                newTextBox.Multiline = MapTextBox.Multiline;
                newTextBox.Size = MapTextBox.Size;
                newTextBox.Anchor = MapTextBox.Anchor;
                newTextBox.AcceptsReturn = MapTextBox.AcceptsReturn;

                var newLabel = new Label();
                newLabel.Parent = this;
                newLabel.Location = new Point(MapLabel.Location.X + offsetX * i, MapLabel.Location.Y + offsetY * i);
                newLabel.Text = $"Mask {i}:";
                newLabel.ForeColor = _brushes.Brushes[i].Color;
                newLabel.Anchor = MapLabel.Anchor;

                _maskTextBoxes.Add(newTextBox);
                _maskLabels.Add(newLabel);
            }

            for (var i = 0; i < _maskTextBoxes.Count; i++)
            {
                _maskTextBoxes[i].ForeColor = _brushes.Brushes[i].Color;
                _maskTextBoxes[i].Font = Font;
            }
        }

        private void RemoveMasks(int maskCount)
        {
            for (var i = 0; i < maskCount; i++)
            {
                var index = _maskTextBoxes.Count - 1;
                _maskTextBoxes[index].Dispose();
                _maskLabels[index].Dispose();
                _maskTextBoxes.RemoveAt(index);
                _maskLabels.RemoveAt(index);
            }
        }

        public void SetMaps(IList<Map> allMaps)
        {
            SetMap(allMaps[0]);
            for (var i = 1; i < allMaps.Count; i++)
            {
                if (i >= _maskTextBoxes.Count)
                {
                    break;
                }
                var maskStr = _visualizer.VisualizeToString(allMaps[i]);
                _maskTextBoxes[i].Text = maskStr;
            }
        }

        public void SetMap(Map map)
        {
            var mapText = _visualizer.VisualizeToString(map);
            MapTextBox.Text = mapText;
        }

        public void SetMasks(IList<Mask> masks)
        {
            if (masks == null)
            {
                SetMaskCount(0);
                return;
            }

            SetMaskCount(masks.Count);
            var maps = _maskConverter.ConvertToMaps(masks).ToList();
            for (var i = 0; i < masks.Count; i++)
            {
                var maskStr = _visualizer.VisualizeToString(maps[i]);
                _maskTextBoxes[i].Text = maskStr;
            }
        }

        public Map GetMap()
        {
            var mapStr = MapTextBox.Text.Replace(";", Environment.NewLine);
            if (string.IsNullOrWhiteSpace(mapStr))
            {
                // TODO: Handle this
            }
            var map = _parser.Parse(mapStr);
            return map;
        }

        public IList<Mask> GetMasks()
        {
            var maps = new List<Mask>();
            for (var i = 0; i < _maskTextBoxes.Count; i++)
            {
                var mapStr = _maskTextBoxes[i].Text.Replace(";", Environment.NewLine);
                if (string.IsNullOrWhiteSpace(mapStr))
                {
                    continue;
                }
                var map = _parser.Parse(mapStr);
                var maskMap = _maskConverter.ConvertToMask(map);
                maps.Add(maskMap);
            }
            return maps;
        }

        public void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            SetMaskCount(2);
            if (results != null)
            {
                var maskHasMine = _maskConverter.ConvertToMask(results, true, map.Width, map.Height);
                var mapHasMine = _maskConverter.ConvertToMap(maskHasMine);
                _maskTextBoxes[0].Text = _visualizer.VisualizeToString(mapHasMine);

                var maskDoesntHaveMine = _maskConverter.ConvertToMask(results, false, map.Width, map.Height);
                var mapDoesntHaveMine = _maskConverter.ConvertToMap(maskDoesntHaveMine);
                _maskTextBoxes[1].Text = _visualizer.VisualizeToString(mapDoesntHaveMine);
            }
        }
    }
}
