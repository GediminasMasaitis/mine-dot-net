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
            this.MapRichTextBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // MapLabel
            //
            this.MapLabel.AutoSize = true;
            this.MapLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.MapLabel.Location = new System.Drawing.Point(3, 3);
            this.MapLabel.Name = "MapLabel";
            this.MapLabel.Size = new System.Drawing.Size(35, 15);
            this.MapLabel.TabIndex = 25;
            this.MapLabel.Text = "MAP";
            //
            // MapRichTextBox
            //
            this.MapRichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.MapRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MapRichTextBox.Location = new System.Drawing.Point(3, 24);
            this.MapRichTextBox.Name = "MapRichTextBox";
            this.MapRichTextBox.Size = new System.Drawing.Size(115, 190);
            this.MapRichTextBox.TabIndex = 26;
            this.MapRichTextBox.Text = "";
            //
            // MapTextVisualizers
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MapRichTextBox);
            this.Controls.Add(this.MapLabel);
            this.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MapTextVisualizers";
            this.Size = new System.Drawing.Size(130, 220);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label MapLabel;
        private System.Windows.Forms.RichTextBox MapRichTextBox;
    }
}
