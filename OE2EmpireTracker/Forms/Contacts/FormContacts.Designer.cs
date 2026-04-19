namespace OE2EmpireTracker.Forms.Contacts
{
    partial class FormContacts
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
            this.tabContacts = new System.Windows.Forms.TabControl();
            this.tabFactions = new System.Windows.Forms.TabPage();
            this.tabExternalChars = new System.Windows.Forms.TabPage();

            // Factions tab controls
            this.flpFactionBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFactionSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFactionFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFactionFilter = new System.Windows.Forms.Label();
            this.txtFactionFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwFactions = new System.Windows.Forms.ListView();
            this.flpFactionCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNewFaction = new System.Windows.Forms.Button();
            this.cmdDeleteFaction = new System.Windows.Forms.Button();
            this.flpFactionDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFactionName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFactionName = new System.Windows.Forms.Label();
            this.txtFactionName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpFactionDescription = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFactionDescription = new System.Windows.Forms.Label();
            this.txtFactionDescription = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdSaveFaction = new System.Windows.Forms.Button();

            // External Characters tab controls
            this.flpCharBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCharSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCharFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCharFilter = new System.Windows.Forms.Label();
            this.txtCharFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwCharacters = new System.Windows.Forms.ListView();
            this.flpCharCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNewChar = new System.Windows.Forms.Button();
            this.cmdDeleteChar = new System.Windows.Forms.Button();
            this.flpCharDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCharName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCharName = new System.Windows.Forms.Label();
            this.txtCharName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpCharFaction = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCharFaction = new System.Windows.Forms.Label();
            this.cmbCharFaction = new System.Windows.Forms.ComboBox();
            this.cmdSaveChar = new System.Windows.Forms.Button();

            this.tabContacts.SuspendLayout();
            this.tabFactions.SuspendLayout();
            this.tabExternalChars.SuspendLayout();
            this.flpFactionBase.SuspendLayout();
            this.flpFactionSearchList.SuspendLayout();
            this.flpFactionFilter.SuspendLayout();
            this.flpFactionCommands.SuspendLayout();
            this.flpFactionDetail.SuspendLayout();
            this.flpFactionName.SuspendLayout();
            this.flpFactionDescription.SuspendLayout();
            this.flpCharBase.SuspendLayout();
            this.flpCharSearchList.SuspendLayout();
            this.flpCharFilter.SuspendLayout();
            this.flpCharCommands.SuspendLayout();
            this.flpCharDetail.SuspendLayout();
            this.flpCharName.SuspendLayout();
            this.flpCharFaction.SuspendLayout();
            this.SuspendLayout();            //
            // tabContacts
            //
            this.tabContacts.Controls.Add(this.tabFactions);
            this.tabContacts.Controls.Add(this.tabExternalChars);
            this.tabContacts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabContacts.Location = new System.Drawing.Point(0, 0);
            this.tabContacts.Name = "tabContacts";
            this.tabContacts.SelectedIndex = 0;
            this.tabContacts.Size = new System.Drawing.Size(850, 550);
            //
            // tabFactions
            //
            this.tabFactions.Controls.Add(this.flpFactionBase);
            this.tabFactions.Location = new System.Drawing.Point(4, 22);
            this.tabFactions.Name = "tabFactions";
            this.tabFactions.Padding = new System.Windows.Forms.Padding(3);
            this.tabFactions.Size = new System.Drawing.Size(842, 524);
            this.tabFactions.Text = "Factions";
            //
            // tabExternalChars
            //
            this.tabExternalChars.Controls.Add(this.flpCharBase);
            this.tabExternalChars.Location = new System.Drawing.Point(4, 22);
            this.tabExternalChars.Name = "tabExternalChars";
            this.tabExternalChars.Padding = new System.Windows.Forms.Padding(3);
            this.tabExternalChars.Size = new System.Drawing.Size(842, 524);
            this.tabExternalChars.Text = "External Characters";
            //
            // flpFactionBase
            //
            this.flpFactionBase.Controls.Add(this.flpFactionSearchList);
            this.flpFactionBase.Controls.Add(this.flpFactionDetail);
            this.flpFactionBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpFactionBase.Location = new System.Drawing.Point(3, 3);
            this.flpFactionBase.Name = "flpFactionBase";
            this.flpFactionBase.Size = new System.Drawing.Size(836, 518);
            this.flpFactionBase.WrapContents = false;
            //
            // flpFactionSearchList
            //
            this.flpFactionSearchList.Controls.Add(this.flpFactionFilter);
            this.flpFactionSearchList.Controls.Add(this.lvwFactions);
            this.flpFactionSearchList.Controls.Add(this.flpFactionCommands);
            this.flpFactionSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpFactionSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpFactionSearchList.Name = "flpFactionSearchList";
            this.flpFactionSearchList.Size = new System.Drawing.Size(280, 512);
            this.flpFactionSearchList.WrapContents = false;
            //
            // flpFactionFilter
            //
            this.flpFactionFilter.AutoSize = true;
            this.flpFactionFilter.Controls.Add(this.lblFactionFilter);
            this.flpFactionFilter.Controls.Add(this.txtFactionFilter);
            this.flpFactionFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFactionFilter.Location = new System.Drawing.Point(3, 3);
            this.flpFactionFilter.Name = "flpFactionFilter";
            this.flpFactionFilter.Size = new System.Drawing.Size(274, 26);
            //
            // lblFactionFilter
            //
            this.lblFactionFilter.AutoSize = true;
            this.lblFactionFilter.Location = new System.Drawing.Point(3, 5);
            this.lblFactionFilter.Name = "lblFactionFilter";
            this.lblFactionFilter.Size = new System.Drawing.Size(32, 13);
            this.lblFactionFilter.Text = "Filter:";
            this.lblFactionFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtFactionFilter
            //
            this.txtFactionFilter.Location = new System.Drawing.Point(41, 3);
            this.txtFactionFilter.Name = "txtFactionFilter";
            this.txtFactionFilter.Size = new System.Drawing.Size(230, 20);            //
            // lvwFactions
            //
            this.lvwFactions.FullRowSelect = true;
            this.lvwFactions.HideSelection = false;
            this.lvwFactions.Location = new System.Drawing.Point(3, 35);
            this.lvwFactions.MultiSelect = false;
            this.lvwFactions.Name = "lvwFactions";
            this.lvwFactions.Size = new System.Drawing.Size(274, 440);
            this.lvwFactions.UseCompatibleStateImageBehavior = false;
            this.lvwFactions.View = System.Windows.Forms.View.Details;
            //
            // flpFactionCommands
            //
            this.flpFactionCommands.AutoSize = true;
            this.flpFactionCommands.Controls.Add(this.cmdNewFaction);
            this.flpFactionCommands.Controls.Add(this.cmdDeleteFaction);
            this.flpFactionCommands.Location = new System.Drawing.Point(3, 481);
            this.flpFactionCommands.Name = "flpFactionCommands";
            this.flpFactionCommands.Size = new System.Drawing.Size(274, 29);
            //
            // cmdNewFaction
            //
            this.cmdNewFaction.Location = new System.Drawing.Point(3, 3);
            this.cmdNewFaction.Name = "cmdNewFaction";
            this.cmdNewFaction.Size = new System.Drawing.Size(75, 23);
            this.cmdNewFaction.Text = "New";
            this.cmdNewFaction.UseVisualStyleBackColor = true;
            //
            // cmdDeleteFaction
            //
            this.cmdDeleteFaction.Location = new System.Drawing.Point(84, 3);
            this.cmdDeleteFaction.Name = "cmdDeleteFaction";
            this.cmdDeleteFaction.Size = new System.Drawing.Size(75, 23);
            this.cmdDeleteFaction.Text = "Delete";
            this.cmdDeleteFaction.UseVisualStyleBackColor = true;
            //
            // flpFactionDetail
            //
            this.flpFactionDetail.Controls.Add(this.flpFactionName);
            this.flpFactionDetail.Controls.Add(this.flpFactionDescription);
            this.flpFactionDetail.Controls.Add(this.cmdSaveFaction);
            this.flpFactionDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpFactionDetail.Location = new System.Drawing.Point(289, 3);
            this.flpFactionDetail.Name = "flpFactionDetail";
            this.flpFactionDetail.Size = new System.Drawing.Size(544, 512);
            this.flpFactionDetail.WrapContents = false;
            //
            // flpFactionName
            //
            this.flpFactionName.AutoSize = true;
            this.flpFactionName.Controls.Add(this.lblFactionName);
            this.flpFactionName.Controls.Add(this.txtFactionName);
            this.flpFactionName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFactionName.Location = new System.Drawing.Point(3, 3);
            this.flpFactionName.Name = "flpFactionName";
            this.flpFactionName.Size = new System.Drawing.Size(538, 26);
            //
            // lblFactionName
            //
            this.lblFactionName.AutoSize = true;
            this.lblFactionName.Location = new System.Drawing.Point(3, 5);
            this.lblFactionName.Name = "lblFactionName";
            this.lblFactionName.Size = new System.Drawing.Size(38, 13);
            this.lblFactionName.Text = "Name:";
            this.lblFactionName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtFactionName
            //
            this.txtFactionName.Location = new System.Drawing.Point(47, 3);
            this.txtFactionName.Name = "txtFactionName";
            this.txtFactionName.Size = new System.Drawing.Size(300, 20);
            //
            // flpFactionDescription
            //
            this.flpFactionDescription.AutoSize = true;
            this.flpFactionDescription.Controls.Add(this.lblFactionDescription);
            this.flpFactionDescription.Controls.Add(this.txtFactionDescription);
            this.flpFactionDescription.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFactionDescription.Location = new System.Drawing.Point(3, 35);
            this.flpFactionDescription.Name = "flpFactionDescription";
            this.flpFactionDescription.Size = new System.Drawing.Size(538, 26);
            //
            // lblFactionDescription
            //
            this.lblFactionDescription.AutoSize = true;
            this.lblFactionDescription.Location = new System.Drawing.Point(3, 5);
            this.lblFactionDescription.Name = "lblFactionDescription";
            this.lblFactionDescription.Size = new System.Drawing.Size(63, 13);
            this.lblFactionDescription.Text = "Description:";
            this.lblFactionDescription.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtFactionDescription
            //
            this.txtFactionDescription.Location = new System.Drawing.Point(72, 3);
            this.txtFactionDescription.Name = "txtFactionDescription";
            this.txtFactionDescription.Size = new System.Drawing.Size(400, 20);
            //
            // cmdSaveFaction
            //
            this.cmdSaveFaction.Location = new System.Drawing.Point(3, 67);
            this.cmdSaveFaction.Name = "cmdSaveFaction";
            this.cmdSaveFaction.Size = new System.Drawing.Size(75, 23);
            this.cmdSaveFaction.Text = "Save";
            this.cmdSaveFaction.UseVisualStyleBackColor = true;            //
            // flpCharBase
            //
            this.flpCharBase.Controls.Add(this.flpCharSearchList);
            this.flpCharBase.Controls.Add(this.flpCharDetail);
            this.flpCharBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCharBase.Location = new System.Drawing.Point(3, 3);
            this.flpCharBase.Name = "flpCharBase";
            this.flpCharBase.Size = new System.Drawing.Size(836, 518);
            this.flpCharBase.WrapContents = false;
            //
            // flpCharSearchList
            //
            this.flpCharSearchList.Controls.Add(this.flpCharFilter);
            this.flpCharSearchList.Controls.Add(this.lvwCharacters);
            this.flpCharSearchList.Controls.Add(this.flpCharCommands);
            this.flpCharSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCharSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpCharSearchList.Name = "flpCharSearchList";
            this.flpCharSearchList.Size = new System.Drawing.Size(280, 512);
            this.flpCharSearchList.WrapContents = false;
            //
            // flpCharFilter
            //
            this.flpCharFilter.AutoSize = true;
            this.flpCharFilter.Controls.Add(this.lblCharFilter);
            this.flpCharFilter.Controls.Add(this.txtCharFilter);
            this.flpCharFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpCharFilter.Location = new System.Drawing.Point(3, 3);
            this.flpCharFilter.Name = "flpCharFilter";
            this.flpCharFilter.Size = new System.Drawing.Size(274, 26);
            //
            // lblCharFilter
            //
            this.lblCharFilter.AutoSize = true;
            this.lblCharFilter.Location = new System.Drawing.Point(3, 5);
            this.lblCharFilter.Name = "lblCharFilter";
            this.lblCharFilter.Size = new System.Drawing.Size(32, 13);
            this.lblCharFilter.Text = "Filter:";
            this.lblCharFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtCharFilter
            //
            this.txtCharFilter.Location = new System.Drawing.Point(41, 3);
            this.txtCharFilter.Name = "txtCharFilter";
            this.txtCharFilter.Size = new System.Drawing.Size(230, 20);
            //
            // lvwCharacters
            //
            this.lvwCharacters.FullRowSelect = true;
            this.lvwCharacters.HideSelection = false;
            this.lvwCharacters.Location = new System.Drawing.Point(3, 35);
            this.lvwCharacters.MultiSelect = false;
            this.lvwCharacters.Name = "lvwCharacters";
            this.lvwCharacters.Size = new System.Drawing.Size(274, 440);
            this.lvwCharacters.UseCompatibleStateImageBehavior = false;
            this.lvwCharacters.View = System.Windows.Forms.View.Details;
            //
            // flpCharCommands
            //
            this.flpCharCommands.AutoSize = true;
            this.flpCharCommands.Controls.Add(this.cmdNewChar);
            this.flpCharCommands.Controls.Add(this.cmdDeleteChar);
            this.flpCharCommands.Location = new System.Drawing.Point(3, 481);
            this.flpCharCommands.Name = "flpCharCommands";
            this.flpCharCommands.Size = new System.Drawing.Size(274, 29);
            //
            // cmdNewChar
            //
            this.cmdNewChar.Location = new System.Drawing.Point(3, 3);
            this.cmdNewChar.Name = "cmdNewChar";
            this.cmdNewChar.Size = new System.Drawing.Size(75, 23);
            this.cmdNewChar.Text = "New";
            this.cmdNewChar.UseVisualStyleBackColor = true;
            //
            // cmdDeleteChar
            //
            this.cmdDeleteChar.Location = new System.Drawing.Point(84, 3);
            this.cmdDeleteChar.Name = "cmdDeleteChar";
            this.cmdDeleteChar.Size = new System.Drawing.Size(75, 23);
            this.cmdDeleteChar.Text = "Delete";
            this.cmdDeleteChar.UseVisualStyleBackColor = true;            //
            // flpCharDetail
            //
            this.flpCharDetail.Controls.Add(this.flpCharName);
            this.flpCharDetail.Controls.Add(this.flpCharFaction);
            this.flpCharDetail.Controls.Add(this.cmdSaveChar);
            this.flpCharDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCharDetail.Location = new System.Drawing.Point(289, 3);
            this.flpCharDetail.Name = "flpCharDetail";
            this.flpCharDetail.Size = new System.Drawing.Size(544, 512);
            this.flpCharDetail.WrapContents = false;
            //
            // flpCharName
            //
            this.flpCharName.AutoSize = true;
            this.flpCharName.Controls.Add(this.lblCharName);
            this.flpCharName.Controls.Add(this.txtCharName);
            this.flpCharName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpCharName.Location = new System.Drawing.Point(3, 3);
            this.flpCharName.Name = "flpCharName";
            this.flpCharName.Size = new System.Drawing.Size(538, 26);
            //
            // lblCharName
            //
            this.lblCharName.AutoSize = true;
            this.lblCharName.Location = new System.Drawing.Point(3, 5);
            this.lblCharName.Name = "lblCharName";
            this.lblCharName.Size = new System.Drawing.Size(38, 13);
            this.lblCharName.Text = "Name:";
            this.lblCharName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtCharName
            //
            this.txtCharName.Location = new System.Drawing.Point(47, 3);
            this.txtCharName.Name = "txtCharName";
            this.txtCharName.Size = new System.Drawing.Size(300, 20);
            //
            // flpCharFaction
            //
            this.flpCharFaction.AutoSize = true;
            this.flpCharFaction.Controls.Add(this.lblCharFaction);
            this.flpCharFaction.Controls.Add(this.cmbCharFaction);
            this.flpCharFaction.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpCharFaction.Location = new System.Drawing.Point(3, 35);
            this.flpCharFaction.Name = "flpCharFaction";
            this.flpCharFaction.Size = new System.Drawing.Size(538, 26);
            //
            // lblCharFaction
            //
            this.lblCharFaction.AutoSize = true;
            this.lblCharFaction.Location = new System.Drawing.Point(3, 5);
            this.lblCharFaction.Name = "lblCharFaction";
            this.lblCharFaction.Size = new System.Drawing.Size(46, 13);
            this.lblCharFaction.Text = "Faction:";
            this.lblCharFaction.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // cmbCharFaction
            //
            this.cmbCharFaction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCharFaction.Location = new System.Drawing.Point(55, 3);
            this.cmbCharFaction.Name = "cmbCharFaction";
            this.cmbCharFaction.Size = new System.Drawing.Size(300, 21);
            //
            // cmdSaveChar
            //
            this.cmdSaveChar.Location = new System.Drawing.Point(3, 67);
            this.cmdSaveChar.Name = "cmdSaveChar";
            this.cmdSaveChar.Size = new System.Drawing.Size(75, 23);
            this.cmdSaveChar.Text = "Save";
            this.cmdSaveChar.UseVisualStyleBackColor = true;
            //
            // FormContacts
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(850, 550);
            this.Controls.Add(this.tabContacts);
            this.Name = "FormContacts";
            this.Text = "Contacts";
            this.tabContacts.ResumeLayout(false);
            this.tabFactions.ResumeLayout(false);
            this.tabExternalChars.ResumeLayout(false);
            this.flpFactionBase.ResumeLayout(false);
            this.flpFactionSearchList.ResumeLayout(false);
            this.flpFactionSearchList.PerformLayout();
            this.flpFactionFilter.ResumeLayout(false);
            this.flpFactionFilter.PerformLayout();
            this.flpFactionCommands.ResumeLayout(false);
            this.flpFactionDetail.ResumeLayout(false);
            this.flpFactionDetail.PerformLayout();
            this.flpFactionName.ResumeLayout(false);
            this.flpFactionName.PerformLayout();
            this.flpFactionDescription.ResumeLayout(false);
            this.flpFactionDescription.PerformLayout();
            this.flpCharBase.ResumeLayout(false);
            this.flpCharSearchList.ResumeLayout(false);
            this.flpCharSearchList.PerformLayout();
            this.flpCharFilter.ResumeLayout(false);
            this.flpCharFilter.PerformLayout();
            this.flpCharCommands.ResumeLayout(false);
            this.flpCharDetail.ResumeLayout(false);
            this.flpCharDetail.PerformLayout();
            this.flpCharName.ResumeLayout(false);
            this.flpCharName.PerformLayout();
            this.flpCharFaction.ResumeLayout(false);
            this.flpCharFaction.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabContacts;
        private System.Windows.Forms.TabPage tabFactions;
        private System.Windows.Forms.TabPage tabExternalChars;

        // Factions tab
        private System.Windows.Forms.FlowLayoutPanel flpFactionBase;
        private System.Windows.Forms.FlowLayoutPanel flpFactionSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFactionFilter;
        private System.Windows.Forms.Label lblFactionFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFactionFilter;
        private System.Windows.Forms.ListView lvwFactions;
        private System.Windows.Forms.FlowLayoutPanel flpFactionCommands;
        private System.Windows.Forms.Button cmdNewFaction;
        private System.Windows.Forms.Button cmdDeleteFaction;
        private System.Windows.Forms.FlowLayoutPanel flpFactionDetail;
        private System.Windows.Forms.FlowLayoutPanel flpFactionName;
        private System.Windows.Forms.Label lblFactionName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFactionName;
        private System.Windows.Forms.FlowLayoutPanel flpFactionDescription;
        private System.Windows.Forms.Label lblFactionDescription;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFactionDescription;
        private System.Windows.Forms.Button cmdSaveFaction;

        // External Characters tab
        private System.Windows.Forms.FlowLayoutPanel flpCharBase;
        private System.Windows.Forms.FlowLayoutPanel flpCharSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpCharFilter;
        private System.Windows.Forms.Label lblCharFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCharFilter;
        private System.Windows.Forms.ListView lvwCharacters;
        private System.Windows.Forms.FlowLayoutPanel flpCharCommands;
        private System.Windows.Forms.Button cmdNewChar;
        private System.Windows.Forms.Button cmdDeleteChar;
        private System.Windows.Forms.FlowLayoutPanel flpCharDetail;
        private System.Windows.Forms.FlowLayoutPanel flpCharName;
        private System.Windows.Forms.Label lblCharName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCharName;
        private System.Windows.Forms.FlowLayoutPanel flpCharFaction;
        private System.Windows.Forms.Label lblCharFaction;
        private System.Windows.Forms.ComboBox cmbCharFaction;
        private System.Windows.Forms.Button cmdSaveChar;
    }
}