namespace OE2EmpireTracker.Forms.DeliveryRoute
{
    partial class FormAutoFill
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
            this.chkCommodities = new System.Windows.Forms.CheckBox();
            this.chkFlatpacks = new System.Windows.Forms.CheckBox();
            this.chkResources = new System.Windows.Forms.CheckBox();
            this.chkWorkers = new System.Windows.Forms.CheckBox();
            this.lblTimeHorizon = new System.Windows.Forms.Label();
            this.txtTimeHorizon = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblTimeHorizonUnit = new System.Windows.Forms.Label();
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.lblHeader = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(12, 12);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Text = "Select request types to auto-fill:";
            // 
            // chkCommodities
            // 
            this.chkCommodities.AutoSize = true;
            this.chkCommodities.Checked = true;
            this.chkCommodities.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCommodities.Location = new System.Drawing.Point(15, 38);
            this.chkCommodities.Name = "chkCommodities";
            this.chkCommodities.Text = "Commodities";
            // 
            // chkFlatpacks
            // 
            this.chkFlatpacks.AutoSize = true;
            this.chkFlatpacks.Enabled = true;
            this.chkFlatpacks.Location = new System.Drawing.Point(15, 61);
            this.chkFlatpacks.Name = "chkFlatpacks";
            this.chkFlatpacks.Text = "Flatpacks";
            // 
            // chkResources
            // 
            this.chkResources.AutoSize = true;
            this.chkResources.Enabled = true;
            this.chkResources.Location = new System.Drawing.Point(15, 84);
            this.chkResources.Name = "chkResources";
            this.chkResources.Text = "Resources for Manufacturing";
            // 
            // chkWorkers
            // 
            this.chkWorkers.AutoSize = true;
            this.chkWorkers.Enabled = true;
            this.chkWorkers.Location = new System.Drawing.Point(15, 107);
            this.chkWorkers.Name = "chkWorkers";
            this.chkWorkers.Text = "Workers";
            //
            // lblTimeHorizon
            //
            this.lblTimeHorizon.AutoSize = true;
            this.lblTimeHorizon.Location = new System.Drawing.Point(15, 133);
            this.lblTimeHorizon.Name = "lblTimeHorizon";
            this.lblTimeHorizon.Text = "Flatpack time horizon:";
            //
            // txtTimeHorizon
            //
            this.txtTimeHorizon.Location = new System.Drawing.Point(150, 130);
            this.txtTimeHorizon.Name = "txtTimeHorizon";
            this.txtTimeHorizon.Size = new System.Drawing.Size(50, 20);
            this.txtTimeHorizon.Text = "0";
            //
            // lblTimeHorizonUnit
            //
            this.lblTimeHorizonUnit.AutoSize = true;
            this.lblTimeHorizonUnit.Location = new System.Drawing.Point(205, 133);
            this.lblTimeHorizonUnit.Name = "lblTimeHorizonUnit";
            this.lblTimeHorizonUnit.Text = "hours (0 = all)";
            // 
            // cmdOK
            // 
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(80, 165);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 23);
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(161, 165);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // FormAutoFill
            // 
            this.AcceptButton = this.cmdOK;
            this.CancelButton = this.cmdCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 200);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.chkCommodities);
            this.Controls.Add(this.chkFlatpacks);
            this.Controls.Add(this.chkResources);
            this.Controls.Add(this.chkWorkers);
            this.Controls.Add(this.lblTimeHorizon);
            this.Controls.Add(this.txtTimeHorizon);
            this.Controls.Add(this.lblTimeHorizonUnit);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.cmdCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAutoFill";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Auto-Fill Delivery Plan";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.CheckBox chkCommodities;
        private System.Windows.Forms.CheckBox chkFlatpacks;
        private System.Windows.Forms.CheckBox chkResources;
        private System.Windows.Forms.CheckBox chkWorkers;
        private System.Windows.Forms.Label lblTimeHorizon;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtTimeHorizon;
        private System.Windows.Forms.Label lblTimeHorizonUnit;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
    }
}
