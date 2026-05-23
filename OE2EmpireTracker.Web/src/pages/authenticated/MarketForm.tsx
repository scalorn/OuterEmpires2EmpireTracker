import { useState, useMemo, useCallback } from 'react';
import { useListings, useListingMutations } from '../../api/hooks/useMarketListings';
import { useTransactions, useRecordPurchase } from '../../api/hooks/useMarketTransactions';
import { useAuthStore } from '../../auth/store';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { TabBar } from '../../components/common/TabBar';
import { FilterBar } from '../../components/common/FilterBar';
import type { FilterDefinition, FilterValues } from '../../components/common/FilterBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import type { MarketListing, MarketTransaction } from '../../api/types/domain';

interface NewListingForm {
  stationName: string;
  itemName: string;
  quantity: string;
  price: string;
  condition: string;
  maxRepair: string;
}

const emptyNewListing: NewListingForm = {
  stationName: '',
  itemName: '',
  quantity: '',
  price: '',
  condition: '',
  maxRepair: '',
};

const MARKET_TABS = [
  { key: 'listings', label: 'Listings' },
  { key: 'transactions', label: 'Transactions' },
  { key: 'summary', label: 'Summary' },
];

/**
 * MarketForm — tabbed layout for managing market listings and transactions.
 *
 * Tabs: Listings, Transactions, Summary
 * Listings tab: active listings table + new listing form + Record Sale action
 *
 * Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7
 */
export function MarketForm() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useListings(characterUUID);
  const { save, recordSale } = useListingMutations();

  const [activeTab, setActiveTab] = useState('listings');
  const [newListing, setNewListing] = useState<NewListingForm>(emptyNewListing);
  const [isDirty, setIsDirty] = useState(false);
  const [saleConfirm, setSaleConfirm] = useState<{ listing: MarketListing; quantity: number } | null>(null);

  useUnsavedChanges(isDirty);

  const listings: MarketListing[] = useMemo(
    () => (Array.isArray(data) ? data : []) as MarketListing[],
    [data],
  );

  // --- New Listing form handlers ---

  const handleNewListingChange = useCallback(
    (field: keyof NewListingForm, value: string) => {
      setNewListing((prev) => ({ ...prev, [field]: value }));
      setIsDirty(true);
    },
    [],
  );

  const handleCreateListing = useCallback(async () => {
    const quantity = Number(newListing.quantity);
    const price = Number(newListing.price);
    if (!newListing.stationName.trim() || !newListing.itemName.trim() || quantity <= 0 || price <= 0) {
      return;
    }

    const condition = newListing.condition ? Number(newListing.condition) : undefined;
    const maxRepair = newListing.maxRepair ? Number(newListing.maxRepair) : undefined;

    await save.mutateAsync({
      data: {
        stationName: newListing.stationName.trim(),
        itemName: newListing.itemName.trim(),
        quantity,
        price,
        condition: condition && !isNaN(condition) ? condition : undefined,
        maxRepair: maxRepair && !isNaN(maxRepair) ? maxRepair : undefined,
      },
    });

    setNewListing(emptyNewListing);
    setIsDirty(false);
  }, [newListing, save]);

  // --- Record Sale handlers ---

  const handleRecordSaleClick = useCallback((listing: MarketListing) => {
    setSaleConfirm({ listing, quantity: 1 });
  }, []);

  const handleConfirmSale = useCallback(async () => {
    if (!saleConfirm) return;
    await recordSale.mutateAsync({
      listingUUID: saleConfirm.listing.uuid,
      quantity: saleConfirm.quantity,
    });
    setSaleConfirm(null);
  }, [saleConfirm, recordSale]);

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading market data..." />;
  if (isError) return <RetryableError message="Failed to load market data." onRetry={() => void refetch()} />;

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-4">
        <h1 className="text-xl font-semibold text-white">Market</h1>
      </div>

      <TabBar tabs={MARKET_TABS} activeTab={activeTab} onTabChange={setActiveTab} />

      <div className="flex-1 overflow-y-auto p-4" role="tabpanel" id={`tabpanel-${activeTab}`} aria-labelledby={`tab-${activeTab}`}>
        {activeTab === 'listings' && (
          <ListingsTab
            listings={listings}
            newListing={newListing}
            onNewListingChange={handleNewListingChange}
            onCreateListing={handleCreateListing}
            onRecordSale={handleRecordSaleClick}
            isSaving={save.isPending}
          />
        )}

        {activeTab === 'transactions' && (
          <TransactionsTab />
        )}

        {activeTab === 'summary' && (
          <SummaryTab />
        )}
      </div>

      <ConfirmDialog
        isOpen={saleConfirm !== null}
        title="Record Sale"
        message={saleConfirm
          ? `Record sale of ${saleConfirm.quantity} × "${saleConfirm.listing.itemName}" at ${saleConfirm.listing.stationName}? This will decrement the listing quantity.`
          : ''}
        confirmLabel="Record Sale"
        onConfirm={() => void handleConfirmSale()}
        onCancel={() => setSaleConfirm(null)}
        variant="warning"
      />
    </div>
  );
}


