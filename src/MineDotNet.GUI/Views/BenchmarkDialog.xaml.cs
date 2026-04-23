using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using MineDotNet.AI.Solvers;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;
using Wpf.Ui.Controls;

namespace MineDotNet.GUI.Views
{
    public partial class BenchmarkDialog : FluentWindow
    {
        private readonly ObservableCollection<SolverRow> _solverRows = new ObservableCollection<SolverRow>();
        private readonly ObservableCollection<ResultRow> _resultRows = new ObservableCollection<ResultRow>();
        private bool _stopRequested;
        private bool _running;

        public BenchmarkDialog()
        {
            InitializeComponent();
            SolversList.ItemsSource = _solverRows;
            ResultsList.ItemsSource = _resultRows;
            // Start with one reasonable default so the Run button works out of the box.
            _solverRows.Add(new SolverRow(new BenchmarkSolverConfig("Default", new BorderSeparationSolverSettings())));
            // Inherit the main window's solver choice as the starting point — user
            // can override per-run via this checkbox if they want A/B timing.
            DirectSolverCheck.IsChecked = SolverSelection.UseDirect;
            // Populate the solver-parameter sweep picker. Only numeric properties
            // are meaningful to sweep (bools can be A/B'd by adding two solvers).
            foreach (var name in GetSweepableParameterNames()) ParamBox.Items.Add(name);
            if (ParamBox.Items.Count > 0) ParamBox.SelectedIndex = 0;
            UpdateButtonStates();
            Closing += OnClosing;
        }

        private static IEnumerable<string> GetSweepableParameterNames()
        {
            foreach (var p in typeof(BorderSeparationSolverSettings).GetProperties())
            {
                if (!p.CanRead || !p.CanWrite) continue;
                var t = p.PropertyType;
                if (t == typeof(int) || t == typeof(long) || t == typeof(double)) yield return p.Name;
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Don't let the user yank the window mid-run — Run() is still on the
            // stack via PushFrame and closing now would leave the runner touching
            // disposed controls in its finally. Flip the stop flag instead and
            // let the run unwind; the user can click X again once it settles.
            if (_running)
            {
                e.Cancel = true;
                _stopRequested = true;
                StopBtn.IsEnabled = false;
                ProgressLabel.Text = "Stopping...";
            }
        }

        private void DensitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DensityLabel != null) DensityLabel.Text = $"Mine density: {(int)DensitySlider.Value}%";
        }

        private void SweepBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ComboBox raises SelectionChanged during initial XAML load (because
            // the default item has IsSelected="True"), before our named fields
            // have been assigned. Bail until the tree is actually wired up.
            if (SweepPanel == null || SweepFromBox == null) return;

            var axis = CurrentSweepAxis();
            SweepPanel.Visibility = axis == BenchmarkSweepAxis.None ? Visibility.Collapsed : Visibility.Visible;
            ParamPanel.Visibility = axis == BenchmarkSweepAxis.SolverParameter ? Visibility.Visible : Visibility.Collapsed;

            // Preset sensible From/To/Step values per axis so the user gets
            // something reasonable on first toggle without having to think.
            switch (axis)
            {
                case BenchmarkSweepAxis.Width:
                case BenchmarkSweepAxis.Height:
                    SweepFromBox.Value = 10; SweepToBox.Value = 30; SweepStepBox.Value = 1;
                    break;
                case BenchmarkSweepAxis.MineDensity:
                    // Density values are percent in the UI; we convert on commit.
                    SweepFromBox.Value = 10; SweepToBox.Value = 30; SweepStepBox.Value = 1;
                    break;
                case BenchmarkSweepAxis.SolverParameter:
                    // Re-apply param-based presets (centred on the property's
                    // current value) now that the param panel is visible.
                    ApplyParamPresets();
                    break;
            }
        }

