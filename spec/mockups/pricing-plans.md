# Pricing Plan Mockups

### FormPricingPlan

MDI child form. Left-list / right-detail with resource price grid and command buttons below the grid.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Pricing Plans                                                           [_][□][X]│
├──────────────────────┬──────────────────────────────────────────────────────────────┤
│ Filter: [__________] │ Name: [Standard Rates_________]                             │
│                      │ Description: [Default pricing for faction orders___________] │
│ ┌──────────────────┐ │                                                             │
│ │▸ Standard Rates  │ │ Fixed Cost/Item: [500_____]  Hourly Rate: [150_____]        │
│ │  Premium Rates   │ │                                                             │
│ │  Bulk Discount   │ │ ┌──────────────────────┬──────────┬────────────┐             │
│ │                  │ │ │ Resource             │ Purity   │ Price      │             │
│ │                  │ │ ├──────────────────────┼──────────┼────────────┤             │
│ │                  │ │ │ Iron                 │ Refined  │       120  │             │
│ │                  │ │ │ Copper               │ Refined  │       200  │             │
│ │                  │ │ │ Titanium             │ Refined  │       350  │             │
│ │                  │ │ │ S1. Translanthanic   │ S1       │       500  │             │
│ │                  │ │ │ S2. Element 126      │ S2       │       800  │             │
│ │                  │ │ │ ...                  │          │            │             │
│ │                  │ │ └──────────────────────┴──────────┴────────────┘             │
│ │                  │ │ [New] [Save] [Delete]                                       │
│ │                  │ │                                                             │
│ └──────────────────┘ │                                                             │
└──────────────────────┴──────────────────────────────────────────────────────────────┘
```

Context menu (right-click on dgvResourcePrices):
```
┌─────────────┐
│ Clear Price │
└─────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- Left: `flpSearchList` (top-down, 220px wide, WrapContents=false):
  - `flpPlanFilter`: `lblPlanFilter`, `txtPlanFilter` (ValidatedTextBox) — filters plan list
  - `lvwPlans` (ListView, full-row select, Details view)
- Right: `flpDetail` (top-down, WrapContents=false):
  - `flpPlanName`: `lblPlanName`, `txtPlanName` (ValidatedTextBox) — plan name
  - `flpDescription`: `lblDescription`, `txtDescription` (ValidatedTextBox) — plan description
  - `flpCosts`: `lblFixedCost`, `txtFixedCost`, `lblHourlyCost`, `txtHourlyCost` — cost fields
  - `dgvResourcePrices` (DataEntryGridView, full-row select, context menu: cmsResourcePrices):
    - `colResourceName` (TextBox, read-only, 220px) — resource name
    - `colPurity` (TextBox, read-only, 80px) — purity level
    - `colPrice` (TextBox, editable, 120px) — price per unit
  - `flpCommands`: `cmdNew`, `cmdSave`, `cmdDelete`
- Context menus:
  - `cmsResourcePrices`: `tsmiClearPrice` — clears the price for the selected resource row

Satisfies: REQ-PRC-010 (pricing plan management), REQ-PRC-020 (resource pricing), REQ-PRC-022 (clear price removes entry)
