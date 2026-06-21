# Helper Dialog Mockups

Small modal dialogs used as helpers within larger forms.

### FormAutoFill

Modal dialog for auto-filling delivery plan items. Launched from the Delivery Route form's
Plan tab. Lets the user select which request types to include and set a time horizon for
flatpack requests.

```
+--------------------------------------------------+
| Auto-Fill Delivery Plan                    [X]   |
+--------------------------------------------------+
| Select request types to auto-fill:               |
|                                                  |
|  [x] Commodities                                 |
|  [ ] Flatpacks                                   |
|  [ ] Resources for Manufacturing                 |
|  [ ] Workers                                     |
|                                                  |
|  Flatpack time horizon: [__0__] hours (0 = all)  |
|                                                  |
|              [  OK  ] [Cancel]                    |
+--------------------------------------------------+
```

Controls:
- `lblHeader` (Label) — Bold header text "Select request types to auto-fill:"
- `chkCommodities` (CheckBox) — Include commodity requests; checked by default
- `chkFlatpacks` (CheckBox) — Include flatpack requests
- `chkResources` (CheckBox) — Include resource-for-manufacturing requests
- `chkWorkers` (CheckBox) — Include worker requests
- `lblTimeHorizon` (Label) — "Flatpack time horizon:" label
- `txtTimeHorizon` (ValidatedTextBox) — Hours value for flatpack horizon; defaults to saved preference
- `lblTimeHorizonUnit` (Label) — "hours (0 = all)" unit hint
- `cmdOK` (Button) — Confirms selection (DialogResult.OK); saves time horizon to preferences
- `cmdCancel` (Button) — Cancels dialog (DialogResult.Cancel)

Behavior:
- On OK, saves `FlatpackTimeHorizonHours` to PreferencesStore
- Exposes `IncludeCommodities`, `IncludeFlatpacks`, `IncludeResources`, `IncludeWorkers` boolean properties
- Exposes `TimeHorizonHours` integer property (parsed from txtTimeHorizon)

---

### FormRecordSale

Modal dialog for recording a sale against an existing market listing. Launched from
FormMarket when the user selects a listing and clicks "Record Sale."

```
+--------------------------------------------------+
| Record Sale - Titanium Ore                 [X]   |
+--------------------------------------------------+
| Available: 500                                   |
| Condition: 100%                                  |
|                                                  |
| Quantity:             [__________]                |
| Price/Unit:           [__12.50___]                |
| Counterparty:         [______________________]   |
| Counterparty Faction: [______________________]   |
|                                                  |
|         [Record Sale] [Cancel]                   |
+--------------------------------------------------+
```

Controls:
- Info labels (dynamic) — "Available: {qty}" and "Condition: {hp%}" from the listing
- `txtQuantity` (TextBox) — Number of units sold; validated as positive integer on OK
- `txtPricePerUnit` (TextBox) — Price per unit; pre-filled from listing price; validated as decimal
- `txtCounterparty` (TextBox) — Name of the buyer (free text)
- `txtCounterpartyFaction` (TextBox) — Faction of the buyer (free text)
- `cmdOK` (Button) — "Record Sale"; validates inputs before closing
- `cmdCancel` (Button) — Cancels dialog

Behavior:
- Title bar shows "Record Sale - {ItemName}" from the listing
- Available quantity and condition are displayed as read-only info
- On OK click, validates quantity > 0 and price is valid decimal; shows MessageBox on failure
- Exposes `SaleQuantity`, `SalePricePerUnit`, `Counterparty`, `CounterpartyFaction` properties

---

### FormListingEdit

Modal dialog for creating or editing a market listing. Launched from FormMarket
when the user clicks "New Listing" or "Edit Listing."

```
+--------------------------------------------------+
| Edit Listing                               [X]   |
+--------------------------------------------------+
| Item Name:  [Titanium Ore________________]       |
| Type:       [Commodity             |v|]          |
| Station:    [Alpha Station         |v|]          |
| Quantity:   [__500___]                           |
| Price/Unit: [__12.50_]                           |
| Current HP: [__100___]                           |
| Max HP:     [__100___]                           |
|                                                  |
|              [ OK ] [Cancel]                     |
+--------------------------------------------------+
```

