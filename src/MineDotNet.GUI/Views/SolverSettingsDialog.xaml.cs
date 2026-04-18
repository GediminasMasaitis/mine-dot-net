using System.IO;
using System.Windows;
using Microsoft.Win32;
using MineDotNet.AI.Solvers;
using Newtonsoft.Json;
using Wpf.Ui.Controls;

namespace MineDotNet.GUI.Views
{
    public partial class SolverSettingsDialog : FluentWindow
    {
        private BorderSeparationSolverSettings _settings;

        // `name` is optional — passing non-null opts into showing the name field
        // at the top of the dialog. The main window's global-settings case
        // passes null (single config, no name) and the name UI stays hidden.
        public SolverSettingsDialog(BorderSeparationSolverSettings settings, string name = null)
        {
            InitializeComponent();
            _settings = settings ?? new BorderSeparationSolverSettings();
            Editor.SetupObject(_settings);
            if (name != null)
            {
                NamePanel.Visibility = Visibility.Visible;
                NameBox.Text = name;
                Loaded += (_, __) => { NameBox.Focus(); NameBox.SelectAll(); };
            }
        }

        public BorderSeparationSolverSettings GetSettings()
        {
            return (BorderSeparationSolverSettings)Editor.GetObject();
        }

        // Trimmed user-entered name, or null if the name field wasn't shown.
        public string GetName()
        {
            if (NamePanel.Visibility != Visibility.Visible) return null;
            var text = (NameBox.Text ?? "").Trim();
            return string.IsNullOrEmpty(text) ? null : text;
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

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON Files|*.json", FileName = "solver-settings.json" };
            if (sfd.ShowDialog(this) != true) return;
            var current = GetSettings();
            File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(current, Formatting.Indented));
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON Files|*.json" };
            if (ofd.ShowDialog(this) != true) return;
            var json = File.ReadAllText(ofd.FileName);
            var loaded = JsonConvert.DeserializeObject<BorderSeparationSolverSettings>(json);
            if (loaded == null) return;
            _settings = loaded;
            Editor.SetupObject(_settings);
        }
    }
}
