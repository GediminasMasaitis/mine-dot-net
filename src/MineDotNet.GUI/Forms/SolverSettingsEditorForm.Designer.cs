namespace MineDotNet.GUI.Forms
{
    partial class SolverSettingsEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.OkButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.NameLabel = new System.Windows.Forms.Label();
            this.SolverNameTextBox = new System.Windows.Forms.TextBox();
            this.MainObjectEditor = new MineDotNet.GUI.UserControls.ObjectEditor();
            this.ImplementationLabel = new System.Windows.Forms.Label();
            this.ImplementationComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            //
            // NameLabel
            //
            this.NameLabel.AutoSize = true;
            this.NameLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.NameLabel.Location = new System.Drawing.Point(14, 14);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(43, 13);
            this.NameLabel.TabIndex = 3;
            this.NameLabel.Text = "Name";
            //
            // SolverNameTextBox
            //
            this.SolverNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SolverNameTextBox.Location = new System.Drawing.Point(130, 12);
            this.SolverNameTextBox.Name = "SolverNameTextBox";
            this.SolverNameTextBox.Size = new System.Drawing.Size(525, 23);
            this.SolverNameTextBox.TabIndex = 4;
            this.SolverNameTextBox.Text = "New solver";
            //
            // ImplementationLabel
            //
            this.ImplementationLabel.AutoSize = true;
            this.ImplementationLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.ImplementationLabel.Location = new System.Drawing.Point(14, 44);
            this.ImplementationLabel.Name = "ImplementationLabel";
            this.ImplementationLabel.Size = new System.Drawing.Size(96, 13);
            this.ImplementationLabel.TabIndex = 5;
            this.ImplementationLabel.Text = "Implementation";
            //
            // ImplementationComboBox
            //
            this.ImplementationComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ImplementationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ImplementationComboBox.FormattingEnabled = true;
            this.ImplementationComboBox.Location = new System.Drawing.Point(130, 41);
            this.ImplementationComboBox.Name = "ImplementationComboBox";
            this.ImplementationComboBox.Size = new System.Drawing.Size(525, 23);
            this.ImplementationComboBox.TabIndex = 6;
            this.ImplementationComboBox.SelectedValueChanged += new System.EventHandler(this.ImplementationComboBox_SelectedValueChanged);
            //
            // MainObjectEditor
            //
            this.MainObjectEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainObjectEditor.AutoScroll = true;
            this.MainObjectEditor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MainObjectEditor.Location = new System.Drawing.Point(14, 76);
            this.MainObjectEditor.Name = "MainObjectEditor";
            this.MainObjectEditor.Size = new System.Drawing.Size(641, 565);
            this.MainObjectEditor.TabIndex = 0;
            //
            // OkButton
            //
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(453, 652);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(100, 30);
            this.OkButton.TabIndex = 1;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            //
            // CancelButton
            //
            this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButton.Location = new System.Drawing.Point(559, 652);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(100, 30);
            this.CancelButton.TabIndex = 2;
            this.CancelButton.Tag = "secondary";
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            //
            // SolverSettingsEditorForm
            //
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(669, 694);
            this.Controls.Add(this.ImplementationComboBox);
            this.Controls.Add(this.ImplementationLabel);
            this.Controls.Add(this.SolverNameTextBox);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.MainObjectEditor);
            this.Name = "SolverSettingsEditorForm";
            this.Text = "Solver settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MineDotNet.GUI.UserControls.ObjectEditor MainObjectEditor;
        private System.Windows.Forms.Button OkButton;
        private new System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.TextBox SolverNameTextBox;
        private System.Windows.Forms.Label ImplementationLabel;
        private System.Windows.Forms.ComboBox ImplementationComboBox;
    }
}
