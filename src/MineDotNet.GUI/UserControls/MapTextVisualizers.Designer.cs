namespace MineDotNet.GUI.UserControls
{
    partial class MapTextVisualizers
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.MapLabel = new System.Windows.Forms.Label();
            this.MapTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Map0Label
            // 
            this.MapLabel.AutoSize = true;
            this.MapLabel.Location = new System.Drawing.Point(0, 3);
            this.MapLabel.Name = "MapLabel";
            this.MapLabel.Size = new System.Drawing.Size(31, 13);
            this.MapLabel.TabIndex = 25;
            this.MapLabel.Text = "Map:";
            // 
            // Map0TextBox
            // 
            this.MapTextBox.AcceptsReturn = true;
            this.MapTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.MapTextBox.Location = new System.Drawing.Point(3, 19);
            this.MapTextBox.Multiline = true;
            this.MapTextBox.Name = "MapTextBox";
            this.MapTextBox.Size = new System.Drawing.Size(156, 209);
            this.MapTextBox.TabIndex = 24;
            // 
            // MapTextVisualizers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MapLabel);
            this.Controls.Add(this.MapTextBox);
            this.Name = "MapTextVisualizers";
            this.Size = new System.Drawing.Size(164, 231);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label MapLabel;
        private System.Windows.Forms.TextBox MapTextBox;
    }
}
