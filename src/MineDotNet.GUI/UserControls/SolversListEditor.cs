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
using MineDotNet.GUI.Services;
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
            Theme.Apply(editor);
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
            Theme.Apply(editor);
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
            for (var i = 0; i < entries.Length; i++)
            {
                SolversCheckedListBox.Items.Add(entries[i], true);
            }
            EnableDisableButtons();
        }

        private void DeleteSolverButton_Click(object sender, EventArgs e)
        {
            var idx = SolversCheckedListBox.SelectedIndex;
            if (idx < 0) return;
            SolversCheckedListBox.Items.RemoveAt(idx);
            EnableDisableButtons();
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            MoveSelected(-1);
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            MoveSelected(+1);
        }

        private void MoveSelected(int delta)
        {
            var idx = SolversCheckedListBox.SelectedIndex;
            var newIdx = idx + delta;
            if (idx < 0 || newIdx < 0 || newIdx >= SolversCheckedListBox.Items.Count) return;
            var isChecked = SolversCheckedListBox.GetItemChecked(idx);
            var item = SolversCheckedListBox.Items[idx];
            SolversCheckedListBox.Items.RemoveAt(idx);
            SolversCheckedListBox.Items.Insert(newIdx, item);
            SolversCheckedListBox.SetItemChecked(newIdx, isChecked);
            SolversCheckedListBox.SelectedIndex = newIdx;
        }

        internal IList<SolverListEntry> GetCheckedEntries()
        {
            return SolversCheckedListBox.CheckedItems.Cast<SolverListEntry>().ToList();
        }
    }
}
