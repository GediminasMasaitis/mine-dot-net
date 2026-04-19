using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MineDotNet.AI.Guessers;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;
using MineDotNet.Game.Models;
using MineDotNet.GUI.Controls;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;
using Wpf.Ui.Controls;

namespace MineDotNet.GUI.Views
{
    public partial class MainWindow : FluentWindow
    {
        // Mask 0 is reserved for ground-truth mine positions; Auto play reads it back
        // when applying verdicts since the solver only sees the player-view map.
        private const int MinesMaskIndex = 0;

        private GameManager _manualGame;
        private CancellationTokenSource _autoPlayCts;
        private IDictionary<Coordinate, SolverResult> _lastResults;
        private MinesweeperBoard _board;
        private BorderSeparationSolverSettings _solverSettings = new BorderSeparationSolverSettings();

        private double MineDensity => DensitySlider.Value / 100.0;
        private int MapWidth => (int)WidthBox.Value;
        private int MapHeight => (int)HeightBox.Value;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _board = BoardHost.Board;
            _board.CellClick += OnBoardCellClick;

            var defaultMap = new Map(16, 30, null, true);
            SetMapAndMasks(defaultMap, null);

            ExtSolver.Logged += OnExtSolverLogged;
        }

        public void SetMapAndMasks(Map map, IList<Mask> masks)
        {
            if (map != null) MapEditor.SetMap(map);
            MapEditor.SetMasks(masks);
            _board?.SetState(map, masks, _lastResults);
        }

        private void OnBoardCellClick(object sender, BoardCellClickEventArgs e)
        {
            if (_manualGame == null) return;

            var gameOver = false;
            if (e.Button == MouseButton.Right)
            {
                if (!_manualGame.GameStarted) return;
                _manualGame.ToggleFlag(e.Coordinate);
            }
            else if (e.Button == MouseButton.Left)
            {
                if (!_manualGame.GameStarted)
                {
                    _manualGame.StartWithMineDensity(MapWidth, MapHeight, e.Coordinate, true, MineDensity);
                }
                else
                {
                    gameOver = !_manualGame.OpenCell(e.Coordinate).OpenCorrect;
                }
            }

            var gameMap = _manualGame.CurrentMap;
            MapEditor.SetMap(gameMap.ToRegularMap());
            _board.SetState(gameMap, MapEditor.GetMasks(), null);

            if (gameOver) System.Windows.MessageBox.Show(this, $"Boom {e.Coordinate}", Title);
        }

