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

        public BenchmarkDialog()
        {
            InitializeComponent();
            SolversList.ItemsSource = _solverRows;
            ResultsList.ItemsSource = _resultRows;
            // Start with one reasonable default so the Run button works out of the box.
            _solverRows.Add(new SolverRow(new BenchmarkSolverConfig("Default", new BorderSeparationSolverSettings())));
            UpdateButtonStates();
        }

        private void DensitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DensityLabel != null) DensityLabel.Text = $"Mine density: {(int)DensitySlider.Value}%";
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var settings = new BorderSeparationSolverSettings();
            var dialog = new SolverSettingsDialog(settings) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            var saved = dialog.GetSettings();
            var name = $"Config {_solverRows.Count + 1}";
            _solverRows.Add(new SolverRow(new BenchmarkSolverConfig(name, saved)));
            SolversList.SelectedIndex = _solverRows.Count - 1;
            UpdateButtonStates();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SolversList.SelectedItem is not SolverRow row) return;
            var dialog = new SolverSettingsDialog(row.Config.Settings) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            row.Config.Settings = dialog.GetSettings();
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

            var config = new BenchmarkConfig
            {
                Width = (int)WidthBox.Value,
                Height = (int)HeightBox.Value,
                MineDensity = DensitySlider.Value / 100.0,
                GameCount = (int)GamesBox.Value,
                Solvers = _solverRows.Select(r => r.Config).ToList()
            };

            _resultRows.Clear();
            foreach (var cfg in config.Solvers) _resultRows.Add(new ResultRow(cfg.Name));

            RunBtn.IsEnabled = false;
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

            void FlushLog()
            {
                var text = logBuffer.Length > MaxLogChars
                    ? logBuffer.ToString(logBuffer.Length - MaxLogChars, MaxLogChars)
                    : logBuffer.ToString();
                LogBox.Text = text;
                LogBox.CaretIndex = LogBox.Text.Length;
                LogBox.ScrollToEnd();
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
                    PumpDispatcher();
                    lastFlushTick = Environment.TickCount;
                }
            };
            ExtSolver.Logged += logHandler;

            Action<BenchmarkProgressUpdate> onProgress = update =>
            {
                _resultRows[update.SolverIndex].Apply(update.LastResult);
                ProgressLabel.Text = $"Game {update.GamesCompleted} of {update.TotalGames}";
                ProgressBar.Value = 100.0 * (update.GamesCompleted * config.Solvers.Count - (config.Solvers.Count - 1 - update.SolverIndex))
                                         / (update.TotalGames * config.Solvers.Count);
            };

            try
            {
                var runner = new BenchmarkRunner();
                runner.Run(config, onProgress);
                ProgressBar.Value = 100;
                ProgressLabel.Text = "Done";
            }
            catch (Exception ex)
            {
                ProgressLabel.Text = $"Error: {ex.Message}";
            }
            finally
            {
                ExtSolver.Logged -= logHandler;
                FlushLog();
                sw.Stop();
                ElapsedLabel.Text = $"{sw.Elapsed:mm\\:ss} total";
                UpdateButtonStates();
            }
        }

        // Classic DoEvents pattern — spin the dispatcher until our sentinel
        // fires. Queueing at Loaded priority means items at Render, DataBind,
        // Normal, Send (all higher-priority) drain first, which lets WPF paint
        // the log + progress updates. Input (priority 5, below Loaded=6) stays
        // queued, so clicks can't re-enter the handler mid-run.
        private void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => frame.Continue = false));
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
