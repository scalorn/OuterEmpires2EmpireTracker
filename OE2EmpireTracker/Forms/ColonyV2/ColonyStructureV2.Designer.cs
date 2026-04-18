namespace OE2EmpireTracker.Forms.ColonyV2
{
    partial class ColonyStructureV2
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
            this.components = new System.ComponentModel.Container();
            this.flpColonyStructure = new System.Windows.Forms.FlowLayoutPanel();
            this.flpHeader = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.chkStaged = new System.Windows.Forms.CheckBox();
            this.chkBuilt = new System.Windows.Forms.CheckBox();
            this.chkOnline = new System.Windows.Forms.CheckBox();
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.flpWorkers = new System.Windows.Forms.FlowLayoutPanel();
            this.chkWorker1 = new System.Windows.Forms.CheckBox();
            this.chkWorker2 = new System.Windows.Forms.CheckBox();
            this.chkWorker3 = new System.Windows.Forms.CheckBox();
            this.chkWorker4 = new System.Windows.Forms.CheckBox();
            this.chkWorker5 = new System.Windows.Forms.CheckBox();
            this.chkWorker6 = new System.Windows.Forms.CheckBox();
            this.flpSurveySelection = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSurveyFilter = new System.Windows.Forms.Label();
            this.txtSurveyFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbSurvey = new System.Windows.Forms.ComboBox();
            this.flpSelection = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSelectionFilter = new System.Windows.Forms.Label();
            this.txtSelectionFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbSelection = new System.Windows.Forms.ComboBox();
            this.flpManufacturing = new System.Windows.Forms.FlowLayoutPanel();
            this.rtbProgressStatus = new System.Windows.Forms.RichTextBox();
            this.txtCompletionTime = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.txtQuantity = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.chkStageResources = new System.Windows.Forms.CheckBox();
            this.cmdStart = new System.Windows.Forms.Button();
            this.cmdDone = new System.Windows.Forms.Button();
            this.flpStructureCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdUp = new System.Windows.Forms.Button();
            this.cmdDown = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.timerCountdown = new System.Windows.Forms.Timer(this.components);
            this.flpColonyStructure.SuspendLayout();
            this.flpHeader.SuspendLayout();
            this.flpWorkers.SuspendLayout();
            this.flpSurveySelection.SuspendLayout();
            this.flpSelection.SuspendLayout();
            this.flpManufacturing.SuspendLayout();
            this.flpStructureCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpColonyStructure
            // 
            this.flpColonyStructure.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpColonyStructure.Controls.Add(this.flpHeader);
            this.flpColonyStructure.Controls.Add(this.rtbStatus);
            this.flpColonyStructure.Controls.Add(this.flpWorkers);
            this.flpColonyStructure.Controls.Add(this.flpSurveySelection);
            this.flpColonyStructure.Controls.Add(this.flpSelection);
            this.flpColonyStructure.Controls.Add(this.flpManufacturing);
            this.flpColonyStructure.Controls.Add(this.flpStructureCommands);
            this.flpColonyStructure.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyStructure.Location = new System.Drawing.Point(0, 0);
            this.flpColonyStructure.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.flpColonyStructure.Name = "flpColonyStructure";
            this.flpColonyStructure.Size = new System.Drawing.Size(620, 200);
            this.flpColonyStructure.TabIndex = 0;
            this.flpColonyStructure.WrapContents = false;
            // 
            // flpHeader
            // 
            this.flpHeader.AutoSize = true;
            this.flpHeader.Controls.Add(this.lblName);
            this.flpHeader.Controls.Add(this.chkStaged);
            this.flpHeader.Controls.Add(this.chkBuilt);
            this.flpHeader.Controls.Add(this.chkOnline);
            this.flpHeader.Location = new System.Drawing.Point(3, 3);
            this.flpHeader.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.flpHeader.Name = "flpHeader";
            this.flpHeader.Size = new System.Drawing.Size(610, 23);
            this.flpHeader.TabIndex = 0;
            this.flpHeader.WrapContents = false;
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblName.Location = new System.Drawing.Point(3, 3);
            this.lblName.Margin = new System.Windows.Forms.Padding(3);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(120, 13);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Blueprint Name #1";
            // 
            // chkStaged
            // 
            this.chkStaged.AutoSize = true;
            this.chkStaged.Location = new System.Drawing.Point(129, 3);
            this.chkStaged.Name = "chkStaged";
            this.chkStaged.Size = new System.Drawing.Size(60, 17);
            this.chkStaged.TabIndex = 1;
            this.chkStaged.Text = "Staged";
            this.chkStaged.UseVisualStyleBackColor = true;
            this.chkStaged.CheckedChanged += new System.EventHandler(this.chkStaged_CheckedChanged);
            // 
            // chkBuilt
            // 
            this.chkBuilt.AutoSize = true;
            this.chkBuilt.Location = new System.Drawing.Point(195, 3);
            this.chkBuilt.Name = "chkBuilt";
            this.chkBuilt.Size = new System.Drawing.Size(46, 17);
            this.chkBuilt.TabIndex = 2;
            this.chkBuilt.Text = "Built";
            this.chkBuilt.UseVisualStyleBackColor = true;
            this.chkBuilt.CheckedChanged += new System.EventHandler(this.chkBuilt_CheckedChanged);
            // 
            // chkOnline
            // 
            this.chkOnline.AutoSize = true;
            this.chkOnline.Location = new System.Drawing.Point(247, 3);
            this.chkOnline.Name = "chkOnline";
            this.chkOnline.Size = new System.Drawing.Size(56, 17);
            this.chkOnline.TabIndex = 3;
            this.chkOnline.Text = "Online";
            this.chkOnline.UseVisualStyleBackColor = true;
            this.chkOnline.CheckedChanged += new System.EventHandler(this.chkOnline_CheckedChanged);
            // 
            // rtbStatus
            // 
            this.rtbStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbStatus.Location = new System.Drawing.Point(3, 26);
            this.rtbStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.ReadOnly = true;
            this.rtbStatus.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbStatus.Size = new System.Drawing.Size(610, 40);
            this.rtbStatus.TabIndex = 1;
            this.rtbStatus.TabStop = false;
            this.rtbStatus.Enabled = false;
            this.rtbStatus.Text = "";
            this.rtbStatus.WordWrap = false;
            this.rtbStatus.ContentsResized += new System.Windows.Forms.ContentsResizedEventHandler(this.rtbStatus_ContentsResized);
            // 
            // flpWorkers
            // 
            this.flpWorkers.AutoSize = true;
            this.flpWorkers.Controls.Add(this.chkWorker1);
            this.flpWorkers.Controls.Add(this.chkWorker2);
            this.flpWorkers.Controls.Add(this.chkWorker3);
            this.flpWorkers.Controls.Add(this.chkWorker4);
            this.flpWorkers.Controls.Add(this.chkWorker5);
            this.flpWorkers.Controls.Add(this.chkWorker6);
            this.flpWorkers.Location = new System.Drawing.Point(3, 66);
            this.flpWorkers.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.flpWorkers.Name = "flpWorkers";
            this.flpWorkers.Size = new System.Drawing.Size(610, 23);
            this.flpWorkers.TabIndex = 2;
            this.flpWorkers.WrapContents = false;
            // 
            // chkWorker1 through chkWorker6 (unchanged)
            // 
            this.chkWorker1.AutoSize = true;
            this.chkWorker1.Location = new System.Drawing.Point(3, 3);
            this.chkWorker1.Name = "chkWorker1";
            this.chkWorker1.Size = new System.Drawing.Size(80, 17);
            this.chkWorker1.TabIndex = 0;
            this.chkWorker1.Text = "Worker 1";
            this.chkWorker1.Visible = false;
            this.chkWorker1.UseVisualStyleBackColor = true;
            this.chkWorker1.CheckedChanged += new System.EventHandler(this.chkWorker_CheckedChanged);
            this.chkWorker2.AutoSize = true;
            this.chkWorker2.Location = new System.Drawing.Point(89, 3);
            this.chkWorker2.Name = "chkWorker2";
            this.chkWorker2.Size = new System.Drawing.Size(80, 17);
            this.chkWorker2.TabIndex = 1;
            this.chkWorker2.Text = "Worker 2";
            this.chkWorker2.Visible = false;
            this.chkWorker2.UseVisualStyleBackColor = true;
            this.chkWorker2.CheckedChanged += new System.EventHandler(this.chkWorker_CheckedChanged);
            this.chkWorker3.AutoSize = true;
            this.chkWorker3.Location = new System.Drawing.Point(175, 3);
            this.chkWorker3.Name = "chkWorker3";
            this.chkWorker3.Size = new System.Drawing.Size(80, 17);
            this.chkWorker3.TabIndex = 2;
            this.chkWorker3.Text = "Worker 3";
            this.chkWorker3.Visible = false;
            this.chkWorker3.UseVisualStyleBackColor = true;
            this.chkWorker3.CheckedChanged += new System.EventHandler(this.chkWorker_CheckedChanged);
            this.chkWorker4.AutoSize = true;
            this.chkWorker4.Location = new System.Drawing.Point(261, 3);
            this.chkWorker4.Name = "chkWorker4";
            this.chkWorker4.Size = new System.Drawing.Size(80, 17);
            this.chkWorker4.TabIndex = 3;
            this.chkWorker4.Text = "Worker 4";
            this.chkWorker4.Visible = false;
            this.chkWorker4.UseVisualStyleBackColor = true;
            this.chkWorker4.CheckedChanged += new System.EventHandler(this.chkWorker_CheckedChanged);
            this.chkWorker5.AutoSize = true;
            this.chkWorker5.Location = new System.Drawing.Point(347, 3);
            this.chkWorker5.Name = "chkWorker5";
            this.chkWorker5.Size = new System.Drawing.Size(80, 17);
            this.chkWorker5.TabIndex = 4;
            this.chkWorker5.Text = "Worker 5";
            this.chkWorker5.Visible = false;
            this.chkWorker5.UseVisualStyleBackColor = true;
            this.chkWorker5.CheckedChanged += new System.EventHandler(this.chkWorker_CheckedChanged);
            this.chkWorker6.AutoSize = true;
            this.chkWorker6.Location = new System.Drawing.Point(433, 3);
            this.chkWorker6.Name = "chkWorker6";
            this.chkWorker6.Size = new System.Drawing.Size(80, 17);
            this.chkWorker6.TabIndex = 5;
            this.chkWorker6.Text = "Worker 6";
            this.chkWorker6.Visible = false;
            this.chkWorker6.UseVisualStyleBackColor = true;
            this.chkWorker6.CheckedChanged += new System.EventHandler(this.chkWorker_CheckedChanged);
            // 
            // flpSurveySelection
            // 
            this.flpSurveySelection.Controls.Add(this.lblSurveyFilter);
            this.flpSurveySelection.Controls.Add(this.txtSurveyFilter);
            this.flpSurveySelection.Controls.Add(this.cmbSurvey);
            this.flpSurveySelection.Location = new System.Drawing.Point(3, 89);
            this.flpSurveySelection.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.flpSurveySelection.Name = "flpSurveySelection";
            this.flpSurveySelection.Size = new System.Drawing.Size(610, 26);
            this.flpSurveySelection.TabIndex = 3;
            this.flpSurveySelection.Visible = false;
            this.flpSurveySelection.WrapContents = false;
            // 
            // lblSurveyFilter
            // 
            this.lblSurveyFilter.Location = new System.Drawing.Point(3, 3);
            this.lblSurveyFilter.Name = "lblSurveyFilter";
            this.lblSurveyFilter.Size = new System.Drawing.Size(45, 13);
            this.lblSurveyFilter.TabIndex = 0;
            this.lblSurveyFilter.Text = "Survey:";
            this.txtSurveyFilter.Location = new System.Drawing.Point(54, 3);
            this.txtSurveyFilter.Name = "txtSurveyFilter";
            this.txtSurveyFilter.Size = new System.Drawing.Size(100, 20);
            this.txtSurveyFilter.TabIndex = 1;
            this.cmbSurvey.FormattingEnabled = true;
            this.cmbSurvey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSurvey.Location = new System.Drawing.Point(160, 3);
            this.cmbSurvey.Name = "cmbSurvey";
            this.cmbSurvey.Size = new System.Drawing.Size(200, 21);
            this.cmbSurvey.TabIndex = 2;
            // 
            // flpSelection
            // 
            this.flpSelection.Controls.Add(this.lblSelectionFilter);
            this.flpSelection.Controls.Add(this.txtSelectionFilter);
            this.flpSelection.Controls.Add(this.cmbSelection);
            this.flpSelection.Controls.Add(this.txtQuantity);
            this.flpSelection.Controls.Add(this.chkStageResources);
            this.flpSelection.Controls.Add(this.cmdStart);
            this.flpSelection.Controls.Add(this.cmdDone);
            this.flpSelection.Location = new System.Drawing.Point(3, 115);
            this.flpSelection.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.flpSelection.Name = "flpSelection";
            this.flpSelection.Size = new System.Drawing.Size(610, 26);
            this.flpSelection.TabIndex = 4;
            this.flpSelection.Visible = false;
            this.flpSelection.WrapContents = false;
            this.lblSelectionFilter.Location = new System.Drawing.Point(3, 3);
            this.lblSelectionFilter.Name = "lblSelectionFilter";
            this.lblSelectionFilter.Size = new System.Drawing.Size(40, 13);
            this.lblSelectionFilter.TabIndex = 0;
            this.lblSelectionFilter.Text = "Select:";
            this.txtSelectionFilter.Location = new System.Drawing.Point(49, 3);
            this.txtSelectionFilter.Name = "txtSelectionFilter";
            this.txtSelectionFilter.Size = new System.Drawing.Size(100, 20);
            this.txtSelectionFilter.TabIndex = 1;
            this.cmbSelection.FormattingEnabled = true;
            this.cmbSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelection.Location = new System.Drawing.Point(155, 3);
            this.cmbSelection.Name = "cmbSelection";
            this.cmbSelection.Size = new System.Drawing.Size(200, 21);
            this.cmbSelection.TabIndex = 2;
            // 
            // flpManufacturing — merged with former flpTimer
            // Layout: [ProgressStatus] [Countdown] [Qty] [StageRes] [Start] [Done]
            // 
            this.flpManufacturing.Controls.Add(this.rtbProgressStatus);
            this.flpManufacturing.Controls.Add(this.txtCompletionTime);
            this.flpManufacturing.Location = new System.Drawing.Point(3, 141);
            this.flpManufacturing.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.flpManufacturing.Name = "flpManufacturing";
            this.flpManufacturing.Size = new System.Drawing.Size(610, 26);
            this.flpManufacturing.TabIndex = 5;
            this.flpManufacturing.Visible = false;
            this.flpManufacturing.WrapContents = false;
            // 
            // rtbProgressStatus
            // 
            this.rtbProgressStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbProgressStatus.Location = new System.Drawing.Point(3, 3);
            this.rtbProgressStatus.Name = "rtbProgressStatus";
            this.rtbProgressStatus.ReadOnly = true;
            this.rtbProgressStatus.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbProgressStatus.Size = new System.Drawing.Size(200, 20);
            this.rtbProgressStatus.TabIndex = 0;
            this.rtbProgressStatus.TabStop = false;
            this.rtbProgressStatus.Enabled = false;
            this.rtbProgressStatus.Text = "";
            this.rtbProgressStatus.WordWrap = false;
            // 
            // txtCompletionTime
            // 
            this.txtCompletionTime.Location = new System.Drawing.Point(209, 3);
            this.txtCompletionTime.Name = "txtCompletionTime";
            this.txtCompletionTime.Size = new System.Drawing.Size(80, 20);
            this.txtCompletionTime.TabIndex = 1;
            // 
            // txtQuantity
            // 
            this.txtQuantity.Location = new System.Drawing.Point(295, 3);
            this.txtQuantity.Name = "txtQuantity";
            this.txtQuantity.Size = new System.Drawing.Size(39, 20);
            this.txtQuantity.TabIndex = 2;
            this.txtQuantity.Visible = false;
            // 
            // chkStageResources
            // 
            this.chkStageResources.AutoSize = true;
            this.chkStageResources.Location = new System.Drawing.Point(340, 3);
            this.chkStageResources.Name = "chkStageResources";
            this.chkStageResources.Size = new System.Drawing.Size(109, 17);
            this.chkStageResources.TabIndex = 3;
            this.chkStageResources.Text = "Stage Resources";
            this.chkStageResources.Visible = false;
            this.chkStageResources.UseVisualStyleBackColor = true;
            // 
            // cmdStart
            // 
            this.cmdStart.Location = new System.Drawing.Point(455, 3);
            this.cmdStart.Name = "cmdStart";
            this.cmdStart.Size = new System.Drawing.Size(50, 23);
            this.cmdStart.TabIndex = 4;
            this.cmdStart.Text = "Start";
            this.cmdStart.UseVisualStyleBackColor = true;
            // 
            // cmdDone
            // 
            this.cmdDone.Location = new System.Drawing.Point(511, 3);
            this.cmdDone.Name = "cmdDone";
            this.cmdDone.Size = new System.Drawing.Size(50, 23);
            this.cmdDone.TabIndex = 5;
            this.cmdDone.Text = "Done";
            this.cmdDone.UseVisualStyleBackColor = true;
            // 
            // flpStructureCommands
            // 
            this.flpStructureCommands.Controls.Add(this.cmdUp);
            this.flpStructureCommands.Controls.Add(this.cmdDown);
            this.flpStructureCommands.Controls.Add(this.cmdDelete);
            this.flpStructureCommands.Location = new System.Drawing.Point(3, 167);
            this.flpStructureCommands.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.flpStructureCommands.Name = "flpStructureCommands";
            this.flpStructureCommands.Size = new System.Drawing.Size(610, 26);
            this.flpStructureCommands.TabIndex = 6;
            this.flpStructureCommands.WrapContents = false;
            this.cmdUp.Location = new System.Drawing.Point(3, 3);
            this.cmdUp.Name = "cmdUp";
            this.cmdUp.Size = new System.Drawing.Size(50, 23);
            this.cmdUp.TabIndex = 0;
            this.cmdUp.Text = "Up";
            this.cmdUp.UseVisualStyleBackColor = true;
            this.cmdUp.Click += new System.EventHandler(this.cmdUp_Click);
            this.cmdDown.Location = new System.Drawing.Point(59, 3);
            this.cmdDown.Name = "cmdDown";
            this.cmdDown.Size = new System.Drawing.Size(50, 23);
            this.cmdDown.TabIndex = 1;
            this.cmdDown.Text = "Down";
            this.cmdDown.UseVisualStyleBackColor = true;
            this.cmdDown.Click += new System.EventHandler(this.cmdDown_Click);
            this.cmdDelete.Location = new System.Drawing.Point(115, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(50, 23);
            this.cmdDelete.TabIndex = 2;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
            // 
            // timerCountdown
            // 
            this.timerCountdown.Tick += new System.EventHandler(this.timerCountdown_Tick);
            // 
            // ColonyStructureV2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flpColonyStructure);
            this.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.Name = "ColonyStructureV2";
            this.Size = new System.Drawing.Size(624, 200);
            this.flpColonyStructure.ResumeLayout(false);
            this.flpColonyStructure.PerformLayout();
            this.flpHeader.ResumeLayout(false);
            this.flpHeader.PerformLayout();
            this.flpWorkers.ResumeLayout(false);
            this.flpWorkers.PerformLayout();
            this.flpSurveySelection.ResumeLayout(false);
            this.flpSurveySelection.PerformLayout();
            this.flpSelection.ResumeLayout(false);
            this.flpSelection.PerformLayout();
            this.flpManufacturing.ResumeLayout(false);
            this.flpManufacturing.PerformLayout();
            this.flpStructureCommands.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpColonyStructure;
        private System.Windows.Forms.FlowLayoutPanel flpHeader;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.CheckBox chkStaged;
        private System.Windows.Forms.CheckBox chkBuilt;
        private System.Windows.Forms.CheckBox chkOnline;
        private System.Windows.Forms.RichTextBox rtbStatus;
        private System.Windows.Forms.FlowLayoutPanel flpWorkers;
        private System.Windows.Forms.CheckBox chkWorker1;
        private System.Windows.Forms.CheckBox chkWorker2;
        private System.Windows.Forms.CheckBox chkWorker3;
        private System.Windows.Forms.CheckBox chkWorker4;
        private System.Windows.Forms.CheckBox chkWorker5;
        private System.Windows.Forms.CheckBox chkWorker6;
        private System.Windows.Forms.FlowLayoutPanel flpSurveySelection;
        private System.Windows.Forms.Label lblSurveyFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSurveyFilter;
        private System.Windows.Forms.ComboBox cmbSurvey;
        private System.Windows.Forms.FlowLayoutPanel flpSelection;
        private System.Windows.Forms.Label lblSelectionFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSelectionFilter;
        private System.Windows.Forms.ComboBox cmbSelection;
        private System.Windows.Forms.FlowLayoutPanel flpManufacturing;
        private System.Windows.Forms.RichTextBox rtbProgressStatus;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCompletionTime;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtQuantity;
        private System.Windows.Forms.CheckBox chkStageResources;
        private System.Windows.Forms.Button cmdStart;
        private System.Windows.Forms.Button cmdDone;
        private System.Windows.Forms.FlowLayoutPanel flpStructureCommands;
        private System.Windows.Forms.Button cmdUp;
        private System.Windows.Forms.Button cmdDown;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Timer timerCountdown;
    }
}
