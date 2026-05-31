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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAreaCashFlow = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAreaIncomeExpenses = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAreaTypeBreakdown = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legendCashFlow = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Legend legendIncomeExpenses = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Legend legendTypeBreakdown = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.lblBalance = new System.Windows.Forms.Label();
            this.btn24h = new System.Windows.Forms.Button();
            this.btn7d = new System.Windows.Forms.Button();
            this.btn30d = new System.Windows.Forms.Button();
            this.btnAllTime = new System.Windows.Forms.Button();
            this.cboType = new System.Windows.Forms.ComboBox();
            this.lblType = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.lblFrom = new System.Windows.Forms.Label();
            this.lblTo = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabTransactions = new System.Windows.Forms.TabPage();
            this.dgvTransactions = new System.Windows.Forms.DataGridView();
            this.colDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCreditChange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOldBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNewBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabCharts = new System.Windows.Forms.TabPage();
            this.pnlChartGrouping = new System.Windows.Forms.Panel();
            this.btnGroupDaily = new System.Windows.Forms.RadioButton();
            this.btnGroupHourly = new System.Windows.Forms.RadioButton();
            this.splitCharts = new System.Windows.Forms.SplitContainer();
            this.chartCashFlow = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.splitChartsLower = new System.Windows.Forms.SplitContainer();
            this.chartIncomeExpenses = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chartTypeBreakdown = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.btnAddTransaction = new System.Windows.Forms.Button();
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.lblIncome = new System.Windows.Forms.Label();
            this.lblExpenses = new System.Windows.Forms.Label();
            this.lblNet = new System.Windows.Forms.Label();
            this.btnImportTransactions = new System.Windows.Forms.Button();
            this.btnImportBalance = new System.Windows.Forms.Button();
            this.tabMain.SuspendLayout();
            this.tabTransactions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).BeginInit();
            this.tabCharts.SuspendLayout();
            this.pnlChartGrouping.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitCharts)).BeginInit();
            this.splitCharts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartCashFlow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitChartsLower)).BeginInit();
            this.splitChartsLower.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartIncomeExpenses)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartTypeBreakdown)).BeginInit();
            this.pnlSummary.SuspendLayout();
            this.SuspendLayout();

            // lblBalance
            this.lblBalance.AutoSize = true;
            this.lblBalance.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblBalance.Location = new System.Drawing.Point(12, 12);
            this.lblBalance.Name = "lblBalance";
            this.lblBalance.Size = new System.Drawing.Size(130, 20);
            this.lblBalance.Text = "Balance: 0.00";

            // btn24h
            this.btn24h.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.btn24h.Location = new System.Drawing.Point(12, 42);
            this.btn24h.Name = "btn24h";
            this.btn24h.Size = new System.Drawing.Size(50, 25);
            this.btn24h.Text = "24h";
            this.btn24h.UseVisualStyleBackColor = true;

            // btn7d
            this.btn7d.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.btn7d.Location = new System.Drawing.Point(66, 42);
            this.btn7d.Name = "btn7d";
            this.btn7d.Size = new System.Drawing.Size(50, 25);
            this.btn7d.Text = "7d";
            this.btn7d.UseVisualStyleBackColor = true;

            // btn30d
            this.btn30d.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.btn30d.Location = new System.Drawing.Point(120, 42);
            this.btn30d.Name = "btn30d";
            this.btn30d.Size = new System.Drawing.Size(50, 25);
            this.btn30d.Text = "30d";
            this.btn30d.UseVisualStyleBackColor = true;

            // btnAllTime
            this.btnAllTime.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.btnAllTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnAllTime.Location = new System.Drawing.Point(174, 42);
            this.btnAllTime.Name = "btnAllTime";
            this.btnAllTime.Size = new System.Drawing.Size(50, 25);
            this.btnAllTime.Text = "All";
            this.btnAllTime.UseVisualStyleBackColor = true;

            // lblType
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(240, 47);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(34, 13);
            this.lblType.Text = "Type:";

            // cboType
            this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboType.FormattingEnabled = true;
            this.cboType.Location = new System.Drawing.Point(278, 44);
            this.cboType.Name = "cboType";
            this.cboType.Size = new System.Drawing.Size(150, 21);

            // lblFrom
            this.lblFrom.AutoSize = true;
            this.lblFrom.Location = new System.Drawing.Point(12, 74);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(33, 13);
            this.lblFrom.Text = "From:";

            // dtpFrom
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Location = new System.Drawing.Point(50, 71);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.ShowCheckBox = true;
            this.dtpFrom.Checked = false;
            this.dtpFrom.Size = new System.Drawing.Size(150, 20);

            // lblTo
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(210, 74);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(23, 13);
            this.lblTo.Text = "To:";

            // dtpTo
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Location = new System.Drawing.Point(238, 71);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.ShowCheckBox = true;
            this.dtpTo.Checked = false;
            this.dtpTo.Size = new System.Drawing.Size(150, 20);

            // tabMain
            this.tabMain.Controls.Add(this.tabTransactions);
            this.tabMain.Controls.Add(this.tabCharts);
            this.tabMain.Location = new System.Drawing.Point(12, 100);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(876, 490);
            this.tabMain.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;

            // tabTransactions
            this.tabTransactions.Controls.Add(this.dgvTransactions);
            this.tabTransactions.Location = new System.Drawing.Point(4, 22);
            this.tabTransactions.Name = "tabTransactions";
            this.tabTransactions.Padding = new System.Windows.Forms.Padding(3);
            this.tabTransactions.Size = new System.Drawing.Size(868, 464);
            this.tabTransactions.TabIndex = 0;
            this.tabTransactions.Text = "Transactions";
            this.tabTransactions.UseVisualStyleBackColor = true;

            // tabCharts
            this.tabCharts.Controls.Add(this.splitCharts);
            this.tabCharts.Controls.Add(this.pnlChartGrouping);
            this.tabCharts.Location = new System.Drawing.Point(4, 22);
            this.tabCharts.Name = "tabCharts";
            this.tabCharts.Padding = new System.Windows.Forms.Padding(3);
            this.tabCharts.Size = new System.Drawing.Size(868, 464);
            this.tabCharts.TabIndex = 1;
            this.tabCharts.Text = "Charts";
            this.tabCharts.UseVisualStyleBackColor = true;

            // pnlChartGrouping
            this.pnlChartGrouping.Controls.Add(this.btnGroupDaily);
            this.pnlChartGrouping.Controls.Add(this.btnGroupHourly);
            this.pnlChartGrouping.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlChartGrouping.Location = new System.Drawing.Point(3, 3);
            this.pnlChartGrouping.Name = "pnlChartGrouping";
            this.pnlChartGrouping.Size = new System.Drawing.Size(862, 30);

            // btnGroupHourly
            this.btnGroupHourly.AutoSize = true;
            this.btnGroupHourly.Location = new System.Drawing.Point(6, 6);
            this.btnGroupHourly.Name = "btnGroupHourly";
            this.btnGroupHourly.Size = new System.Drawing.Size(56, 17);
            this.btnGroupHourly.Text = "Hourly";
            this.btnGroupHourly.UseVisualStyleBackColor = true;

            // btnGroupDaily
            this.btnGroupDaily.AutoSize = true;
            this.btnGroupDaily.Checked = true;
            this.btnGroupDaily.Location = new System.Drawing.Point(80, 6);
            this.btnGroupDaily.Name = "btnGroupDaily";
            this.btnGroupDaily.Size = new System.Drawing.Size(49, 17);
            this.btnGroupDaily.TabStop = true;
            this.btnGroupDaily.Text = "Daily";
            this.btnGroupDaily.UseVisualStyleBackColor = true;

            // splitCharts
            this.splitCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitCharts.Location = new System.Drawing.Point(3, 33);
            this.splitCharts.Name = "splitCharts";
            this.splitCharts.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitCharts.Size = new System.Drawing.Size(862, 428);
            this.splitCharts.SplitterDistance = 200;
            this.splitCharts.Panel1.Controls.Add(this.chartCashFlow);
            this.splitCharts.Panel2.Controls.Add(this.splitChartsLower);

            // splitChartsLower
            this.splitChartsLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitChartsLower.Location = new System.Drawing.Point(0, 0);
            this.splitChartsLower.Name = "splitChartsLower";
            this.splitChartsLower.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitChartsLower.Size = new System.Drawing.Size(862, 224);
            this.splitChartsLower.SplitterDistance = 112;
            this.splitChartsLower.Panel1.Controls.Add(this.chartIncomeExpenses);
            this.splitChartsLower.Panel2.Controls.Add(this.chartTypeBreakdown);

            // chartCashFlow
            chartAreaCashFlow.Name = "CashFlowArea";
            chartAreaCashFlow.AxisX.Title = "Time";
            chartAreaCashFlow.AxisY.Title = "Credits";
            chartAreaCashFlow.AxisX.LabelStyle.Angle = -45;
            this.chartCashFlow.ChartAreas.Add(chartAreaCashFlow);
            legendCashFlow.Name = "Default";
            legendCashFlow.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            this.chartCashFlow.Legends.Add(legendCashFlow);
            this.chartCashFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartCashFlow.Location = new System.Drawing.Point(0, 0);
            this.chartCashFlow.Name = "chartCashFlow";
            this.chartCashFlow.Size = new System.Drawing.Size(862, 200);
            this.chartCashFlow.TabIndex = 0;
            this.chartCashFlow.Text = "Cash Flow";

            // chartIncomeExpenses
            chartAreaIncomeExpenses.Name = "IncomeExpensesArea";
            chartAreaIncomeExpenses.AxisX.Title = "Time";
            chartAreaIncomeExpenses.AxisY.Title = "Credits";
            chartAreaIncomeExpenses.AxisX.LabelStyle.Angle = -45;
            this.chartIncomeExpenses.ChartAreas.Add(chartAreaIncomeExpenses);
            legendIncomeExpenses.Name = "Default";
            legendIncomeExpenses.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            this.chartIncomeExpenses.Legends.Add(legendIncomeExpenses);
            this.chartIncomeExpenses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartIncomeExpenses.Location = new System.Drawing.Point(0, 0);
            this.chartIncomeExpenses.Name = "chartIncomeExpenses";
            this.chartIncomeExpenses.Size = new System.Drawing.Size(862, 112);
            this.chartIncomeExpenses.TabIndex = 0;
            this.chartIncomeExpenses.Text = "Income vs Expenses";

            // chartTypeBreakdown
            chartAreaTypeBreakdown.Name = "TypeBreakdownArea";
            this.chartTypeBreakdown.ChartAreas.Add(chartAreaTypeBreakdown);
            legendTypeBreakdown.Name = "Default";
            legendTypeBreakdown.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Right;
            this.chartTypeBreakdown.Legends.Add(legendTypeBreakdown);
            this.chartTypeBreakdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartTypeBreakdown.Location = new System.Drawing.Point(0, 0);
            this.chartTypeBreakdown.Name = "chartTypeBreakdown";
            this.chartTypeBreakdown.Size = new System.Drawing.Size(862, 108);
            this.chartTypeBreakdown.TabIndex = 0;
            this.chartTypeBreakdown.Text = "Type Breakdown";

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
            this.dgvTransactions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTransactions.Location = new System.Drawing.Point(3, 3);
            this.dgvTransactions.Name = "dgvTransactions";
            this.dgvTransactions.ReadOnly = true;
            this.dgvTransactions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTransactions.Size = new System.Drawing.Size(862, 458);

            // lblIncome
            this.lblIncome.AutoSize = true;
            this.lblIncome.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblIncome.Location = new System.Drawing.Point(8, 6);
            this.lblIncome.Name = "lblIncome";
            this.lblIncome.Size = new System.Drawing.Size(100, 16);
            this.lblIncome.Text = "Income: 0.00";

            // lblExpenses
            this.lblExpenses.AutoSize = true;
            this.lblExpenses.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblExpenses.Location = new System.Drawing.Point(200, 6);
            this.lblExpenses.Name = "lblExpenses";
            this.lblExpenses.Size = new System.Drawing.Size(120, 16);
            this.lblExpenses.Text = "Expenses: 0.00";

            // lblNet
            this.lblNet.AutoSize = true;
            this.lblNet.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblNet.Location = new System.Drawing.Point(400, 6);
            this.lblNet.Name = "lblNet";
            this.lblNet.Size = new System.Drawing.Size(80, 16);
            this.lblNet.Text = "Net: 0.00";

            // btnImportTransactions
            this.btnImportTransactions.Location = new System.Drawing.Point(12, 598);
            this.btnImportTransactions.Name = "btnImportTransactions";
            this.btnImportTransactions.Size = new System.Drawing.Size(130, 28);
            this.btnImportTransactions.Text = "Import Transactions";
            this.btnImportTransactions.UseVisualStyleBackColor = true;
            this.btnImportTransactions.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;

            // btnImportBalance
            this.btnImportBalance.Location = new System.Drawing.Point(148, 598);
            this.btnImportBalance.Name = "btnImportBalance";
            this.btnImportBalance.Size = new System.Drawing.Size(110, 28);
            this.btnImportBalance.Text = "Import Balance";
            this.btnImportBalance.UseVisualStyleBackColor = true;
            this.btnImportBalance.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;

            // btnAddTransaction
            this.btnAddTransaction.Location = new System.Drawing.Point(450, 44);
            this.btnAddTransaction.Name = "btnAddTransaction";
            this.btnAddTransaction.Size = new System.Drawing.Size(110, 25);
            this.btnAddTransaction.Text = "Add Transaction";
            this.btnAddTransaction.UseVisualStyleBackColor = true;

            // pnlSummary
            this.pnlSummary.Controls.Add(this.lblIncome);
            this.pnlSummary.Controls.Add(this.lblExpenses);
            this.pnlSummary.Controls.Add(this.lblNet);
            this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSummary.Location = new System.Drawing.Point(0, 596);
            this.pnlSummary.Name = "pnlSummary";
            this.pnlSummary.Size = new System.Drawing.Size(900, 32);

            // FormBanking
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 660);
            this.Controls.Add(this.btnImportTransactions);
            this.Controls.Add(this.btnImportBalance);
            this.Controls.Add(this.btnAddTransaction);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.pnlSummary);
            this.Controls.Add(this.dtpTo);
            this.Controls.Add(this.lblTo);
            this.Controls.Add(this.dtpFrom);
            this.Controls.Add(this.lblFrom);
            this.Controls.Add(this.cboType);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.btnAllTime);
            this.Controls.Add(this.btn30d);
            this.Controls.Add(this.btn7d);
            this.Controls.Add(this.btn24h);
            this.Controls.Add(this.lblBalance);
            this.Name = "FormBanking";
            this.Text = "Banking";
            this.tabMain.ResumeLayout(false);
            this.tabTransactions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).EndInit();
            this.tabCharts.ResumeLayout(false);
            this.pnlChartGrouping.ResumeLayout(false);
            this.pnlChartGrouping.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitCharts)).EndInit();
            this.splitCharts.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartCashFlow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitChartsLower)).EndInit();
            this.splitChartsLower.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartIncomeExpenses)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartTypeBreakdown)).EndInit();
            this.pnlSummary.ResumeLayout(false);
            this.pnlSummary.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblBalance;
        private System.Windows.Forms.Button btn24h;
        private System.Windows.Forms.Button btn7d;
        private System.Windows.Forms.Button btn30d;
        private System.Windows.Forms.Button btnAllTime;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabTransactions;
        private System.Windows.Forms.DataGridView dgvTransactions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCreditChange;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOldBalance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNewBalance;
        private System.Windows.Forms.TabPage tabCharts;
        private System.Windows.Forms.Panel pnlChartGrouping;
        private System.Windows.Forms.RadioButton btnGroupHourly;
        private System.Windows.Forms.RadioButton btnGroupDaily;
        private System.Windows.Forms.SplitContainer splitCharts;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartCashFlow;
        private System.Windows.Forms.SplitContainer splitChartsLower;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartIncomeExpenses;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartTypeBreakdown;
        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Label lblIncome;
        private System.Windows.Forms.Label lblExpenses;
        private System.Windows.Forms.Label lblNet;
        private System.Windows.Forms.Button btnImportTransactions;
        private System.Windows.Forms.Button btnImportBalance;
        private System.Windows.Forms.Button btnAddTransaction;
    }
}
