namespace OE2EmpireTracker.Forms.Banking
{
    partial class FormBanking
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
            this.lblBalance = new System.Windows.Forms.Label();
            this.dgvTransactions = new System.Windows.Forms.DataGridView();
            this.colDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCreditChange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOldBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNewBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).BeginInit();
            this.SuspendLayout();

            // lblBalance
            this.lblBalance.AutoSize = true;
            this.lblBalance.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblBalance.Location = new System.Drawing.Point(12, 12);
            this.lblBalance.Name = "lblBalance";
            this.lblBalance.Size = new System.Drawing.Size(130, 20);
            this.lblBalance.Text = "Balance: 0.00";

            // colDate
            this.colDate.HeaderText = "Date";
            this.colDate.Name = "colDate";
            this.colDate.ReadOnly = true;
            this.colDate.Width = 130;

            // colType
            this.colType.HeaderText = "Type";
            this.colType.Name = "colType";
            this.colType.ReadOnly = true;
            this.colType.Width = 120;

            // colDetail
            this.colDetail.HeaderText = "Detail";
            this.colDetail.Name = "colDetail";
            this.colDetail.ReadOnly = true;
            this.colDetail.Width = 180;

            // colCreditChange
            this.colCreditChange.HeaderText = "Credit Change";
            this.colCreditChange.Name = "colCreditChange";
            this.colCreditChange.ReadOnly = true;
            this.colCreditChange.Width = 100;

            // colOldBalance
            this.colOldBalance.HeaderText = "Old Balance";
            this.colOldBalance.Name = "colOldBalance";
            this.colOldBalance.ReadOnly = true;
            this.colOldBalance.Width = 100;

            // colNewBalance
            this.colNewBalance.HeaderText = "New Balance";
            this.colNewBalance.Name = "colNewBalance";
            this.colNewBalance.ReadOnly = true;
            this.colNewBalance.Width = 100;

            // dgvTransactions
            this.dgvTransactions.AllowUserToAddRows = false;
            this.dgvTransactions.AllowUserToDeleteRows = false;
            this.dgvTransactions.AllowUserToOrderColumns = true;
            this.dgvTransactions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTransactions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                this.colDate,
                this.colType,
                this.colDetail,
                this.colCreditChange,
                this.colOldBalance,
                this.colNewBalance,
            });
            this.dgvTransactions.Location = new System.Drawing.Point(12, 44);
            this.dgvTransactions.Name = "dgvTransactions";
            this.dgvTransactions.ReadOnly = true;
            this.dgvTransactions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTransactions.Size = new System.Drawing.Size(760, 505);
            this.dgvTransactions.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;

            // FormBanking
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.dgvTransactions);
            this.Controls.Add(this.lblBalance);
            this.Name = "FormBanking";
            this.Text = "Banking";
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblBalance;
        private System.Windows.Forms.DataGridView dgvTransactions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCreditChange;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOldBalance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNewBalance;
    }
}