// --- Listings Tab sub-component ---

interface ListingsTabProps {
  listings: MarketListing[];
  newListing: NewListingForm;
  onNewListingChange: (field: keyof NewListingForm, value: string) => void;
  onCreateListing: () => Promise<void>;
  onRecordSale: (listing: MarketListing) => void;
  isSaving: boolean;
}

function ListingsTab({
  listings,
  newListing,
  onNewListingChange,
  onCreateListing,
  onRecordSale,
  isSaving,
}: ListingsTabProps) {
  return (
    <div className="space-y-6">
      {/* Active Listings Table */}
      <section>
        <h2 className="mb-3 text-lg font-semibold text-white">Active Listings</h2>
        {listings.length === 0 ? (
          <EmptyState title="No listings" message="No active market listings. Create one below." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-800 text-xs uppercase text-gray-400">
                <tr>
                  <th className="px-3 py-2">Station</th>
                  <th className="px-3 py-2">Item</th>
                  <th className="px-3 py-2">Quantity</th>
                  <th className="px-3 py-2">Price</th>
                  <th className="px-3 py-2">Condition</th>
                  <th className="px-3 py-2">Actions</th>
                </tr>
              </thead>
              <tbody>
                {listings.map((listing) => (
                  <tr key={listing.uuid} className="border-b border-gray-700 hover:bg-gray-750">
                    <td className="px-3 py-2 text-gray-300">{listing.stationName}</td>
                    <td className="px-3 py-2 text-white">{listing.itemName}</td>
                    <td className="px-3 py-2 text-gray-300">{listing.quantity}</td>
                    <td className="px-3 py-2 text-gray-300">{listing.price.toLocaleString()}</td>
                    <td className="px-3 py-2 text-gray-300">
                      {listing.condition != null ? `${listing.condition}%` : '—'}
                    </td>
                    <td className="px-3 py-2">
                      <button
                        onClick={() => onRecordSale(listing)}
                        className="rounded bg-amber-600 px-2 py-1 text-xs text-white hover:bg-amber-700"
                      >
                        Record Sale
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* New Listing Form */}
      <section>
        <h2 className="mb-3 text-lg font-semibold text-white">New Listing</h2>
        <div className="grid max-w-2xl grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div>
            <label htmlFor="new-station" className="mb-1 block text-sm text-gray-400">Station</label>
            <input
              id="new-station"
              type="text"
              value={newListing.stationName}
              onChange={(e) => onNewListingChange('stationName', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Station name"
            />
          </div>
          <div>
            <label htmlFor="new-item" className="mb-1 block text-sm text-gray-400">Item</label>
            <input
              id="new-item"
              type="text"
              value={newListing.itemName}
              onChange={(e) => onNewListingChange('itemName', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Item name"
            />
          </div>
          <div>
            <label htmlFor="new-quantity" className="mb-1 block text-sm text-gray-400">Quantity</label>
            <input
              id="new-quantity"
              type="number"
              min={1}
              value={newListing.quantity}
              onChange={(e) => onNewListingChange('quantity', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="0"
            />
          </div>
          <div>
            <label htmlFor="new-price" className="mb-1 block text-sm text-gray-400">Price</label>
            <input
              id="new-price"
              type="number"
              min={0}
              step="0.01"
              value={newListing.price}
              onChange={(e) => onNewListingChange('price', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="0"
            />
          </div>
          <div>
            <label htmlFor="new-condition" className="mb-1 block text-sm text-gray-400">Condition (%)</label>
            <input
              id="new-condition"
              type="number"
              min={0}
              max={100}
              value={newListing.condition}
              onChange={(e) => onNewListingChange('condition', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Optional"
            />
          </div>
          <div>
            <label htmlFor="new-maxrepair" className="mb-1 block text-sm text-gray-400">Max Repair (%)</label>
            <input
              id="new-maxrepair"
              type="number"
              min={0}
              max={100}
              value={newListing.maxRepair}
              onChange={(e) => onNewListingChange('maxRepair', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Optional"
            />
          </div>
        </div>
        <div className="mt-4">
          <button
            onClick={() => void onCreateListing()}
            disabled={isSaving || !newListing.stationName.trim() || !newListing.itemName.trim() || Number(newListing.quantity) <= 0 || Number(newListing.price) <= 0}
            className="rounded bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
          >
            {isSaving ? 'Creating...' : 'Create Listing'}
          </button>
        </div>
      </section>
    </div>
  );
}


// --- Transactions Tab sub-component ---

interface PurchaseForm {
  itemName: string;
  quantity: string;
  price: string;
  stationName: string;
  counterparty: string;
}

const emptyPurchase: PurchaseForm = {
  itemName: '',
  quantity: '',
  price: '',
  stationName: '',
  counterparty: '',
};

const TRANSACTION_FILTERS: FilterDefinition[] = [
  {
    type: 'dropdown',
    key: 'type',
    label: 'Type',
    options: [
      { value: 'Buy', label: 'Buy' },
      { value: 'Sell', label: 'Sell' },
    ],
  },
  { type: 'text', key: 'item', placeholder: 'Filter by item...' },
  { type: 'text', key: 'counterparty', placeholder: 'Filter by counterparty...' },
  { type: 'text', key: 'faction', placeholder: 'Filter by faction...' },
  { type: 'text', key: 'station', placeholder: 'Filter by station...' },
];

const defaultFilterValues: FilterValues = {
  type: '',
  item: '',
  counterparty: '',
  faction: '',
  station: '',
};

function TransactionsTab() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useTransactions(characterUUID);
  const recordPurchase = useRecordPurchase();

  const [filterValues, setFilterValues] = useState<FilterValues>(defaultFilterValues);
  const [purchase, setPurchase] = useState<PurchaseForm>(emptyPurchase);

  const transactions: MarketTransaction[] = useMemo(
    () => (Array.isArray(data) ? data : []) as MarketTransaction[],
    [data],
  );

  const filteredTransactions = useMemo(() => {
    return transactions.filter((tx) => {
      const typeFilter = filterValues.type as string;
      if (typeFilter && tx.type !== typeFilter) return false;

      const itemFilter = (filterValues.item as string).toLowerCase();
      if (itemFilter && !tx.itemName.toLowerCase().includes(itemFilter)) return false;

      const counterpartyFilter = (filterValues.counterparty as string).toLowerCase();
      if (counterpartyFilter && !(tx.counterparty ?? '').toLowerCase().includes(counterpartyFilter)) return false;

      const factionFilter = (filterValues.faction as string).toLowerCase();
      if (factionFilter && !(tx.faction ?? '').toLowerCase().includes(factionFilter)) return false;

      const stationFilter = (filterValues.station as string).toLowerCase();
      if (stationFilter && !tx.stationName.toLowerCase().includes(stationFilter)) return false;

      return true;
    });
  }, [transactions, filterValues]);

  const handlePurchaseChange = useCallback(
    (field: keyof PurchaseForm, value: string) => {
      setPurchase((prev) => ({ ...prev, [field]: value }));
    },
    [],
  );

  const handleRecordPurchase = useCallback(async () => {
    const quantity = Number(purchase.quantity);
    const price = Number(purchase.price);
    if (!purchase.itemName.trim() || quantity <= 0 || price <= 0 || !purchase.stationName.trim()) {
      return;
    }

    await recordPurchase.mutateAsync({
      itemName: purchase.itemName.trim(),
      quantity,
      price,
      stationName: purchase.stationName.trim(),
      counterparty: purchase.counterparty.trim() || undefined,
    });

    setPurchase(emptyPurchase);
  }, [purchase, recordPurchase]);

  const handleClearFilters = useCallback(() => {
    setFilterValues(defaultFilterValues);
  }, []);

  if (isLoading) return <LoadingSpinner message="Loading transactions..." />;
  if (isError) return <RetryableError message="Failed to load transactions." onRetry={() => void refetch()} />;

  return (
    <div className="space-y-6">
      {/* Transaction History */}
      <section>
        <h2 className="mb-3 text-lg font-semibold text-white">Transaction History</h2>
        <FilterBar
          filters={TRANSACTION_FILTERS}
          values={filterValues}
          onChange={setFilterValues}
          onClear={handleClearFilters}
        />
        {filteredTransactions.length === 0 ? (
          <EmptyState title="No transactions" message="No transactions match the current filters." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-800 text-xs uppercase text-gray-400">
                <tr>
                  <th className="px-3 py-2">Type</th>
                  <th className="px-3 py-2">Item</th>
                  <th className="px-3 py-2">Quantity</th>
                  <th className="px-3 py-2">Price</th>
                  <th className="px-3 py-2">Station</th>
                  <th className="px-3 py-2">Counterparty</th>
                  <th className="px-3 py-2">Faction</th>
                  <th className="px-3 py-2">Date</th>
                </tr>
              </thead>
              <tbody>
                {filteredTransactions.map((tx) => (
                  <tr key={tx.uuid} className="border-b border-gray-700 hover:bg-gray-750">
                    <td className="px-3 py-2">
                      <span className={tx.type === 'Buy' ? 'text-red-400' : 'text-green-400'}>
                        {tx.type}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-white">{tx.itemName}</td>
                    <td className="px-3 py-2 text-gray-300">{tx.quantity}</td>
                    <td className="px-3 py-2 text-gray-300">{tx.price.toLocaleString()}</td>
                    <td className="px-3 py-2 text-gray-300">{tx.stationName}</td>
                    <td className="px-3 py-2 text-gray-300">{tx.counterparty ?? '—'}</td>
                    <td className="px-3 py-2 text-gray-300">{tx.faction ?? '—'}</td>
                    <td className="px-3 py-2 text-gray-300">
                      {new Date(tx.transactionDate).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Record Purchase Form */}
      <section>
        <h2 className="mb-3 text-lg font-semibold text-white">Record Purchase</h2>
        <div className="grid max-w-2xl grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div>
            <label htmlFor="purchase-item" className="mb-1 block text-sm text-gray-400">Item</label>
            <input
              id="purchase-item"
              type="text"
              value={purchase.itemName}
              onChange={(e) => handlePurchaseChange('itemName', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Item name"
            />
          </div>
          <div>
            <label htmlFor="purchase-quantity" className="mb-1 block text-sm text-gray-400">Quantity</label>
            <input
              id="purchase-quantity"
              type="number"
              min={1}
              value={purchase.quantity}
              onChange={(e) => handlePurchaseChange('quantity', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="0"
            />
          </div>
          <div>
            <label htmlFor="purchase-price" className="mb-1 block text-sm text-gray-400">Price</label>
            <input
              id="purchase-price"
              type="number"
              min={0}
              step="0.01"
              value={purchase.price}
              onChange={(e) => handlePurchaseChange('price', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="0"
            />
          </div>
          <div>
            <label htmlFor="purchase-station" className="mb-1 block text-sm text-gray-400">Station</label>
            <input
              id="purchase-station"
              type="text"
              value={purchase.stationName}
              onChange={(e) => handlePurchaseChange('stationName', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Station name"
            />
          </div>
          <div>
            <label htmlFor="purchase-counterparty" className="mb-1 block text-sm text-gray-400">Counterparty</label>
            <input
              id="purchase-counterparty"
              type="text"
              value={purchase.counterparty}
              onChange={(e) => handlePurchaseChange('counterparty', e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              placeholder="Seller name (optional)"
            />
          </div>
        </div>
        <div className="mt-4">
          <button
            onClick={() => void handleRecordPurchase()}
            disabled={recordPurchase.isPending || !purchase.itemName.trim() || Number(purchase.quantity) <= 0 || Number(purchase.price) <= 0 || !purchase.stationName.trim()}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {recordPurchase.isPending ? 'Recording...' : 'Record'}
          </button>
        </div>
      </section>
    </div>
  );
}
