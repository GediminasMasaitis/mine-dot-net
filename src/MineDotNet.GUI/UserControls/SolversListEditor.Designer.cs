namespace MineDotNet.GUI.UserControls
{
    partial class SolversListEditor
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SolversCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.AddSolverButton = new System.Windows.Forms.Button();
            this.EditSolverButton = new System.Windows.Forms.Button();
            this.DeleteSolverButton = new System.Windows.Forms.Button();
            this.LoadSolversButton = new System.Windows.Forms.Button();
            this.SaveSolversButton = new System.Windows.Forms.Button();
            this.MoveUpButton = new System.Windows.Forms.Button();
            this.MoveDownButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // SolversCheckedListBox
            // 
            this.SolversCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SolversCheckedListBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SolversCheckedListBox.FormattingEnabled = true;
            this.SolversCheckedListBox.Location = new System.Drawing.Point(3, 3);
            this.SolversCheckedListBox.Name = "SolversCheckedListBox";
            this.SolversCheckedListBox.Size = new System.Drawing.Size(207, 304);
            this.SolversCheckedListBox.TabIndex = 0;
            this.SolversCheckedListBox.SelectedIndexChanged += new System.EventHandler(this.SolversCheckedListBox_SelectedIndexChanged);
            // 
            // AddSolverButton
            // 
            this.AddSolverButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddSolverButton.Location = new System.Drawing.Point(216, 3);
            this.AddSolverButton.Name = "AddSolverButton";
            this.AddSolverButton.Size = new System.Drawing.Size(94, 23);
            this.AddSolverButton.TabIndex = 1;
            this.AddSolverButton.Text = "Add solver";
            this.AddSolverButton.UseVisualStyleBackColor = true;
            this.AddSolverButton.Click += new System.EventHandler(this.AddSolverButton_Click);
            // 
            // EditSolverButton
            // 
            this.EditSolverButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.EditSolverButton.Location = new System.Drawing.Point(216, 32);
            this.EditSolverButton.Name = "EditSolverButton";
            this.EditSolverButton.Size = new System.Drawing.Size(94, 23);
            this.EditSolverButton.TabIndex = 2;
            this.EditSolverButton.Text = "Edit solver";
            this.EditSolverButton.UseVisualStyleBackColor = true;
            this.EditSolverButton.Click += new System.EventHandler(this.EditSolverButton_Click);
            // 
            // DeleteSolverButton
            // 
            this.DeleteSolverButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DeleteSolverButton.Location = new System.Drawing.Point(216, 61);
            this.DeleteSolverButton.Name = "DeleteSolverButton";
            this.DeleteSolverButton.Size = new System.Drawing.Size(94, 23);
            this.DeleteSolverButton.TabIndex = 3;
            this.DeleteSolverButton.Text = "Delete solver";
            this.DeleteSolverButton.UseVisualStyleBackColor = true;
            // 
            // LoadSolversButton
            // 
            this.LoadSolversButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.LoadSolversButton.Location = new System.Drawing.Point(216, 284);
            this.LoadSolversButton.Name = "LoadSolversButton";
            this.LoadSolversButton.Size = new System.Drawing.Size(94, 23);
            this.LoadSolversButton.TabIndex = 4;
            this.LoadSolversButton.Text = "Load solvers";
            this.LoadSolversButton.UseVisualStyleBackColor = true;
            this.LoadSolversButton.Click += new System.EventHandler(this.LoadSolversButton_Click);
            // 
            // SaveSolversButton
            // 
            this.SaveSolversButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveSolversButton.Location = new System.Drawing.Point(216, 255);
            this.SaveSolversButton.Name = "SaveSolversButton";
            this.SaveSolversButton.Size = new System.Drawing.Size(94, 23);
            this.SaveSolversButton.TabIndex = 5;
            this.SaveSolversButton.Text = "Save solvers";
            this.SaveSolversButton.UseVisualStyleBackColor = true;
            this.SaveSolversButton.Click += new System.EventHandler(this.SaveSolversButton_Click);
            // 
            // MoveUpButton
            // 
            this.MoveUpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MoveUpButton.Location = new System.Drawing.Point(216, 90);
            this.MoveUpButton.Name = "MoveUpButton";
            this.MoveUpButton.Size = new System.Drawing.Size(94, 23);
            this.MoveUpButton.TabIndex = 6;
            this.MoveUpButton.Text = "Move up";
            this.MoveUpButton.UseVisualStyleBackColor = true;
            // 
            // MoveDownButton
            // 
            this.MoveDownButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MoveDownButton.Location = new System.Drawing.Point(216, 119);
            this.MoveDownButton.Name = "MoveDownButton";
            this.MoveDownButton.Size = new System.Drawing.Size(94, 23);
            this.MoveDownButton.TabIndex = 7;
            this.MoveDownButton.Text = "Move down";
            this.MoveDownButton.UseVisualStyleBackColor = true;
            // 
            // SolversListEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MoveDownButton);
            this.Controls.Add(this.MoveUpButton);
            this.Controls.Add(this.SaveSolversButton);
            this.Controls.Add(this.LoadSolversButton);
            this.Controls.Add(this.DeleteSolverButton);
            this.Controls.Add(this.EditSolverButton);
            this.Controls.Add(this.AddSolverButton);
            this.Controls.Add(this.SolversCheckedListBox);
            this.Name = "SolversListEditor";
            this.Size = new System.Drawing.Size(313, 311);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox SolversCheckedListBox;
        private System.Windows.Forms.Button AddSolverButton;
        private System.Windows.Forms.Button EditSolverButton;
        private System.Windows.Forms.Button DeleteSolverButton;
        private System.Windows.Forms.Button LoadSolversButton;
        private System.Windows.Forms.Button SaveSolversButton;
        private System.Windows.Forms.Button MoveUpButton;
        private System.Windows.Forms.Button MoveDownButton;
    }
}
