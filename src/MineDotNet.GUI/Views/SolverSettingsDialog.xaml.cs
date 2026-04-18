using System;
using System.Windows;
using MineDotNet.AI.Solvers;
using MineDotNet.GUI.Models;
using Wpf.Ui.Controls;

namespace MineDotNet.GUI.Views
{
    public partial class SolverSettingsDialog : FluentWindow
    {
        private SolverListEntry _entry;

        public SolverSettingsDialog() : this(null) { }

        public SolverSettingsDialog(SolverListEntry entry)
        {
            InitializeComponent();
            _entry = entry ?? new SolverListEntry(null, ExtSolver.Alias, new BorderSeparationSolverSettings());
            Editor.SetupObject(_entry.Settings);
            ImplBox.ItemsSource = new[] { ExtSolver.Alias };
            ImplBox.SelectedIndex = Array.IndexOf(new[] { ExtSolver.Alias }, _entry.SolverImplementation);
            NameBox.Text = _entry.SolverName ?? "New solver";
            if (_entry.SolverName == null) NameBox.Focus();
        }

        public SolverListEntry GetEntry()
        {
            _entry.SolverName = NameBox.Text;
            _entry.SolverImplementation = ImplBox.SelectedItem as string;
            _entry.Settings = (BorderSeparationSolverSettings)Editor.GetObject();
            return _entry;
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ImplBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (OkBtn != null) OkBtn.IsEnabled = ImplBox.SelectedItem != null;
        }
    }
}
