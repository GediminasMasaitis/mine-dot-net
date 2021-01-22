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
            this.MLTestButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.MineDensityTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.WidthNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.HeightNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ShowMapsButton
            // 
            this.ShowMapsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ShowMapsButton.Location = new System.Drawing.Point(917, 7);
            this.ShowMapsButton.Name = "ShowMapsButton";
            this.ShowMapsButton.Size = new System.Drawing.Size(110, 23);
            this.ShowMapsButton.TabIndex = 3;
            this.ShowMapsButton.Text = "Show";
            this.ShowMapsButton.UseVisualStyleBackColor = true;
            this.ShowMapsButton.Click += new System.EventHandler(this.ShowMapsButton_Click);
            // 
            // SolveMapButton
            // 
            this.SolveMapButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SolveMapButton.Location = new System.Drawing.Point(917, 36);
            this.SolveMapButton.Name = "SolveMapButton";
            this.SolveMapButton.Size = new System.Drawing.Size(110, 23);
            this.SolveMapButton.TabIndex = 24;
            this.SolveMapButton.Text = "Solve";
            this.SolveMapButton.UseVisualStyleBackColor = true;
            this.SolveMapButton.Click += new System.EventHandler(this.SolveMapButton_Click);
            // 
            // AutoPlayButton
            // 
            this.AutoPlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AutoPlayButton.Location = new System.Drawing.Point(917, 218);
            this.AutoPlayButton.Name = "AutoPlayButton";
            this.AutoPlayButton.Size = new System.Drawing.Size(110, 23);
            this.AutoPlayButton.TabIndex = 25;
            this.AutoPlayButton.Text = "Auto play";
            this.AutoPlayButton.UseVisualStyleBackColor = true;
            this.AutoPlayButton.Click += new System.EventHandler(this.AutoPlayButton_Click);
            // 
            // MineDensityTrackBar
            // 
            this.MineDensityTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MineDensityTrackBar.Location = new System.Drawing.Point(912, 167);
            this.MineDensityTrackBar.Maximum = 100;
            this.MineDensityTrackBar.Name = "MineDensityTrackBar";
            this.MineDensityTrackBar.Size = new System.Drawing.Size(115, 45);
            this.MineDensityTrackBar.TabIndex = 26;
            this.MineDensityTrackBar.Value = 20;
            this.MineDensityTrackBar.ValueChanged += new System.EventHandler(this.MineDensityTrackBar_ValueChanged);
            // 
            // MineDensityLabel
            // 
            this.MineDensityLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MineDensityLabel.AutoSize = true;
            this.MineDensityLabel.Location = new System.Drawing.Point(926, 199);
            this.MineDensityLabel.Name = "MineDensityLabel";
            this.MineDensityLabel.Size = new System.Drawing.Size(92, 13);
            this.MineDensityLabel.TabIndex = 27;
            this.MineDensityLabel.Text = "Mine density: 20%";
            // 
            // ManualPlayButton
            // 
            this.ManualPlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ManualPlayButton.Location = new System.Drawing.Point(917, 247);
            this.ManualPlayButton.Name = "ManualPlayButton";
            this.ManualPlayButton.Size = new System.Drawing.Size(110, 23);
            this.ManualPlayButton.TabIndex = 28;
            this.ManualPlayButton.Text = "Play";
            this.ManualPlayButton.UseVisualStyleBackColor = true;
            this.ManualPlayButton.Click += new System.EventHandler(this.ManualPlayButton_Click);
            // 
            // WidthLabel
            // 
            this.WidthLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.WidthLabel.AutoSize = true;
            this.WidthLabel.Location = new System.Drawing.Point(926, 116);
            this.WidthLabel.Name = "WidthLabel";
            this.WidthLabel.Size = new System.Drawing.Size(38, 13);
            this.WidthLabel.TabIndex = 29;
            this.WidthLabel.Text = "Width:";
            // 
            // WidthNumericUpDown
            // 
            this.WidthNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.WidthNumericUpDown.Location = new System.Drawing.Point(970, 114);
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
            this.WidthNumericUpDown.Size = new System.Drawing.Size(48, 20);
            this.WidthNumericUpDown.TabIndex = 30;
            this.WidthNumericUpDown.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // HeightNumericUpDown
            // 
            this.HeightNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HeightNumericUpDown.Location = new System.Drawing.Point(970, 140);
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
            this.HeightNumericUpDown.Size = new System.Drawing.Size(48, 20);
            this.HeightNumericUpDown.TabIndex = 32;
            this.HeightNumericUpDown.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // HeightLabel
            // 
            this.HeightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HeightLabel.AutoSize = true;
            this.HeightLabel.Location = new System.Drawing.Point(923, 142);
            this.HeightLabel.Name = "HeightLabel";
            this.HeightLabel.Size = new System.Drawing.Size(41, 13);
            this.HeightLabel.TabIndex = 31;
            this.HeightLabel.Text = "Height:";
            // 
            // MainPictureBox
            // 
            this.MainPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainPictureBox.Location = new System.Drawing.Point(0, 0);
            this.MainPictureBox.Name = "MainPictureBox";
            this.MainPictureBox.Size = new System.Drawing.Size(539, 539);
            this.MainPictureBox.TabIndex = 0;
            this.MainPictureBox.TabStop = false;
            // 
            // MapTextVisualizers
            // 
            this.MapTextVisualizers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MapTextVisualizers.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MapTextVisualizers.Location = new System.Drawing.Point(0, 545);
            this.MapTextVisualizers.Margin = new System.Windows.Forms.Padding(4);
            this.MapTextVisualizers.Name = "MapTextVisualizers";
            this.MapTextVisualizers.Size = new System.Drawing.Size(1027, 231);
            this.MapTextVisualizers.TabIndex = 34;
            // 
            // solversListEditor1
            // 
            this.solversListEditor1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.solversListEditor1.Location = new System.Drawing.Point(545, 0);
            this.solversListEditor1.Name = "solversListEditor1";
            this.solversListEditor1.Size = new System.Drawing.Size(313, 311);
            this.solversListEditor1.TabIndex = 33;
            // 
            // MLTestButton
            // 
            this.MLTestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MLTestButton.Location = new System.Drawing.Point(917, 276);
            this.MLTestButton.Name = "MLTestButton";
            this.MLTestButton.Size = new System.Drawing.Size(110, 23);
            this.MLTestButton.TabIndex = 35;
            this.MLTestButton.Text = "ML";
            this.MLTestButton.UseVisualStyleBackColor = true;
            this.MLTestButton.Click += new System.EventHandler(this.MLTestButton_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.ShowMapsButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1034, 779);
            this.Controls.Add(this.MLTestButton);
            this.Controls.Add(this.MapTextVisualizers);
            this.Controls.Add(this.solversListEditor1);
            this.Controls.Add(this.HeightNumericUpDown);
            this.Controls.Add(this.HeightLabel);
            this.Controls.Add(this.WidthNumericUpDown);
            this.Controls.Add(this.WidthLabel);
            this.Controls.Add(this.ManualPlayButton);
            this.Controls.Add(this.MineDensityLabel);
            this.Controls.Add(this.MineDensityTrackBar);
            this.Controls.Add(this.AutoPlayButton);
            this.Controls.Add(this.SolveMapButton);
            this.Controls.Add(this.ShowMapsButton);
            this.Controls.Add(this.MainPictureBox);
            this.Name = "MainForm";
            this.Text = "Mine viewer";
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
        private System.Windows.Forms.Button MLTestButton;
    }
}

