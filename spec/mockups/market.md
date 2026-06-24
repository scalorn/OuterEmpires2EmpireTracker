<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Market Mockups

### FormMarket (Iteration 5)

MDI child form. Tabbed layout (no left-list â€” listings and transactions are in separate tabs).

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ #1 - Market                                                             [_][â–¡][X]â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ â”Œâ”€ Listings â”€â”¬â”€ Transactions â”€â”¬â”€ Summary â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ Station: [Filter:____] [Station Alpha              â–¼]                    â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”            â”‚   â”‚
â”‚ â”‚ â”‚ Type     â”‚ Item             â”‚ Qty â”‚ Price/Unit â”‚ Station  â”‚            â”‚   â”‚
â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤            â”‚   â”‚
â”‚ â”‚ â”‚ Commodty â”‚ Reactor Mk3      â”‚  25 â”‚    12,500  â”‚ Stn Alphaâ”‚            â”‚   â”‚
â”‚ â”‚ â”‚ Commodty â”‚ Drive Mk3        â”‚  15 â”‚     8,200  â”‚ Stn Alphaâ”‚            â”‚   â”‚
â”‚ â”‚ â”‚ Blueprnt â”‚ Hull Clipper Mk3 â”‚   5 â”‚    45,000  â”‚ Stn Alphaâ”‚            â”‚   â”‚
â”‚ â”‚ â”‚ Resource â”‚ Refined Titanium â”‚ 500 â”‚       120  â”‚ Outpost Bâ”‚            â”‚   â”‚
â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜            â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ Add Listing:                                                             â”‚   â”‚
â”‚ â”‚ Type:[Commodityâ–¼] Filter:[______] Item:[Reactor Mk3 â–¼]                  â”‚   â”‚
â”‚ â”‚ Qty:[25] Price/Unit:[12500] Station:[Station Alpha â–¼]                   â”‚   â”‚
â”‚ â”‚ [Add Listing]                                                            â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ [Record Sale] [Edit] [Delete]                                            â”‚   â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                                                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ [Save]                                                                          â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Transactions tab:

```
â”‚ â”Œâ”€ Listings â”€â”¬â”€ Transactions â”€â”¬â”€ Summary â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ Filters: Type:[All    â–¼] Item:[________] Counterparty:[________]        â”‚   â”‚
â”‚ â”‚          Station:[All â–¼] Faction:[All â–¼] From:[________] To:[________]  â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”â”‚   â”‚
â”‚ â”‚ â”‚ Type â”‚ TxnType â”‚ Item         â”‚ Qty â”‚ Price  â”‚ Total  â”‚Ctrparty â”‚Cond â”‚â”‚   â”‚
â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”¤â”‚   â”‚
â”‚ â”‚ â”‚ Part â”‚ Sell    â”‚ Reactor Mk3  â”‚   1 â”‚ 12,500 â”‚ 12,500 â”‚ Bob     â”‚ 95% â”‚â”‚   â”‚
â”‚ â”‚ â”‚ Comm â”‚ Sell    â”‚ Reactor Mk3  â”‚   5 â”‚ 12,500 â”‚ 62,500 â”‚ Bob     â”‚     â”‚â”‚   â”‚
â”‚ â”‚ â”‚ Res  â”‚ Buy     â”‚ Ref Titanium â”‚ 200 â”‚    120 â”‚ 24,000 â”‚ Alice   â”‚     â”‚â”‚   â”‚
â”‚ â”‚ â”‚ Comm â”‚ Sell    â”‚ Drive Mk3    â”‚   3 â”‚  8,200 â”‚ 24,600 â”‚ Charlie â”‚     â”‚â”‚   â”‚
â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”˜â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ [Add Transaction] [Edit] [Delete]                                        â”‚   â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
```

Summary tab:

```
â”‚ â”Œâ”€ Listings â”€â”¬â”€ Transactions â”€â”¬â”€ Summary â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ Pricing Plan: [Filter:____] [Standard Pricing Plan           â–¼]         â”‚   â”‚
â”‚ â”‚ Date Range:   From:[________] To:[________]                             â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”                            â”‚   â”‚
â”‚ â”‚ â”‚ Total Sales              â”‚    111,100 cr   â”‚                            â”‚   â”‚
â”‚ â”‚ â”‚ Total Purchases          â”‚     24,000 cr   â”‚                            â”‚   â”‚
â”‚ â”‚ â”‚ Net Profit/Loss          â”‚  +  87,100 cr   â”‚                            â”‚   â”‚
â”‚ â”‚ â”‚ Plan Valuation (Sales)   â”‚     98,000 cr   â”‚                            â”‚   â”‚
â”‚ â”‚ â”‚ Margin vs Plan           â”‚  +  13,100 cr   â”‚                            â”‚   â”‚
â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜                            â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ Per-Item Breakdown:                                                      â”‚   â”‚
â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”        â”‚   â”‚
â”‚ â”‚ â”‚ Item         â”‚ Sold â”‚ Revenue  â”‚PlanValue â”‚ Margin   â”‚ Bought â”‚        â”‚   â”‚
â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¤        â”‚   â”‚
â”‚ â”‚ â”‚ Reactor Mk3  â”‚    5 â”‚   62,500 â”‚   55,000 â”‚  + 7,500 â”‚      0 â”‚        â”‚   â”‚
â”‚ â”‚ â”‚ Drive Mk3    â”‚    3 â”‚   24,600 â”‚   21,000 â”‚  + 3,600 â”‚      0 â”‚        â”‚   â”‚
â”‚ â”‚ â”‚ Ref Titanium â”‚    0 â”‚        0 â”‚        0 â”‚        0 â”‚    200 â”‚        â”‚   â”‚
â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”˜        â”‚   â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
```