Controls:
- `txtItemName` (TextBox) — Item name; editable free text
- `cmbItemType` (ComboBox, DropDownList) — Item type enum (Commodity, Resource, Flatpack, etc.)
- `cmbStation` (ComboBox, DropDownList) — Station where listed; populated from player stations
- `txtQuantity` (TextBox) — Quantity available
- `txtPricePerUnit` (TextBox) — Price per unit
- `txtCurrentHP` (TextBox) — Current hit points (item condition)
- `txtMaxHP` (TextBox) — Maximum hit points
- `cmdOK` (Button) — Confirms edit; reads all fields into Edited* properties
- `cmdCancel` (Button) — Cancels dialog

Behavior:
- Title shows "Edit Listing" when editing, "New Listing" when creating
- Station dropdown includes "(None)" as first entry plus all player stations sorted
- When editing, all fields pre-populated from the ReadOnlyMarketListing
- When creating, defaults: name="New Listing", qty=1, price=0, HP=0
- On OK, populates `EditedItemName`, `EditedItemType`, `EditedStationUUID`, `EditedQuantity`, `EditedPricePerUnit`, `EditedCurrentHP`, `EditedMaxHP`

---

### FormStructureAllocation

Modal dialog for allocating a build plan item to a colony structure. Launched from
FormBuildPlanner when the user clicks "Allocate" on a build item row.

```
+--------------------------------------------------------------+
| Allocate Structure                                     [X]   |
+--------------------------------------------------------------+
| Item: Titanium Plating (3 runs)                              |
|                                                              |
| Filter: [______________]  [x] Show idle only                 |
|                                                              |
| +------------+----------------+------------+--------+        |
| | Colony     | Structure      | Type       | Status |        |
| +------------+----------------+------------+--------+        |
| | Alpha Base | Manufactory #1 | Manufactory| Idle   |        |
| | Alpha Base | Manufactory #2 | Manufactory| Busy   |        |
| | Beta Outpo | Manufactory #3 | Manufactory| Idle   |        |
| +------------+----------------+------------+--------+        |
|                                                              |
| Ship and station build locations will be supported in a      |
| future update.                                               |
|                                                              |
|                              [Cancel] [Allocate]             |
+--------------------------------------------------------------+
```

Controls:
- `lblItemInfo` (Label, bold) — "Item: {name} ({qty} runs)" identifying the build item
- `lblFilter` (Label) — "Filter:" label
- `txtFilter` (ValidatedTextBox) — Text filter; filters grid by colony or structure name
- `chkIdleOnly` (CheckBox) — "Show idle only"; checked by default; hides busy structures
- `dgvStructures` (DataGridView, read-only, single-select, full-row) — Lists eligible structures
  - `colColony` (TextBoxColumn, 140px) — Colony name
  - `colStructure` (TextBoxColumn, 140px) — Structure name (blueprint output or name)
  - `colType` (TextBoxColumn, 80px) — Type label (Manufactory, Commodity, Mining Rig, etc.)
  - `colStatus` (TextBoxColumn, 80px) — "Idle" or "Busy"
- `lblFutureNote` (Label, gray) — Note about future ship/station support
- `cmdAllocate` (Button) — Allocates selected structure; validates selection first
- `cmdCancel` (Button) — Cancels dialog

Behavior:
- Grid shows only structures eligible for the build item's type (Manufactory items -> Manufactory structures, Commodity items -> Commodity Factory structures, etc.)
- Filter textbox and idle-only checkbox trigger immediate grid refresh
- Double-clicking a row is equivalent to clicking Allocate
- On allocate with no selection, shows warning MessageBox
- Exposes `SelectedColonyUUID` and `SelectedStructureUUID` after successful allocation
- Rows sorted by colony name then structure name

