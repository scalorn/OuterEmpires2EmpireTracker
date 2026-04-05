namespace OE2EmpireTracker.Forms.DeliveryExecution
{
    partial class FormDeliveryExecution
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
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSelectors = new System.Windows.Forms.FlowLayoutPanel();
            this.lblRoute = new System.Windows.Forms.Label();
            this.cmbRoute = new System.Windows.Forms.ComboBox();
            this.lblPlan = new System.Windows.Forms.Label();
            this.cmbPlan = new System.Windows.Forms.ComboBox();
            this.pnlExecution = new System.Windows.Forms.Panel();
            this.lblLoadListHeader = new System.Windows.Forms.Label();
            this.dgvLoadList = new System.Windows.Forms.DataGridView();
            this.colLoadType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLoadName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLoadQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpStops = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBase.SuspendLayout();
            this.flpSelectors.SuspendLayout();
            this.pnlExecution.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLoadList)).BeginInit();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpSelectors);
            this.flpBase.Controls.Add(this.pnlExecution);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1000, 600);
            this.flpBase.TabIndex = 0;
            this.flpBase.WrapContents = false;
            // 
            // flpSelectors
            // 
            this.flpSelectors.Controls.Add(this.lblRoute);
            this.flpSelectors.Controls.Add(this.cmbRoute);
            this.flpSelectors.Controls.Add(this.lblPlan);
            this.flpSelectors.Controls.Add(this.cmbPlan);
            this.flpSelectors.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSelectors.Location = new System.Drawing.Point(3, 3);
            this.flpSelectors.Name = "flpSelectors";
            this.flpSelectors.Size = new System.Drawing.Size(220, 594);
            this.flpSelectors.TabIndex = 0;
            // 
            // lblRoute
            // 
            this.lblRoute.AutoSize = true;
            this.lblRoute.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblRoute.Location = new System.Drawing.Point(3, 3);
            this.lblRoute.Margin = new System.Windows.Forms.Padding(3);
            this.lblRoute.Name = "lblRoute";
            this.lblRoute.Size = new System.Drawing.Size(40, 13);
            this.lblRoute.Text = "Route";
            // 
            // cmbRoute
            // 
            this.cmbRoute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRoute.Location = new System.Drawing.Point(3, 22);
            this.cmbRoute.Name = "cmbRoute";
            this.cmbRoute.Size = new System.Drawing.Size(214, 21);
            // 
            // lblPlan
            // 
            this.lblPlan.AutoSize = true;
            this.lblPlan.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblPlan.Location = new System.Drawing.Point(3, 52);
            this.lblPlan.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.lblPlan.Name = "lblPlan";
            this.lblPlan.Size = new System.Drawing.Size(30, 13);
            this.lblPlan.Text = "Plan";
            // 
            // cmbPlan
            // 
            this.cmbPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlan.Location = new System.Drawing.Point(3, 71);
            this.cmbPlan.Name = "cmbPlan";
            this.cmbPlan.Size = new System.Drawing.Size(214, 21);
            // 
            // pnlExecution
            // 
            this.pnlExecution.AutoScroll = true;
            this.pnlExecution.Controls.Add(this.flpExecutionContent);
            this.pnlExecution.Location = new System.Drawing.Point(229, 3);
            this.pnlExecution.Name = "pnlExecution";
            this.pnlExecution.Size = new System.Drawing.Size(768, 594);
            this.pnlExecution.TabIndex = 1;
            // 
            // flpExecutionContent
            // 
            this.flpExecutionContent = new System.Windows.Forms.FlowLayoutPanel();
            this.flpExecutionContent.AutoSize = true;
            this.flpExecutionContent.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpExecutionContent.Controls.Add(this.lblLoadListHeader);
            this.flpExecutionContent.Controls.Add(this.dgvLoadList);
            this.flpExecutionContent.Controls.Add(this.flpStops);
            this.flpExecutionContent.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpExecutionContent.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpExecutionContent.Name = "flpExecutionContent";
            this.flpExecutionContent.Size = new System.Drawing.Size(768, 400);
            this.flpExecutionContent.WrapContents = false;
            // 
            // lblLoadListHeader
            // 
            this.lblLoadListHeader.AutoSize = true;
            this.lblLoadListHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblLoadListHeader.Location = new System.Drawing.Point(3, 3);
            this.lblLoadListHeader.Margin = new System.Windows.Forms.Padding(3);
            this.lblLoadListHeader.Name = "lblLoadListHeader";
            this.lblLoadListHeader.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.lblLoadListHeader.Size = new System.Drawing.Size(150, 21);
            this.lblLoadListHeader.Text = "Load Before Departure";
            // 
            // dgvLoadList
            // 
            this.dgvLoadList.AllowUserToAddRows = false;
            this.dgvLoadList.AllowUserToDeleteRows = false;
            this.dgvLoadList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLoadList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLoadType, this.colLoadName, this.colLoadQty});
            this.dgvLoadList.Dock = System.Windows.Forms.DockStyle.None;
            this.dgvLoadList.Location = new System.Drawing.Point(2, 30);
            this.dgvLoadList.Name = "dgvLoadList";
            this.dgvLoadList.ReadOnly = true;
            this.dgvLoadList.RowHeadersVisible = false;
            this.dgvLoadList.Size = new System.Drawing.Size(768, 120);
            this.dgvLoadList.TabIndex = 1;
            // 
            // colLoadType
            // 
            this.colLoadType.HeaderText = "Type";
            this.colLoadType.Name = "colLoadType";
            this.colLoadType.ReadOnly = true;
            this.colLoadType.Width = 100;
            // 
            // colLoadName
            // 
            this.colLoadName.HeaderText = "Item";
            this.colLoadName.Name = "colLoadName";
            this.colLoadName.ReadOnly = true;
            this.colLoadName.Width = 450;
            // 
            // colLoadQty
            // 
            this.colLoadQty.HeaderText = "Qty";
            this.colLoadQty.Name = "colLoadQty";
            this.colLoadQty.ReadOnly = true;
            this.colLoadQty.Width = 80;
            // 
            // flpStops
            // 
            this.flpStops.AutoSize = true;
            this.flpStops.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpStops.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpStops.Location = new System.Drawing.Point(2, 155);
            this.flpStops.Name = "flpStops";
            this.flpStops.Size = new System.Drawing.Size(768, 0);
            this.flpStops.WrapContents = false;
            // 
            // FormDeliveryExecution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.flpBase);
            this.Name = "FormDeliveryExecution";
            this.Text = "Delivery Execution";
            this.flpBase.ResumeLayout(false);
            this.flpSelectors.ResumeLayout(false);
            this.flpSelectors.PerformLayout();
            this.pnlExecution.ResumeLayout(false);
            this.pnlExecution.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLoadList)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSelectors;
        private System.Windows.Forms.Label lblRoute;
        private System.Windows.Forms.ComboBox cmbRoute;
        private System.Windows.Forms.Label lblPlan;
        private System.Windows.Forms.ComboBox cmbPlan;
        private System.Windows.Forms.Panel pnlExecution;
        private System.Windows.Forms.FlowLayoutPanel flpExecutionContent;
        private System.Windows.Forms.Label lblLoadListHeader;
        private System.Windows.Forms.DataGridView dgvLoadList;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLoadType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLoadName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLoadQty;
        private System.Windows.Forms.FlowLayoutPanel flpStops;
    }
}
