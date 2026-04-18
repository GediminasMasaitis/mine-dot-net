namespace MineDotNet.GUI.Forms
{
    partial class MainForm
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
            if (disposing && (components != null))
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
            this.ShowMapsButton = new System.Windows.Forms.Button();
            this.SolveMapButton = new System.Windows.Forms.Button();
            this.GenerateButton = new System.Windows.Forms.Button();
            this.AutoPlayButton = new System.Windows.Forms.Button();
            this.MineDensityTrackBar = new System.Windows.Forms.TrackBar();
            this.MineDensityLabel = new System.Windows.Forms.Label();
            this.ManualPlayButton = new System.Windows.Forms.Button();
            this.WidthLabel = new System.Windows.Forms.Label();
            this.WidthNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.HeightNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.HeightLabel = new System.Windows.Forms.Label();
            this.MainPictureBox = new System.Windows.Forms.PictureBox();
            this.MapTextVisualizers = new MineDotNet.GUI.UserControls.MapTextVisualizers();
            this.solversListEditor1 = new MineDotNet.GUI.UserControls.SolversListEditor();
            this.CommLogTextBox = new System.Windows.Forms.RichTextBox();
            this.ClearLogButton = new System.Windows.Forms.Button();
            this.ActionsHeader = new System.Windows.Forms.Label();
            this.BoardHeader = new System.Windows.Forms.Label();
            this.GameHeader = new System.Windows.Forms.Label();
            this.SolversHeader = new System.Windows.Forms.Label();
            this.LogHeader = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.MineDensityTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.WidthNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.HeightNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
            this.SuspendLayout();
            //
            // ActionsHeader
            //
            this.ActionsHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ActionsHeader.AutoSize = true;
            this.ActionsHeader.Location = new System.Drawing.Point(908, 10);
            this.ActionsHeader.Name = "ActionsHeader";
            this.ActionsHeader.Size = new System.Drawing.Size(60, 15);
            this.ActionsHeader.Text = "ACTIONS";
            this.ActionsHeader.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.ActionsHeader.Tag = "header";
            //
            // ShowMapsButton
            //
            this.ShowMapsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ShowMapsButton.Location = new System.Drawing.Point(908, 30);
            this.ShowMapsButton.Name = "ShowMapsButton";
            this.ShowMapsButton.Size = new System.Drawing.Size(120, 30);
            this.ShowMapsButton.TabIndex = 3;
            this.ShowMapsButton.Text = "Show";
            this.ShowMapsButton.UseVisualStyleBackColor = true;
            this.ShowMapsButton.Click += new System.EventHandler(this.ShowMapsButton_Click);
            //
            // SolveMapButton
            //
            this.SolveMapButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SolveMapButton.Location = new System.Drawing.Point(908, 64);
            this.SolveMapButton.Name = "SolveMapButton";
            this.SolveMapButton.Size = new System.Drawing.Size(120, 30);
            this.SolveMapButton.TabIndex = 24;
            this.SolveMapButton.Text = "Solve";
            this.SolveMapButton.UseVisualStyleBackColor = true;
            this.SolveMapButton.Click += new System.EventHandler(this.SolveMapButton_Click);
            //
            // BoardHeader
            //
            this.BoardHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BoardHeader.AutoSize = true;
            this.BoardHeader.Location = new System.Drawing.Point(908, 108);
            this.BoardHeader.Name = "BoardHeader";
            this.BoardHeader.Size = new System.Drawing.Size(50, 15);
            this.BoardHeader.Text = "BOARD";
            this.BoardHeader.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.BoardHeader.Tag = "header";
            //
            // WidthLabel
            //
            this.WidthLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.WidthLabel.AutoSize = true;
            this.WidthLabel.Location = new System.Drawing.Point(908, 132);
            this.WidthLabel.Name = "WidthLabel";
            this.WidthLabel.Size = new System.Drawing.Size(38, 13);
            this.WidthLabel.TabIndex = 29;
            this.WidthLabel.Text = "Width";
            //
            // WidthNumericUpDown
            //
            this.WidthNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.WidthNumericUpDown.Location = new System.Drawing.Point(968, 130);
            this.WidthNumericUpDown.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.WidthNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.WidthNumericUpDown.Name = "WidthNumericUpDown";
            this.WidthNumericUpDown.Size = new System.Drawing.Size(60, 23);
            this.WidthNumericUpDown.TabIndex = 30;
            this.WidthNumericUpDown.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            //
            // HeightLabel
            //
            this.HeightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HeightLabel.AutoSize = true;
            this.HeightLabel.Location = new System.Drawing.Point(908, 160);
            this.HeightLabel.Name = "HeightLabel";
            this.HeightLabel.Size = new System.Drawing.Size(41, 13);
            this.HeightLabel.TabIndex = 31;
            this.HeightLabel.Text = "Height";
            //
            // HeightNumericUpDown
            //
            this.HeightNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HeightNumericUpDown.Location = new System.Drawing.Point(968, 158);
            this.HeightNumericUpDown.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.HeightNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.HeightNumericUpDown.Name = "HeightNumericUpDown";
            this.HeightNumericUpDown.Size = new System.Drawing.Size(60, 23);
            this.HeightNumericUpDown.TabIndex = 32;
            this.HeightNumericUpDown.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            //
            // MineDensityLabel
            //
            this.MineDensityLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MineDensityLabel.AutoSize = true;
            this.MineDensityLabel.Location = new System.Drawing.Point(908, 188);
            this.MineDensityLabel.Name = "MineDensityLabel";
            this.MineDensityLabel.Size = new System.Drawing.Size(92, 13);
            this.MineDensityLabel.TabIndex = 27;
            this.MineDensityLabel.Text = "Mine density: 20%";
            //
            // MineDensityTrackBar
            //
            this.MineDensityTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MineDensityTrackBar.Location = new System.Drawing.Point(905, 206);
            this.MineDensityTrackBar.Maximum = 100;
            this.MineDensityTrackBar.Name = "MineDensityTrackBar";
            this.MineDensityTrackBar.Size = new System.Drawing.Size(125, 45);
            this.MineDensityTrackBar.TabIndex = 26;
            this.MineDensityTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.MineDensityTrackBar.Value = 21;
            this.MineDensityTrackBar.ValueChanged += new System.EventHandler(this.MineDensityTrackBar_ValueChanged);
            //
            // GameHeader
            //
            this.GameHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.GameHeader.AutoSize = true;
            this.GameHeader.Location = new System.Drawing.Point(908, 258);
            this.GameHeader.Name = "GameHeader";
            this.GameHeader.Size = new System.Drawing.Size(50, 15);
            this.GameHeader.Text = "GAME";
            this.GameHeader.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.GameHeader.Tag = "header";
            //
            // GenerateButton
            //
            this.GenerateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.GenerateButton.Location = new System.Drawing.Point(908, 278);
            this.GenerateButton.Name = "GenerateButton";
            this.GenerateButton.Size = new System.Drawing.Size(120, 30);
            this.GenerateButton.TabIndex = 35;
            this.GenerateButton.Text = "Generate";
            this.GenerateButton.UseVisualStyleBackColor = true;
            this.GenerateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            //
            // AutoPlayButton
            //
            this.AutoPlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AutoPlayButton.Location = new System.Drawing.Point(908, 312);
            this.AutoPlayButton.Name = "AutoPlayButton";
            this.AutoPlayButton.Size = new System.Drawing.Size(120, 30);
            this.AutoPlayButton.TabIndex = 25;
            this.AutoPlayButton.Text = "Auto play";
            this.AutoPlayButton.UseVisualStyleBackColor = true;
            this.AutoPlayButton.Click += new System.EventHandler(this.AutoPlayButton_Click);
            //
            // ManualPlayButton
            //
            this.ManualPlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ManualPlayButton.Location = new System.Drawing.Point(908, 346);
            this.ManualPlayButton.Name = "ManualPlayButton";
            this.ManualPlayButton.Size = new System.Drawing.Size(120, 30);
            this.ManualPlayButton.TabIndex = 28;
            this.ManualPlayButton.Text = "Play";
            this.ManualPlayButton.UseVisualStyleBackColor = true;
            this.ManualPlayButton.Click += new System.EventHandler(this.ManualPlayButton_Click);
            //
            // MainPictureBox
            //
            this.MainPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainPictureBox.Location = new System.Drawing.Point(12, 12);
            this.MainPictureBox.Name = "MainPictureBox";
            this.MainPictureBox.Size = new System.Drawing.Size(527, 525);
            this.MainPictureBox.TabIndex = 0;
            this.MainPictureBox.TabStop = false;
            //
            // SolversHeader
            //
            this.SolversHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SolversHeader.AutoSize = true;
            this.SolversHeader.Location = new System.Drawing.Point(548, 10);
            this.SolversHeader.Name = "SolversHeader";
            this.SolversHeader.Size = new System.Drawing.Size(60, 15);
            this.SolversHeader.Text = "SOLVERS";
            this.SolversHeader.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.SolversHeader.Tag = "header";
            //
            // solversListEditor1
            //
            this.solversListEditor1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.solversListEditor1.Location = new System.Drawing.Point(548, 28);
            this.solversListEditor1.Name = "solversListEditor1";
            this.solversListEditor1.Size = new System.Drawing.Size(340, 300);
            this.solversListEditor1.TabIndex = 33;
            //
            // LogHeader
            //
            this.LogHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LogHeader.AutoSize = true;
            this.LogHeader.Location = new System.Drawing.Point(548, 348);
            this.LogHeader.Name = "LogHeader";
            this.LogHeader.Size = new System.Drawing.Size(40, 15);
            this.LogHeader.Text = "LOG";
            this.LogHeader.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.LogHeader.Tag = "header";
            //
            // ClearLogButton
            //
            this.ClearLogButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ClearLogButton.Location = new System.Drawing.Point(838, 344);
            this.ClearLogButton.Name = "ClearLogButton";
            this.ClearLogButton.Size = new System.Drawing.Size(60, 22);
            this.ClearLogButton.TabIndex = 37;
            this.ClearLogButton.Text = "Clear";
            this.ClearLogButton.UseVisualStyleBackColor = true;
            this.ClearLogButton.Click += new System.EventHandler(this.ClearLogButton_Click);
            //
            // CommLogTextBox
            //
            this.CommLogTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CommLogTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CommLogTextBox.Location = new System.Drawing.Point(548, 370);
            this.CommLogTextBox.Name = "CommLogTextBox";
            this.CommLogTextBox.ReadOnly = true;
            this.CommLogTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.CommLogTextBox.Size = new System.Drawing.Size(480, 167);
            this.CommLogTextBox.TabIndex = 36;
            this.CommLogTextBox.Text = "";
            this.CommLogTextBox.WordWrap = false;
            //
            // MapTextVisualizers
            //
            this.MapTextVisualizers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MapTextVisualizers.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MapTextVisualizers.Location = new System.Drawing.Point(12, 548);
            this.MapTextVisualizers.Margin = new System.Windows.Forms.Padding(4);
            this.MapTextVisualizers.Name = "MapTextVisualizers";
            this.MapTextVisualizers.Size = new System.Drawing.Size(1016, 235);
            this.MapTextVisualizers.TabIndex = 34;
            //
            // MainForm
            //
            this.AcceptButton = this.ShowMapsButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 795);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.ActionsHeader);
            this.Controls.Add(this.BoardHeader);
            this.Controls.Add(this.GameHeader);
            this.Controls.Add(this.SolversHeader);
            this.Controls.Add(this.LogHeader);
            this.Controls.Add(this.MapTextVisualizers);
            this.Controls.Add(this.ClearLogButton);
            this.Controls.Add(this.CommLogTextBox);
            this.Controls.Add(this.solversListEditor1);
            this.Controls.Add(this.HeightNumericUpDown);
            this.Controls.Add(this.HeightLabel);
            this.Controls.Add(this.WidthNumericUpDown);
            this.Controls.Add(this.WidthLabel);
            this.Controls.Add(this.ManualPlayButton);
            this.Controls.Add(this.MineDensityLabel);
            this.Controls.Add(this.MineDensityTrackBar);
            this.Controls.Add(this.GenerateButton);
            this.Controls.Add(this.AutoPlayButton);
            this.Controls.Add(this.SolveMapButton);
            this.Controls.Add(this.ShowMapsButton);
            this.Controls.Add(this.MainPictureBox);
            this.Name = "MainForm";
            this.Text = "Minesweeper Solver";
            ((System.ComponentModel.ISupportInitialize)(this.MineDensityTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.WidthNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.HeightNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button ShowMapsButton;
        private System.Windows.Forms.Button SolveMapButton;
        private System.Windows.Forms.Button GenerateButton;
        private System.Windows.Forms.Button AutoPlayButton;
        private System.Windows.Forms.TrackBar MineDensityTrackBar;
        private System.Windows.Forms.Label MineDensityLabel;
        private System.Windows.Forms.Button ManualPlayButton;
        private System.Windows.Forms.Label WidthLabel;
        private System.Windows.Forms.NumericUpDown WidthNumericUpDown;
        private System.Windows.Forms.NumericUpDown HeightNumericUpDown;
        private System.Windows.Forms.Label HeightLabel;
        private UserControls.SolversListEditor solversListEditor1;
        private System.Windows.Forms.PictureBox MainPictureBox;
        private UserControls.MapTextVisualizers MapTextVisualizers;
        private System.Windows.Forms.RichTextBox CommLogTextBox;
        private System.Windows.Forms.Button ClearLogButton;
        private System.Windows.Forms.Label ActionsHeader;
        private System.Windows.Forms.Label BoardHeader;
        private System.Windows.Forms.Label GameHeader;
        private System.Windows.Forms.Label SolversHeader;
        private System.Windows.Forms.Label LogHeader;
    }
}
