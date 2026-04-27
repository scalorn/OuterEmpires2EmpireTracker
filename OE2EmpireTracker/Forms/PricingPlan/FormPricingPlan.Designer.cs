namespace OE2EmpireTracker.Forms.PricingPlan
{
    partial class FormPricingPlan
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
            this.flpPlanFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanFilter = new System.Windows.Forms.Label();
            this.txtPlanFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwPlans = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlanName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanName = new System.Windows.Forms.Label();
            this.txtPlanName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpDescription = new System.Windows.Forms.FlowLayoutPanel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpCosts = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFixedCost = new System.Windows.Forms.Label();
            this.txtFixedCost = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblHourlyCost = new System.Windows.Forms.Label();
            this.txtHourlyCost = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.dgvResourcePrices = new DataEntryGridView();
            this.colResourceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpPlanFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpPlanName.SuspendLayout();
            this.flpDescription.SuspendLayout();
            this.flpCosts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResourcePrices)).BeginInit();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpDetail);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(850, 550);
            this.flpBase.WrapContents = false;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpPlanFilter);
            this.flpSearchList.Controls.Add(this.lvwPlans);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 544);
            this.flpSearchList.WrapContents = false;
            // 
            // flpPlanFilter
            // 
            this.flpPlanFilter.AutoSize = true;
            this.flpPlanFilter.Controls.Add(this.lblPlanFilter);
            this.flpPlanFilter.Controls.Add(this.txtPlanFilter);
            this.flpPlanFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpPlanFilter.Location = new System.Drawing.Point(3, 3);
            this.flpPlanFilter.Name = "flpPlanFilter";
            this.flpPlanFilter.Size = new System.Drawing.Size(214, 26);
            // 
            // lblPlanFilter
            // 
            this.lblPlanFilter.AutoSize = true;
            this.lblPlanFilter.Location = new System.Drawing.Point(3, 5);
            this.lblPlanFilter.Name = "lblPlanFilter";
            this.lblPlanFilter.Size = new System.Drawing.Size(32, 13);
            this.lblPlanFilter.Text = "Filter:";
            this.lblPlanFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtPlanFilter
            // 
            this.txtPlanFilter.Location = new System.Drawing.Point(41, 3);
            this.txtPlanFilter.Name = "txtPlanFilter";
            this.txtPlanFilter.Size = new System.Drawing.Size(170, 20);
            // 
            // lvwPlans
            // 
            this.lvwPlans.FullRowSelect = true;
            this.lvwPlans.HideSelection = false;
            this.lvwPlans.Location = new System.Drawing.Point(3, 35);
            this.lvwPlans.MultiSelect = false;
            this.lvwPlans.Name = "lvwPlans";
            this.lvwPlans.Size = new System.Drawing.Size(214, 470);
            this.lvwPlans.UseCompatibleStateImageBehavior = false;
            this.lvwPlans.View = System.Windows.Forms.View.Details;
            // 
            // flpCommands
            // 
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(3, 511);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);

            // 
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(84, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // 
            // flpDetail
            // 
            this.flpDetail.Controls.Add(this.flpPlanName);
            this.flpDetail.Controls.Add(this.flpDescription);
            this.flpDetail.Controls.Add(this.flpCosts);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.dgvResourcePrices);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(618, 544);
            this.flpDetail.WrapContents = false;
            // 
            // flpPlanName
            // 
            this.flpPlanName.AutoSize = true;
            this.flpPlanName.Controls.Add(this.lblPlanName);
            this.flpPlanName.Controls.Add(this.txtPlanName);
            this.flpPlanName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpPlanName.Location = new System.Drawing.Point(3, 3);
            this.flpPlanName.Name = "flpPlanName";
            this.flpPlanName.Size = new System.Drawing.Size(612, 26);
            // 
            // lblPlanName
            // 
            this.lblPlanName.AutoSize = true;
            this.lblPlanName.Location = new System.Drawing.Point(3, 5);
            this.lblPlanName.Name = "lblPlanName";
            this.lblPlanName.Size = new System.Drawing.Size(38, 13);
            this.lblPlanName.Text = "Name:";
            this.lblPlanName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtPlanName
            // 
            this.txtPlanName.Location = new System.Drawing.Point(47, 3);
            this.txtPlanName.Name = "txtPlanName";
            this.txtPlanName.Size = new System.Drawing.Size(300, 20);
            // 
            // flpDescription
            // 
            this.flpDescription.AutoSize = true;
            this.flpDescription.Controls.Add(this.lblDescription);
            this.flpDescription.Controls.Add(this.txtDescription);
            this.flpDescription.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpDescription.Location = new System.Drawing.Point(3, 35);
            this.flpDescription.Name = "flpDescription";
            this.flpDescription.Size = new System.Drawing.Size(612, 26);
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(3, 5);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(63, 13);
            this.lblDescription.Text = "Description:";
            this.lblDescription.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(72, 3);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(400, 20);

            // 
            // flpCosts
            // 
            this.flpCosts.AutoSize = true;
            this.flpCosts.Controls.Add(this.lblFixedCost);
            this.flpCosts.Controls.Add(this.txtFixedCost);
            this.flpCosts.Controls.Add(this.lblHourlyCost);
            this.flpCosts.Controls.Add(this.txtHourlyCost);
            this.flpCosts.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpCosts.Location = new System.Drawing.Point(3, 67);
            this.flpCosts.Name = "flpCosts";
            this.flpCosts.Size = new System.Drawing.Size(612, 26);
            // 
            // lblFixedCost
            // 
            this.lblFixedCost.AutoSize = true;
            this.lblFixedCost.Location = new System.Drawing.Point(3, 5);
            this.lblFixedCost.Name = "lblFixedCost";
            this.lblFixedCost.Size = new System.Drawing.Size(93, 13);
            this.lblFixedCost.Text = "Fixed Cost/Item:";
            this.lblFixedCost.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtFixedCost
            // 
            this.txtFixedCost.Location = new System.Drawing.Point(102, 3);
            this.txtFixedCost.Name = "txtFixedCost";
            this.txtFixedCost.Size = new System.Drawing.Size(100, 20);
            // 
            // lblHourlyCost
            // 
            this.lblHourlyCost.AutoSize = true;
            this.lblHourlyCost.Location = new System.Drawing.Point(208, 5);
            this.lblHourlyCost.Name = "lblHourlyCost";
            this.lblHourlyCost.Size = new System.Drawing.Size(72, 13);
            this.lblHourlyCost.Text = "Hourly Rate:";
            this.lblHourlyCost.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtHourlyCost
            // 
            this.txtHourlyCost.Location = new System.Drawing.Point(286, 3);
            this.txtHourlyCost.Name = "txtHourlyCost";
            this.txtHourlyCost.Size = new System.Drawing.Size(100, 20);
            // 
            // cmdSave
            // 
            this.cmdSave.Location = new System.Drawing.Point(3, 99);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            // 
            // dgvResourcePrices
            // 
            this.dgvResourcePrices.AllowUserToAddRows = false;
            this.dgvResourcePrices.AllowUserToDeleteRows = false;
            this.dgvResourcePrices.AllowUserToOrderColumns = true;
            this.dgvResourcePrices.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResourcePrices.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colResourceName,
            this.colPurity,
            this.colPrice});
            this.dgvResourcePrices.Location = new System.Drawing.Point(3, 128);
            this.dgvResourcePrices.Name = "dgvResourcePrices";
            this.dgvResourcePrices.Size = new System.Drawing.Size(612, 410);
            this.dgvResourcePrices.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            // 
            // colResourceName
            // 
            this.colResourceName.HeaderText = "Resource";
            this.colResourceName.Name = "colResourceName";
            this.colResourceName.ReadOnly = true;
            this.colResourceName.Width = 220;
            // 
            // colPurity
            // 
            this.colPurity.HeaderText = "Purity";
            this.colPurity.Name = "colPurity";
            this.colPurity.ReadOnly = true;
            this.colPurity.Width = 80;
            // 
            // colPrice
            // 
            this.colPrice.HeaderText = "Price";
            this.colPrice.Name = "colPrice";
            this.colPrice.Width = 120;
            // 
            // FormPricingPlan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(850, 550);
            this.Controls.Add(this.flpBase);
            this.Name = "FormPricingPlan";
            this.Text = "Pricing Plans";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpPlanFilter.ResumeLayout(false);
            this.flpPlanFilter.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpDetail.PerformLayout();
            this.flpPlanName.ResumeLayout(false);
            this.flpPlanName.PerformLayout();
            this.flpDescription.ResumeLayout(false);
            this.flpDescription.PerformLayout();
            this.flpCosts.ResumeLayout(false);
            this.flpCosts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResourcePrices)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpPlanFilter;
        private System.Windows.Forms.Label lblPlanFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanFilter;
        private System.Windows.Forms.ListView lvwPlans;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpPlanName;
        private System.Windows.Forms.Label lblPlanName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanName;
        private System.Windows.Forms.FlowLayoutPanel flpDescription;
        private System.Windows.Forms.Label lblDescription;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtDescription;
        private System.Windows.Forms.FlowLayoutPanel flpCosts;
        private System.Windows.Forms.Label lblFixedCost;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFixedCost;
        private System.Windows.Forms.Label lblHourlyCost;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtHourlyCost;
        private System.Windows.Forms.Button cmdSave;
        private DataEntryGridView dgvResourcePrices;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResourceName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrice;
    }
}