        private void OnExtSolverLogged(string line, bool sent)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<string, bool>(OnExtSolverLogged), line, sent);
                return;
            }
            LogBox.AppendText((sent ? "→ " : "← ") + line + Environment.NewLine);

            // Trim the oldest quarter once we exceed the cap so a long session can't
            // run away on memory. The scroll stays pinned to the bottom.
            const int MaxChars = 50000;
            if (LogBox.Text.Length > MaxChars)
            {
                LogBox.Text = LogBox.Text.Substring(LogBox.Text.Length - MaxChars * 3 / 4);
                LogBox.CaretIndex = LogBox.Text.Length;
            }
            LogBox.ScrollToEnd();
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e) => LogBox.Clear();

        private void DensitySlider_OnValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (DensityLabel != null) DensityLabel.Text = $"Mine density: {(int)DensitySlider.Value}%";
        }

        private void ShowBtn_Click(object sender, RoutedEventArgs e) => RedrawFromEditor();

        private void RedrawFromEditor(IDictionary<Coordinate, SolverResult> results = null)
        {
            var map = MapEditor.GetMap();
            var masks = MapEditor.GetMasks();
            _board.SetState(map, masks, results);
        }

        private void SolveBtn_Click(object sender, RoutedEventArgs e)
        {
            var map = MapEditor.GetMap();
            if (map == null) return;
            var results = SolveMap(map);
            DisplayResults(map, results);
        }

        // Runs the solver with the currently configured settings. Routes through
        // DirectSolver (in-process P/Invoke) or ExtSolver (UMSI stdio child
        // process) based on SolverSelection.UseDirect — set by the main window's
        // checkbox. Falls back to the guesser if no definitive verdict is
        // produced, same as AI.AI.Solve does.
        private IDictionary<Coordinate, SolverResult> SolveMap(IMap map)
        {
            var results = new Dictionary<Coordinate, SolverResult>();
            if (SolverSelection.UseDirect)
            {
                DirectSolver.Instance.InitSolver(_solverSettings);
                foreach (var kv in DirectSolver.Instance.Solve(map)) results[kv.Key] = kv.Value;
            }
            else
            {
                ExtSolver.Instance.InitSolver(_solverSettings);
                foreach (var kv in ExtSolver.Instance.Solve(map)) results[kv.Key] = kv.Value;
            }

            if (!results.Any(x => x.Value.Verdict.HasValue))
            {
                var guess = new LowestProbabilityGuesser().Guess(map, results);
                if (guess != null) results[guess.Coordinate] = guess;
            }
            return results;
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SolverSettingsDialog(_solverSettings) { Owner = this };
            if (dialog.ShowDialog() == true) _solverSettings = dialog.GetSettings();
        }

        private void DirectSolverCheck_OnChanged(object sender, RoutedEventArgs e)
        {
            SolverSelection.UseDirect = DirectSolverCheck.IsChecked == true;
        }

        private void BenchmarkBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BenchmarkDialog { Owner = this };
            dialog.ShowDialog();
        }

        private void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            _lastResults = results;
            MapEditor.DisplayResults(map, results);
            RedrawFromEditor(results);
        }

        private async void AutoPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            _autoPlayCts?.Cancel();
            _autoPlayCts = new CancellationTokenSource();
            var token = _autoPlayCts.Token;

            AutoPlayBtn.IsEnabled = false;
            try
            {
                if (MapEditor.GetMask(MinesMaskIndex) == null) GenerateFreshGame();

                while (!token.IsCancellationRequested)
                {
                    var playerView = MapEditor.GetMap();
                    var minesMask = MapEditor.GetMask(MinesMaskIndex);
                    if (minesMask == null || minesMask.Width != playerView.Width || minesMask.Height != playerView.Height)
                    {
                        System.Windows.MessageBox.Show(this, "Mask 0 (mines) is missing or doesn't match the map size.", Title);
                        return;
                    }

                    // Rebuild a disposable engine from (textbox + Mask 0) each iteration —
                    // lets edits to either channel take effect immediately, and keeps the
                    // engine out of being a competing state store.
                    var engine = BuildEngineFromState(playerView, minesMask);
                    var results = SolveMap(playerView);
                    if (results.Count == 0)
                    {
                        System.Windows.MessageBox.Show(this, "Done", Title);
                        return;
                    }
                    foreach (var r in results)
                    {
                        switch (r.Value.Verdict)
                        {
                            case true:
                                engine.SetFlag(r.Key, CellFlag.HasMine);
                                break;
                            case false:
                                if (!engine.OpenCell(r.Key).OpenCorrect)
                                {
                                    var boom = engine.CurrentMap.ToRegularMap();
                                    boom[r.Key].State = CellState.Mine;
                                    MapEditor.SetMap(boom);
                                    DisplayResults(boom, results);
                                    System.Windows.MessageBox.Show(this, $"Boom {r.Key}", Title);
                                    return;
                                }
                                break;
                        }
                    }
                    var post = engine.CurrentMap.ToRegularMap();
                    MapEditor.SetMap(post);
                    DisplayResults(post, results);
                    await Task.Delay(100, token);
                }
            }
            catch (TaskCanceledException) { }
            finally { AutoPlayBtn.IsEnabled = true; }
        }

        private void ManualPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            _manualGame = new GameManager(new GameMapGenerator(), new GameEngine());
            // Wipe game-state masks so manual play doesn't inherit mines (mask 0) or
            // solver verdicts (1/2) from a prior Generate/Solve. Masks 3+ stay.
            MapEditor.ClearMask(0);
            MapEditor.ClearMask(1);
            MapEditor.ClearMask(2);
            var empty = new Map(MapWidth, MapHeight, null, true, CellState.Filled);
            SetMapAndMasks(empty, null);
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            _manualGame = null;
            _lastResults = null;
            GenerateFreshGame();
        }

        // Generates a random board at the current W/H/density, seeds the main textbox
        // (player view) and Mask 0 (ground-truth mines), clears stale solver-verdict
        // masks, and re-renders. Shared between Generate and Auto play's cold start.
        private void GenerateFreshGame()
        {
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.StartWithMineDensity(MapWidth, MapHeight, new Coordinate(MapWidth / 2, MapHeight / 2), true, MineDensity);
            var playerView = engine.CurrentMap.ToRegularMap();
            var mines = BuildMinesMaskMap(engine.CurrentMap);

            MapEditor.SetMap(playerView);
            MapEditor.SetMask(MinesMaskIndex, mines);
            MapEditor.ClearMask(1);
            MapEditor.ClearMask(2);
            _board.SetState(playerView, MapEditor.GetMasks(), null);
        }

        private static Map BuildMinesMaskMap(GameMap gm)
        {
            var cells = gm.AllCells.Select(gc => new Cell(
                gc.Coordinate,
                gc.HasMine ? CellState.Filled : CellState.Empty,
                CellFlag.None,
                0)).ToList();
            return new Map(cells, null);
        }

        // Reconstructs a playable GameMap from (player view + mine-positions mask). Hints
        // are recomputed from the mask so that edits in the main textbox don't desync.
        private static GameManager BuildEngineFromState(Map playerView, Mask minesMask)
        {
            var gm = new GameMap(playerView.Width, playerView.Height, 0, null, false);
            for (var i = 0; i < playerView.Width; i++)
            {
                for (var j = 0; j < playerView.Height; j++)
                {
                    var cell = playerView.Cells[i, j];
                    var hasMine = minesMask.Cells[i, j];
                    var state = cell?.State ?? CellState.Filled;
                    var flag = cell?.Flag ?? CellFlag.None;
                    gm.Cells[i, j] = new GameCell(new Coordinate(i, j), hasMine, state, flag, 0);
                }
            }
            foreach (var gc in gm.AllCells)
            {
                gc.Hint = gc.HasMine ? 0 : gm.CalculateNeighboursOf(gc.Coordinate).Count(n => n.HasMine);
            }
            gm.RemainingMineCount = gm.AllCells.Count(c => c.HasMine);
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.Start(gm);
            return engine;
        }
    }
}
