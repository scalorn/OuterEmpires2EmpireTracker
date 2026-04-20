# Pricing Plan Mockups

### FormPricingPlan

MDI child form. Left-list / right-detail with resource price grid and time cost fields.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Pricing Plans                                                           [_][□][X]│
├──────────────────────┬──────────────────────────────────────────────────────────────┤
│ Filter: [__________] │ Name: [Standard Rates_________]                             │
│                      │ Description: [Default pricing for faction orders___________] │
│ ┌──────────────────┐ │                                                             │
│ │▸ Standard Rates  │ │ Fixed Cost/Item: [500_____]  Hourly Rate: [150_____]        │
│ │  Premium Rates   │ │ [Save]                                                      │
│ │  Bulk Discount   │ │                                                             │
│ │                  │ │ ┌──────────────────────┬──────────┬────────────┐             │
│ │                  │ │ │ Resource             │ Purity   │ Price      │             │
│ │                  │ │ ├──────────────────────┼──────────┼────────────┤             │
│ │                  │ │ │ Iron                 │ High     │       120  │             │
│ │                  │ │ │ Iron                 │ Medium   │        80  │             │
│ │                  │ │ │ Iron                 │ Low      │        40  │             │
│ │                  │ │ │ Copper               │ High     │       200  │             │
│ │                  │ │ │ Copper               │ Medium   │       140  │             │
│ │                  │ │ │ Titanium             │ High     │       350  │             │
│ │                  │ │ │ Refined Iron         │ High     │       500  │             │
│ │                  │ │ │ Refined Iron         │ Medium   │       350  │             │
│ │                  │ │ │ ...                  │          │            │             │
│ │                  │ │ └──────────────────────┴──────────┴────────────┘             │
│ └──────────────────┘ │                                                             │
│ [New] [Delete]       │                                                             │
└──────────────────────┴──────────────────────────────────────────────────────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- Left: `flpSearchList` (top-down, 220px wide, WrapContents=false):
  - `txtPlanFilter` (ValidatedTextBox) — filters plan list
  - `lvwPlans` (ListView, full-row select, Details view)
  - `flpCommands`: `cmdNew`, `cmdDelete`
- Right: `flpDetail` (top-down, WrapContents=false):
  - `txtPlanName` (ValidatedTextBox) — plan name
  - `txtDescription` (ValidatedTextBox) — plan description
  - `txtFixedCost` (ValidatedTextBox) — fixed cost per item
  - `txtHourlyCost` (ValidatedTextBox) — hourly rate for time-based pricing
  - `cmdSave` (Button)
  - `dgvResourcePrices` (DataGridView, full-row select) — resource price grid:
    - `colResourceName` (TextBox, read-only, 220px) — resource name
    - `colPurity` (TextBox, read-only, 80px) — purity level
    - `colPrice` (TextBox, editable, 120px) — price per unit
  - Computed prices for commodities and blueprints are derived from resource prices + time costs

Satisfies: REQ-PRC-010 (pricing plan management), REQ-PRC-020 (resource pricing)
