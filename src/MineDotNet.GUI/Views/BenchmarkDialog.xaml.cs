using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
            UpdateButtonStates();
            Closing += OnClosing;
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

            // Preset sensible From/To/Step values per axis so the user gets
            // something reasonable on first toggle without having to think.
            switch (axis)
            {
                case BenchmarkSweepAxis.Width:
                case BenchmarkSweepAxis.Height:
                    SweepFromBox.Value = 10; SweepToBox.Value = 30; SweepStepBox.Value = 5;
                    break;
                case BenchmarkSweepAxis.MineDensity:
                    // Density values are percent in the UI; we convert on commit.
                    SweepFromBox.Value = 10; SweepToBox.Value = 30; SweepStepBox.Value = 5;
                    break;
            }
        }

        private BenchmarkSweepAxis CurrentSweepAxis()
        {
            if (SweepBox == null || SweepBox.SelectedIndex <= 0) return BenchmarkSweepAxis.None;
            return SweepBox.SelectedIndex switch
            {
                1 => BenchmarkSweepAxis.Width,
                2 => BenchmarkSweepAxis.Height,
                3 => BenchmarkSweepAxis.MineDensity,
                _ => BenchmarkSweepAxis.None
            };
        }

        private static string SweepAxisLabel(BenchmarkSweepAxis a) => a switch
        {
            BenchmarkSweepAxis.Width => "Width",
            BenchmarkSweepAxis.Height => "Height",
            BenchmarkSweepAxis.MineDensity => "Mine density",
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

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_solverRows.Count == 0) return;

            var axis = CurrentSweepAxis();
            var config = new BenchmarkConfig
            {
                Width = (int)WidthBox.Value,
                Height = (int)HeightBox.Value,
                MineDensity = DensitySlider.Value / 100.0,
                GameCount = (int)GamesBox.Value,
                Solvers = _solverRows.Select(r => r.Config).ToList(),
                SweepAxis = axis,
                // Density is entered as percent in the UI for all axes but gets
                // converted to a fraction for the runner in the MineDensity case.
                SweepFrom = axis == BenchmarkSweepAxis.MineDensity ? (SweepFromBox.Value ?? 0) / 100.0 : (SweepFromBox.Value ?? 0),
                SweepTo = axis == BenchmarkSweepAxis.MineDensity ? (SweepToBox.Value ?? 0) / 100.0 : (SweepToBox.Value ?? 0),
                SweepStep = axis == BenchmarkSweepAxis.MineDensity ? (SweepStepBox.Value ?? 1) / 100.0 : (SweepStepBox.Value ?? 1)
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

            PumpDispatcher();

            var sw = Stopwatch.StartNew();

            // Buffer log lines in memory during the run and flush to the TextBox
            // on a throttled schedule (~20fps) via PumpDispatcher(). Appending
            // per-line to a WPF TextBox is O(n) in existing content, so thousands
            // of chatty solver messages would tank perf; setting Text once per
            // tick is much cheaper. Cap buffer growth to keep memory bounded.
            const int MaxLogChars = 200_000;
            var logBuffer = new StringBuilder();
            var lastFlushTick = Environment.TickCount;
            IReadOnlyList<BenchmarkSolverRun> lastRuns = null;

            void FlushLog()
            {
                var text = logBuffer.Length > MaxLogChars
                    ? logBuffer.ToString(logBuffer.Length - MaxLogChars, MaxLogChars)
                    : logBuffer.ToString();
                LogBox.Text = text;
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

            Action<string, bool> logHandler = (line, sent) =>
            {
                logBuffer.Append(sent ? "→ " : "← ").Append(line).Append('\n');
                if (logBuffer.Length > MaxLogChars * 2)
                {
                    // Keep only the tail so memory stays bounded on long runs.
                    var tail = logBuffer.ToString(logBuffer.Length - MaxLogChars, MaxLogChars);
                    logBuffer.Clear();
                    logBuffer.Append(tail);
                }
                if (Environment.TickCount - lastFlushTick > 50)
                {
                    FlushLog();
                    UpdateCharts();
                    PumpDispatcher();
                    lastFlushTick = Environment.TickCount;
                }
            };
            ExtSolver.Logged += logHandler;

            Action<BenchmarkProgressUpdate> onProgress = update =>
            {
                _resultRows[update.SolverIndex].Apply(update.LastResult);
                lastRuns = update.Runs;
                ProgressLabel.Text = $"Game {update.GamesCompleted} of {update.TotalGames}";
                ProgressBar.Value = 100.0 * (update.GamesCompleted * config.Solvers.Count - (config.Solvers.Count - 1 - update.SolverIndex))
                                         / (update.TotalGames * config.Solvers.Count);
            };

            try
            {
                var runner = new BenchmarkRunner();
                runner.Run(config, onProgress, () => _stopRequested);
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
                ExtSolver.Logged -= logHandler;
                FlushLog();
                UpdateCharts();
                sw.Stop();
                ElapsedLabel.Text = $"{sw.Elapsed:mm\\:ss} total";
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
