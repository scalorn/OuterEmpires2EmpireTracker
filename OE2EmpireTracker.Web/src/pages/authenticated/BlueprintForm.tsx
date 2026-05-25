import { useState, useMemo, useCallback } from 'react';
import { useBlueprints, useBlueprintDetail, useBlueprintMutations } from '../../api/hooks/useBlueprints';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useAuthStore } from '../../auth/store';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { EditableGrid, type GridColumn } from '../../components/common/EditableGrid';
import { TabBar } from '../../components/common/TabBar';
import { applyFilters, type FilterConfig } from '../../utils/filterUtils';
import type { Blueprint } from '../../api/types/domain';

interface BlueprintFormState {
  name: string;
  blueprintType: string;
  shipClass: string;
  techLevel: string;
  evolution: number;
  nickName: string;
}

interface PropertyRow {
  key: string;
  value: string;
}

const emptyForm: BlueprintFormState = {
  name: '',
  blueprintType: '',
  shipClass: '',
  techLevel: '',
  evolution: 0,
  nickName: '',
};

function formFromBlueprint(bp: Blueprint): BlueprintFormState {
  return {
    name: bp.name,
    blueprintType: bp.bluePrintType,
    shipClass: bp.shipClass ?? '',
    techLevel: bp.techLevel,
    evolution: bp.evolution,
    nickName: bp.nickName ?? '',
  };
}

function propertiesToRows(properties: Record<string, string>): PropertyRow[] {
  return Object.entries(properties).map(([key, value]) => ({ key, value }));
}

function rowsToProperties(rows: PropertyRow[]): Record<string, string> {
  const result: Record<string, string> = {};
  for (const row of rows) {
    if (row.key.trim()) {
      result[row.key.trim()] = row.value;
    }
  }
  return result;
}

/** Row type for the blueprint resources grid (key=resource name, value=quantity string). */
interface ResourceEntry {
  resourceName: string;
  quantity: string;
}

const BLUEPRINT_TABS = [
  { key: 'details', label: 'Details' },
  { key: 'statistics', label: 'Statistics' },
  { key: 'resources', label: 'Resources' },
  { key: 'evolution', label: 'Evolution' },
];

/**
 * BlueprintForm — master-detail layout for managing blueprints.
 *
 * Left panel: filterable, sortable list of blueprints
 * Right panel: editable detail panel with tabbed content
 *   - Details: basic fields (name, type, ship class, tech level, evolution, nick name, global)
 *   - Statistics: editable grid of numeric properties (Record<string, number>)
 *   - Resources: editable grid of manufacturing resource requirements
 *   - Evolution: placeholder for future evolution chart
 *
 * Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.7, 2.8, 2.9
 */
