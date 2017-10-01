namespace MineDotNet.GUI.Forms
{
    partial class LauncherForm
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
            this.BenchmarkingButton = new System.Windows.Forms.Button();
            this.SolvingButton = new System.Windows.Forms.Button();
            this.PlayingButton = new System.Windows.Forms.Button();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BenchmarkingButton
            // 
            this.BenchmarkingButton.Location = new System.Drawing.Point(31, 119);
            this.BenchmarkingButton.Name = "BenchmarkingButton";
            this.BenchmarkingButton.Size = new System.Drawing.Size(114, 27);
            this.BenchmarkingButton.TabIndex = 0;
            this.BenchmarkingButton.Text = "Benchmarking";
            this.BenchmarkingButton.UseVisualStyleBackColor = true;
            this.BenchmarkingButton.Click += new System.EventHandler(this.BenchmarkingButton_Click);
            // 
            // SolvingButton
            // 
            this.SolvingButton.Location = new System.Drawing.Point(31, 53);
            this.SolvingButton.Name = "SolvingButton";
            this.SolvingButton.Size = new System.Drawing.Size(114, 27);
            this.SolvingButton.TabIndex = 1;
            this.SolvingButton.Text = "Solving";
            this.SolvingButton.UseVisualStyleBackColor = true;
            this.SolvingButton.Click += new System.EventHandler(this.SolvingButton_Click);
            // 
            // PlayingButton
            // 
            this.PlayingButton.Location = new System.Drawing.Point(31, 86);
            this.PlayingButton.Name = "PlayingButton";
            this.PlayingButton.Size = new System.Drawing.Size(114, 27);
            this.PlayingButton.TabIndex = 2;
            this.PlayingButton.Text = "Playing";
            this.PlayingButton.UseVisualStyleBackColor = true;
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TitleLabel.Location = new System.Drawing.Point(28, 18);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(119, 13);
            this.TitleLabel.TabIndex = 3;
            this.TitleLabel.Text = "Minesweeper solver";
            // 
            // LauncherForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(178, 159);
            this.Controls.Add(this.TitleLabel);
            this.Controls.Add(this.PlayingButton);
            this.Controls.Add(this.SolvingButton);
            this.Controls.Add(this.BenchmarkingButton);
            this.Name = "LauncherForm";
            this.Text = "LauncherForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BenchmarkingButton;
        private System.Windows.Forms.Button SolvingButton;
        private System.Windows.Forms.Button PlayingButton;
        private System.Windows.Forms.Label TitleLabel;
    }
}