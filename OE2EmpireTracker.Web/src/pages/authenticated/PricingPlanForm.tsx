import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { usePricingPlans, usePricingPlanDetail, usePricingPlanMutations } from '../../api/hooks/usePricingPlans';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { EditableGrid } from '../../components/common/EditableGrid';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { GridColumn } from '../../components/common/EditableGrid';
import type { PricingPlan, PricingPlanItem } from '../../api/types/domain';

interface FormState {
  name: string;
  items: PricingPlanItem[];
}

const emptyForm: FormState = {
  name: '',
  items: [],
};

function formFromPlan(plan: PricingPlan): FormState {
  return {
    name: plan.name,
    items: plan.items ?? [],
  };
}

const itemColumns: GridColumn<PricingPlanItem>[] = [
  { key: 'itemName', header: 'Item Name', type: 'text' },
  { key: 'itemType', header: 'Item Type', type: 'text' },
  { key: 'unitPrice', header: 'Unit Price', type: 'number' },
];

