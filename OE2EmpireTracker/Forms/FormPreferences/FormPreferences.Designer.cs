namespace OE2EmpireTracker.Forms
{
    partial class FormPreferences
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.grpStructureCount = new System.Windows.Forms.GroupBox();
            this.lblStructureYellow = new System.Windows.Forms.Label();
            this.txtStructureYellow = new System.Windows.Forms.TextBox();
            this.lblStructureRed = new System.Windows.Forms.Label();
            this.txtStructureRed = new System.Windows.Forms.TextBox();
            this.grpWorkerRequest = new System.Windows.Forms.GroupBox();
            this.lblWorkerYellow = new System.Windows.Forms.Label();
            this.txtWorkerYellow = new System.Windows.Forms.TextBox();
            this.lblWorkerRed = new System.Windows.Forms.Label();
            this.txtWorkerRed = new System.Windows.Forms.TextBox();
            this.grpColonyImport = new System.Windows.Forms.GroupBox();
            this.lblColonyImportYellow = new System.Windows.Forms.Label();
            this.txtColonyImportYellow = new System.Windows.Forms.TextBox();
            this.lblColonyImportRed = new System.Windows.Forms.Label();
            this.txtColonyImportRed = new System.Windows.Forms.TextBox();
            this.grpBackgroundProcessing = new System.Windows.Forms.GroupBox();
            this.lblBackgroundInterval = new System.Windows.Forms.Label();
            this.txtBackgroundInterval = new System.Windows.Forms.TextBox();
            this.grpAdminReport = new System.Windows.Forms.GroupBox();
            this.lblAdminRefresh = new System.Windows.Forms.Label();
            this.txtAdminRefresh = new System.Windows.Forms.TextBox();
            this.grpCountdownDisplay = new System.Windows.Forms.GroupBox();
            this.lblCountdownRefresh = new System.Windows.Forms.Label();
            this.txtCountdownRefresh = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnResetDefaults = new System.Windows.Forms.Button();
            this.grpStructureCount.SuspendLayout();
            this.grpWorkerRequest.SuspendLayout();
            this.grpColonyImport.SuspendLayout();
            this.grpBackgroundProcessing.SuspendLayout();
            this.grpAdminReport.SuspendLayout();
            this.grpCountdownDisplay.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpStructureCount
            // 
            this.grpStructureCount.Controls.Add(this.lblStructureYellow);
            this.grpStructureCount.Controls.Add(this.txtStructureYellow);
            this.grpStructureCount.Controls.Add(this.lblStructureRed);
            this.grpStructureCount.Controls.Add(this.txtStructureRed);
            this.grpStructureCount.Location = new System.Drawing.Point(12, 12);
            this.grpStructureCount.Name = "grpStructureCount";
            this.grpStructureCount.Size = new System.Drawing.Size(460, 75);
            this.grpStructureCount.TabIndex = 0;
            this.grpStructureCount.TabStop = false;
            this.grpStructureCount.Text = "Structure Count";
            // 
            // lblStructureYellow
            // 
            this.lblStructureYellow.AutoSize = true;
            this.lblStructureYellow.Location = new System.Drawing.Point(15, 28);
            this.lblStructureYellow.Name = "lblStructureYellow";
            this.lblStructureYellow.Size = new System.Drawing.Size(93, 13);
            this.lblStructureYellow.Text = "Yellow Threshold:";
            // 
            // txtStructureYellow
            // 
            this.txtStructureYellow.Location = new System.Drawing.Point(150, 25);
            this.txtStructureYellow.Name = "txtStructureYellow";
            this.txtStructureYellow.Size = new System.Drawing.Size(80, 20);
            this.txtStructureYellow.TabIndex = 1;
            // 
            // lblStructureRed
            // 
            this.lblStructureRed.AutoSize = true;
            this.lblStructureRed.Location = new System.Drawing.Point(250, 28);
            this.lblStructureRed.Name = "lblStructureRed";
            this.lblStructureRed.Size = new System.Drawing.Size(78, 13);
            this.lblStructureRed.Text = "Red Threshold:";
            // 
            // txtStructureRed
            // 
            this.txtStructureRed.Location = new System.Drawing.Point(370, 25);
            this.txtStructureRed.Name = "txtStructureRed";
            this.txtStructureRed.Size = new System.Drawing.Size(80, 20);
            this.txtStructureRed.TabIndex = 2;
            // 
            // grpWorkerRequest
            // 
            this.grpWorkerRequest.Controls.Add(this.lblWorkerYellow);
            this.grpWorkerRequest.Controls.Add(this.txtWorkerYellow);
            this.grpWorkerRequest.Controls.Add(this.lblWorkerRed);
            this.grpWorkerRequest.Controls.Add(this.txtWorkerRed);
            this.grpWorkerRequest.Location = new System.Drawing.Point(12, 93);
            this.grpWorkerRequest.Name = "grpWorkerRequest";
            this.grpWorkerRequest.Size = new System.Drawing.Size(460, 75);
            this.grpWorkerRequest.TabIndex = 1;
            this.grpWorkerRequest.TabStop = false;
            this.grpWorkerRequest.Text = "Worker Request Due Window";
            // 
            // lblWorkerYellow
            // 
            this.lblWorkerYellow.AutoSize = true;
            this.lblWorkerYellow.Location = new System.Drawing.Point(15, 28);
            this.lblWorkerYellow.Name = "lblWorkerYellow";
            this.lblWorkerYellow.Size = new System.Drawing.Size(93, 13);
            this.lblWorkerYellow.Text = "Yellow Threshold:";
            // 
            // txtWorkerYellow
            // 
            this.txtWorkerYellow.Location = new System.Drawing.Point(150, 25);
            this.txtWorkerYellow.Name = "txtWorkerYellow";
            this.txtWorkerYellow.Size = new System.Drawing.Size(100, 20);
            this.txtWorkerYellow.TabIndex = 3;
            // 
            // lblWorkerRed
            // 
            this.lblWorkerRed.AutoSize = true;
            this.lblWorkerRed.Location = new System.Drawing.Point(270, 28);
            this.lblWorkerRed.Name = "lblWorkerRed";
            this.lblWorkerRed.Size = new System.Drawing.Size(78, 13);
            this.lblWorkerRed.Text = "Red Threshold:";
            // 
            // txtWorkerRed
            // 
            this.txtWorkerRed.Location = new System.Drawing.Point(370, 25);
            this.txtWorkerRed.Name = "txtWorkerRed";
            this.txtWorkerRed.Size = new System.Drawing.Size(80, 20);
            this.txtWorkerRed.TabIndex = 4;
            // 
            // grpColonyImport
            // 
            this.grpColonyImport.Controls.Add(this.lblColonyImportYellow);
            this.grpColonyImport.Controls.Add(this.txtColonyImportYellow);
            this.grpColonyImport.Controls.Add(this.lblColonyImportRed);
            this.grpColonyImport.Controls.Add(this.txtColonyImportRed);
            this.grpColonyImport.Location = new System.Drawing.Point(12, 174);
            this.grpColonyImport.Name = "grpColonyImport";
            this.grpColonyImport.Size = new System.Drawing.Size(460, 75);
            this.grpColonyImport.TabIndex = 2;
            this.grpColonyImport.TabStop = false;
            this.grpColonyImport.Text = "Colony Import Staleness";
            // 
            // lblColonyImportYellow
            // 
            this.lblColonyImportYellow.AutoSize = true;
            this.lblColonyImportYellow.Location = new System.Drawing.Point(15, 28);
            this.lblColonyImportYellow.Name = "lblColonyImportYellow";
            this.lblColonyImportYellow.Size = new System.Drawing.Size(93, 13);
            this.lblColonyImportYellow.Text = "Yellow Threshold:";
            // 
            // txtColonyImportYellow
            // 
            this.txtColonyImportYellow.Location = new System.Drawing.Point(150, 25);
            this.txtColonyImportYellow.Name = "txtColonyImportYellow";
            this.txtColonyImportYellow.Size = new System.Drawing.Size(100, 20);
            this.txtColonyImportYellow.TabIndex = 5;
            // 
            // lblColonyImportRed
            // 
            this.lblColonyImportRed.AutoSize = true;
            this.lblColonyImportRed.Location = new System.Drawing.Point(270, 28);
            this.lblColonyImportRed.Name = "lblColonyImportRed";
            this.lblColonyImportRed.Size = new System.Drawing.Size(78, 13);
            this.lblColonyImportRed.Text = "Red Threshold:";
            // 
            // txtColonyImportRed
            // 
            this.txtColonyImportRed.Location = new System.Drawing.Point(370, 25);
            this.txtColonyImportRed.Name = "txtColonyImportRed";
            this.txtColonyImportRed.Size = new System.Drawing.Size(80, 20);
            this.txtColonyImportRed.TabIndex = 6;
            // 
            // grpBackgroundProcessing
            // 
            this.grpBackgroundProcessing.Controls.Add(this.lblBackgroundInterval);
            this.grpBackgroundProcessing.Controls.Add(this.txtBackgroundInterval);
            this.grpBackgroundProcessing.Location = new System.Drawing.Point(12, 255);
            this.grpBackgroundProcessing.Name = "grpBackgroundProcessing";
            this.grpBackgroundProcessing.Size = new System.Drawing.Size(460, 55);
            this.grpBackgroundProcessing.TabIndex = 3;
            this.grpBackgroundProcessing.TabStop = false;
            this.grpBackgroundProcessing.Text = "Background Processing";
            // 
            // lblBackgroundInterval
            // 
            this.lblBackgroundInterval.AutoSize = true;
            this.lblBackgroundInterval.Location = new System.Drawing.Point(15, 25);
            this.lblBackgroundInterval.Name = "lblBackgroundInterval";
            this.lblBackgroundInterval.Size = new System.Drawing.Size(45, 13);
            this.lblBackgroundInterval.Text = "Interval:";
            // 
            // txtBackgroundInterval
            // 
            this.txtBackgroundInterval.Location = new System.Drawing.Point(150, 22);
            this.txtBackgroundInterval.Name = "txtBackgroundInterval";
            this.txtBackgroundInterval.Size = new System.Drawing.Size(100, 20);
            this.txtBackgroundInterval.TabIndex = 7;
            // 
            // grpAdminReport
            // 
            this.grpAdminReport.Controls.Add(this.lblAdminRefresh);
            this.grpAdminReport.Controls.Add(this.txtAdminRefresh);
            this.grpAdminReport.Location = new System.Drawing.Point(12, 316);
            this.grpAdminReport.Name = "grpAdminReport";
            this.grpAdminReport.Size = new System.Drawing.Size(460, 55);
            this.grpAdminReport.TabIndex = 4;
            this.grpAdminReport.TabStop = false;
            this.grpAdminReport.Text = "Administration Report";
            // 
            // lblAdminRefresh
            // 
            this.lblAdminRefresh.AutoSize = true;
            this.lblAdminRefresh.Location = new System.Drawing.Point(15, 25);
            this.lblAdminRefresh.Name = "lblAdminRefresh";
            this.lblAdminRefresh.Size = new System.Drawing.Size(87, 13);
            this.lblAdminRefresh.Text = "Refresh Interval:";
            // 
            // txtAdminRefresh
            // 
            this.txtAdminRefresh.Location = new System.Drawing.Point(150, 22);
            this.txtAdminRefresh.Name = "txtAdminRefresh";
            this.txtAdminRefresh.Size = new System.Drawing.Size(100, 20);
            this.txtAdminRefresh.TabIndex = 8;
            // 
            // grpCountdownDisplay
            // 
            this.grpCountdownDisplay.Controls.Add(this.lblCountdownRefresh);
            this.grpCountdownDisplay.Controls.Add(this.txtCountdownRefresh);
            this.grpCountdownDisplay.Location = new System.Drawing.Point(12, 377);
            this.grpCountdownDisplay.Name = "grpCountdownDisplay";
            this.grpCountdownDisplay.Size = new System.Drawing.Size(460, 55);
            this.grpCountdownDisplay.TabIndex = 5;
            this.grpCountdownDisplay.TabStop = false;
            this.grpCountdownDisplay.Text = "Countdown Display";
            // 
            // lblCountdownRefresh
            // 
            this.lblCountdownRefresh.AutoSize = true;
            this.lblCountdownRefresh.Location = new System.Drawing.Point(15, 25);
            this.lblCountdownRefresh.Name = "lblCountdownRefresh";
            this.lblCountdownRefresh.Size = new System.Drawing.Size(72, 13);
            this.lblCountdownRefresh.Text = "Refresh Rate:";
            // 
            // txtCountdownRefresh
            // 
            this.txtCountdownRefresh.Location = new System.Drawing.Point(150, 22);
            this.txtCountdownRefresh.Name = "txtCountdownRefresh";
            this.txtCountdownRefresh.Size = new System.Drawing.Size(100, 20);
            this.txtCountdownRefresh.TabIndex = 9;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(216, 445);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 10;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(297, 445);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnResetDefaults
            // 
            this.btnResetDefaults.Location = new System.Drawing.Point(397, 445);
            this.btnResetDefaults.Name = "btnResetDefaults";
            this.btnResetDefaults.Size = new System.Drawing.Size(75, 23);
            this.btnResetDefaults.TabIndex = 12;
            this.btnResetDefaults.Text = "Reset";
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            // 
            // FormPreferences
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(484, 481);
            this.Controls.Add(this.grpStructureCount);
            this.Controls.Add(this.grpWorkerRequest);
            this.Controls.Add(this.grpColonyImport);
            this.Controls.Add(this.grpBackgroundProcessing);
            this.Controls.Add(this.grpAdminReport);
            this.Controls.Add(this.grpCountdownDisplay);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnResetDefaults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormPreferences";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.grpStructureCount.ResumeLayout(false);
            this.grpStructureCount.PerformLayout();
            this.grpWorkerRequest.ResumeLayout(false);
            this.grpWorkerRequest.PerformLayout();
            this.grpColonyImport.ResumeLayout(false);
            this.grpColonyImport.PerformLayout();
            this.grpBackgroundProcessing.ResumeLayout(false);
            this.grpBackgroundProcessing.PerformLayout();
            this.grpAdminReport.ResumeLayout(false);
            this.grpAdminReport.PerformLayout();
            this.grpCountdownDisplay.ResumeLayout(false);
            this.grpCountdownDisplay.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox grpStructureCount;
        private System.Windows.Forms.Label lblStructureYellow;
        internal System.Windows.Forms.TextBox txtStructureYellow;
        private System.Windows.Forms.Label lblStructureRed;
        internal System.Windows.Forms.TextBox txtStructureRed;
        private System.Windows.Forms.GroupBox grpWorkerRequest;
        private System.Windows.Forms.Label lblWorkerYellow;
        internal System.Windows.Forms.TextBox txtWorkerYellow;
        private System.Windows.Forms.Label lblWorkerRed;
        internal System.Windows.Forms.TextBox txtWorkerRed;
        private System.Windows.Forms.GroupBox grpColonyImport;
        private System.Windows.Forms.Label lblColonyImportYellow;
        internal System.Windows.Forms.TextBox txtColonyImportYellow;
        private System.Windows.Forms.Label lblColonyImportRed;
        internal System.Windows.Forms.TextBox txtColonyImportRed;
        private System.Windows.Forms.GroupBox grpBackgroundProcessing;
        private System.Windows.Forms.Label lblBackgroundInterval;
        internal System.Windows.Forms.TextBox txtBackgroundInterval;
        private System.Windows.Forms.GroupBox grpAdminReport;
        private System.Windows.Forms.Label lblAdminRefresh;
        internal System.Windows.Forms.TextBox txtAdminRefresh;
        private System.Windows.Forms.GroupBox grpCountdownDisplay;
        private System.Windows.Forms.Label lblCountdownRefresh;
        internal System.Windows.Forms.TextBox txtCountdownRefresh;
        internal System.Windows.Forms.Button btnOK;
        internal System.Windows.Forms.Button btnCancel;
        internal System.Windows.Forms.Button btnResetDefaults;
    }
}
