// <copyright file="FormMail.Designer.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Forms.Mail
{
    /// <summary>
    /// Designer-generated layout for FormMail.
    /// </summary>
    partial class FormMail
    {
        private System.ComponentModel.IContainer components = null;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlFilter = new System.Windows.Forms.Panel();
            this.cboReadFilter = new System.Windows.Forms.ComboBox();
            this.cboTypeFilter = new System.Windows.Forms.ComboBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblMessageCount = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lvwMail = new System.Windows.Forms.ListView();
            this.colFrom = new System.Windows.Forms.ColumnHeader();
            this.colSubject = new System.Windows.Forms.ColumnHeader();
            this.colDate = new System.Windows.Forms.ColumnHeader();
            this.lblDetailFrom = new System.Windows.Forms.Label();
            this.lblDetailDate = new System.Windows.Forms.Label();
            this.lblDetailSubject = new System.Windows.Forms.Label();
            this.txtDetailContent = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblSyncStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblPlaceholder = new System.Windows.Forms.ToolStripStatusLabel();
            this.pnlFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // pnlFilter
            this.pnlFilter.Controls.Add(this.lblMessageCount);
            this.pnlFilter.Controls.Add(this.txtSearch);
            this.pnlFilter.Controls.Add(this.cboTypeFilter);
            this.pnlFilter.Controls.Add(this.cboReadFilter);
            this.pnlFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilter.Location = new System.Drawing.Point(0, 0);
            this.pnlFilter.Name = "pnlFilter";
            this.pnlFilter.Padding = new System.Windows.Forms.Padding(6);
            this.pnlFilter.Size = new System.Drawing.Size(900, 36);

            // cboReadFilter
            this.cboReadFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboReadFilter.FormattingEnabled = true;
            this.cboReadFilter.Location = new System.Drawing.Point(9, 7);
            this.cboReadFilter.Name = "cboReadFilter";
            this.cboReadFilter.Size = new System.Drawing.Size(100, 21);

            // cboTypeFilter
            this.cboTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTypeFilter.FormattingEnabled = true;
            this.cboTypeFilter.Location = new System.Drawing.Point(115, 7);
            this.cboTypeFilter.Name = "cboTypeFilter";
            this.cboTypeFilter.Size = new System.Drawing.Size(120, 21);

            // txtSearch
            this.txtSearch.Location = new System.Drawing.Point(241, 7);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(200, 20);
            this.txtSearch.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;

            // lblMessageCount
            this.lblMessageCount.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lblMessageCount.Location = new System.Drawing.Point(780, 10);
            this.lblMessageCount.Name = "lblMessageCount";
            this.lblMessageCount.Size = new System.Drawing.Size(110, 17);
            this.lblMessageCount.Text = "0 messages";
            this.lblMessageCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // splitContainer
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 36);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Size = new System.Drawing.Size(900, 486);
            this.splitContainer.SplitterDistance = 450;

            // splitContainer.Panel1
            this.splitContainer.Panel1.Controls.Add(this.lvwMail);

            // splitContainer.Panel2
            this.splitContainer.Panel2.Controls.Add(this.txtDetailContent);
            this.splitContainer.Panel2.Controls.Add(this.lblDetailSubject);
            this.splitContainer.Panel2.Controls.Add(this.lblDetailDate);
            this.splitContainer.Panel2.Controls.Add(this.lblDetailFrom);

            // lvwMail
            this.lvwMail.Columns.AddRange(new System.Windows.Forms.ColumnHeader[]
            {
                this.colFrom,
                this.colSubject,
                this.colDate,
            });
            this.lvwMail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwMail.FullRowSelect = true;
            this.lvwMail.HideSelection = false;
            this.lvwMail.Location = new System.Drawing.Point(0, 0);
            this.lvwMail.MultiSelect = false;
            this.lvwMail.Name = "lvwMail";
            this.lvwMail.Size = new System.Drawing.Size(450, 486);
            this.lvwMail.UseCompatibleStateImageBehavior = false;
            this.lvwMail.View = System.Windows.Forms.View.Details;

            // colFrom
            this.colFrom.Text = "From";
            this.colFrom.Width = 150;

            // colSubject
            this.colSubject.Text = "Subject";
            this.colSubject.Width = 250;

            // colDate
            this.colDate.Text = "Date";
            this.colDate.Width = 120;

            // lblDetailFrom
            this.lblDetailFrom.AutoSize = true;
            this.lblDetailFrom.Location = new System.Drawing.Point(6, 8);
            this.lblDetailFrom.Name = "lblDetailFrom";
            this.lblDetailFrom.Size = new System.Drawing.Size(36, 13);
            this.lblDetailFrom.Text = "From:";

            // lblDetailDate
            this.lblDetailDate.AutoSize = true;
            this.lblDetailDate.Location = new System.Drawing.Point(6, 28);
            this.lblDetailDate.Name = "lblDetailDate";
            this.lblDetailDate.Size = new System.Drawing.Size(33, 13);
            this.lblDetailDate.Text = "Date:";

            // lblDetailSubject
            this.lblDetailSubject.AutoSize = true;
            this.lblDetailSubject.Location = new System.Drawing.Point(6, 48);
            this.lblDetailSubject.Name = "lblDetailSubject";
            this.lblDetailSubject.Size = new System.Drawing.Size(46, 13);
            this.lblDetailSubject.Text = "Subject:";

            // txtDetailContent
            this.txtDetailContent.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.txtDetailContent.Location = new System.Drawing.Point(6, 70);
            this.txtDetailContent.Multiline = true;
            this.txtDetailContent.Name = "txtDetailContent";
            this.txtDetailContent.ReadOnly = true;
            this.txtDetailContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetailContent.Size = new System.Drawing.Size(430, 410);

            // statusStrip
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.lblSyncStatus,
                this.lblPlaceholder,
            });
            this.statusStrip.Location = new System.Drawing.Point(0, 522);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(900, 22);

            // lblSyncStatus
            this.lblSyncStatus.Name = "lblSyncStatus";
            this.lblSyncStatus.Size = new System.Drawing.Size(79, 17);
            this.lblSyncStatus.Text = "Last sync: Never";

            // lblPlaceholder
            this.lblPlaceholder.Name = "lblPlaceholder";
            this.lblPlaceholder.Size = new System.Drawing.Size(806, 17);
            this.lblPlaceholder.Spring = true;

            // FormMail
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 544);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.pnlFilter);
            this.Controls.Add(this.statusStrip);
            this.Name = "FormMail";
            this.Text = "Mail";
            this.pnlFilter.ResumeLayout(false);
            this.pnlFilter.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlFilter;
        private System.Windows.Forms.ComboBox cboReadFilter;
        private System.Windows.Forms.ComboBox cboTypeFilter;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblMessageCount;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ListView lvwMail;
        private System.Windows.Forms.ColumnHeader colFrom;
        private System.Windows.Forms.ColumnHeader colSubject;
        private System.Windows.Forms.ColumnHeader colDate;
        private System.Windows.Forms.Label lblDetailFrom;
        private System.Windows.Forms.Label lblDetailDate;
        private System.Windows.Forms.Label lblDetailSubject;
        private System.Windows.Forms.TextBox txtDetailContent;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblSyncStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblPlaceholder;
    }
}
