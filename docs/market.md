# Market

The Market form tracks your trading activity — what you have listed for sale, what you've bought and sold, and whether you're actually making money.

## Opening the Form

Open from **Manage → Market**.

## Listings Tab

The Listings tab shows everything you currently have for sale across all stations.

### Creating a Listing

1. Select a **Station** where the item is listed.
2. Pick the **Item** from the dropdown (filter by type to narrow things down).
3. Set the **Quantity** and **Price**.
4. For damaged items, set the **Condition** and **Max Repair** fields — buyers care about this stuff.
5. Click **Add** to create the listing.

### Recording a Sale

Click **Record Sale** on a listing to log the transaction. Here's what happens:

- The listing quantity decrements by the sale amount.
- A transaction record is created with the item, price, quantity, and a snapshot of the condition at time of sale.
- If the buyer's faction is set, it's captured in the transaction too.
- When quantity hits zero, the listing is removed.

## Transactions Tab

The Transactions tab gives you a filterable history of all your buys and sells.

### Filters

Narrow down your transaction history with:

- **Type** — Buy or Sell
- **Item** — specific item name
- **Counterparty** — who you traded with
- **Faction** — filter by the counterparty's faction
- **Station** — where the trade happened
- **Date Range** — start and end dates to bracket a time period

All filters combine, so you can drill down to exactly what you're looking for.

## Recording Purchases

Use the **Record Purchase** button to log items you've bought. Same fields as a sale — item, quantity, price, station, counterparty — but recorded as a Buy transaction. This feeds into the Summary tab's profit/loss calculations.

## Summary Tab

The Summary tab is where you find out if your trading empire is actually profitable.

- **Profit/Loss Totals** — net result across all transactions in the filtered period.
- **Per-Item Breakdown** — see which items are making you money and which are dragging you down.
- **Pricing Plan Comparison** — if you have pricing plans set up, the summary shows how your actual sale prices compare to your computed costs. Handy for spotting items you're selling below cost.

## Related Topics

- [Stations](stations.md) — Listings are tied to stations
- [Contacts](contacts.md) — Counterparties come from your contacts
- [Pricing Plans](pricing-plans.md) — Cost comparison in the Summary tab
