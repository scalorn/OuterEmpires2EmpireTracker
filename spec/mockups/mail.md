# Mail Mockups

### FormMail

MDI child form. Filter bar at top, split layout with mail list (left) and detail panel (right), status strip at bottom.

```
+---------------------------------------------------------------------------------+
| Mail (3 unread)                                                  [_][box][X]    |
+---------------------------------------------------------------------------------+
| [All      v] [All Types   v] [Search________________]         3 messages       |
+---------------------------------------------------------------------------------+
| +--- Mail List (left) ---+  +--- Detail Panel (right) ----------------------+ |
| |                         |  | From: System                                  | |
| | From     | Subject | Dt |  | Date: 2024-01-31 14:30                        | |
| |----------+---------+----|  | Subject: Colony income deposited              | |
| | **System** | **Colony** | **01** |  |                                                | |
| | System   | Research | 01 |  | Your colony Alpha has generated 5,000 credits | |
| | Alice    | Trade of | 01 |  | of income from worker production this cycle.  | |
| |          |         |    |  |                                                | |
| |          |         |    |  |                                                | |
| |          |         |    |  |                                                | |
| +-------------------------+  +------------------------------------------------+ |
+---------------------------------------------------------------------------------+
| Last sync: 2024-01-31 14:35                                                     |
+---------------------------------------------------------------------------------+
```

### Control List — FormMail

| Control | Type | Purpose |
|---------|------|---------|
| pnlFilter | Panel (Dock=Top) | Filter controls container |
| cboReadFilter | ComboBox (DropDownList) | Read status filter (All/Unread/Read) |
| cboTypeFilter | ComboBox (DropDownList) | Mail type filter (All/Player-System/Colony/Research/Skill) |
| txtSearch | TextBox (anchored) | Text search across From/Subject/Content |
| lblMessageCount | Label (right-aligned) | Displays filtered message count |
| splitContainer | SplitContainer | Divides list and detail panels |
| lvwMail | ListView (Details view) | Mail message list |
| colFrom | ColumnHeader | Sender name column |
| colSubject | ColumnHeader | Message subject column |
| colDate | ColumnHeader | Sent date column |
| lblDetailFrom | Label | "From:" header in detail panel |
| lblDetailDate | Label | "Date:" header in detail panel |
| lblDetailSubject | Label | "Subject:" header in detail panel |
| txtDetailContent | TextBox (Multiline, ReadOnly) | Full message content display |
| statusStrip | StatusStrip | Bottom status bar |
| lblSyncStatus | ToolStripStatusLabel | Last sync time / sync progress |