export function BlueprintForm() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useBlueprints(characterUUID);
  const { data: baseline } = useBaseline();
  const { create, save, remove } = useBlueprintMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<BlueprintFormState>(emptyForm);
  const [properties, setProperties] = useState<PropertyRow[]>([]);
  const [resources, setResources] = useState<ResourceEntry[]>([]);
  const [activeTab, setActiveTab] = useState('details');
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterValues, setFilterValues] = useState<FilterValues>({
    search: '',
    blueprintType: '',
    shipClass: '',
    techLevel: '',
    evolution: '',
  });
  const [sortField, setSortField] = useState<keyof Blueprint>('name');
  const [sortAsc, setSortAsc] = useState(true);

  // Fetch detail for selected blueprint
  const { data: selectedBlueprint } = useBlueprintDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  const blueprints: Blueprint[] = useMemo(
    () => (Array.isArray(data) ? data : []) as Blueprint[],
    [data],
  );

  const blueprintTypes = baseline?.blueprintTypes ?? [];
  const shipClasses = baseline?.shipClasses ?? [];
  const techLevels = baseline?.techLevels ?? [];
  const purities = baseline?.purities ?? [];

  // Derive unique evolution levels from the blueprint data
  const evolutionLevels = useMemo(() => {
    const levels = new Set<number>();
    for (const bp of blueprints) {
      levels.add(bp.evolution);
    }
    return Array.from(levels).sort((a, b) => a - b);
  }, [blueprints]);

  // Derive unique tech levels from the blueprint data (fallback if baseline is empty)
  const techLevelOptions = useMemo(() => {
    if (techLevels.length > 0) {
      return techLevels.map((tl) => ({ value: String(tl.level), label: tl.name }));
    }
    const levels = new Set<string>();
    for (const bp of blueprints) {
      if (bp.techLevel) levels.add(bp.techLevel);
    }
    return Array.from(levels)
      .sort()
      .map((l) => ({ value: l, label: l }));
  }, [techLevels, blueprints]);

  // --- Grid column definitions ---

  const statisticsColumns: GridColumn<PropertyRow>[] = useMemo(() => [
    { key: 'key', header: 'Property', type: 'text' as const },
    { key: 'value', header: 'Value', type: 'number' as const },
  ], []);

  const resourceColumns: GridColumn<ResourceEntry>[] = useMemo(() => [
    { key: 'resourceName', header: 'Resource', type: 'text' as const },
    { key: 'quantity', header: 'Quantity', type: 'text' as const },
    {
      key: 'purity',
      header: 'Purity',
      type: 'select' as const,
      options: purities.map((p) => ({ value: p, label: p })),
    },
  ], [purities]);

  const filterDefs: FilterDefinition[] = useMemo(() => [
    { type: 'text', key: 'search', placeholder: 'Search name or nick name...' },
    {
      type: 'dropdown',
      key: 'blueprintType',
      label: 'Type',
      options: blueprintTypes.map((t) => ({ value: t, label: t })),
    },
    {
      type: 'dropdown',
      key: 'shipClass',
      label: 'Ship Class',
      options: shipClasses.map((sc) => ({ value: sc.name, label: sc.name })),
    },
    {
      type: 'dropdown',
      key: 'techLevel',
      label: 'Tech Level',
      options: techLevelOptions,
    },
    {
      type: 'dropdown',
      key: 'evolution',
      label: 'Evolution',
      options: evolutionLevels.map((e) => ({ value: String(e), label: `Evo ${e}` })),
    },
  ], [blueprintTypes, shipClasses, techLevelOptions, evolutionLevels]);

  const filteredBlueprints = useMemo(() => {
    const filters: FilterConfig<Blueprint>[] = [];

    const searchQuery = (filterValues.search as string) ?? '';
    if (searchQuery.trim()) {
      filters.push({
        type: 'text',
        fields: ['name', 'nickName'],
        query: searchQuery,
      });
    }

    const bpType = (filterValues.blueprintType as string) ?? '';
    if (bpType) {
      filters.push({
        type: 'dropdown',
        field: 'bluePrintType',
        selected: bpType,
      });
    }

    const shipClass = (filterValues.shipClass as string) ?? '';
    if (shipClass) {
      filters.push({
        type: 'dropdown',
        field: 'shipClass',
        selected: shipClass,
      });
    }

    let result = applyFilters(blueprints, filters);

    // Tech level filter
    const techLevelStr = (filterValues.techLevel as string) ?? '';
    if (techLevelStr) {
      result = result.filter((bp) => bp.techLevel === techLevelStr);
    }

    // Evolution filter (numeric comparison)
    const evoStr = (filterValues.evolution as string) ?? '';
    if (evoStr) {
      const evo = Number(evoStr);
      if (!isNaN(evo)) {
        result = result.filter((bp) => bp.evolution === evo);
      }
    }

    return result;
  }, [blueprints, filterValues]);

  const sortedBlueprints = useMemo(() => {
    const sorted = [...filteredBlueprints].sort((a, b) => {
      const aVal = a[sortField] ?? '';
      const bVal = b[sortField] ?? '';
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortAsc ? aVal - bVal : bVal - aVal;
      }
      const cmp = String(aVal).localeCompare(String(bVal));
      return sortAsc ? cmp : -cmp;
    });
    return sorted;
  }, [filteredBlueprints, sortField, sortAsc]);

  // --- Handlers ---

  const handleSort = (field: keyof Blueprint) => {
    if (field === sortField) {
      setSortAsc(!sortAsc);
    } else {
      setSortField(field);
      setSortAsc(true);
    }
  };

  const handleClearFilters = () => {
    setFilterValues({ search: '', blueprintType: '', shipClass: '', techLevel: '', evolution: '' });
  };

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const bp = blueprints.find((b) => b.uuid === uuid);
    if (bp) {
      setForm(formFromBlueprint(bp));
      setProperties(propertiesToRows(bp.properties));
      setResources(Object.entries(bp.resources).map(([k, v]) => ({ resourceName: k, quantity: v })));
      setIsDirty(false);
    }
  }, [blueprints]);

  const handleBack = useCallback(() => {
    setSelectedId(null);
    setIsNewMode(false);
    setIsDirty(false);
  }, []);

  const handleNew = useCallback(() => {
    setSelectedId('new');
    setIsNewMode(true);
    setForm(emptyForm);
    setProperties([]);
    setResources([]);
    setActiveTab('details');
    setIsDirty(false);
  }, []);

  const handleFieldChange = useCallback(
    (field: keyof BlueprintFormState, value: string | number | boolean) => {
      setForm((prev) => ({ ...prev, [field]: value }));
      setIsDirty(true);
    },
    [],
  );

  // --- Properties (Statistics) handlers ---

  const handlePropertyChange = useCallback((index: number, row: PropertyRow) => {
    setProperties((prev) => {
      const next = [...prev];
      next[index] = row;
      return next;
    });
    setIsDirty(true);
  }, []);

  const handlePropertyAdd = useCallback(() => {
    setProperties((prev) => [...prev, { key: '', value: '0' }]);
    setIsDirty(true);
  }, []);

  const handlePropertyRemove = useCallback((index: number) => {
    setProperties((prev) => prev.filter((_, i) => i !== index));
    setIsDirty(true);
  }, []);

  // --- Resources handlers ---

  const handleResourceChange = useCallback((index: number, row: ResourceEntry) => {
    setResources((prev) => {
      const next = [...prev];
      next[index] = row;
      return next;
    });
    setIsDirty(true);
  }, []);

  const handleResourceAdd = useCallback(() => {
    setResources((prev) => [...prev, { resourceName: '', quantity: '0' }]);
    setIsDirty(true);
  }, []);

  const handleResourceRemove = useCallback((index: number) => {
    setResources((prev) => prev.filter((_, i) => i !== index));
    setIsDirty(true);
  }, []);

  // --- Save / Delete ---

  const handleSave = useCallback(async () => {
    const propsRecord = rowsToProperties(properties);
    const resourcesRecord: Record<string, string> = {};
    for (const r of resources) {
      if (r.resourceName.trim()) {
        resourcesRecord[r.resourceName.trim()] = r.quantity;
      }
    }
    if (isNewMode) {
      const result = await create.mutateAsync({
        name: form.name,
        bluePrintType: form.blueprintType,
        shipClass: form.shipClass || undefined,
        techLevel: form.techLevel,
        evolution: form.evolution,
        nickName: form.nickName || undefined,
        properties: propsRecord,
        resources: resourcesRecord,
      });
      setSelectedId(result.uuid);
      setIsNewMode(false);
    } else if (selectedId) {
      await save.mutateAsync({
        entityUUID: selectedId,
        data: {
          name: form.name,
          bluePrintType: form.blueprintType,
          shipClass: form.shipClass || undefined,
          techLevel: form.techLevel,
          evolution: form.evolution,
          nickName: form.nickName || undefined,
          properties: propsRecord,
          resources: resourcesRecord,
        },
      });
    }
    setIsDirty(false);
  }, [isNewMode, selectedId, form, properties, resources, create, save]);

  const handleDelete = useCallback(async () => {
    if (!selectedId || isNewMode) return;
    await remove.mutateAsync(selectedId);
    setSelectedId(null);
    setForm(emptyForm);
    setProperties([]);
    setResources([]);
    setIsDirty(false);
    setShowDeleteConfirm(false);
  }, [selectedId, isNewMode, remove]);

  // Sync form when detail loads from server
  const detailUUID = selectedBlueprint?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedBlueprint && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromBlueprint(selectedBlueprint));
    setProperties(propertiesToRows(selectedBlueprint.properties));
    setResources(Object.entries(selectedBlueprint.resources).map(([k, v]) => ({ resourceName: k, quantity: v })));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading blueprints..." />;
  if (isError) return <RetryableError message="Failed to load blueprints." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col p-4">
      <h2 className="mb-3 text-lg font-semibold text-white">Blueprints</h2>
      <FilterBar
        filters={filterDefs}
        values={filterValues}
        onChange={setFilterValues}
        onClear={handleClearFilters}
      />
      {sortedBlueprints.length === 0 ? (
        <EmptyState title="No blueprints" message="No blueprints match the current filters." />
      ) : (
        <div className="min-h-0 flex-1 overflow-y-auto">
          <table className="w-full text-left text-sm">
            <thead className="sticky top-0 bg-gray-800 text-xs uppercase text-gray-400">
              <tr>
                <SortHeader field="bluePrintType" label="Type" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="name" label="Name" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="techLevel" label="TL" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="evolution" label="Evo" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="nickName" label="Nick Name" current={sortField} asc={sortAsc} onSort={handleSort} />
              </tr>
            </thead>
            <tbody>
              {sortedBlueprints.map((bp) => (
                <tr
                  key={bp.uuid}
                  onClick={() => handleSelect(bp.uuid)}
                  className={[
                    'cursor-pointer border-b border-gray-700 hover:bg-gray-750',
                    selectedId === bp.uuid ? 'bg-gray-700' : '',
                  ].join(' ')}
                >
                  <td className="px-3 py-2 text-gray-300">{bp.bluePrintType}</td>
                  <td className="px-3 py-2 text-white">{bp.name}</td>
                  <td className="px-3 py-2 text-gray-300">{bp.techLevel}</td>
                  <td className="px-3 py-2 text-gray-300">{bp.evolution}</td>
                  <td className="px-3 py-2 text-gray-300">{bp.nickName ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );

  const detailPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-white">
            {isNewMode ? 'New Blueprint' : 'Blueprint Details'}
          </h2>
          <div className="flex gap-2">
            <button
              onClick={handleNew}
              className="rounded bg-green-600 px-3 py-1.5 text-sm text-white hover:bg-green-700"
            >
              New
            </button>
            <button
              onClick={() => void handleSave()}
              disabled={!isDirty && !isNewMode}
              className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {save.isPending || create.isPending ? 'Saving...' : 'Save'}
            </button>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              disabled={isNewMode || !selectedId}
              className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700 disabled:opacity-50"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      {!selectedId && !isNewMode ? (
        <EmptyState title="No blueprint selected" message="Select a blueprint from the list or create a new one." />
      ) : (
        <div className="flex flex-1 flex-col overflow-hidden">
          <TabBar tabs={BLUEPRINT_TABS} activeTab={activeTab} onTabChange={setActiveTab} />
          <div
            className="flex-1 overflow-y-auto p-4"
            role="tabpanel"
            id={`tabpanel-${activeTab}`}
            aria-labelledby={`tab-${activeTab}`}
          >
            {activeTab === 'details' && (
              <DetailsTab
                form={form}
                blueprintTypes={blueprintTypes}
                shipClasses={shipClasses}
                onFieldChange={handleFieldChange}
              />
            )}

            {activeTab === 'statistics' && (
              <div>
                <p className="mb-3 text-sm text-gray-400">
                  Numeric properties for this blueprint. Edit values inline.
                </p>
                <EditableGrid<PropertyRow>
                  columns={statisticsColumns}
                  rows={properties}
                  onRowChange={handlePropertyChange}
                  onRowAdd={handlePropertyAdd}
                  onRowRemove={handlePropertyRemove}
                  keyExtractor={(row) => `prop-${properties.indexOf(row)}`}
                />
              </div>
            )}

            {activeTab === 'resources' && (
              <div>
                <p className="mb-3 text-sm text-gray-400">
                  Manufacturing resource requirements. Edit values inline.
                </p>
                <EditableGrid<ResourceEntry>
                  columns={resourceColumns}
                  rows={resources}
                  onRowChange={handleResourceChange}
                  onRowAdd={handleResourceAdd}
                  onRowRemove={handleResourceRemove}
                  keyExtractor={(row) => `res-${resources.indexOf(row)}`}
                />
              </div>
            )}

            {activeTab === 'evolution' && (
              <EmptyState
                title="Evolution Graph"
                message="Evolution chart will be implemented in a future task."
              />
            )}
          </div>
        </div>
      )}
    </div>
  );

  return (
    <>
      <MasterDetailLayout
        listPanel={listPanel}
        detailPanel={detailPanel}
        selectedId={selectedId}
        onBack={handleBack}
      />
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        title="Delete Blueprint"
        message={`Are you sure you want to delete "${form.name || 'this blueprint'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}

// --- Sub-components ---

interface DetailsTabProps {
  form: BlueprintFormState;
  blueprintTypes: string[];
  shipClasses: { name: string }[];
  onFieldChange: (field: keyof BlueprintFormState, value: string | number | boolean) => void;
}

function DetailsTab({ form, blueprintTypes, shipClasses, onFieldChange }: DetailsTabProps) {
  return (
    <div className="max-w-lg space-y-4">
      <div>
        <label htmlFor="bp-name" className="mb-1 block text-sm text-gray-400">Name</label>
        <input
          id="bp-name"
          type="text"
          value={form.name}
          onChange={(e) => onFieldChange('name', e.target.value)}
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
        />
      </div>
      <div>
        <label htmlFor="bp-type" className="mb-1 block text-sm text-gray-400">Type</label>
        <FilteredDropdown
          options={blueprintTypes.map((t) => ({ value: t, label: t }))}
          value={form.blueprintType}
          onChange={(v) => onFieldChange('blueprintType', v)}
          placeholder="Select type..."
        />
      </div>
      <div>
        <label htmlFor="bp-shipclass" className="mb-1 block text-sm text-gray-400">Ship Class</label>
        <FilteredDropdown
          options={shipClasses.map((sc) => ({ value: sc.name, label: sc.name }))}
          value={form.shipClass}
          onChange={(v) => onFieldChange('shipClass', v)}
          placeholder="Select ship class..."
        />
      </div>
      <div>
        <label htmlFor="bp-techlevel" className="mb-1 block text-sm text-gray-400">Tech Level</label>
        <input
          id="bp-techlevel"
          type="number"
          min={0}
          value={form.techLevel}
          onChange={(e) => onFieldChange('techLevel', Number(e.target.value) || 0)}
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
        />
      </div>
      <div>
        <label htmlFor="bp-evolution" className="mb-1 block text-sm text-gray-400">Evolution</label>
        <input
          id="bp-evolution"
          type="number"
          min={0}
          value={form.evolution}
          onChange={(e) => onFieldChange('evolution', Number(e.target.value) || 0)}
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
        />
      </div>
      <div>
        <label htmlFor="bp-nickname" className="mb-1 block text-sm text-gray-400">Nick Name</label>
        <input
          id="bp-nickname"
          type="text"
          value={form.nickName}
          onChange={(e) => onFieldChange('nickName', e.target.value)}
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
        />
      </div>
    </div>
  );
}

interface SortHeaderProps {
  field: keyof Blueprint;
  label: string;
  current: keyof Blueprint;
  asc: boolean;
  onSort: (field: keyof Blueprint) => void;
}

function SortHeader({ field, label, current, asc, onSort }: SortHeaderProps) {
  const isActive = current === field;
  return (
    <th
      className="cursor-pointer px-3 py-2 select-none hover:text-white"
      onClick={() => onSort(field)}
    >
      {label}
      {isActive && (
        <span className="ml-1">{asc ? '▲' : '▼'}</span>
      )}
    </th>
  );
}
