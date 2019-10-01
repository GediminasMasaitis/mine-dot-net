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
            this.Map0Label = new System.Windows.Forms.Label();
            this.Map0TextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Map0Label
            // 
            this.Map0Label.AutoSize = true;
            this.Map0Label.Location = new System.Drawing.Point(0, 3);
            this.Map0Label.Name = "Map0Label";
            this.Map0Label.Size = new System.Drawing.Size(31, 13);
            this.Map0Label.TabIndex = 25;
            this.Map0Label.Text = "Map:";
            // 
            // Map0TextBox
            // 
            this.Map0TextBox.AcceptsReturn = true;
            this.Map0TextBox.Location = new System.Drawing.Point(3, 19);
            this.Map0TextBox.Multiline = true;
            this.Map0TextBox.Name = "Map0TextBox";
            this.Map0TextBox.Size = new System.Drawing.Size(156, 209);
            this.Map0TextBox.TabIndex = 24;
            // 
            // MapTextVisualizers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Map0Label);
            this.Controls.Add(this.Map0TextBox);
            this.Name = "MapTextVisualizers";
            this.Size = new System.Drawing.Size(164, 231);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Map0Label;
        private System.Windows.Forms.TextBox Map0TextBox;
    }
}
