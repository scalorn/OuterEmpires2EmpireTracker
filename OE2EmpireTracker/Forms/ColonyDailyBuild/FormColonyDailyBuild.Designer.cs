namespace OE2EmpireTracker.Forms.ColonyDailyBuild
{
    partial class FormColonyDailyBuild
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
            this.txtRouteFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbRoute = new System.Windows.Forms.ComboBox();
            this.pnlContent = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBase.SuspendLayout();
            this.flpSelectors.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpSelectors);
            this.flpBase.Controls.Add(this.pnlContent);
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
            this.flpSelectors.Controls.Add(this.txtRouteFilter);
            this.flpSelectors.Controls.Add(this.cmbRoute);
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
            // txtRouteFilter
            // 
            this.txtRouteFilter.Location = new System.Drawing.Point(3, 22);
            this.txtRouteFilter.Name = "txtRouteFilter";
            this.txtRouteFilter.Size = new System.Drawing.Size(214, 20);
            // 
            // cmbRoute
            // 
            this.cmbRoute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRoute.Location = new System.Drawing.Point(3, 48);
            this.cmbRoute.Name = "cmbRoute";
            this.cmbRoute.Size = new System.Drawing.Size(214, 21);
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlContent.Location = new System.Drawing.Point(229, 3);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(768, 594);
            this.pnlContent.TabIndex = 1;
            this.pnlContent.WrapContents = false;
            // 
            // FormColonyDailyBuild
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.flpBase);
            this.Name = "FormColonyDailyBuild";
            this.Text = "Colony Daily Build";
            this.flpBase.ResumeLayout(false);
            this.flpSelectors.ResumeLayout(false);
            this.flpSelectors.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSelectors;
        private System.Windows.Forms.Label lblRoute;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtRouteFilter;
        private System.Windows.Forms.ComboBox cmbRoute;
        private System.Windows.Forms.FlowLayoutPanel pnlContent;
    }
}
