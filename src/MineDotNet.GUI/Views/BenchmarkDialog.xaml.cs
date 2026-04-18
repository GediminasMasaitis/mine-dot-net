using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private CancellationTokenSource _cts;

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
            RunBtn.IsEnabled = _solverRows.Count > 0 && _cts == null;
        }

        private async void RunBtn_Click(object sender, RoutedEventArgs e)
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

            // Seed empty result rows so the UI shows all solvers before any
            // games complete. Each row stays bound in place and just ticks.
            _resultRows.Clear();
            foreach (var cfg in config.Solvers) _resultRows.Add(new ResultRow(cfg.Name));

            _cts = new CancellationTokenSource();
            RunBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;

            var sw = Stopwatch.StartNew();
            var progress = new Progress<BenchmarkProgressUpdate>(update =>
            {
                var solverRow = _resultRows[update.SolverIndex];
                solverRow.Apply(update.LastResult);
                ProgressLabel.Text = $"Game {update.GamesCompleted} of {update.TotalGames}";
                ProgressBar.Value = 100.0 * (update.GamesCompleted * config.Solvers.Count - (config.Solvers.Count - 1 - update.SolverIndex))
                                         / (update.TotalGames * config.Solvers.Count);
                ElapsedLabel.Text = $"{sw.Elapsed:mm\\:ss} elapsed";
            });

            try
            {
                var runner = new BenchmarkRunner();
                await runner.RunAsync(config, progress, _cts.Token);
                ProgressLabel.Text = _cts.IsCancellationRequested ? "Cancelled" : "Done";
                ProgressBar.Value = _cts.IsCancellationRequested ? ProgressBar.Value : 100;
            }
            catch (Exception ex)
            {
                ProgressLabel.Text = $"Error: {ex.Message}";
            }
            finally
            {
                sw.Stop();
                ElapsedLabel.Text = $"{sw.Elapsed:mm\\:ss} total";
                _cts?.Dispose();
                _cts = null;
                StopBtn.IsEnabled = false;
                UpdateButtonStates();
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            StopBtn.IsEnabled = false;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            Close();
        }

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
