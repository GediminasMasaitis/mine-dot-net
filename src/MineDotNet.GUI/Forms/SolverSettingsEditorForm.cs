using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Forms
{
    partial class SolverSettingsEditorForm : Form
    {
        private string SolverName
        {
            get => SolverNameTextBox.Text;
            set => SolverNameTextBox.Text = value;
        }

        private string Implementation => ImplementationComboBox.SelectedItem as string;

        private SolverListEntry CurrentEntry { get; set; }

        public SolverSettingsEditorForm() : this(null)
        {
            
        }

        public SolverSettingsEditorForm(SolverListEntry entry)
        {
            InitializeComponent();
            entry = entry ?? new SolverListEntry(null, ExtSolver.Alias, new BorderSeparationSolverSettings());
            MainObjectEditor.SetupObject(entry.Settings);
            var implNames = new[]
            {
                BorderSeparationSolver.Alias,
                ExtSolver.Alias
            };
            var currentIndex = Array.IndexOf(implNames, entry.SolverImplementation);
            ImplementationComboBox.Items.AddRange(implNames);
            ImplementationComboBox.SelectedIndex = currentIndex;
            if(entry.SolverName == null)
            {
                SolverName = "New solver";
                SolverNameTextBox.Select();
            }
            else
            {
                SolverName = entry.SolverName;
            }
            CurrentEntry = entry;
            EnableDisableButtons();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public SolverListEntry GetEntry()
        {
            CurrentEntry.SolverName = SolverName;
            CurrentEntry.SolverImplementation = Implementation;
            CurrentEntry.Settings = (BorderSeparationSolverSettings) MainObjectEditor.GetObject();
            return CurrentEntry;
        }

        private void ImplementationComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            EnableDisableButtons();
        }

        void EnableDisableButtons()
        {
            OkButton.Enabled = ImplementationComboBox.SelectedItem != null;
        }
    }
}
