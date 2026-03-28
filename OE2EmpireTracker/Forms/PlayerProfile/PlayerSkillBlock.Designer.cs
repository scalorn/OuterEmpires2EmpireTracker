namespace OE2EmpireTracker.Forms.PlayerProfile
{
    partial class PlayerSkillBlock
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
            this.flowLayoutPanel20 = new System.Windows.Forms.FlowLayoutPanel();
            this.label23 = new System.Windows.Forms.Label();
            this.lblSkillName = new System.Windows.Forms.Label();
            this.lblCompletion = new System.Windows.Forms.Label();
            this.cmdStart = new System.Windows.Forms.Button();
            this.timerCountdown = new System.Windows.Forms.Timer(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.txtSkillLevel = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.txtCompletion = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flowLayoutPanel20.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel20
            // 
            this.flowLayoutPanel20.Controls.Add(this.label23);
            this.flowLayoutPanel20.Controls.Add(this.lblSkillName);
            this.flowLayoutPanel20.Controls.Add(this.txtSkillLevel);
            this.flowLayoutPanel20.Controls.Add(this.lblCompletion);
            this.flowLayoutPanel20.Controls.Add(this.txtCompletion);
            this.flowLayoutPanel20.Controls.Add(this.cmdStart);
            this.flowLayoutPanel20.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel20.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel20.MaximumSize = new System.Drawing.Size(450, 24);
            this.flowLayoutPanel20.MinimumSize = new System.Drawing.Size(450, 24);
            this.flowLayoutPanel20.Name = "flowLayoutPanel20";
            this.flowLayoutPanel20.Size = new System.Drawing.Size(450, 24);
            this.flowLayoutPanel20.TabIndex = 10;
            this.flowLayoutPanel20.WrapContents = false;
            // 
            // label23
            // 
            this.label23.Location = new System.Drawing.Point(3, 0);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(40, 17);
            this.label23.TabIndex = 9;
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSkillName
            // 
            this.lblSkillName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSkillName.Location = new System.Drawing.Point(48, 3);
            this.lblSkillName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSkillName.Name = "lblSkillName";
            this.lblSkillName.Size = new System.Drawing.Size(119, 20);
            this.lblSkillName.TabIndex = 2;
            this.lblSkillName.Text = "SkillName";
            this.lblSkillName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCompletion
            // 
            this.lblCompletion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCompletion.Location = new System.Drawing.Point(206, 3);
            this.lblCompletion.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblCompletion.Name = "lblCompletion";
            this.lblCompletion.Size = new System.Drawing.Size(61, 20);
            this.lblCompletion.TabIndex = 11;
            this.lblCompletion.Text = "Completion";
            this.lblCompletion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmdStart
            // 
            this.cmdStart.Location = new System.Drawing.Point(366, 3);
            this.cmdStart.Name = "cmdStart";
            this.cmdStart.Size = new System.Drawing.Size(75, 20);
            this.cmdStart.TabIndex = 13;
            this.cmdStart.Text = "Start";
            this.cmdStart.UseVisualStyleBackColor = true;
            this.cmdStart.Click += new System.EventHandler(this.cmdStart_Click);
            // 
            // timerCountdown
            // 
            this.timerCountdown.Tick += new System.EventHandler(this.timerCountdown_Tick);
            // 
            // txtSkillLevel
            // 
            this.txtSkillLevel.AllowSpaces = false;
            this.txtSkillLevel.AutoFormat = true;
            this.txtSkillLevel.ErrorMessage = "";
            this.txtSkillLevel.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtSkillLevel.IsValid = true;
            this.txtSkillLevel.Location = new System.Drawing.Point(171, 2);
            this.txtSkillLevel.Margin = new System.Windows.Forms.Padding(2);
            this.txtSkillLevel.Name = "txtSkillLevel";
            this.txtSkillLevel.Size = new System.Drawing.Size(31, 20);
            this.txtSkillLevel.TabIndex = 7;
            this.txtSkillLevel.ValidationPattern = "^(10|[0-9])$";
            this.txtSkillLevel.ValidColor = System.Drawing.Color.White;
            // 
            // txtCompletion
            // 
            this.txtCompletion.AllowSpaces = true;
            this.txtCompletion.AutoFormat = true;
            this.txtCompletion.BackColor = System.Drawing.Color.White;
            this.txtCompletion.ErrorMessage = "";
            this.txtCompletion.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtCompletion.IsValid = true;
            this.txtCompletion.Location = new System.Drawing.Point(272, 3);
            this.txtCompletion.Name = "txtCompletion";
            this.txtCompletion.Size = new System.Drawing.Size(88, 20);
            this.txtCompletion.TabIndex = 12;
            this.txtCompletion.Text = "99d 23h 59m 59s";
            this.txtCompletion.ValidationPattern = "^\\s*(?:\\d+d\\s*)?(?:(?:[01]?\\d|2[0-3])h\\s*)?(?:(?:[0-5]?\\d)m\\s*)?(?:(?:[0-5]?\\d)s)" +
    "?\\s*$";
            this.txtCompletion.ValidColor = System.Drawing.Color.White;
            this.txtCompletion.Enter += new System.EventHandler(this.txtCompletion_Enter);
            this.txtCompletion.Leave += new System.EventHandler(this.txtCompletion_Leave);
            // 
            // PlayerSkillBlock
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.flowLayoutPanel20);
            this.MaximumSize = new System.Drawing.Size(450, 24);
            this.MinimumSize = new System.Drawing.Size(450, 24);
            this.Name = "PlayerSkillBlock";
            this.Size = new System.Drawing.Size(450, 24);
            this.flowLayoutPanel20.ResumeLayout(false);
            this.flowLayoutPanel20.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel20;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label lblSkillName;
        private Controls.ValidatedTextBox txtSkillLevel;
        private System.Windows.Forms.Label lblCompletion;
        private Controls.ValidatedTextBox txtCompletion;
        private System.Windows.Forms.Button cmdStart;
        private System.Windows.Forms.Timer timerCountdown;
        private System.Windows.Forms.Timer timer1;
    }
}
