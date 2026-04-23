using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

        private CancellationTokenSource _autoPlayCts;
        private IDictionary<Coordinate, SolverResult> _lastResults;
        private MinesweeperBoard _board;
        private BorderSeparationSolverSettings _solverSettings = new BorderSeparationSolverSettings();

        // ExtSolver.Logged fires on whichever thread is driving the solver —
        // that's the UI thread for buttons here, but a threadpool worker when
        // the benchmark runs. Either way, calling Dispatcher.BeginInvoke per
        // line (old behaviour) floods the UI queue with thousands of O(n)
        // TextBox appends and locks the whole app. Instead, buffer under a
        // lock and let a DispatcherTimer flush at a fixed cadence.
        private readonly StringBuilder _extLogBuffer = new StringBuilder();
        private readonly object _extLogBufferLock = new object();
        private DispatcherTimer _extLogFlushTimer;

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
            _extLogFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _extLogFlushTimer.Tick += OnExtLogFlushTick;
            _extLogFlushTimer.Start();

            // Redraw when the user toggles any mask's visibility checkbox.
            // Preserves the current solver-result overlay so probabilities
            // stick around across visibility flips.
            MapEditor.VisibilityChanged += (_, __) => RedrawFromEditor(_lastResults);
        }

        public void SetMapAndMasks(Map map, IList<Mask> masks)
        {
            if (map != null) MapEditor.SetMap(map);
            MapEditor.SetMasks(masks);
            // Read back from the editor so the board honours the per-mask
            // visibility checkboxes (SetMasks populates all of them; the
            // checkboxes are new columns default-checked, so freshly loaded
            // content is visible unless the user toggled off a column later).
            _board?.SetState(map, MapEditor.GetVisibleMasks(), _lastResults);
        }

        // Board clicks always act on the MapEditor's state. If Mask 0 (the
        // ground-truth mines) isn't set yet, the first left-click seeds a
        // fresh game centred on the clicked cell — classic Minesweeper
        // safe-first-click behaviour. Once Mask 0 exists, every click
        // rebuilds a disposable engine from (player view + Mask 0) and
        // writes the post-action state back, exactly like Auto play does
        // per iteration. One state path for every interaction.
        private void OnBoardCellClick(object sender, BoardCellClickEventArgs e)
        {
            var playerView = MapEditor.GetMap();
            if (playerView == null) return;

            var minesMask = MapEditor.GetMask(MinesMaskIndex);
            var cold = minesMask == null
                       || minesMask.Width != playerView.Width
                       || minesMask.Height != playerView.Height;

            if (cold)
            {
                // Right-click on an unseeded board does nothing — nothing to
                // flag. Left-click kicks off a new game.
                if (e.Button != MouseButton.Left) return;

                var fresh = new GameManager(new GameMapGenerator(), new GameEngine());
                fresh.StartWithMineDensity(MapWidth, MapHeight, e.Coordinate, true, MineDensity);
                var post = fresh.CurrentMap.ToRegularMap();
                MapEditor.SetMap(post);
                MapEditor.SetMask(MinesMaskIndex, BuildMinesMaskMap(fresh.CurrentMap));
                MapEditor.ClearMask(1);
                MapEditor.ClearMask(2);
                _lastResults = null;
                _board.SetState(post, MapEditor.GetVisibleMasks(), null);
                return;
            }

            var manager = BuildEngineFromState(playerView, minesMask);
            var gameOver = false;
            if (e.Button == MouseButton.Right)
            {
                manager.ToggleFlag(e.Coordinate);
            }
            else if (e.Button == MouseButton.Left)
            {
                gameOver = !manager.OpenCell(e.Coordinate).OpenCorrect;
            }
            else
            {
                return;
            }

            var updated = manager.CurrentMap.ToRegularMap();
            MapEditor.SetMap(updated);
            _board.SetState(updated, MapEditor.GetVisibleMasks(), _lastResults);

            if (gameOver) System.Windows.MessageBox.Show(this, $"Boom {e.Coordinate}", Title);
        }

        private void OnExtSolverLogged(string line, bool sent)
        {
            // Runs on whichever thread the solver is on. Must not touch WPF
            // directly — just buffer; the flush timer copies to the TextBox.
            lock (_extLogBufferLock)
            {
                _extLogBuffer.Append(sent ? "→ " : "← ").Append(line).Append(Environment.NewLine);
                // Cap buffer growth so a chatty benchmark doesn't blow memory.
                const int MaxBufferChars = 200_000;
                if (_extLogBuffer.Length > MaxBufferChars * 2)
                {
                    var tail = _extLogBuffer.ToString(_extLogBuffer.Length - MaxBufferChars, MaxBufferChars);
                    _extLogBuffer.Clear();
                    _extLogBuffer.Append(tail);
                }
            }
        }

        private void OnExtLogFlushTick(object sender, EventArgs e)
        {
            string toAppend;
            lock (_extLogBufferLock)
            {
                if (_extLogBuffer.Length == 0) return;
                toAppend = _extLogBuffer.ToString();
                _extLogBuffer.Clear();
            }
            LogBox.AppendText(toAppend);

            // Trim the oldest quarter once we exceed the cap so a long session can't
            // run away on memory. The scroll stays pinned to the bottom.
            const int MaxChars = 50_000;
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
            var masks = MapEditor.GetVisibleMasks();
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

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
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
            _board.SetState(playerView, MapEditor.GetVisibleMasks(), null);
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
