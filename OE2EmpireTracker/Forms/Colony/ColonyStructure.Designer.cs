namespace OE2EmpireTracker.Forms.Colony
{
    partial class ColonyStructure
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
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.flpColonyStructure = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdUp = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdDown = new System.Windows.Forms.Button();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel7 = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSelection = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSelection = new System.Windows.Forms.Label();
            this.txtSelectionFilter = new System.Windows.Forms.TextBox();
            this.cmbSelection = new System.Windows.Forms.ComboBox();
            this.cmdStart = new System.Windows.Forms.Button();
            this.flpSubSelection = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSubSelection = new System.Windows.Forms.Label();
            this.txtSubSelectionFilter = new System.Windows.Forms.TextBox();
            this.cmbSubSelection = new System.Windows.Forms.ComboBox();
            this.cmdSubStart = new System.Windows.Forms.Button();
            this.flpCompletionTime = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.txtCompletionTime = new System.Windows.Forms.TextBox();
            this.cmdDone = new System.Windows.Forms.Button();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.chkBuilt = new System.Windows.Forms.CheckBox();
            this.chkStaged = new System.Windows.Forms.CheckBox();
            this.chkOnline = new System.Windows.Forms.CheckBox();
            this.chkWorkDetail1 = new System.Windows.Forms.CheckBox();
            this.chkWorkDetail2 = new System.Windows.Forms.CheckBox();
            this.chkWorkDetail3 = new System.Windows.Forms.CheckBox();
            this.timerCountdown = new System.Windows.Forms.Timer(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.flpColonyStructure.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel7.SuspendLayout();
            this.flpSelection.SuspendLayout();
            this.flpSubSelection.SuspendLayout();
            this.flpCompletionTime.SuspendLayout();
            this.flowLayoutPanel5.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtbStatus
            // 
            this.rtbStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbStatus.Location = new System.Drawing.Point(3, 3);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.ReadOnly = true;
            this.rtbStatus.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbStatus.Size = new System.Drawing.Size(421, 40);
            this.rtbStatus.TabIndex = 0;
            this.rtbStatus.Text = "[red]6[/red]/4";
            this.rtbStatus.WordWrap = false;
            this.rtbStatus.ContentsResized += new System.Windows.Forms.ContentsResizedEventHandler(this.rtbStatus_ContentsResized);
            // 
            // flpColonyStructure
            // 
            this.flpColonyStructure.AutoScroll = true;
            this.flpColonyStructure.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpColonyStructure.Controls.Add(this.flowLayoutPanel2);
            this.flpColonyStructure.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyStructure.Location = new System.Drawing.Point(0, 0);
            this.flpColonyStructure.MinimumSize = new System.Drawing.Size(572, 160);
            this.flpColonyStructure.Name = "flpColonyStructure";
            this.flpColonyStructure.Size = new System.Drawing.Size(672, 229);
            this.flpColonyStructure.TabIndex = 1;
            this.flpColonyStructure.WrapContents = false;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel3);
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel5);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(660, 200);
            this.flowLayoutPanel2.TabIndex = 1;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoScroll = true;
            this.flowLayoutPanel3.Controls.Add(this.cmdUp);
            this.flowLayoutPanel3.Controls.Add(this.cmdDelete);
            this.flowLayoutPanel3.Controls.Add(this.cmdDown);
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(56, 87);
            this.flowLayoutPanel3.TabIndex = 0;
            // 
            // cmdUp
            // 
            this.cmdUp.Location = new System.Drawing.Point(3, 3);
            this.cmdUp.Name = "cmdUp";
            this.cmdUp.Size = new System.Drawing.Size(50, 23);
            this.cmdUp.TabIndex = 0;
            this.cmdUp.Text = "Up";
            this.cmdUp.UseVisualStyleBackColor = true;
            this.cmdUp.Click += new System.EventHandler(this.cmdUp_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(3, 32);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(50, 23);
            this.cmdDelete.TabIndex = 0;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
            // 
            // cmdDown
            // 
            this.cmdDown.Location = new System.Drawing.Point(3, 61);
            this.cmdDown.Name = "cmdDown";
            this.cmdDown.Size = new System.Drawing.Size(50, 23);
            this.cmdDown.TabIndex = 1;
            this.cmdDown.Text = "Down";
            this.cmdDown.UseVisualStyleBackColor = true;
            this.cmdDown.Click += new System.EventHandler(this.cmdDown_Click);
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.rtbStatus);
            this.flowLayoutPanel4.Controls.Add(this.flowLayoutPanel7);
            this.flowLayoutPanel4.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(65, 3);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(435, 192);
            this.flowLayoutPanel4.TabIndex = 1;
            this.flowLayoutPanel4.WrapContents = false;
            // 
            // flowLayoutPanel7
            // 
            this.flowLayoutPanel7.Controls.Add(this.flpSelection);
            this.flowLayoutPanel7.Controls.Add(this.flpSubSelection);
            this.flowLayoutPanel7.Controls.Add(this.flpCompletionTime);
            this.flowLayoutPanel7.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel7.Location = new System.Drawing.Point(3, 49);
            this.flowLayoutPanel7.Name = "flowLayoutPanel7";
            this.flowLayoutPanel7.Size = new System.Drawing.Size(432, 121);
            this.flowLayoutPanel7.TabIndex = 2;
            this.flowLayoutPanel7.WrapContents = false;
            // 
            // flpSelection
            // 
            this.flpSelection.Controls.Add(this.lblSelection);
            this.flpSelection.Controls.Add(this.txtSelectionFilter);
            this.flpSelection.Controls.Add(this.cmbSelection);
            this.flpSelection.Controls.Add(this.cmdStart);
            this.flpSelection.Location = new System.Drawing.Point(3, 3);
            this.flpSelection.Name = "flpSelection";
            this.flpSelection.Size = new System.Drawing.Size(418, 29);
            this.flpSelection.TabIndex = 5;
            // 
            // lblSelection
            // 
            this.lblSelection.Location = new System.Drawing.Point(3, 0);
            this.lblSelection.Name = "lblSelection";
            this.lblSelection.Size = new System.Drawing.Size(40, 13);
            this.lblSelection.TabIndex = 3;
            this.lblSelection.Text = "Select:";
            // 
            // txtSelectionFilter
            // 
            this.txtSelectionFilter.Location = new System.Drawing.Point(49, 3);
            this.txtSelectionFilter.Name = "txtSelectionFilter";
            this.txtSelectionFilter.Size = new System.Drawing.Size(100, 20);
            this.txtSelectionFilter.TabIndex = 4;
            this.txtSelectionFilter.TextChanged += new System.EventHandler(this.txtSelectionFilter_TextChanged);
            // 
            // cmbSelection
            // 
            this.cmbSelection.FormattingEnabled = true;
            this.cmbSelection.Location = new System.Drawing.Point(155, 3);
            this.cmbSelection.Name = "cmbSelection";
            this.cmbSelection.Size = new System.Drawing.Size(121, 21);
            this.cmbSelection.TabIndex = 0;
            this.cmbSelection.SelectedIndexChanged += new System.EventHandler(this.cmbSelection_SelectedIndexChanged);
            // 
            // cmdStart
            // 
            this.cmdStart.Location = new System.Drawing.Point(282, 3);
            this.cmdStart.Name = "cmdStart";
            this.cmdStart.Size = new System.Drawing.Size(39, 23);
            this.cmdStart.TabIndex = 1;
            this.cmdStart.Text = "Start";
            this.cmdStart.UseVisualStyleBackColor = true;
            this.cmdStart.Click += new System.EventHandler(this.cmdStart_Click);
            // 
            // flpSubSelection
            // 
            this.flpSubSelection.Controls.Add(this.lblSubSelection);
            this.flpSubSelection.Controls.Add(this.txtSubSelectionFilter);
            this.flpSubSelection.Controls.Add(this.cmbSubSelection);
            this.flpSubSelection.Controls.Add(this.cmdSubStart);
            this.flpSubSelection.Location = new System.Drawing.Point(3, 38);
            this.flpSubSelection.Name = "flpSubSelection";
            this.flpSubSelection.Size = new System.Drawing.Size(418, 29);
            this.flpSubSelection.TabIndex = 6;
            this.flpSubSelection.WrapContents = false;
            // 
            // lblSubSelection
            // 
            this.lblSubSelection.Location = new System.Drawing.Point(3, 0);
            this.lblSubSelection.Name = "lblSubSelection";
            this.lblSubSelection.Size = new System.Drawing.Size(40, 13);
            this.lblSubSelection.TabIndex = 3;
            this.lblSubSelection.Text = "Select:";
            // 
            // txtSubSelectionFilter
            // 
            this.txtSubSelectionFilter.Location = new System.Drawing.Point(49, 3);
            this.txtSubSelectionFilter.Name = "txtSubSelectionFilter";
            this.txtSubSelectionFilter.Size = new System.Drawing.Size(100, 20);
            this.txtSubSelectionFilter.TabIndex = 4;
            this.txtSubSelectionFilter.TextChanged += new System.EventHandler(this.txtSubSelectionFilter_TextChanged);
            // 
            // cmbSubSelection
            // 
            this.cmbSubSelection.FormattingEnabled = true;
            this.cmbSubSelection.Location = new System.Drawing.Point(155, 3);
            this.cmbSubSelection.Name = "cmbSubSelection";
            this.cmbSubSelection.Size = new System.Drawing.Size(166, 21);
            this.cmbSubSelection.TabIndex = 0;
            this.cmbSubSelection.SelectedIndexChanged += new System.EventHandler(this.cmbSubSelection_SelectedIndexChanged);
            // 
            // cmdSubStart
            // 
            this.cmdSubStart.Location = new System.Drawing.Point(327, 3);
            this.cmdSubStart.Name = "cmdSubStart";
            this.cmdSubStart.Size = new System.Drawing.Size(39, 23);
            this.cmdSubStart.TabIndex = 1;
            this.cmdSubStart.Text = "Start";
            this.cmdSubStart.UseVisualStyleBackColor = true;
            this.cmdSubStart.Click += new System.EventHandler(this.cmdSubStart_Click);
            // 
            // flpCompletionTime
            // 
            this.flpCompletionTime.Controls.Add(this.label1);
            this.flpCompletionTime.Controls.Add(this.txtCompletionTime);
            this.flpCompletionTime.Controls.Add(this.cmdDone);
            this.flpCompletionTime.Location = new System.Drawing.Point(3, 73);
            this.flpCompletionTime.Name = "flpCompletionTime";
            this.flpCompletionTime.Size = new System.Drawing.Size(281, 29);
            this.flpCompletionTime.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Completion Time:";
            // 
            // txtCompletionTime
            // 
            this.txtCompletionTime.Location = new System.Drawing.Point(97, 3);
            this.txtCompletionTime.Name = "txtCompletionTime";
            this.txtCompletionTime.Size = new System.Drawing.Size(100, 20);
            this.txtCompletionTime.TabIndex = 3;
            this.txtCompletionTime.Enter += new System.EventHandler(this.txtCompletionTime_Enter);
            this.txtCompletionTime.Leave += new System.EventHandler(this.txtCompletionTime_Leave);
            // 
            // cmdDone
            // 
            this.cmdDone.Location = new System.Drawing.Point(203, 3);
            this.cmdDone.Name = "cmdDone";
            this.cmdDone.Size = new System.Drawing.Size(75, 23);
            this.cmdDone.TabIndex = 6;
            this.cmdDone.Text = "Done";
            this.cmdDone.UseVisualStyleBackColor = true;
            this.cmdDone.Click += new System.EventHandler(this.cmdDone_Click);
            // 
            // flowLayoutPanel5
            // 
            this.flowLayoutPanel5.Controls.Add(this.chkBuilt);
            this.flowLayoutPanel5.Controls.Add(this.chkStaged);
            this.flowLayoutPanel5.Controls.Add(this.chkOnline);
            this.flowLayoutPanel5.Controls.Add(this.chkWorkDetail1);
            this.flowLayoutPanel5.Controls.Add(this.chkWorkDetail2);
            this.flowLayoutPanel5.Controls.Add(this.chkWorkDetail3);
            this.flowLayoutPanel5.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel5.Location = new System.Drawing.Point(506, 3);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Size = new System.Drawing.Size(132, 138);
            this.flowLayoutPanel5.TabIndex = 2;
            this.flowLayoutPanel5.WrapContents = false;
            // 
            // chkBuilt
            // 
            this.chkBuilt.Location = new System.Drawing.Point(3, 3);
            this.chkBuilt.Name = "chkBuilt";
            this.chkBuilt.Size = new System.Drawing.Size(46, 17);
            this.chkBuilt.TabIndex = 2;
            this.chkBuilt.Text = "Built";
            this.chkBuilt.UseVisualStyleBackColor = true;
            this.chkBuilt.CheckStateChanged += new System.EventHandler(this.chkBuilt_CheckStateChanged);
            // 
            // chkStaged
            // 
            this.chkStaged.Location = new System.Drawing.Point(3, 26);
            this.chkStaged.Name = "chkStaged";
            this.chkStaged.Size = new System.Drawing.Size(60, 17);
            this.chkStaged.TabIndex = 3;
            this.chkStaged.Text = "Staged";
            this.chkStaged.UseVisualStyleBackColor = true;
            this.chkStaged.CheckStateChanged += new System.EventHandler(this.chkStaged_CheckStateChanged);
            // 
            // chkOnline
            // 
            this.chkOnline.Location = new System.Drawing.Point(3, 49);
            this.chkOnline.Name = "chkOnline";
            this.chkOnline.Size = new System.Drawing.Size(56, 17);
            this.chkOnline.TabIndex = 1;
            this.chkOnline.Text = "Online";
            this.chkOnline.UseVisualStyleBackColor = true;
            this.chkOnline.CheckStateChanged += new System.EventHandler(this.chkOnline_CheckStateChanged);
            // 
            // chkWorkDetail1
            // 
            this.chkWorkDetail1.Location = new System.Drawing.Point(3, 72);
            this.chkWorkDetail1.Name = "chkWorkDetail1";
            this.chkWorkDetail1.Size = new System.Drawing.Size(126, 17);
            this.chkWorkDetail1.TabIndex = 1;
            this.chkWorkDetail1.Text = "Support - Blue Collar";
            this.chkWorkDetail1.UseVisualStyleBackColor = true;
            this.chkWorkDetail1.CheckStateChanged += new System.EventHandler(this.chkWorkDetail1_CheckStateChanged);
            // 
            // chkWorkDetail2
            // 
            this.chkWorkDetail2.Location = new System.Drawing.Point(3, 95);
            this.chkWorkDetail2.Name = "chkWorkDetail2";
            this.chkWorkDetail2.Size = new System.Drawing.Size(126, 17);
            this.chkWorkDetail2.TabIndex = 2;
            this.chkWorkDetail2.Text = "Support - White Collar";
            this.chkWorkDetail2.UseVisualStyleBackColor = true;
            this.chkWorkDetail2.CheckStateChanged += new System.EventHandler(this.chkWorkDetail2_CheckStateChanged);
            // 
            // chkWorkDetail3
            // 
            this.chkWorkDetail3.Location = new System.Drawing.Point(3, 118);
            this.chkWorkDetail3.Name = "chkWorkDetail3";
            this.chkWorkDetail3.Size = new System.Drawing.Size(126, 17);
            this.chkWorkDetail3.TabIndex = 3;
            this.chkWorkDetail3.Text = "Support - Specialist";
            this.chkWorkDetail3.UseVisualStyleBackColor = true;
            this.chkWorkDetail3.CheckStateChanged += new System.EventHandler(this.chkWorkDetail3_CheckStateChanged);
            // 
            // timerCountdown
            // 
            this.timerCountdown.Tick += new System.EventHandler(this.timerCountdown_Tick);
            // 
            // ColonyStructure
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flpColonyStructure);
            this.Name = "ColonyStructure";
            this.Size = new System.Drawing.Size(787, 232);
            this.flpColonyStructure.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel7.ResumeLayout(false);
            this.flpSelection.ResumeLayout(false);
            this.flpSelection.PerformLayout();
            this.flpSubSelection.ResumeLayout(false);
            this.flpSubSelection.PerformLayout();
            this.flpCompletionTime.ResumeLayout(false);
            this.flpCompletionTime.PerformLayout();
            this.flowLayoutPanel5.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbStatus;
        private System.Windows.Forms.FlowLayoutPanel flpColonyStructure;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Button cmdUp;
        private System.Windows.Forms.Button cmdDown;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.CheckBox chkWorkDetail1;
        private System.Windows.Forms.CheckBox chkWorkDetail2;
        private System.Windows.Forms.CheckBox chkWorkDetail3;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.CheckBox chkOnline;
        private System.Windows.Forms.CheckBox chkBuilt;
        private System.Windows.Forms.CheckBox chkStaged;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel7;
        private System.Windows.Forms.ComboBox cmbSelection;
        private System.Windows.Forms.Button cmdStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtCompletionTime;
        private System.Windows.Forms.FlowLayoutPanel flpSelection;
        private System.Windows.Forms.Label lblSelection;
        private System.Windows.Forms.TextBox txtSelectionFilter;
        private System.Windows.Forms.FlowLayoutPanel flpCompletionTime;
        private System.Windows.Forms.Button cmdDone;
        private System.Windows.Forms.FlowLayoutPanel flpSubSelection;
        private System.Windows.Forms.Label lblSubSelection;
        private System.Windows.Forms.TextBox txtSubSelectionFilter;
        private System.Windows.Forms.ComboBox cmbSubSelection;
        private System.Windows.Forms.Button cmdSubStart;
        private System.Windows.Forms.Timer timerCountdown;
        private System.Windows.Forms.Timer timer1;
    }
}
