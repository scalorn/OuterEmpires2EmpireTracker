namespace OE2EmpireTracker
{
    partial class MainWindow
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorFileExit = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addSurveyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.managePlayerProfiles = new System.Windows.Forms.ToolStripMenuItem();
            this.deliveryRoutesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pricingPlansToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deliveryExecutionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colonyDailyBuildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colonyActivityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addColonyV2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addBlueprintV2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buildPlannerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contactsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shipTemplatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shipsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.marketToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.asteroidsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cascadeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tileHorizontalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tileVerticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorWindowList = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripNextProcess = new System.Windows.Forms.ToolStripLabel();
            this.toolStripPerformance = new System.Windows.Forms.ToolStripLabel();
            this.toolStripPlayerLabel = new System.Windows.Forms.ToolStripLabel();
            this.cmbCurrentPlayer = new System.Windows.Forms.ToolStripComboBox();
            this.timerNextProcess = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.cmbCurrentPlayer,
            this.toolStripPlayerLabel,
            this.toolStripNextProcess,
            this.toolStripPerformance});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.MdiWindowListItem = this.windowToolStripMenuItem;
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.menuStrip1.Size = new System.Drawing.Size(1064, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.preferencesToolStripMenuItem,
            this.toolStripSeparatorFileExit,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 22);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.newToolStripMenuItem.Text = "New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.saveAsToolStripMenuItem.Text = "Save As";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // preferencesToolStripMenuItem
            // 
            this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            this.preferencesToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.preferencesToolStripMenuItem.Text = "Preferences...";
            this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.preferencesToolStripMenuItem_Click);
            // 
            // toolStripSeparatorFileExit
            // 
            this.toolStripSeparatorFileExit.Name = "toolStripSeparatorFileExit";
            this.toolStripSeparatorFileExit.Size = new System.Drawing.Size(118, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.colonyActivityToolStripMenuItem,
            this.colonyDailyBuildToolStripMenuItem,
            this.contactsToolStripMenuItem,
            this.deliveryExecutionToolStripMenuItem,
            this.deliveryRoutesToolStripMenuItem,
            this.pricingPlansToolStripMenuItem,
            this.buildPlannerToolStripMenuItem,
            this.shipTemplatesToolStripMenuItem,
            this.shipsToolStripMenuItem,
            this.stationsToolStripMenuItem,
            this.marketToolStripMenuItem,
            this.asteroidsToolStripMenuItem,
            this.addBlueprintV2ToolStripMenuItem,
            this.addColonyV2ToolStripMenuItem,
            this.managePlayerProfiles,
            this.addSurveyToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(62, 22);
            this.editToolStripMenuItem.Text = "Manage";
            // 
            // addSurveyToolStripMenuItem
            // 
            this.addSurveyToolStripMenuItem.Name = "addSurveyToolStripMenuItem";
            this.addSurveyToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.addSurveyToolStripMenuItem.Text = "Manage Surveys";
            this.addSurveyToolStripMenuItem.Click += new System.EventHandler(this.addSurveyToolStripMenuItem_Click);
            // 
            // addBlueprintV2ToolStripMenuItem
            // 
            this.addBlueprintV2ToolStripMenuItem.Name = "addBlueprintV2ToolStripMenuItem";
            this.addBlueprintV2ToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.addBlueprintV2ToolStripMenuItem.Text = "Manage Blueprints";
            this.addBlueprintV2ToolStripMenuItem.Click += new System.EventHandler(this.addBlueprintV2ToolStripMenuItem_Click);
            // 
            //
            // addColonyV2ToolStripMenuItem
            // 
            this.addColonyV2ToolStripMenuItem.Name = "addColonyV2ToolStripMenuItem";
            this.addColonyV2ToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.addColonyV2ToolStripMenuItem.Text = "Manage Colonies";
            this.addColonyV2ToolStripMenuItem.Click += new System.EventHandler(this.addColonyV2ToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contentsToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 22);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // contentsToolStripMenuItem
            // 
            this.contentsToolStripMenuItem.Name = "contentsToolStripMenuItem";
            this.contentsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F1)));
            this.contentsToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.contentsToolStripMenuItem.Text = "Contents";
            this.contentsToolStripMenuItem.Click += new System.EventHandler(this.contentsToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // managePlayerProfiles
            // 
            this.managePlayerProfiles.Name = "managePlayerProfiles";
            this.managePlayerProfiles.Size = new System.Drawing.Size(194, 22);
            this.managePlayerProfiles.Text = "Manage Player Profiles";
            this.managePlayerProfiles.Click += new System.EventHandler(this.managePlayerProfiles_Click);
            // 
            // deliveryRoutesToolStripMenuItem
            // 
            this.deliveryRoutesToolStripMenuItem.Name = "deliveryRoutesToolStripMenuItem";
            this.deliveryRoutesToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.deliveryRoutesToolStripMenuItem.Text = "Delivery Routes";
            this.deliveryRoutesToolStripMenuItem.Click += new System.EventHandler(this.deliveryRoutesToolStripMenuItem_Click);
            // 
            // pricingPlansToolStripMenuItem
            // 
            this.pricingPlansToolStripMenuItem.Name = "pricingPlansToolStripMenuItem";
            this.pricingPlansToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.pricingPlansToolStripMenuItem.Text = "Pricing Plans";
            this.pricingPlansToolStripMenuItem.Click += new System.EventHandler(this.pricingPlansToolStripMenuItem_Click);
            // 
            // buildPlannerToolStripMenuItem
            // 
            this.buildPlannerToolStripMenuItem.Name = "buildPlannerToolStripMenuItem";
            this.buildPlannerToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.buildPlannerToolStripMenuItem.Text = "Build Planner";
            this.buildPlannerToolStripMenuItem.Click += new System.EventHandler(this.buildPlannerToolStripMenuItem_Click);
            // 
            // contactsToolStripMenuItem
            // 
            this.contactsToolStripMenuItem.Name = "contactsToolStripMenuItem";
            this.contactsToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.contactsToolStripMenuItem.Text = "Contacts";
            this.contactsToolStripMenuItem.Click += new System.EventHandler(this.contactsToolStripMenuItem_Click);
            // 
            // shipTemplatesToolStripMenuItem
            // 
            this.shipTemplatesToolStripMenuItem.Name = "shipTemplatesToolStripMenuItem";
            this.shipTemplatesToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.shipTemplatesToolStripMenuItem.Text = "Ship Templates";
            this.shipTemplatesToolStripMenuItem.Click += new System.EventHandler(this.shipTemplatesToolStripMenuItem_Click);
            // 
            // shipsToolStripMenuItem
            // 
            this.shipsToolStripMenuItem.Name = "shipsToolStripMenuItem";
            this.shipsToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.shipsToolStripMenuItem.Text = "Ships";
            this.shipsToolStripMenuItem.Click += new System.EventHandler(this.shipsToolStripMenuItem_Click);
            // 
            // stationsToolStripMenuItem
            // 
            this.stationsToolStripMenuItem.Name = "stationsToolStripMenuItem";
            this.stationsToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.stationsToolStripMenuItem.Text = "Stations";
            this.stationsToolStripMenuItem.Click += new System.EventHandler(this.stationsToolStripMenuItem_Click);
            // 
            // marketToolStripMenuItem
            // 
            this.marketToolStripMenuItem.Name = "marketToolStripMenuItem";
            this.marketToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.marketToolStripMenuItem.Text = "Market";
            this.marketToolStripMenuItem.Click += new System.EventHandler(this.marketToolStripMenuItem_Click);
            // 
            // asteroidsToolStripMenuItem
            // 
            this.asteroidsToolStripMenuItem.Name = "asteroidsToolStripMenuItem";
            this.asteroidsToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.asteroidsToolStripMenuItem.Text = "Asteroids";
            this.asteroidsToolStripMenuItem.Click += new System.EventHandler(this.asteroidsToolStripMenuItem_Click);
            // 
            // deliveryExecutionToolStripMenuItem
            // 
            this.deliveryExecutionToolStripMenuItem.Name = "deliveryExecutionToolStripMenuItem";
            this.deliveryExecutionToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.deliveryExecutionToolStripMenuItem.Text = "Delivery Execution";
            this.deliveryExecutionToolStripMenuItem.Click += new System.EventHandler(this.deliveryExecutionToolStripMenuItem_Click);
            // 
            // colonyDailyBuildToolStripMenuItem
            // 
            this.colonyDailyBuildToolStripMenuItem.Name = "colonyDailyBuildToolStripMenuItem";
            this.colonyDailyBuildToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.colonyDailyBuildToolStripMenuItem.Text = "Colony Daily Build";
            this.colonyDailyBuildToolStripMenuItem.Click += new System.EventHandler(this.colonyDailyBuildToolStripMenuItem_Click);
            // 
            // colonyActivityToolStripMenuItem
            // 
            this.colonyActivityToolStripMenuItem.Name = "colonyActivityToolStripMenuItem";
            this.colonyActivityToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.colonyActivityToolStripMenuItem.Text = "Colony Activity";
            this.colonyActivityToolStripMenuItem.Click += new System.EventHandler(this.colonyActivityToolStripMenuItem_Click);
            // 
            // toolStripNextProcess
            // 
            this.toolStripNextProcess.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripNextProcess.Name = "toolStripNextProcess";
            this.toolStripNextProcess.Size = new System.Drawing.Size(100, 22);
            this.toolStripNextProcess.Text = "Next Process: --";
            // 
            // toolStripPerformance
            // 
            this.toolStripPerformance.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripPerformance.Name = "toolStripPerformance";
            this.toolStripPerformance.Size = new System.Drawing.Size(130, 22);
            this.toolStripPerformance.Text = "Mem: -- MB | CPU: --%";
            // 
            // toolStripPlayerLabel
            // 
            this.toolStripPlayerLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripPlayerLabel.Name = "toolStripPlayerLabel";
            this.toolStripPlayerLabel.Size = new System.Drawing.Size(43, 22);
            this.toolStripPlayerLabel.Text = "Player:";
            // 
            // cmbCurrentPlayer
            // 
            this.cmbCurrentPlayer.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.cmbCurrentPlayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCurrentPlayer.Name = "cmbCurrentPlayer";
            this.cmbCurrentPlayer.Size = new System.Drawing.Size(160, 22);
            this.cmbCurrentPlayer.SelectedIndexChanged += new System.EventHandler(this.cmbCurrentPlayer_SelectedIndexChanged);
            // 
            // windowToolStripMenuItem
            // 
            this.windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cascadeToolStripMenuItem,
            this.tileHorizontalToolStripMenuItem,
            this.tileVerticalToolStripMenuItem,
            this.toolStripSeparatorWindowList});
            this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            this.windowToolStripMenuItem.Size = new System.Drawing.Size(63, 22);
            this.windowToolStripMenuItem.Text = "&Window";
            // 
            // cascadeToolStripMenuItem
            // 
            this.cascadeToolStripMenuItem.Name = "cascadeToolStripMenuItem";
            this.cascadeToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.cascadeToolStripMenuItem.Text = "&Cascade";
            this.cascadeToolStripMenuItem.Click += new System.EventHandler(this.cascadeToolStripMenuItem_Click);
            // 
            // tileHorizontalToolStripMenuItem
            // 
            this.tileHorizontalToolStripMenuItem.Name = "tileHorizontalToolStripMenuItem";
            this.tileHorizontalToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.tileHorizontalToolStripMenuItem.Text = "Tile &Horizontal";
            this.tileHorizontalToolStripMenuItem.Click += new System.EventHandler(this.tileHorizontalToolStripMenuItem_Click);
            // 
            // tileVerticalToolStripMenuItem
            // 
            this.tileVerticalToolStripMenuItem.Name = "tileVerticalToolStripMenuItem";
            this.tileVerticalToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.tileVerticalToolStripMenuItem.Text = "Tile &Vertical";
            this.tileVerticalToolStripMenuItem.Click += new System.EventHandler(this.tileVerticalToolStripMenuItem_Click);
            // 
            // toolStripSeparatorWindowList
            // 
            this.toolStripSeparatorWindowList.Name = "toolStripSeparatorWindowList";
            this.toolStripSeparatorWindowList.Size = new System.Drawing.Size(157, 6);
            // 
            // timerNextProcess
            // 
            this.timerNextProcess.Interval = 1000;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1064, 729);
            this.Controls.Add(this.menuStrip1);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "MainWindow";
            this.Text = "OE2 Empire Tracker";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorFileExit;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addSurveyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addBlueprintV2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contentsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem addColonyV2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem managePlayerProfiles;
        private System.Windows.Forms.ToolStripMenuItem deliveryRoutesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pricingPlansToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem buildPlannerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contactsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shipTemplatesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shipsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem marketToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem asteroidsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deliveryExecutionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colonyDailyBuildToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colonyActivityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cascadeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tileHorizontalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tileVerticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorWindowList;
        private System.Windows.Forms.ToolStripLabel toolStripPlayerLabel;
        private System.Windows.Forms.ToolStripComboBox cmbCurrentPlayer;
        private System.Windows.Forms.ToolStripLabel toolStripNextProcess;
        private System.Windows.Forms.ToolStripLabel toolStripPerformance;
        private System.Windows.Forms.Timer timerNextProcess;
    }
}

