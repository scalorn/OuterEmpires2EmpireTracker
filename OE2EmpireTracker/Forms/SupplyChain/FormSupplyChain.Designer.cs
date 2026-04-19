namespace OE2EmpireTracker.Forms.SupplyChain
{
    partial class FormSupplyChain
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
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwChains = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpNameRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtChainName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.lblStages = new System.Windows.Forms.Label();
            this.dgvStages = new System.Windows.Forms.DataGridView();
            this.colSeq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStageType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRoute = new System.Windows.Forms.DataGridViewTextBoxColumn();            this.flpStageEdit = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSeq = new System.Windows.Forms.Label();
            this.txtSequence = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblStageType = new System.Windows.Forms.Label();
            this.cmbStageType = new System.Windows.Forms.ComboBox();
            this.lblLocationType = new System.Windows.Forms.Label();
            this.cmbLocationType = new System.Windows.Forms.ComboBox();
            this.lblLocation = new System.Windows.Forms.Label();
            this.cmbLocation = new System.Windows.Forms.ComboBox();
            this.flpStageEdit2 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblResource = new System.Windows.Forms.Label();
            this.cmbResource = new System.Windows.Forms.ComboBox();
            this.lblPurity = new System.Windows.Forms.Label();
            this.cmbPurity = new System.Windows.Forms.ComboBox();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.txtThreshold = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblRate = new System.Windows.Forms.Label();
            this.txtRate = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpStageEdit3 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblRoute = new System.Windows.Forms.Label();
            this.cmbRoute = new System.Windows.Forms.ComboBox();
            this.flpStageButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdAddStage = new System.Windows.Forms.Button();
            this.cmdUpdateStage = new System.Windows.Forms.Button();
            this.cmdRemoveStage = new System.Windows.Forms.Button();
            this.cmdMoveUp = new System.Windows.Forms.Button();
            this.cmdMoveDown = new System.Windows.Forms.Button();
            this.lblFlowSummary = new System.Windows.Forms.Label();
            this.txtFlowSummary = new System.Windows.Forms.Label();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpNameRow.SuspendLayout();
            this.flpStageEdit.SuspendLayout();
            this.flpStageEdit2.SuspendLayout();
            this.flpStageEdit3.SuspendLayout();
            this.flpStageButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStages)).BeginInit();
            this.SuspendLayout();            // flpBase
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpDetail);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1050, 700);
            this.flpBase.WrapContents = false;
            // flpSearchList
            this.flpSearchList.Controls.Add(this.flpFilter);
            this.flpSearchList.Controls.Add(this.lvwChains);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 694);
            this.flpSearchList.WrapContents = false;
            // flpFilter
            this.flpFilter.AutoSize = true;
            this.flpFilter.Controls.Add(this.lblFilter);
            this.flpFilter.Controls.Add(this.txtFilter);
            this.flpFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilter.Location = new System.Drawing.Point(3, 3);
            this.flpFilter.Name = "flpFilter";
            this.flpFilter.Size = new System.Drawing.Size(214, 26);
            // lblFilter
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new System.Drawing.Point(3, 5);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(32, 13);
            this.lblFilter.Text = "Filter:";
            this.lblFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // txtFilter
            this.txtFilter.Location = new System.Drawing.Point(41, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(170, 20);
            // lvwChains
            this.lvwChains.FullRowSelect = true;
            this.lvwChains.HideSelection = false;
            this.lvwChains.Location = new System.Drawing.Point(3, 35);
            this.lvwChains.MultiSelect = false;
            this.lvwChains.Name = "lvwChains";
            this.lvwChains.Size = new System.Drawing.Size(214, 600);
            this.lvwChains.UseCompatibleStateImageBehavior = false;
            this.lvwChains.View = System.Windows.Forms.View.Details;
            // flpCommands
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(3, 641);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);            // cmdNew
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(55, 23);
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            // cmdDelete
            this.cmdDelete.Location = new System.Drawing.Point(64, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(55, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // flpDetail
            this.flpDetail.Controls.Add(this.flpNameRow);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.lblStages);
            this.flpDetail.Controls.Add(this.dgvStages);
            this.flpDetail.Controls.Add(this.flpStageEdit);
            this.flpDetail.Controls.Add(this.flpStageEdit2);
            this.flpDetail.Controls.Add(this.flpStageEdit3);
            this.flpDetail.Controls.Add(this.flpStageButtons);
            this.flpDetail.Controls.Add(this.lblFlowSummary);
            this.flpDetail.Controls.Add(this.txtFlowSummary);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(818, 694);
            this.flpDetail.WrapContents = false;
            this.flpDetail.AutoScroll = true;
            // flpNameRow
            this.flpNameRow.AutoSize = true;
            this.flpNameRow.Controls.Add(this.lblName);
            this.flpNameRow.Controls.Add(this.txtChainName);
            this.flpNameRow.Controls.Add(this.chkActive);
            this.flpNameRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpNameRow.Location = new System.Drawing.Point(3, 3);
            this.flpNameRow.Name = "flpNameRow";
            this.flpNameRow.Size = new System.Drawing.Size(812, 26);
            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(3, 5);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.Text = "Name:";
            this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // txtChainName
            this.txtChainName.Location = new System.Drawing.Point(47, 3);
            this.txtChainName.Name = "txtChainName";
            this.txtChainName.Size = new System.Drawing.Size(300, 20);
            // chkActive
            this.chkActive.AutoSize = true;
            this.chkActive.Checked = true;
            this.chkActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkActive.Location = new System.Drawing.Point(353, 3);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(56, 17);
            this.chkActive.Text = "Active";
            this.chkActive.UseVisualStyleBackColor = true;            // cmdSave
            this.cmdSave.Location = new System.Drawing.Point(3, 35);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            // lblStages
            this.lblStages.AutoSize = true;
            this.lblStages.Location = new System.Drawing.Point(3, 64);
            this.lblStages.Name = "lblStages";
            this.lblStages.Size = new System.Drawing.Size(43, 13);
            this.lblStages.Text = "Stages:";
            this.lblStages.Font = new System.Drawing.Font(this.lblStages.Font, System.Drawing.FontStyle.Bold);
            // dgvStages
            this.dgvStages.AllowUserToAddRows = false;
            this.dgvStages.AllowUserToDeleteRows = false;
            this.dgvStages.AllowUserToOrderColumns = true;
            this.dgvStages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStages.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSeq, this.colStageType, this.colLocation, this.colResource, this.colThreshold, this.colRate, this.colRoute});
            this.dgvStages.Location = new System.Drawing.Point(3, 83);
            this.dgvStages.Name = "dgvStages";
            this.dgvStages.ReadOnly = true;
            this.dgvStages.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStages.Size = new System.Drawing.Size(812, 200);
            // colSeq
            this.colSeq.HeaderText = "Seq";
            this.colSeq.Name = "colSeq";
            this.colSeq.ReadOnly = true;
            this.colSeq.Width = 40;
            // colStageType
            this.colStageType.HeaderText = "Type";
            this.colStageType.Name = "colStageType";
            this.colStageType.ReadOnly = true;
            this.colStageType.Width = 100;
            // colLocation
            this.colLocation.HeaderText = "Location";
            this.colLocation.Name = "colLocation";
            this.colLocation.ReadOnly = true;
            this.colLocation.Width = 150;
            // colResource
            this.colResource.HeaderText = "Resource";
            this.colResource.Name = "colResource";
            this.colResource.ReadOnly = true;
            this.colResource.Width = 140;
            // colThreshold
            this.colThreshold.HeaderText = "Threshold";
            this.colThreshold.Name = "colThreshold";
            this.colThreshold.ReadOnly = true;
            this.colThreshold.Width = 80;
            // colRate
            this.colRate.HeaderText = "Rate/hr";
            this.colRate.Name = "colRate";
            this.colRate.ReadOnly = true;
            this.colRate.Width = 70;
            // colRoute
            this.colRoute.HeaderText = "Route";
            this.colRoute.Name = "colRoute";
            this.colRoute.ReadOnly = true;
            this.colRoute.Width = 150;            // flpStageEdit - row 1: Seq, Type, LocationType, Location
            this.flpStageEdit.AutoSize = true;
            this.flpStageEdit.Controls.Add(this.lblSeq);
            this.flpStageEdit.Controls.Add(this.txtSequence);
            this.flpStageEdit.Controls.Add(this.lblStageType);
            this.flpStageEdit.Controls.Add(this.cmbStageType);
            this.flpStageEdit.Controls.Add(this.lblLocationType);
            this.flpStageEdit.Controls.Add(this.cmbLocationType);
            this.flpStageEdit.Controls.Add(this.lblLocation);
            this.flpStageEdit.Controls.Add(this.cmbLocation);
            this.flpStageEdit.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpStageEdit.Location = new System.Drawing.Point(3, 289);
            this.flpStageEdit.Name = "flpStageEdit";
            this.flpStageEdit.Size = new System.Drawing.Size(812, 30);
            // lblSeq
            this.lblSeq.AutoSize = true;
            this.lblSeq.Text = "Seq:";
            this.lblSeq.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSeq.Name = "lblSeq";
            // txtSequence
            this.txtSequence.Size = new System.Drawing.Size(40, 20);
            this.txtSequence.Name = "txtSequence";
            // lblStageType
            this.lblStageType.AutoSize = true;
            this.lblStageType.Text = "Type:";
            this.lblStageType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblStageType.Name = "lblStageType";
            // cmbStageType
            this.cmbStageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStageType.Size = new System.Drawing.Size(110, 21);
            this.cmbStageType.Name = "cmbStageType";
            // lblLocationType
            this.lblLocationType.AutoSize = true;
            this.lblLocationType.Text = "Loc Type:";
            this.lblLocationType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLocationType.Name = "lblLocationType";
            // cmbLocationType
            this.cmbLocationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLocationType.Size = new System.Drawing.Size(90, 21);
            this.cmbLocationType.Name = "cmbLocationType";
            // lblLocation
            this.lblLocation.AutoSize = true;
            this.lblLocation.Text = "Location:";
            this.lblLocation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLocation.Name = "lblLocation";
            // cmbLocation
            this.cmbLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLocation.Size = new System.Drawing.Size(180, 21);
            this.cmbLocation.Name = "cmbLocation";            // flpStageEdit2 - row 2: Resource, Purity, Threshold, Rate
            this.flpStageEdit2.AutoSize = true;
            this.flpStageEdit2.Controls.Add(this.lblResource);
            this.flpStageEdit2.Controls.Add(this.cmbResource);
            this.flpStageEdit2.Controls.Add(this.lblPurity);
            this.flpStageEdit2.Controls.Add(this.cmbPurity);
            this.flpStageEdit2.Controls.Add(this.lblThreshold);
            this.flpStageEdit2.Controls.Add(this.txtThreshold);
            this.flpStageEdit2.Controls.Add(this.lblRate);
            this.flpStageEdit2.Controls.Add(this.txtRate);
            this.flpStageEdit2.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpStageEdit2.Location = new System.Drawing.Point(3, 325);
            this.flpStageEdit2.Name = "flpStageEdit2";
            this.flpStageEdit2.Size = new System.Drawing.Size(812, 30);
            // lblResource
            this.lblResource.AutoSize = true;
            this.lblResource.Text = "Resource:";
            this.lblResource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblResource.Name = "lblResource";
            // cmbResource
            this.cmbResource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResource.Size = new System.Drawing.Size(130, 21);
            this.cmbResource.Name = "cmbResource";
            // lblPurity
            this.lblPurity.AutoSize = true;
            this.lblPurity.Text = "Purity:";
            this.lblPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPurity.Name = "lblPurity";
            // cmbPurity
            this.cmbPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPurity.Size = new System.Drawing.Size(100, 21);
            this.cmbPurity.Name = "cmbPurity";
            // lblThreshold
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Text = "Threshold:";
            this.lblThreshold.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblThreshold.Name = "lblThreshold";
            // txtThreshold
            this.txtThreshold.Size = new System.Drawing.Size(70, 20);
            this.txtThreshold.Name = "txtThreshold";
            // lblRate
            this.lblRate.AutoSize = true;
            this.lblRate.Text = "Rate/hr:";
            this.lblRate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRate.Name = "lblRate";
            // txtRate
            this.txtRate.Size = new System.Drawing.Size(70, 20);
            this.txtRate.Name = "txtRate";            // flpStageEdit3 - row 3: Route
            this.flpStageEdit3.AutoSize = true;
            this.flpStageEdit3.Controls.Add(this.lblRoute);
            this.flpStageEdit3.Controls.Add(this.cmbRoute);
            this.flpStageEdit3.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpStageEdit3.Location = new System.Drawing.Point(3, 361);
            this.flpStageEdit3.Name = "flpStageEdit3";
            this.flpStageEdit3.Size = new System.Drawing.Size(812, 30);
            // lblRoute
            this.lblRoute.AutoSize = true;
            this.lblRoute.Text = "Route:";
            this.lblRoute.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRoute.Name = "lblRoute";
            // cmbRoute
            this.cmbRoute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRoute.Size = new System.Drawing.Size(300, 21);
            this.cmbRoute.Name = "cmbRoute";
            // flpStageButtons
            this.flpStageButtons.AutoSize = true;
            this.flpStageButtons.Controls.Add(this.cmdAddStage);
            this.flpStageButtons.Controls.Add(this.cmdUpdateStage);
            this.flpStageButtons.Controls.Add(this.cmdRemoveStage);
            this.flpStageButtons.Controls.Add(this.cmdMoveUp);
            this.flpStageButtons.Controls.Add(this.cmdMoveDown);
            this.flpStageButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpStageButtons.Location = new System.Drawing.Point(3, 397);
            this.flpStageButtons.Name = "flpStageButtons";
            this.flpStageButtons.Size = new System.Drawing.Size(812, 29);
            // cmdAddStage
            this.cmdAddStage.Size = new System.Drawing.Size(75, 23);
            this.cmdAddStage.Text = "Add Stage";
            this.cmdAddStage.UseVisualStyleBackColor = true;
            this.cmdAddStage.Name = "cmdAddStage";
            // cmdUpdateStage
            this.cmdUpdateStage.Size = new System.Drawing.Size(90, 23);
            this.cmdUpdateStage.Text = "Update Stage";
            this.cmdUpdateStage.UseVisualStyleBackColor = true;
            this.cmdUpdateStage.Name = "cmdUpdateStage";
            // cmdRemoveStage
            this.cmdRemoveStage.Size = new System.Drawing.Size(95, 23);
            this.cmdRemoveStage.Text = "Remove Stage";
            this.cmdRemoveStage.UseVisualStyleBackColor = true;
            this.cmdRemoveStage.Name = "cmdRemoveStage";
            // cmdMoveUp
            this.cmdMoveUp.Size = new System.Drawing.Size(65, 23);
            this.cmdMoveUp.Text = "\u25B2 Up";
            this.cmdMoveUp.UseVisualStyleBackColor = true;
            this.cmdMoveUp.Name = "cmdMoveUp";
            // cmdMoveDown
            this.cmdMoveDown.Size = new System.Drawing.Size(75, 23);
            this.cmdMoveDown.Text = "\u25BC Down";
            this.cmdMoveDown.UseVisualStyleBackColor = true;
            this.cmdMoveDown.Name = "cmdMoveDown";            // lblFlowSummary
            this.lblFlowSummary.AutoSize = true;
            this.lblFlowSummary.Location = new System.Drawing.Point(3, 432);
            this.lblFlowSummary.Name = "lblFlowSummary";
            this.lblFlowSummary.Size = new System.Drawing.Size(80, 13);
            this.lblFlowSummary.Text = "Flow Summary:";
            this.lblFlowSummary.Font = new System.Drawing.Font(this.lblFlowSummary.Font, System.Drawing.FontStyle.Bold);
            // txtFlowSummary
            this.txtFlowSummary.AutoSize = true;
            this.txtFlowSummary.Location = new System.Drawing.Point(3, 451);
            this.txtFlowSummary.Name = "txtFlowSummary";
            this.txtFlowSummary.Size = new System.Drawing.Size(812, 40);
            this.txtFlowSummary.Text = "";
            // FormSupplyChain
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1050, 700);
            this.Controls.Add(this.flpBase);
            this.Name = "FormSupplyChain";
            this.Text = "Supply Chains";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpFilter.ResumeLayout(false);
            this.flpCommands.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpNameRow.ResumeLayout(false);
            this.flpStageEdit.ResumeLayout(false);
            this.flpStageEdit2.ResumeLayout(false);
            this.flpStageEdit3.ResumeLayout(false);
            this.flpStageButtons.ResumeLayout(false);
            this.flpFilter.PerformLayout();
            this.flpNameRow.PerformLayout();
            this.flpCommands.PerformLayout();
            this.flpStageEdit.PerformLayout();
            this.flpStageEdit2.PerformLayout();
            this.flpStageEdit3.PerformLayout();
            this.flpStageButtons.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStages)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFilter;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.ListView lvwChains;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpNameRow;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtChainName;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Label lblStages;
        private System.Windows.Forms.DataGridView dgvStages;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeq;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStageType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRoute;
        private System.Windows.Forms.FlowLayoutPanel flpStageEdit;
        private System.Windows.Forms.Label lblSeq;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSequence;
        private System.Windows.Forms.Label lblStageType;
        private System.Windows.Forms.ComboBox cmbStageType;
        private System.Windows.Forms.Label lblLocationType;
        private System.Windows.Forms.ComboBox cmbLocationType;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.ComboBox cmbLocation;
        private System.Windows.Forms.FlowLayoutPanel flpStageEdit2;
        private System.Windows.Forms.Label lblResource;
        private System.Windows.Forms.ComboBox cmbResource;
        private System.Windows.Forms.Label lblPurity;
        private System.Windows.Forms.ComboBox cmbPurity;
        private System.Windows.Forms.Label lblThreshold;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtThreshold;
        private System.Windows.Forms.Label lblRate;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtRate;
        private System.Windows.Forms.FlowLayoutPanel flpStageEdit3;
        private System.Windows.Forms.Label lblRoute;
        private System.Windows.Forms.ComboBox cmbRoute;
        private System.Windows.Forms.FlowLayoutPanel flpStageButtons;
        private System.Windows.Forms.Button cmdAddStage;
        private System.Windows.Forms.Button cmdUpdateStage;
        private System.Windows.Forms.Button cmdRemoveStage;
        private System.Windows.Forms.Button cmdMoveUp;
        private System.Windows.Forms.Button cmdMoveDown;
        private System.Windows.Forms.Label lblFlowSummary;
        private System.Windows.Forms.Label txtFlowSummary;
    }
}