# Banking Mockups

### FormBanking

MDI child form. Balance header, period/type/detail filters, date range pickers, tabbed content (Transactions + Charts), and bottom summary panel.

```
+---------------------------------------------------------------------------------+
| Banking                                                          [_][box][X]    |
+---------------------------------------------------------------------------------+
| Balance: 1,234,567.89                                                           |
|                                                                                 |
| [24h] [7d] [30d] [All]  Type:[All          v]  [Add Transaction]               |
|                          Detail:[______________]                                |
| From:[x][2024-01-01]  To:[x][2024-01-31]                                       |
|                                                                                 |
| +-- Transactions --+-- Charts --+                                               |
| |                                                                             | |
| | +----------+----------+------------------+-----------+-----------+---------+| |
| | | Date     | Type     | Detail           | Cr Change | Old Bal   | New Bal || |
| | +----------+----------+------------------+-----------+-----------+---------+| |
| | | 01-31 14 | Worker W | Worker wages for  | -1,200.00 | 5,000.00  | 3,800  || |
| | | 01-31 10 | Market S | Sold Reactor Mk3  | 12,500.00 | 3,800.00  | 16,300 || |
| | | 01-30 22 | Refuelin | Refueled at Stn A | -500.00   | 16,300.00 | 15,800 || |
| | +----------+----------+------------------+-----------+-----------+---------+| |
| |                                                                             | |
| +-----------------------------------------------------------------------------+ |
|                                                                                 |
| Income: 12,500.00    Expenses: 1,700.00    Net: 10,800.00                       |
+---------------------------------------------------------------------------------+
```

Charts tab:

```
| +-- Transactions --+-- Charts --+                                               |
| |                                                                             | |
| | (o) Hourly  (o) Daily   [x] Net Change  [x] Cumulative                     | |
| |                                                                             | |
| | +--- Cash Flow --------------------------------------------------------+   | |
| | |  Credits ^                                                            |   | |
| | |          | ___/\___/\___                                              |   | |
| | |          |/              \___                                          |   | |
| | |          +-----------------------------------------------------> Time |   | |
| | +-------------------------------------------------------------------+   | |
| |                                                                             | |
| | +--- Income vs Expenses -------+  +--- Type Breakdown ---------------+ | |
| | | Credits ^                     |  |         [Pie chart]              | | |
| | |     |  __|__                  |  |   Worker Wages: 45%             | | |
| | |     | |  |  |                 |  |   Market Sales: 30%             | | |
| | |     +-+--+--+---> Time       |  |   Refueling: 25%                | | |
| | +-------------------------------+  +----------------------------------+ | |
| +-----------------------------------------------------------------------------+ |
```

### FormBankingEntry

Modal dialog for manual transaction entry. Fixed-size, centered on parent.

```
+----------------------------------------------+
| Add Banking Transaction              [X]     |
+----------------------------------------------+
| Date/Time:    [2024-01-31 14:30         ]    |
|                                              |
| Credit Change:[-1200.00                 ]    |
|                                              |
| Type:         [Worker Wages            v]    |
|                                              |
| Detail:       [Worker wages for colony  ]    |
|               [Alpha sector             ]    |
|               [                         ]    |
|                                              |
|                                              |
|                         [  OK  ] [ Cancel ]  |
+----------------------------------------------+
```

### Control List — FormBanking

| Control | Type | Purpose |
|---------|------|---------|
| lblBalance | Label (bold, 12pt) | Displays current bank balance |
| btn24h | Button | Period filter: last 24 hours |
| btn7d | Button | Period filter: last 7 days |
| btn30d | Button | Period filter: last 30 days |
| btnAllTime | Button (bold) | Period filter: all time |
| lblType | Label | "Type:" caption |
| cboType | ComboBox (DropDownList) | Transaction type filter |
| lblDetail | Label | "Detail:" caption |
| txtDetailFilter | TextBox | Detail text filter |
| lblFrom | Label | "From:" caption |
| dtpFrom | DateTimePicker (ShowCheckBox) | Date range start |
| lblTo | Label | "To:" caption |
| dtpTo | DateTimePicker (ShowCheckBox) | Date range end |
| tabMain | TabControl | Transactions / Charts tabs |
| dgvTransactions | DataGridView (read-only) | Transaction list |
| colDate | DataGridViewTextBoxColumn | Transaction date |
| colType | DataGridViewTextBoxColumn | Transaction type label |
| colDetail | DataGridViewTextBoxColumn | Transaction detail |
| colCreditChange | DataGridViewTextBoxColumn | Credit change amount |
| colOldBalance | DataGridViewTextBoxColumn | Balance before |
| colNewBalance | DataGridViewTextBoxColumn | Balance after |
| pnlChartGrouping | Panel | Chart grouping controls |
| btnGroupHourly | RadioButton | Hourly chart grouping |
| btnGroupDaily | RadioButton (checked) | Daily chart grouping |
| chkNetChange | CheckBox (checked) | Toggle net change series |
| chkCumulative | CheckBox (checked) | Toggle cumulative series |
| chartCashFlow | Chart | Cash flow over time |
| chartIncomeExpenses | Chart | Income vs expenses bars |
| chartTypeBreakdown | Chart | Pie chart by type |
| btnAddTransaction | Button | Opens FormBankingEntry |
| pnlSummary | Panel (Dock=Bottom) | Summary totals |
| lblIncome | Label (bold) | Total income display |
| lblExpenses | Label (bold) | Total expenses display |
| lblNet | Label (bold) | Net change display |

### Control List — FormBankingEntry

| Control | Type | Purpose |
|---------|------|---------|
| lblDateTime | Label | "Date/Time:" caption |
| dtpDateTime | DateTimePicker (Custom format) | Transaction date/time |
| lblCreditChange | Label | "Credit Change:" caption |
| txtCreditChange | TextBox | Amount input |
| lblType | Label | "Type:" caption |
| cboType | ComboBox (DropDownList) | Transaction type selection |
| lblDetail | Label | "Detail:" caption |
| txtDetail | TextBox (Multiline, 500 max) | Detail text input |
| lblError | Label (Red) | Validation error message |
| btnOK | Button (AcceptButton) | Confirm entry |
| btnCancel | Button (CancelButton) | Cancel entry |
