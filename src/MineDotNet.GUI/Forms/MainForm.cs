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
using MineDotNet.Game.Models;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;
using MineDotNet.IO;

namespace MineDotNet.GUI.Forms
{
    internal partial class MainForm : Form
    {
        private readonly IDisplayService _display;
        private readonly IGameHandler _gameHandler;
        
        private GameManager CurrentManualGameEngine { get; set; }
        private CancellationTokenSource _autoPlayCts;
        private IDictionary<Coordinate, SolverResult> _lastResults;

        // Index of the mask that holds ground-truth mine positions. Written
        // by Generate; read by Auto play. The solver never sees it — it only
        // reads the main map textbox.
        private const int MinesMaskIndex = 0;

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

            // Stream the UMSI transcript into the log textbox. Fires from
            // whatever thread is driving the solver (currently the UI thread,
            // since Solve is synchronous), but we marshal defensively so
            // this keeps working once Solve goes async.
            ExtSolver.Logged += OnExtSolverLogged;
        }

        private void OnExtSolverLogged(string line, bool sent)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, bool>(OnExtSolverLogged), line, sent);
                return;
            }
            CommLogTextBox.AppendText((sent ? "→ " : "← ") + line + Environment.NewLine);

            // Cap growth — trim the oldest quarter when we're over budget so
            // a long session doesn't balloon memory. Cheap because it only
            // triggers every few hundred lines.
            const int MaxChars = 50000;
            if (CommLogTextBox.TextLength > MaxChars)
            {
                var keepFrom = CommLogTextBox.TextLength - (MaxChars * 3 / 4);
                CommLogTextBox.Text = CommLogTextBox.Text.Substring(keepFrom);
                CommLogTextBox.SelectionStart = CommLogTextBox.TextLength;
            }
            CommLogTextBox.ScrollToCaret();
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
            // Only one implementation exists now — the external UMSI engine
            // (ExtSolver). The C# BorderSeparationSolver was removed as
            // strictly less capable than the C++ version it was mirroring.
            // `entry.SolverImplementation` is ignored for now; kept on the
            // entry for forward compatibility if more engines arrive.
            ExtSolver.Instance.InitSolver(entry.Settings);
            return ExtSolver.Instance;
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
                // Source of truth is the main textbox + Mask 0 (mines). If
                // no mines mask exists yet (cold start), generate a fresh
                // board and seed both textbox and Mask 0 so there's
                // something to play. Otherwise we trust whatever's there —
                // including user edits to either channel.
                if (MapTextVisualizers.GetMask(MinesMaskIndex) == null)
                {
                    GenerateFreshGame();
                }

                while (!token.IsCancellationRequested)
                {
                    var playerView = MapTextVisualizers.GetMap();
                    var minesMask = MapTextVisualizers.GetMask(MinesMaskIndex);
                    if (minesMask == null || minesMask.Width != playerView.Width || minesMask.Height != playerView.Height)
                    {
                        MessageBox.Show("Mask 0 (mines) is missing or doesn't match the map size.");
                        return;
                    }

                    // Rebuild a disposable engine from the current textbox +
                    // Mask 0 each iteration. Lets edits to either channel take
                    // effect immediately, and keeps the engine out of being a
                    // competing state store — it's purely a mechanism for
                    // applying verdicts.
                    var engine = BuildEngineFromState(playerView, minesMask);

                    var results = SolveMap(playerView);
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
                                    var boom = engine.CurrentMap.ToRegularMap();
                                    boom[result.Key].State = CellState.Mine;
                                    MapTextVisualizers.SetMap(boom);
                                    DisplayResults(boom, results);
                                    MessageBox.Show($"Boom {result.Key}");
                                    return;
                                }
                                break;
                            case null:
                                break;
                        }
                    }
                    var postIteration = engine.CurrentMap.ToRegularMap();
                    MapTextVisualizers.SetMap(postIteration);
                    DisplayResults(postIteration, results);
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
            // Wipe game-state masks so a manual session doesn't inherit
            // mines (Mask 0) or solver verdicts (Mask 1/2) from a prior
            // Generate/Solve. User-authored masks in slots 3+ are left alone.
            MapTextVisualizers.ClearMask(0);
            MapTextVisualizers.ClearMask(1);
            MapTextVisualizers.ClearMask(2);
            var emptyMap = new Map(MapWidth, MapHeight, null, true, CellState.Filled);
            SetMapAndMasks(emptyMap, null);
        }

        // Generates a fresh minesweeper board at the current W/H/density and
        // opens the centre cell (same initial-safe-click trick auto-play
        // uses), then displays the resulting partially-revealed state. Does
        // not start a manual game (CurrentManualGameEngine stays null, so
        // clicks don't trigger play) and does not auto-solve — the user
        // drives what happens next with Solve, Auto play, etc.
        private void GenerateButton_Click(object sender, EventArgs e)
        {
            CurrentManualGameEngine = null;
            _lastResults = null;
            GenerateFreshGame();
        }

        // Generates a random board at the current W/H/density, seeds both
        // the main textbox (player view) and Mask 0 (ground-truth mines),
        // clears stale solver-verdict masks, and re-renders. Shared between
        // the Generate button and Auto play's cold-start fallback so both
        // paths leave the UI in the same initial state.
        private void GenerateFreshGame()
        {
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.StartWithMineDensity(MapWidth, MapHeight, new Coordinate(MapWidth / 2, MapHeight / 2), true, MineDensity);
            var playerView = engine.CurrentMap.ToRegularMap();
            var minesMap = BuildMinesMaskMap(engine.CurrentMap);

            MapTextVisualizers.SetMap(playerView);
            MapTextVisualizers.SetMask(MinesMaskIndex, minesMap);
            // Stale solver-verdict overlays from a previous Solve would be
            // meaningless on the newly generated board.
            MapTextVisualizers.ClearMask(1);
            MapTextVisualizers.ClearMask(2);
            _gameHandler.Map = playerView;
            _display.DisplayMap(playerView, MapTextVisualizers.GetMasks());
        }

        // Converts a GameMap's mine layout into a regular Map where mine cells
        // are CellState.Filled (rendered as `#`) and the rest CellState.Empty
        // (rendered as `.`). Stored in Mask 0 by Generate; read back by
        // Auto play to know which covered cells are mines.
        private static Map BuildMinesMaskMap(GameMap gm)
        {
            var cells = gm.AllCells.Select(gc => new Cell(
                gc.Coordinate,
                gc.HasMine ? CellState.Filled : CellState.Empty,
                CellFlag.None,
                0)).ToList();
            return new Map(cells, null);
        }

        // Reconstructs a playable GameMap from the main textbox (player view)
        // plus a mine-positions mask. Hints are recomputed from the mask so
        // edits in the main textbox don't desync. Used by Auto play every
        // iteration so textbox/mask edits take effect immediately.
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
                gc.Hint = gc.HasMine
                    ? 0
                    : gm.CalculateNeighboursOf(gc.Coordinate).Count(n => n.HasMine);
            }
            gm.RemainingMineCount = gm.AllCells.Count(c => c.HasMine);
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.Start(gm);
            return engine;
        }

    }
}
