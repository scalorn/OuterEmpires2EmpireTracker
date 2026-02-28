namespace OE2EmpireTracker.Controls
{
    partial class DataGridViewFilteredComboBoxColumn
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
            this.cmbFilteredCombo = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.SuspendLayout();
            // 
            // cmbFilteredCombo
            // 
            this.cmbFilteredCombo.Location = new System.Drawing.Point(0, 0);
            this.cmbFilteredCombo.Name = "cmbFilteredCombo";
            this.cmbFilteredCombo.Size = new System.Drawing.Size(313, 25);
            this.cmbFilteredCombo.TabIndex = 0;
            // 
            // DataGridViewFilteredComboBoxColumn
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbFilteredCombo);
            this.Name = "DataGridViewFilteredComboBoxColumn";
            this.Size = new System.Drawing.Size(310, 26);
            this.ResumeLayout(false);

        }

        #endregion

        private FilteredTextComboSet cmbFilteredCombo;
    }
}
