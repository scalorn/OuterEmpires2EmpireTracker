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
import type { Blueprint, BlueprintResource } from '../../api/types/domain';

interface BlueprintFormState {
  name: string;
  blueprintType: string;
  shipClass: string;
  techLevel: number;
  evolution: number;
  nickName: string;
  isGlobal: boolean;
}

interface PropertyRow {
  key: string;
  value: number;
}

const emptyForm: BlueprintFormState = {
  name: '',
  blueprintType: '',
  shipClass: '',
  techLevel: 1,
  evolution: 0,
  nickName: '',
  isGlobal: false,
};

function formFromBlueprint(bp: Blueprint): BlueprintFormState {
  return {
    name: bp.name,
    blueprintType: bp.blueprintType,
    shipClass: bp.shipClass ?? '',
    techLevel: bp.techLevel,
    evolution: bp.evolution,
    nickName: bp.nickName ?? '',
    isGlobal: bp.isGlobal,
  };
}

function propertiesToRows(properties: Record<string, number>): PropertyRow[] {
  return Object.entries(properties).map(([key, value]) => ({ key, value }));
}

function rowsToProperties(rows: PropertyRow[]): Record<string, number> {
  const result: Record<string, number> = {};
  for (const row of rows) {
    if (row.key.trim()) {
      result[row.key.trim()] = row.value;
    }
  }
  return result;
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
 * Right panel: editable detail panel with tabbed content (Details, Statistics, Resources, Evolution)
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
  const [resources, setResources] = useState<BlueprintResource[]>([]);
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
    const levels = new Set<number>();
    for (const bp of blueprints) {
      levels.add(bp.techLevel);
    }
    return Array.from(levels)
      .sort((a, b) => a - b)
      .map((l) => ({ value: String(l), label: `TL ${l}` }));
  }, [techLevels, blueprints]);

