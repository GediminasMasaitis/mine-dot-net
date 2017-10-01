using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.GUI.Forms;
using MineDotNet.GUI.Models;
using Newtonsoft.Json;

namespace MineDotNet.GUI.UserControls
{
    public partial class SolversListEditor : UserControl
    {
        public SolversListEditor()
        {
            InitializeComponent();
            EnableDisableButtons();
        }

        private void AddSolverButton_Click(object sender, EventArgs e)
        {
            var editor = new SolverSettingsEditorForm();
            var result = editor.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
            var entry = editor.GetEntry();
            SolversCheckedListBox.Items.Add(entry);
            SolversCheckedListBox.SetItemChecked(SolversCheckedListBox.Items.Count-1, true);
            EnableDisableButtons();
        }

        private void EnableDisableButtons()
        {
            var selected = SolversCheckedListBox.SelectedIndex >= 0;
            EditSolverButton.Enabled = selected;
            DeleteSolverButton.Enabled = selected;
            MoveUpButton.Enabled = selected && SolversCheckedListBox.SelectedIndex != 0;
            MoveDownButton.Enabled = selected && SolversCheckedListBox.SelectedIndex < SolversCheckedListBox.Items.Count - 1;
        }

        private void SolversCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableDisableButtons();
        }

        private void EditSolverButton_Click(object sender, EventArgs e)
        {
            var entry = (SolverListEntry) SolversCheckedListBox.SelectedItem;
            var editor = new SolverSettingsEditorForm(entry);
            var result = editor.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
            editor.GetEntry();
            SolversCheckedListBox.Refresh();
        }

        private void SaveSolversButton_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "JSON Files | *.json";
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
            var path = dialog.FileName;
            var entries = SolversCheckedListBox.Items.Cast<SolverListEntry>().ToList();
            var json = JsonConvert.SerializeObject(entries);
            File.WriteAllText(path, json);
        }

        private void LoadSolversButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "JSON Files | *.json";
            var result = dialog.ShowDialog();
            if(result != DialogResult.OK)
            {
                return;
            }
            var path = dialog.FileName;
            var json = File.ReadAllText(path);
            var entries = JsonConvert.DeserializeObject<SolverListEntry[]>(json);
            SolversCheckedListBox.Items.Clear();
            SolversCheckedListBox.Items.AddRange(entries);
        }
    }
}
