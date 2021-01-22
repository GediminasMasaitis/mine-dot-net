using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;
using MineDotNet.IO;
using MineDotNet.ML;
using MineDotNet.ML.Solvers;
using MineDotNet.Umsi;

namespace MineDotNet.GUI.Forms
{
    internal partial class MainForm : Form
    {
        private readonly IDisplayService _display;
        private readonly IGameHandler _gameHandler;
        
        private GameManager CurrentManualGameEngine { get; set; }

        private double MineDensity => MineDensityTrackBar.Value/(double) 100;
        private int MapWidth => (int)WidthNumericUpDown.Value;
        private int MapHeight => (int)HeightNumericUpDown.Value;

        public MainForm()
        {
            InitializeComponent();

            if (Designer.IsDesignTime)
            {
                return;
            } 

            _display = IOCC.GetService<IDisplayService>();
            _display.Target = MainPictureBox;

            _gameHandler = IOCC.GetService<IGameHandler>();
            _gameHandler.Target = MainPictureBox;
            _gameHandler.CellClick += DisplayOnCellClick;

            var defaultMap = new Map(12, 12, null, true);
            SetMapAndMasks(defaultMap, null);
        }

        public void SetMapAndMasks(Map map, IList<Mask> masks)
        {
            if (map != null)
            {
                MapTextVisualizers.SetMap(map);
            }

            MapTextVisualizers.SetMasks(masks);
            _gameHandler.Map = map;
            _display.DisplayMap(map, masks);
        }

        private void DisplayOnCellClick(object sender, CellClickEventArgs args)
        {
            if (CurrentManualGameEngine == null)
            {
                return;
            }

            var gameOver = false;
            if ((args.Buttons & MouseButtons.Right) != 0)
            {
                if (!CurrentManualGameEngine.GameStarted)
                {
                    return;
                }
                CurrentManualGameEngine.ToggleFlag(args.Coordinate);
            }
            else if((args.Buttons & MouseButtons.Left) != 0)
            {
                if (!CurrentManualGameEngine.GameStarted)
                {
                    CurrentManualGameEngine.StartWithMineDensity(MapWidth, MapHeight, args.Coordinate, true, MineDensity);
                }
                else
                {
                    var openResult = CurrentManualGameEngine.OpenCell(args.Coordinate);
                    gameOver = !openResult.OpenCorrect;
                }
            }

            var gameMap = CurrentManualGameEngine.CurrentMap;
            var regularMap = gameMap.ToRegularMap();
            MapTextVisualizers.SetMap(regularMap);
            var maskMaps = MapTextVisualizers.GetMasks();
            _display.DisplayMap(gameMap, maskMaps);

            if (gameOver)
            {
                MessageBox.Show($"Boom {args.Coordinate}");
            }
        }

        private void ShowMapsButton_Click(object sender, EventArgs e)
        {
            GetAndDisplayMap();
        }

        private void GetAndDisplayMap(IDictionary<Coordinate, SolverResult> results = null)
        {
            var map = MapTextVisualizers.GetMap();
            var maskMaps = MapTextVisualizers.GetMasks();
            _display.DisplayMap(map, maskMaps, results);
        }

        private void SolveMapButton_Click(object sender, EventArgs e)
        {
            var map = MapTextVisualizers.GetMap();
            var results = AI.AI.Solve(map);
            DisplayResults(map, results);
        }

        private void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            MapTextVisualizers.DisplayResults(map, results);
            GetAndDisplayMap(results);
        }

        private void AutoPlayButton_Click(object sender, EventArgs e)
        {
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.StartWithMineDensity(MapWidth, MapHeight, new Coordinate(MapWidth/2, MapHeight/2), true, MineDensity);
            while (true)
            {
                var regularMap = engine.CurrentMap.ToRegularMap();
                MapTextVisualizers.SetMap(regularMap);
                Application.DoEvents();
                var results = AI.AI.Solve(regularMap);
                if (results.Count == 0)
                {
                    MessageBox.Show("Done");
                    return;
                }
                foreach (var result in results)
                {
                    switch (result.Value.Verdict)
                    {
                        case true:
                            engine.SetFlag(result.Key, CellFlag.HasMine);
                            break;
                        case false:
                            var openCorrect = engine.OpenCell(result.Key).OpenCorrect;
                            if (!openCorrect)
                            {
                                regularMap[result.Key].State = CellState.Mine;
                                MapTextVisualizers.SetMap(regularMap);
                                DisplayResults(regularMap, results);
                                MessageBox.Show($"Boom {result.Key}");
                                return;
                            }
                            break;
                        case null:
                            break;
                    }
                }
                DisplayResults(regularMap, results);
                Application.DoEvents();
                Thread.Sleep(100);
            }
        }

        private void MineDensityTrackBar_ValueChanged(object sender, EventArgs e)
        {
            MineDensityLabel.Text = $"Mine density: {MineDensityTrackBar.Value}%";
        }

        private void ManualPlayButton_Click(object sender, EventArgs e)
        {
            CurrentManualGameEngine = new GameManager(new GameMapGenerator(), new GameEngine());
            var emptyMap = new Map(MapWidth, MapHeight, null, true, CellState.Filled);
            SetMapAndMasks(emptyMap, null);
        }

        private void MLTestButton_Click(object sender, EventArgs e)
        {
            var map = MapTextVisualizers.GetMap();
            using var umsiProgram = new UmsiProgram();
            var visualizer = new TextMapVisualizer();
            var solver = new UmsiSolver(umsiProgram, visualizer);
            solver.Solve(map);

            //var random = new Random(0);
            //var gen = new TrainMapGenerator(random, new GameManager(new GameMapGenerator(random), new GameEngine()));
            //var maps = gen.CreateMaps().Take(1000).ToList();
            ////foreach (var map in maps)
            ////{
            //    _display.DisplayMap(maps[0], null);
            ////}

            //var solver = new MlSolver();
            //solver.Run(maps);
        }
    }
}
