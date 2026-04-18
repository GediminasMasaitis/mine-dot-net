using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Views;
using Newtonsoft.Json;

namespace MineDotNet.GUI.Controls
{
    public partial class SolversListEditor : UserControl
    {
        private readonly ObservableCollection<SolverRow> _rows = new ObservableCollection<SolverRow>();

        public SolversListEditor()
        {
            InitializeComponent();
            List.ItemsSource = _rows;
            UpdateButtonState();
        }

        internal IList<SolverListEntry> GetCheckedEntries() => _rows.Where(r => r.IsChecked).Select(r => r.Entry).ToList();

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SolverSettingsDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;
            var entry = dialog.GetEntry();
            _rows.Add(new SolverRow(entry) { IsChecked = true });
            List.SelectedIndex = _rows.Count - 1;
            UpdateButtonState();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (List.SelectedItem is not SolverRow row) return;
            var dialog = new SolverSettingsDialog(row.Entry) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;
            dialog.GetEntry();
            row.Refresh();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var idx = List.SelectedIndex;
            if (idx < 0) return;
            _rows.RemoveAt(idx);
            UpdateButtonState();
        }

        private void UpBtn_Click(object sender, RoutedEventArgs e) => Move(-1);
        private void DownBtn_Click(object sender, RoutedEventArgs e) => Move(+1);

        private void Move(int delta)
        {
            var idx = List.SelectedIndex;
            var newIdx = idx + delta;
            if (idx < 0 || newIdx < 0 || newIdx >= _rows.Count) return;
            _rows.Move(idx, newIdx);
            List.SelectedIndex = newIdx;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON Files|*.json" };
            if (sfd.ShowDialog(Window.GetWindow(this)) != true) return;
            var entries = _rows.Select(r => r.Entry).ToList();
            File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(entries));
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON Files|*.json" };
            if (ofd.ShowDialog(Window.GetWindow(this)) != true) return;
            var json = File.ReadAllText(ofd.FileName);
            var entries = JsonConvert.DeserializeObject<SolverListEntry[]>(json) ?? System.Array.Empty<SolverListEntry>();
            _rows.Clear();
            foreach (var entry in entries)
            {
                _rows.Add(new SolverRow(entry) { IsChecked = true });
            }
            UpdateButtonState();
        }

        private void List_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtonState();

        private void UpdateButtonState()
        {
            var selected = List.SelectedIndex >= 0;
            EditBtn.IsEnabled = selected;
            DeleteBtn.IsEnabled = selected;
            UpBtn.IsEnabled = selected && List.SelectedIndex > 0;
            DownBtn.IsEnabled = selected && List.SelectedIndex < _rows.Count - 1;
        }

        // Wraps a SolverListEntry with checkbox state + a Display string for the template binding.
        // We can't bind to SolverListEntry directly because there's no IsChecked there, and
        // ToString() changes when the entry is edited don't raise notifications.
        internal sealed class SolverRow : INotifyPropertyChanged
        {
            private bool _isChecked;

            public SolverRow(SolverListEntry entry) { Entry = entry; }

            public SolverListEntry Entry { get; }
            public string Display => Entry.ToString();

            public bool IsChecked
            {
                get => _isChecked;
                set { if (_isChecked != value) { _isChecked = value; Notify(); } }
            }

            public void Refresh() => Notify(nameof(Display));

            public event PropertyChangedEventHandler PropertyChanged;
            private void Notify([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
