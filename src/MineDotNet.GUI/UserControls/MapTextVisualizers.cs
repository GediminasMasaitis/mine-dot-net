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

namespace MineDotNet.GUI.UserControls
{
    internal partial class MapTextVisualizers : UserControl
    {
        private IList<TextBox> MaskTextBoxes { get; }

        private readonly TextMapParser _parser;
        private readonly TextMapVisualizer _visualizer;

        private readonly IBrushProvider _brushes;

        public MapTextVisualizers()
        {
            InitializeComponent();

            _parser = new TextMapParser();
            _visualizer = new TextMapVisualizer();
            _brushes = IOCC.GetService<IBrushProvider>();

            MaskTextBoxes = new List<TextBox>();
        }

        public void SetMaskCount(int maskCount)
        {
            var offsetX = 170;
            var offsetY = 0;
            
            for (var i = 1; i <= maskCount; i++)
            {
                var newTextBox = new TextBox();
                newTextBox.Parent = this;
                newTextBox.Location = new Point(Map0TextBox.Location.X + offsetX * i, Map0TextBox.Location.Y + offsetY * i);
                newTextBox.Multiline = Map0TextBox.Multiline;
                newTextBox.Size = Map0TextBox.Size;
                newTextBox.Anchor = Map0TextBox.Anchor;
                newTextBox.AcceptsReturn = Map0TextBox.AcceptsReturn;

                var newLabel = new Label();
                newLabel.Parent = this;
                newLabel.Location = new Point(Map0Label.Location.X + offsetX * i, Map0Label.Location.Y + offsetY * i);
                newLabel.Text = $"Mask {i}:";
                newLabel.ForeColor = _brushes.Brushes[i].Color;
                newLabel.Anchor = Map0Label.Anchor;

                MaskTextBoxes.Add(newTextBox);
            }

            for (var i = 0; i < MaskTextBoxes.Count; i++)
            {
                MaskTextBoxes[i].ForeColor = _brushes.Brushes[i].Color;
                MaskTextBoxes[i].Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Bold);
            }
        }

        public void SetMaps(IList<Map> allMaps)
        {
            SetMap(allMaps[0]);
            for (var i = 1; i < allMaps.Count; i++)
            {
                if (i >= MaskTextBoxes.Count)
                {
                    break;
                }
                var maskStr = _visualizer.VisualizeToString(allMaps[i]);
                MaskTextBoxes[i].Text = maskStr;
            }
        }

        public void SetMap(Map map)
        {
            var mapText = _visualizer.VisualizeToString(map);
            Map0TextBox.Text = mapText;
        }

        public Map GetMap()
        {
            var mapStr = Map0TextBox.Text.Replace(";", Environment.NewLine);
            if (string.IsNullOrWhiteSpace(mapStr))
            {
                // TODO: Handle this
            }
            var map = _parser.Parse(mapStr);
            return map;
        }

        public IList<MaskMap> GetMaskMaps()
        {
            var maps = new List<MaskMap>();
            for (var i = 0; i < MaskTextBoxes.Count; i++)
            {
                var mapStr = MaskTextBoxes[i].Text.Replace(";", Environment.NewLine);
                if (string.IsNullOrWhiteSpace(mapStr))
                {
                    continue;
                }
                var map = _parser.Parse(mapStr);
                var maskMap = MaskMap.FromMap(map);
                maps.Add(maskMap);
            }
            return maps;
        }

        public void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            if (results != null)
            {
                var maskHasMine = GetMask(results, true, map.Width, map.Height);
                var maskDoesntHaveMine = GetMask(results, false, map.Width, map.Height);
                MaskTextBoxes[0].Text = _visualizer.VisualizeToString(maskDoesntHaveMine);
                MaskTextBoxes[1].Text = _visualizer.VisualizeToString(maskHasMine);
            }
        }

        private Map GetMask(IDictionary<Coordinate, SolverResult> results, bool targetVerdict, int width, int height)
        {
            var map = new Map(width, height, null, true);
            foreach (var result in results)
            {
                if (result.Value.Verdict == targetVerdict)
                {
                    map[result.Key].State = CellState.Filled;
                }
            }
            return map;
        }
    }
}
