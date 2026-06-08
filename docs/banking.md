# Banking

The Banking form tracks all credit movements in your game account — worker wages, market sales and purchases, colony income, refueling costs, and other transactions. Data synchronizes automatically from the game API in the background.

## Opening the Form

Open **View → Banking** from the main menu. The form loads your transaction history and current balance.

## Balance Display

Your current bank balance is shown in bold at the top of the form. This updates automatically when background sync imports new data from the game API.

## Filtering Transactions

### Period Filters

Click the period buttons to quickly filter by time range:

- **24h** — Last 24 hours
- **7d** — Last 7 days
- **30d** — Last 30 days
- **All** — All transactions (default)

### Type Filter

Select a transaction type from the Type dropdown to show only that category (e.g., "Worker Wages", "Market Sale"). Select "All" to show everything.

### Detail Filter

Type text in the Detail field to filter transactions by description (substring match).

### Date Range

Check the From/To date pickers and set specific dates for custom date ranges. Unchecked pickers apply no date constraint.

All filters combine with AND logic — a transaction must match all active filters to appear.

## Transactions Tab

Displays all matching transactions in a grid with columns:

- **Date** — Transaction timestamp in local time
- **Type** — Human-readable transaction type label
- **Detail** — Description from the game
- **Credit Change** — Amount (positive = income, negative = expense)
- **Old Balance** — Balance before the transaction
- **New Balance** — Balance after the transaction

Transactions are sorted newest-first by default.

## Charts Tab

Visual analytics for your financial data:

- **Cash Flow** — Line chart showing balance over time
- **Income vs Expenses** — Bar chart comparing income and expenses per period
- **Type Breakdown** — Pie chart showing spending by transaction type

Use the grouping controls (Hourly/Daily) and series toggles (Net Change/Cumulative) to customize the view.

## Summary Panel

The bottom panel shows totals for the current filtered set:

- **Income** — Sum of all positive credit changes
- **Expenses** — Sum of all negative credit changes (shown as positive number)
- **Net** — Income minus Expenses

## Adding Manual Transactions

Click **Add Transaction** to open the entry dialog. Fill in:

- **Date/Time** — When the transaction occurred (defaults to now)
- **Credit Change** — Amount (must be non-zero)
- **Type** — Select from known transaction types
- **Detail** — Description (required, max 500 characters)

Click OK to save. The transaction is added to your local history.

## Background Sync

Banking transactions sync automatically via the background scheduler alongside your other game data (profile, colonies, assets). No manual import is required. The form refreshes automatically when new data arrives.

## Error Handling

- If the game API is unreachable, previously imported data remains available
- Sync errors are logged and retried on the next scheduler cycle
- Manual entries are always stored locally regardless of API connectivity
