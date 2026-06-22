namespace OE2EmpireTracker.Forms
{
    partial class FormMigrationProgress
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblPhase = new System.Windows.Forms.Label();
            this.lblEntityType = new System.Windows.Forms.Label();
            this.lblCount = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            //
            // lblPhase
            //
            this.lblPhase.AutoSize = true;
            this.lblPhase.Location = new System.Drawing.Point(12, 15);
            this.lblPhase.Name = "lblPhase";
            this.lblPhase.Size = new System.Drawing.Size(40, 13);
            this.lblPhase.TabIndex = 0;
            this.lblPhase.Text = "Phase:";
            //
            // lblEntityType
            //
            this.lblEntityType.AutoSize = true;
            this.lblEntityType.Location = new System.Drawing.Point(12, 38);
            this.lblEntityType.Name = "lblEntityType";
            this.lblEntityType.Size = new System.Drawing.Size(46, 13);
            this.lblEntityType.TabIndex = 1;
            this.lblEntityType.Text = "Current:";
            //
            // lblCount
            //
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(12, 61);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(112, 13);
            this.lblCount.TabIndex = 2;
            this.lblCount.Text = "Entities processed: 0";
            //
            // progressBar
            //
            this.progressBar.Location = new System.Drawing.Point(12, 87);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(360, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 3;
            //
            // FormMigrationProgress
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 121);
            this.ControlBox = false;
            this.Controls.Add(this.lblPhase);
            this.Controls.Add(this.lblEntityType);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormMigrationProgress";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Migrating Data...";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblPhase;
        private System.Windows.Forms.Label lblEntityType;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}
