using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MineDotNet.AI.Guessers;
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
        private CancellationTokenSource _autoPlayCts;
        private IDictionary<Coordinate, SolverResult> _lastResults;

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

            var defaultMap = new Map(16, 30, null, true);
            SetMapAndMasks(defaultMap, null);

            this.SizeChanged += (s, e) =>
            {
                if (MainPictureBox.Width <= 0 || MainPictureBox.Height <= 0) return;
                GetAndDisplayMap(_lastResults);
            };
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
            var results = SolveMap(map);
            DisplayResults(map, results);
        }

        // Runs the solvers the user has checked in the SolversListEditor, in
        // list order, merging verdicts/probabilities (later entries win on
        // collision). Falls back to the app-wide default (ExtSolver with
        // built-in defaults) when nothing is checked, so the button still
        // works out of the box. After running solvers, if no definitive
        // verdict is produced, a LowestProbabilityGuesser guess is appended
        // — same fallback that AI.AI.Solve applies.
        private IDictionary<Coordinate, SolverResult> SolveMap(IMap map)
        {
            var entries = solversListEditor1.GetCheckedEntries();
            IDictionary<Coordinate, SolverResult> results;
            if (entries.Count == 0)
            {
                results = AI.AI.Solve(map);
            }
            else
            {
                results = new Dictionary<Coordinate, SolverResult>();
                foreach (var entry in entries)
                {
                    var solver = CreateSolver(entry);
                    foreach (var kv in solver.Solve(map))
                    {
                        results[kv.Key] = kv.Value;
                    }
                }
                if (!results.Any(x => x.Value.Verdict.HasValue))
                {
                    var guess = new LowestProbabilityGuesser().Guess(map, results);
                    if (guess != null)
                    {
                        results[guess.Coordinate] = guess;
                    }
                }
            }
            return results;
        }

        private static ISolver CreateSolver(SolverListEntry entry)
        {
            if (entry.SolverImplementation == ExtSolver.Alias)
            {
                ExtSolver.Instance.InitSolver(entry.Settings);
                return ExtSolver.Instance;
            }
            return new BorderSeparationSolver(entry.Settings);
        }

        private void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            _lastResults = results;
            MapTextVisualizers.DisplayResults(map, results);
            GetAndDisplayMap(results);
        }

        private async void AutoPlayButton_Click(object sender, EventArgs e)
        {
            _autoPlayCts?.Cancel();
            _autoPlayCts = new CancellationTokenSource();
            var token = _autoPlayCts.Token;

            AutoPlayButton.Enabled = false;
            try
            {
                var engine = new GameManager(new GameMapGenerator(), new GameEngine());
                engine.StartWithMineDensity(MapWidth, MapHeight, new Coordinate(MapWidth/2, MapHeight/2), true, MineDensity);
                while (!token.IsCancellationRequested)
                {
                    var regularMap = engine.CurrentMap.ToRegularMap();
                    MapTextVisualizers.SetMap(regularMap);
                    var results = SolveMap(regularMap);
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
                    await Task.Delay(100, token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                AutoPlayButton.Enabled = true;
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
        }
    }
}
