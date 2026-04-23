namespace OE2EmpireTracker.Forms
{
    partial class FormAbout
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
            this.lblAppName = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblCopyright = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lnkGame = new System.Windows.Forms.LinkLabel();
            this.lblDesigner = new System.Windows.Forms.Label();
            this.lblDeveloped = new System.Windows.Forms.Label();
            this.lnkKiro = new System.Windows.Forms.LinkLabel();
            this.lblGitHub = new System.Windows.Forms.Label();
            this.lnkGitHub = new System.Windows.Forms.LinkLabel();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblAppName
            // 
            this.lblAppName.AutoSize = true;
            this.lblAppName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblAppName.Location = new System.Drawing.Point(20, 20);
            this.lblAppName.Name = "lblAppName";
            this.lblAppName.Size = new System.Drawing.Size(100, 20);
            this.lblAppName.Text = "Application";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(22, 48);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(60, 13);
            this.lblVersion.Text = "Version";
            // 
            // lblCopyright
            // 
            this.lblCopyright.AutoSize = true;
            this.lblCopyright.Location = new System.Drawing.Point(22, 68);
            this.lblCopyright.Name = "lblCopyright";
            this.lblCopyright.Size = new System.Drawing.Size(60, 13);
            this.lblCopyright.Text = "Copyright";
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(22, 98);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(250, 13);
            this.lblDescription.Text = "Empire Tracking Tool for Outer Empires 2";
            // 
            // lnkGame
            // 
            this.lnkGame.AutoSize = true;
            this.lnkGame.Location = new System.Drawing.Point(22, 116);
            this.lnkGame.Name = "lnkGame";
            this.lnkGame.Size = new System.Drawing.Size(150, 13);
            this.lnkGame.Text = "https://outerempires.net/";
            this.lnkGame.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkGame_LinkClicked);
            // 
            // lblDesigner
            // 
            this.lblDesigner.AutoSize = true;
            this.lblDesigner.Location = new System.Drawing.Point(22, 146);
            this.lblDesigner.Name = "lblDesigner";
            this.lblDesigner.Size = new System.Drawing.Size(300, 13);
            this.lblDesigner.Text = "Designed by Scalorn Scorpus - scalorn@gmail.com";
            // 
            // lblDeveloped
            // 
            this.lblDeveloped.AutoSize = true;
            this.lblDeveloped.Location = new System.Drawing.Point(22, 176);
            this.lblDeveloped.Name = "lblDeveloped";
            this.lblDeveloped.Size = new System.Drawing.Size(400, 13);
            this.lblDeveloped.Text = "Developed and maintained via a specification development process by Kiro";
            // 
            // lnkKiro
            // 
            this.lnkKiro.AutoSize = true;
            this.lnkKiro.Location = new System.Drawing.Point(22, 194);
            this.lnkKiro.Name = "lnkKiro";
            this.lnkKiro.Size = new System.Drawing.Size(100, 13);
            this.lnkKiro.Text = "https://kiro.dev/";
            this.lnkKiro.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkKiro_LinkClicked);
            // 
            // lblGitHub
            // 
            this.lblGitHub.AutoSize = true;
            this.lblGitHub.Location = new System.Drawing.Point(22, 224);
            this.lblGitHub.Name = "lblGitHub";
            this.lblGitHub.Size = new System.Drawing.Size(100, 13);
            this.lblGitHub.Text = "GitHub project at";
            // 
            // lnkGitHub
            // 
            this.lnkGitHub.AutoSize = true;
            this.lnkGitHub.Location = new System.Drawing.Point(22, 242);
            this.lnkGitHub.Name = "lnkGitHub";
            this.lnkGitHub.Size = new System.Drawing.Size(350, 13);
            this.lnkGitHub.Text = "https://github.com/scalorn/OuterEmpires2EmpireTracker";
            this.lnkGitHub.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkGitHub_LinkClicked);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(185, 275);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // FormAbout
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 315);
            this.Controls.Add(this.lblAppName);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblCopyright);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lnkGame);
            this.Controls.Add(this.lblDesigner);
            this.Controls.Add(this.lblDeveloped);
            this.Controls.Add(this.lnkKiro);
            this.Controls.Add(this.lblGitHub);
            this.Controls.Add(this.lnkGitHub);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAbout";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.LinkLabel lnkGame;
        private System.Windows.Forms.Label lblDesigner;
        private System.Windows.Forms.Label lblDeveloped;
        private System.Windows.Forms.LinkLabel lnkKiro;
        private System.Windows.Forms.Label lblGitHub;
        private System.Windows.Forms.LinkLabel lnkGitHub;
        private System.Windows.Forms.Button btnOK;
    }
}