Controls:
- `tabMarket` (TabControl with Listings, Transactions, Summary tabs)
- Listings tab: station filter, `dgvListings` (DataGridView, editable qty/price), add-listing panel, `cmdListingAdd`, `cmdRecordSale` / `cmdListingEdit` / `cmdListingDelete`
- Transactions tab: filter row (type, item, counterparty, faction, station, date range), `dgvTransactions` (DataGridView with Condition column â€” shows percentage when non-zero), `cmbTxType`, `txtTxItem`, `txtTxCounterparty`, `txtTxFaction`, `cmbTxStation`, `cmdTxApply`. Faction filter matches against the `CounterpartyFaction` snapshot field on each transaction (not the counterparty's current faction).
- Summary tab: `cmbPricingPlan` (FilteredComboBox), date range, `cmbSumStation`, `cmdCompute`, `dgvSummary` (read-only DataGridView)
- "Record Sale" opens a dialog to enter sale details (quantity, counterparty, notes) and auto-creates the transaction + decrements listing

### FormMarket — Saved Searches Tab

```
│ ┌─ Listings ─┬─ Transactions ─┬─ Summary ─┬─ Saved Searches ─┬─ ...    │
│ │                                                                      │
│ │ Character:[All chars ▼] Name:[________] Type:[All  ▼]              │
│ │ Item:[All items ▼] Purity:[All ▼] Order Type:[All ▼]              │
│ │ Range:[150 ↕] [✓]RunOnSync [Test Search]                            │
│ │                                                                      │
│ │ ┌──────────────┬──────────┬─────────┐                                │
│ │ │ Name         │ Type     │ Enabled │                                │
│ │ ├──────────────┼──────────┼─────────┤  dgvSavedSearches              │
│ │ │ All Ores     │ Resource │   ✓     │                                │
│ │ │ Steel Buy    │ Commodity│   ✓     │                                │
│ │ └──────────────┴──────────┴─────────┘                                │
│ │                                                                      │
│ │ ┌──────────────┬────────┬─────┬──────────┬──────────┐               │
│ │ │ Item         │ Price  │ Qty │ Seller   │ Station  │               │
│ │ ├──────────────┼────────┼─────┼──────────┼──────────┤ dgvTestResults│
│ │ │ Iron (High)  │  12.50 │ 500 │ Bob      │ Stn A   │               │
│ │ └──────────────┴────────┴─────┴──────────┴──────────┘               │
│ │                                                                      │
│ │ [Save] [Delete]                                                      │
│ └──────────────────────────────────────────────────────────────────────┘
```

Controls:
- `cmbSearchCharacter` — character selector dropdown
- `dgvSavedSearches` — list of saved searches (Name, Type, Enabled columns)
- `txtSearchName` — search name input
- `cmbSearchType` — item type filter
- `cmbSearchItem` — specific item filter
- `cmbSearchPurity` — purity filter
- `cmbSearchOrderType` — buy/sell/all filter
- `lblRange` — "Range:" label
- `nudRange` — trade-network range (0–500, default 150)
- `chkRunOnSync` — run on sync checkbox
- `cmdTestSearch` — test search button (calls real Game API)
- `cmdSaveSearch` — save search button
- `cmdDeleteSearch` — delete search button
- `dgvTestResults` — test results grid

### FormMarket — My Orders Tab

```
│ ┌─ ... ─┬─ My Orders ─┬─ ...                                          │
│ │                                                                      │
│ │ Character:[All ▼] Type:[All ▼] Search:[________] Location:[All ▼]  │
│ │                                                                      │
│ │ Sell Orders                                                          │
│ │ ┌──────────────┬──────┬────────┬──────────┬────────┐                │
│ │ │ Item         │ Qty  │ Price  │ Station  │ Status │                │
│ │ ├──────────────┼──────┼────────┼──────────┼────────┤ dgvSellOrders │
│ │ │ Steel        │  500 │  45.00 │ Stn A    │ Active │                │
│ │ └──────────────┴──────┴────────┴──────────┴────────┘                │
│ │                                                                      │
│ │ Buy Orders                                                           │
│ │ ┌──────────────┬──────┬────────┬──────────┬────────┐                │
│ │ │ Item         │ Qty  │ Price  │ Station  │ Status │                │
│ │ ├──────────────┼──────┼────────┼──────────┼────────┤ dgvBuyOrders  │
│ │ │ Titanium     │ 1000 │  10.00 │ Stn B    │ Active │                │
│ │ └──────────────┴──────┴────────┴──────────┴────────┘                │
│ └──────────────────────────────────────────────────────────────────────┘
```

Controls:
- `cmbOrderCharacter` — character selector
- `cmbOrderType` — item type filter
- `txtOrderSearch` — text search filter
- `cmbOrderLocation` — location filter
- `dgvSellOrders` — sell orders grid (Item, Qty, Price, Station, Status)
- `dgvBuyOrders` — buy orders grid (Item, Qty, Price, Station, Status)

### FormMarket — Prices Tab

```
│ ┌─ ... ─┬─ Prices ─┬─ ...                                             │
│ │                                                                      │
│ │ Type:[All ▼] Item:[All ▼] Purity:[All ▼] Days:[30] [✓]BuyOrders  │
│ │ [Fetch]                                                              │
│ │                                                                      │
│ │ ┌──────────────┬────────┬────────┬────────┬─────────┬─────────┐    │
│ │ │ Item         │ Low    │ Avg    │ High   │ Samples │ Fetched │    │
│ │ ├──────────────┼────────┼────────┼────────┼─────────┼─────────┤    │
│ │ │ Iron (High)  │  10.00 │  12.50 │  15.00 │      42 │ 2025-01 │    │
│ │ └──────────────┴────────┴────────┴────────┴─────────┴─────────┘    │
│ │                                                       dgvPriceStats │
│ │ Plan:[Standard Pricing ▼] [Auto-Populate]                          │
│ └──────────────────────────────────────────────────────────────────────┘
```

Controls:
- `cmbPriceType` — item type filter
- `cmbPriceItem` — specific item filter
- `cmbPricePurity` — purity filter
- `numDaysBack` — days back numeric input
- `chkBuyOrderPrices` — include buy orders checkbox
- `cmdFetchPrices` — fetch prices button
- `dgvPriceStats` — price stats grid (Item, Low, Avg, High, Samples, Fetched)
- `cmdAutoPopulate` — auto-populate pricing plan button
- `cmbAutoPopPlan` — pricing plan selector

### FormMarket — Alerts Tab

```
│ ┌─ ... ─┬─ Alerts ─┬─ ...                                             │
│ │                                                                      │
│ │ Name:[________] Alert Type:[Sell Appears ▼] Item Type:[All ▼]      │
│ │ Item:[All ▼] Purity:[All ▼] Condition:[Any ▼] Threshold:[______]  │
│ │ Location:[All ▼] [✓]Enabled                                        │
│ │                                                                      │
│ │ ┌──────────┬──────────┬──────────┬───────────┬─────────┬──────────┐│
│ │ │ Name     │ Type     │ Item     │ Condition │ Enabled │ LastTrig ││
│ │ ├──────────┼──────────┼──────────┼───────────┼─────────┼──────────┤│
│ │ │ Cheap Ore│ Sell App │ Iron     │ ≤ 10.00   │   ✓     │ Jan 15   ││
│ │ └──────────┴──────────┴──────────┴───────────┴─────────┴──────────┘│
│ │                                                        dgvAlerts    │
│ │ [Add] [Edit] [Delete]                                               │
│ └──────────────────────────────────────────────────────────────────────┘
```

Controls:
- `dgvAlerts` — alerts grid (Name, Type, Item, Condition, Enabled, LastTriggered)
- `txtAlertName` — alert name input
- `cmbAlertType` — alert type selector (SellOrderAppears/BuyOrderAppears)
- `cmbAlertItemType` — item type filter
- `cmbAlertItem` — specific item filter
- `cmbAlertPurity` — purity filter
- `cmbPriceCondition` — price condition selector
- `txtPriceThreshold` — price threshold input
- `cmbAlertLocation` — location filter
- `cmdAddAlert` — add alert button
- `cmdEditAlert` — edit alert button
- `cmdDeleteAlert` — delete alert button
- `chkAlertEnabled` — enabled checkbox

### FormListingEdit

`FormListingEdit` is a modal dialog for editing an existing market listing's quantity, price, and station assignment.

### FormRecordSale

`FormRecordSale` is a modal dialog for recording a sale against a listing — captures quantity sold, counterparty, price, and optional notes, then creates a MarketTransaction and decrements the listing quantity.