        private void ParamBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SweepFromBox == null) return;
            if (CurrentSweepAxis() == BenchmarkSweepAxis.SolverParameter) ApplyParamPresets();
        }

        // Pick a starting range centred on the property's current value in the
        // first configured solver, so the user doesn't have to hunt for a sane
        // From/To. Bools skip this (the sweep will binarize via OverrideSetting).
        private void ApplyParamPresets()
        {
            var name = ParamBox.SelectedItem as string;
            if (string.IsNullOrEmpty(name)) return;
            var prop = typeof(BorderSeparationSolverSettings).GetProperty(name);
            if (prop == null || _solverRows.Count == 0) return;
            var current = prop.GetValue(_solverRows[0].Config.Settings);
            double v;
            try { v = Convert.ToDouble(current); } catch { v = 0; }
            // Step always defaults to 1 per user preference. From/To centre
            // on the property's current value with a fixed ±5 window; user
            // can widen manually for large-valued params.
            SweepFromBox.Value = Math.Max(0, v - 5);
            SweepToBox.Value = v + 5;
            SweepStepBox.Value = 1;
        }

        private BenchmarkSweepAxis CurrentSweepAxis()
        {
            if (SweepBox == null || SweepBox.SelectedIndex <= 0) return BenchmarkSweepAxis.None;
            return SweepBox.SelectedIndex switch
            {
                1 => BenchmarkSweepAxis.Width,
                2 => BenchmarkSweepAxis.Height,
                3 => BenchmarkSweepAxis.MineDensity,
                4 => BenchmarkSweepAxis.SolverParameter,
                _ => BenchmarkSweepAxis.None
            };
        }

        private string SweepAxisLabel(BenchmarkSweepAxis a) => a switch
        {
            BenchmarkSweepAxis.Width => "Width",
            BenchmarkSweepAxis.Height => "Height",
            BenchmarkSweepAxis.MineDensity => "Mine density",
            BenchmarkSweepAxis.SolverParameter => (ParamBox?.SelectedItem as string) ?? "Parameter",
            _ => ""
        };

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var settings = new BorderSeparationSolverSettings();
            var defaultName = $"Config {_solverRows.Count + 1}";
            var dialog = new SolverSettingsDialog(settings, defaultName) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            var name = dialog.GetName() ?? defaultName;
            _solverRows.Add(new SolverRow(new BenchmarkSolverConfig(name, dialog.GetSettings())));
            SolversList.SelectedIndex = _solverRows.Count - 1;
            UpdateButtonStates();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SolversList.SelectedItem is not SolverRow row) return;
            var dialog = new SolverSettingsDialog(row.Config.Settings, row.Config.Name) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            row.Config.Settings = dialog.GetSettings();
            var newName = dialog.GetName();
            if (!string.IsNullOrEmpty(newName) && newName != row.Config.Name)
            {
                row.Config.Name = newName;
                row.Refresh();
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var idx = SolversList.SelectedIndex;
            if (idx < 0) return;
            _solverRows.RemoveAt(idx);
            UpdateButtonStates();
        }

        private void SolversList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtonStates();

        private void UpdateButtonStates()
        {
            var selected = SolversList.SelectedIndex >= 0;
            EditBtn.IsEnabled = selected;
            DeleteBtn.IsEnabled = selected;
            RunBtn.IsEnabled = _solverRows.Count > 0;
        }

        private async void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_solverRows.Count == 0) return;

            var axis = CurrentSweepAxis();
            var config = new BenchmarkConfig
            {
                Width = (int)WidthBox.Value,
                Height = (int)HeightBox.Value,
                MineDensity = DensitySlider.Value / 100.0,
                GameCount = (int)GamesBox.Value,
                Parallelism = (int)(ParallelismBox.Value ?? 1),
                Solvers = _solverRows.Select(r => r.Config).ToList(),
                SweepAxis = axis,
                // Density is entered as percent in the UI for all axes but gets
                // converted to a fraction for the runner in the MineDensity case.
                SweepFrom = axis == BenchmarkSweepAxis.MineDensity ? (SweepFromBox.Value ?? 0) / 100.0 : (SweepFromBox.Value ?? 0),
                SweepTo = axis == BenchmarkSweepAxis.MineDensity ? (SweepToBox.Value ?? 0) / 100.0 : (SweepToBox.Value ?? 0),
                SweepStep = axis == BenchmarkSweepAxis.MineDensity ? (SweepStepBox.Value ?? 1) / 100.0 : (SweepStepBox.Value ?? 1),
                SweepParameterName = axis == BenchmarkSweepAxis.SolverParameter ? ParamBox.SelectedItem as string : null
            };

            _resultRows.Clear();
            foreach (var cfg in config.Solvers) _resultRows.Add(new ResultRow(cfg.Name));

            // Switch which chart set is visible based on sweep mode. Sweep charts
            // want (solver × axis-value) series; fixed charts want single-point
            // per solver. Showing the wrong set draws nothing useful.
            var inSweep = axis != BenchmarkSweepAxis.None;
            FixedChartsPanel.Visibility = inSweep ? Visibility.Collapsed : Visibility.Visible;
            SweepChartsPanel.Visibility = inSweep ? Visibility.Visible : Visibility.Collapsed;

            // Propagate the axis label so the sweep charts show something better
            // than "Parameter" in their titles and x-axis ticks.
            var axisLabel = SweepAxisLabel(axis);
            WinRateSweepChart.AxisName = axisLabel;
            AvgTimeSweepChart.AxisName = axisLabel;
            AvgIterationsSweepChart.AxisName = axisLabel;

            // Reset chart state too — otherwise a previous stopped run leaves its
            // data visible until the first game of the new run finishes and fires
            // the next progress update.
            var palette = IOCC.GetService<IPaletteProvider>();
            var solverColors = palette.MaskColors;
            var emptyRuns = new List<BenchmarkSolverRun>();
            if (inSweep)
            {
                foreach (var v in config.SweepValues())
                    for (var i = 0; i < config.Solvers.Count; i++)
                        emptyRuns.Add(new BenchmarkSolverRun(i, config.Solvers[i].Name, v));
            }
            else
            {
                for (var i = 0; i < config.Solvers.Count; i++)
                    emptyRuns.Add(new BenchmarkSolverRun(i, config.Solvers[i].Name));
            }
            OutcomeChart.SetRuns(emptyRuns, solverColors);
            CdfChart.SetRuns(emptyRuns, solverColors);
            ScatterChart.SetRuns(emptyRuns, solverColors);
            WinRateSweepChart.SetRuns(emptyRuns, solverColors);
            AvgTimeSweepChart.SetRuns(emptyRuns, solverColors);
            AvgIterationsSweepChart.SetRuns(emptyRuns, solverColors);

            _stopRequested = false;
            _running = true;
            RunBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
            CloseBtn.IsEnabled = false;
            ProgressLabel.Text = "Running...";
            ProgressBar.Value = 0;
            LogBox.Clear();

            var sw = Stopwatch.StartNew();
            var uiPumpMs = 0.0;

            // Buffer log lines in memory during the run and flush to the TextBox
            // on a throttled schedule (~20fps) via PumpDispatcher(). Appending
            // per-line to a WPF TextBox is O(n) in existing content, so thousands
            // of chatty solver messages would tank perf; setting Text once per
            // tick is much cheaper. Cap buffer growth to keep memory bounded.
            const int MaxLogChars = 200_000;
            var logBuffer = new StringBuilder();
            // ExtSolver.Logged fires on whichever thread calls Send/Read. With
            // Parallelism > 1 that's a pool of worker threads racing to append
            // to logBuffer. Lock guards every read/write so we don't corrupt
            // the StringBuilder.
            var logBufferLock = new object();
            var lastFlushTick = Environment.TickCount;
            IReadOnlyList<BenchmarkSolverRun> lastRuns = null;

            void FlushLog()
            {
                // Incremental flush: grab and clear whatever the worker threads
                // accumulated since the last tick, then append that delta to
                // the TextBox. Writing only the new slice is O(delta) instead
                // of O(full-buffer) per flush — at a chatty 20Hz cadence on a
                // 200K-char buffer, the old "LogBox.Text = fullBuffer" pattern
                // was moving megabytes per second through WPF layout.
                string delta;
                lock (logBufferLock)
                {
                    if (logBuffer.Length == 0) return;
                    delta = logBuffer.ToString();
                    logBuffer.Clear();
                }
                LogBox.AppendText(delta);
                // Cap the TextBox content so a long run doesn't grow the text
                // store unboundedly. Trim the oldest quarter when over cap,
                // same pattern MainWindow uses.
                if (LogBox.Text.Length > MaxLogChars)
                {
                    LogBox.Text = LogBox.Text.Substring(LogBox.Text.Length - MaxLogChars * 3 / 4);
                }
                LogBox.CaretIndex = LogBox.Text.Length;
                LogBox.ScrollToEnd();
            }

            // Push the latest runs into whichever chart set is active. Called
            // from the same throttled window as the log flush so we don't redraw
            // every game.
            void UpdateCharts()
            {
                if (lastRuns == null) return;
                if (inSweep)
                {
                    WinRateSweepChart.SetRuns(lastRuns, solverColors);
                    AvgTimeSweepChart.SetRuns(lastRuns, solverColors);
                    AvgIterationsSweepChart.SetRuns(lastRuns, solverColors);
                }
                else
                {
                    OutcomeChart.SetRuns(lastRuns, solverColors);
                    CdfChart.SetRuns(lastRuns, solverColors);
                    ScatterChart.SetRuns(lastRuns, solverColors);
                }
            }

            // When the log is on, ExtSolver.Logged fires once per line sent to and
            // received from the engine — that's thousands of events per second on
            // a chatty solver. Each fire does a StringBuilder.Append and, every
            // 50ms, a full TextBox flush + chart redraw + dispatcher pump. Letting
            // the user turn all of that off is the quickest way to see how much
            // the logging is actually costing on a given run.
            var liveLog = LiveLogCheck.IsChecked == true;
            Action<string, bool> logHandler = null;
            if (liveLog)
            {
                // Handler runs on worker threads — must NOT touch WPF controls
                // (ElapsedLabel, LogBox, etc.) or call PumpIfDue, which does.
                // Only append to the shared buffer under lock; the UI-side
                // PumpIfDue triggered by onProgress handles flushing.
                logHandler = (line, sent) =>
                {
                    lock (logBufferLock)
                    {
                        logBuffer.Append(sent ? "→ " : "← ").Append(line).Append('\n');
                        if (logBuffer.Length > MaxLogChars * 2)
                        {
                            // Keep only the tail so memory stays bounded on long runs.
                            var tail = logBuffer.ToString(logBuffer.Length - MaxLogChars, MaxLogChars);
                            logBuffer.Clear();
                            logBuffer.Append(tail);
                        }
                    }
                };
                ExtSolver.Logged += logHandler;
            }
            else
            {
                LogBox.Text = "(UMSI log disabled for this run — check to re-enable)";
            }

            // Throttled UI pump shared by the log handler and the progress
            // callback. Whichever fires first after the 50ms window triggers
            // the paint — so even with logging off, progress updates keep the
            // dialog alive.
            void PumpIfDue()
            {
                if (Environment.TickCount - lastFlushTick <= 50) return;
                var pumpSw = Stopwatch.StartNew();
                ElapsedLabel.Text = $"{sw.Elapsed:mm\\:ss}";
                if (liveLog) FlushLog();
                UpdateCharts();
                uiPumpMs += pumpSw.Elapsed.TotalMilliseconds;
                lastFlushTick = Environment.TickCount;
            }

            Action<BenchmarkProgressUpdate> onProgress = update =>
            {
                _resultRows[update.SolverIndex].Apply(update.LastResult);
                lastRuns = update.Runs;
                // GamesCompleted / TotalGames are counted in solver-games now
                // (one tick per solver finishing one board) — with parallelism
                // boards complete out of order, so per-solver granularity is
                // the finest consistent unit.
                ProgressLabel.Text = $"Game {update.GamesCompleted} of {update.TotalGames}";
                ProgressBar.Value = update.TotalGames > 0
                    ? 100.0 * update.GamesCompleted / update.TotalGames
                    : 0;
                PumpIfDue();
            };

            BenchmarkRunner runner = null;
            // Marshal progress back to the UI thread. Task.Run puts the runner
            // and its worker drain loop on the threadpool, so onProgress would
            // otherwise fire from a background thread; touching WPF from there
            // would throw. InvokeAsync queues on the UI dispatcher — fire and
            // forget, UI processes each at its own pace.
            var uiDispatcher = Dispatcher;
            Action<BenchmarkProgressUpdate> marshaledOnProgress = update =>
                uiDispatcher.InvokeAsync(() => onProgress(update));
            try
            {
                runner = new BenchmarkRunner { UseDirectSolver = DirectSolverCheck.IsChecked == true };
                // Run on a threadpool thread so the UI thread stays free to
                // pump input, paint, and handle queued InvokeAsync callbacks
                // throughout the whole benchmark.
                await Task.Run(() => runner.Run(config, marshaledOnProgress, () => _stopRequested));
                if (_stopRequested)
                {
                    ProgressLabel.Text = "Stopped";
                }
                else
                {
                    ProgressBar.Value = 100;
                    ProgressLabel.Text = "Done";
                }
            }
            catch (Exception ex)
            {
                ProgressLabel.Text = $"Error: {ex.Message}";
            }
            finally
            {
                if (logHandler != null) ExtSolver.Logged -= logHandler;
                if (liveLog) FlushLog();
                UpdateCharts();
                sw.Stop();
                ElapsedLabel.Text = $"{sw.Elapsed:mm\\:ss} total";

                // Where did the time go? Runner categories + UI-pump time we track
                // here + remainder ("other") should sum to wall time. Prints
                // directly into the LogBox (log-off mode clears the placeholder
                // so the breakdown still shows).
                if (runner != null)
                {
                    var total = sw.Elapsed.TotalMilliseconds;
                    var solver = runner.TotalSolverMs;
                    var init = runner.TotalInitMs;
                    var snap = runner.TotalSnapshotMs;
                    var build = runner.TotalEngineBuildMs;
                    var ops = runner.TotalEngineOpsMs;
                    var guesser = runner.TotalGuesserMs;
                    var other = Math.Max(0, total - solver - init - snap - build - ops - guesser - uiPumpMs);
                    string Pct(double ms) => total <= 0 ? "   0.0%" : $"{100.0 * ms / total,6:F1}%";
                    var lines = new StringBuilder();
                    lines.AppendLine();
                    lines.AppendLine("────────── timing breakdown ──────────");
                    lines.AppendLine($"total          {total,9:F0} ms");
                    lines.AppendLine($"  solver.Solve {solver,9:F0} ms  {Pct(solver)}  ({runner.TotalSolveCalls} calls, {(runner.TotalSolveCalls > 0 ? solver / runner.TotalSolveCalls : 0):F2} ms avg)");
                    lines.AppendLine($"  solver.Init  {init,9:F0} ms  {Pct(init)}");
                    lines.AppendLine($"  guesser      {guesser,9:F0} ms  {Pct(guesser)}");
                    lines.AppendLine($"  gen snapshot {snap,9:F0} ms  {Pct(snap)}");
                    lines.AppendLine($"  build engine {build,9:F0} ms  {Pct(build)}");
                    lines.AppendLine($"  engine ops   {ops,9:F0} ms  {Pct(ops)}");
                    lines.AppendLine($"  ui pump      {uiPumpMs,9:F0} ms  {Pct(uiPumpMs)}");
                    lines.AppendLine($"  other        {other,9:F0} ms  {Pct(other)}");
                    if (!liveLog) LogBox.Clear();
                    LogBox.AppendText(lines.ToString());
                    LogBox.ScrollToEnd();
                }

                _running = false;
                StopBtn.IsEnabled = false;
                CloseBtn.IsEnabled = true;
                UpdateButtonStates();
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _stopRequested = true;
            StopBtn.IsEnabled = false;
            ProgressLabel.Text = "Stopping...";
        }

        // Classic DoEvents pattern — spin the dispatcher until our sentinel
        // fires. Queued at Background priority so items at Input, Render, etc.
        // (all higher-priority) drain first. We NEED Input to drain here because
        // the Stop button click has to register mid-run. Re-entrancy is bounded:
        // Run/Close are disabled while _running, so only Stop's handler can fire,
        // and all it does is set _stopRequested.
        private void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e) => LogBox.Clear();

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        // View-model for one row in the solver list. Wrapped because
        // BenchmarkSolverConfig mutates through the settings dialog and we
        // want ListBox to pick up the name change if someone renames it.
        private sealed class SolverRow : INotifyPropertyChanged
        {
            public SolverRow(BenchmarkSolverConfig config) { Config = config; }
            public BenchmarkSolverConfig Config { get; }
            public string Name => Config.Name;
            public event PropertyChangedEventHandler PropertyChanged;
            public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }

        // View-model for one row in the results list. Formatted strings so the
        // XAML stays dumb; WinRateBrush lets us colour the number red/yellow/green
        // as results come in without a converter.
        private sealed class ResultRow : INotifyPropertyChanged
        {
            private int _played, _won, _lost, _stuck, _totalIter;
            private double _totalMs;

            public ResultRow(string name) { Name = name; }
            public string Name { get; }

            public void Apply(BenchmarkGameResult result)
            {
                _played++;
                switch (result.Outcome)
                {
                    case BenchmarkOutcome.Won: _won++; break;
                    case BenchmarkOutcome.Lost: _lost++; break;
                    case BenchmarkOutcome.Stuck: _stuck++; break;
                }
                _totalMs += result.ElapsedMs;
                _totalIter += result.Iterations;
                NotifyAll();
            }

            public string GamesPlayedText => $"{_played} games";
            public string WinRateText => _played == 0 ? "—" : $"{100.0 * _won / _played:F1}% win";
            public string TallyText => $"W {_won} · L {_lost} · S {_stuck}";
            public string AvgTimeText => _played == 0 ? "" : $"{_totalMs / _played:F0} ms avg";
            public string AvgIterText => _played == 0 ? "" : $"{(double)_totalIter / _played:F1} iters avg";

            public Brush WinRateBrush
            {
                get
                {
                    if (_played == 0) return Brushes.Gray;
                    var rate = (double)_won / _played;
                    if (rate >= 0.7) return new SolidColorBrush(Color.FromRgb(110, 205, 130));
                    if (rate >= 0.4) return new SolidColorBrush(Color.FromRgb(225, 205, 110));
                    return new SolidColorBrush(Color.FromRgb(230, 100, 100));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyAll()
            {
                foreach (var name in new[] { nameof(GamesPlayedText), nameof(WinRateText), nameof(TallyText), nameof(AvgTimeText), nameof(AvgIterText), nameof(WinRateBrush) })
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