---

### FormAbout

Modal dialog displaying application information. Launched from Help → About menu item.

```
+--------------------------------------------------+
| About                                      [X]   |
+--------------------------------------------------+
| OE2 Empire Tracker                (bold, 12pt)   |
| Version 1.0.0.0                                  |
| Copyright © 2024                                 |
|                                                  |
| Empire Tracking Tool for Outer Empires 2         |
| https://outerempires.net/                (link)  |
|                                                  |
| Designed by Scalorn Scorpus                      |
|                                                  |
| Developed and maintained via a specification     |
| development process by Kiro                      |
| https://kiro.dev/                        (link)  |
|                                                  |
| GitHub project at                                |
| https://github.com/scalorn/...           (link)  |
|                                                  |
|                   [ OK ]                         |
+--------------------------------------------------+
```

Controls:
- `lblAppName` (Label, Bold 12pt) — Application name "OE2 Empire Tracker"
- `lblVersion` (Label) — Assembly version string
- `lblCopyright` (Label) — Copyright notice
- `lblDescription` (Label) — "Empire Tracking Tool for Outer Empires 2"
- `lnkGame` (LinkLabel) — Game website URL; opens in default browser on click
- `lblDesigner` (Label) — Designer credit with email
- `lblDeveloped` (Label) — Development process attribution
- `lnkKiro` (LinkLabel) — Kiro website URL; opens in default browser on click
- `lblGitHub` (Label) — "GitHub project at" prefix text
- `lnkGitHub` (LinkLabel) — GitHub repository URL; opens in default browser on click
- `btnOK` (Button) — Closes the dialog (DialogResult.OK, AcceptButton)

Behavior:
- Fixed-size dialog (450×315), non-resizable (FixedDialog border style)
- No minimize/maximize buttons
- Centered on parent window
- All link labels open URLs via Process.Start on LinkClicked

---

### FormHelp

Non-modal dialog displaying in-app help documentation rendered from markdown files. Launched from Help → Contents (Ctrl+F1) or F1 context-sensitive help.

```
+------------------------------------------------------------------+
| Help                                                       [X]   |
+------------------------------------------------------------------+
| +---Topics---+  +---Content----------------------------+         |
| | ▸ Getting  |  |                                      |         |
| |   Started  |  |  # Getting Started                   |         |
| | ▸ Colonies |  |                                      |         |
| | ▸ Colony   |  |  Welcome to OE2 Empire Tracker...    |         |
| |   Activity |  |                                      |         |
| | ▸ Bluepri..|  |  ## Creating a Player Profile        |         |
| | ▸ Surveys  |  |                                      |         |
| | ▸ Delivery |  |  To get started, open the Player     |         |
| |   Routes   |  |  Profile form from the Manage menu.  |         |
| | ▸ Ships    |  |                                      |         |
| | ▸ Stations |  |  ...                                 |         |
| | ▸ Market   |  |                                      |         |
| +------------+  +--------------------------------------+         |
+------------------------------------------------------------------+
```

Controls:
- `splitContainer` (SplitContainer, Dock.Fill) — Divides form into topic tree and content panel
  - Panel1 (250px default):
    - `treeViewTopics` (TreeView, Dock.Fill, HideSelection=false) — Hierarchical topic list populated from docs/ folder
  - Panel2:
    - `webBrowser` (WebBrowser, Dock.Fill, AllowNavigation=true) — Renders markdown-to-HTML content via HelpRenderer

Behavior:
- 900×600 default size, centered on parent, resizable
- TreeView populated on load from HelpTopicRegistry (all docs/*.md files)
- Selecting a tree node loads the corresponding markdown file, renders via Markdig to HTML, and displays in WebBrowser
- Internal links (href to other .md files) are intercepted via Navigating event and load the target topic instead of navigating away
- F1 from any form navigates directly to that form's mapped topic via HelpTopicRegistry
- Context-sensitive: if opened with a specific topic, selects that node and displays its content immediately
