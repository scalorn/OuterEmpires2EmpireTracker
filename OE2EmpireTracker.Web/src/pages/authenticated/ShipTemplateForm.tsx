import { useState, useMemo } from 'react';
import { useShipTemplates, useTemplateDetail } from '../../api/hooks/useShipTemplates';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useAuthStore } from '../../auth/store';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { ShipTemplate, ShipClassDef, SlotDefinition } from '../../api/types/domain';

/**
 * ShipTemplateForm — master-detail layout for ship template management.
 *
 * Left panel: list of templates showing name and hull class.
 * Right panel: template name (editable), hull dropdown from baseline ship classes,
 * and slot groups displayed when a hull is selected.
 *
 * Slot assignment and stats panel will be added in task 14.2.
 */
export function ShipTemplateForm() {
  const { characterUUID } = useAuthStore();
  const { data: templates, isLoading, isError, refetch } = useShipTemplates(characterUUID);
  const { data: baseline } = useBaseline();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [templateName, setTemplateName] = useState('');
  const [selectedHull, setSelectedHull] = useState('');

  const selectedTemplate = useMemo(() => {
    if (!selectedId || !templates) return null;
    return (templates as ShipTemplate[]).find((t) => t.uuid === selectedId) ?? null;
  }, [selectedId, templates]);

  // When a template is selected, load its detail into local state
  const handleSelectTemplate = (template: ShipTemplate) => {
    setSelectedId(template.uuid);
    setTemplateName(template.name);
    setSelectedHull(template.hullShipClass ?? '');
  };

  const handleBack = () => {
    setSelectedId(null);
  };

  // Get the ship class definition for the currently selected hull
  const selectedShipClass: ShipClassDef | null = useMemo(() => {
    if (!selectedHull || !baseline?.shipClasses) return null;
    return baseline.shipClasses.find((sc) => sc.name === selectedHull) ?? null;
  }, [selectedHull, baseline]);

  // Group slots by type for display
  const slotGroups: Record<string, number> = useMemo(() => {
    if (!selectedShipClass) return {};
    const groups: Record<string, number> = {};
    for (const slot of selectedShipClass.slots) {
      groups[slot.slotType] = (groups[slot.slotType] ?? 0) + slot.count;
    }
    return groups;
  }, [selectedShipClass]);

  // Hull dropdown options from baseline
  const hullOptions = useMemo(() => {
    if (!baseline?.shipClasses) return [];
    return baseline.shipClasses.map((sc) => ({ value: sc.name, label: sc.name }));
  }, [baseline]);

  if (isLoading) return <LoadingSpinner message="Loading ship templates..." />;
  if (isError) return <RetryableError message="Failed to load ship templates." onRetry={() => void refetch()} />;

  const templateList = (templates as ShipTemplate[]) ?? [];

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Ship Templates</h2>
      </div>
      {templateList.length === 0 ? (
        <EmptyState title="No templates" message="Create a ship template to get started." />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {templateList.map((template) => (
            <li key={template.uuid}>
              <button
                onClick={() => handleSelectTemplate(template)}
                className={[
                  'w-full px-3 py-2 text-left transition-colors hover:bg-gray-800',
                  selectedId === template.uuid ? 'bg-gray-800 border-l-2 border-blue-500' : '',
                ].join(' ')}
              >
                <div className="text-sm font-medium text-white">{template.name}</div>
                <div className="text-xs text-gray-400">{template.hullShipClass || 'No hull selected'}</div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );

  const detailPanel = (
    <div className="flex h-full flex-col p-4">
      {!selectedId ? (
        <EmptyState title="No template selected" message="Select a template from the list to view details." />
      ) : (
        <>
          {/* Template name */}
          <div className="mb-4">
            <label htmlFor="template-name" className="mb-1 block text-sm font-medium text-gray-300">
              Template Name
            </label>
            <input
              id="template-name"
              type="text"
              value={templateName}
              onChange={(e) => setTemplateName(e.target.value)}
              className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Enter template name"
            />
          </div>

          {/* Hull selection */}
          <div className="mb-4">
            <label className="mb-1 block text-sm font-medium text-gray-300">
              Hull Class
            </label>
            <FilteredDropdown
              options={hullOptions}
              value={selectedHull}
              onChange={setSelectedHull}
              placeholder="Select hull class..."
            />
          </div>

          {/* Slot groups display */}
          {selectedShipClass && (
            <div className="mt-2">
              <h3 className="mb-2 text-sm font-semibold text-gray-200">Component Slots</h3>
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
                {Object.entries(slotGroups).map(([slotType, count]) => (
                  <div
                    key={slotType}
                    className="rounded border border-gray-600 bg-gray-800 px-3 py-2"
                  >
                    <div className="text-xs font-medium uppercase tracking-wide text-gray-400">
                      {slotType}
                    </div>
                    <div className="text-lg font-bold text-white">{count}</div>
                    <div className="text-xs text-gray-500">
                      {count === 1 ? 'slot' : 'slots'}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {!selectedShipClass && selectedId && (
            <p className="text-sm text-gray-500">Select a hull class to view available component slots.</p>
          )}
        </>
      )}
    </div>
  );

  return (
    <MasterDetailLayout
      listPanel={listPanel}
      detailPanel={detailPanel}
      selectedId={selectedId}
      onBack={handleBack}
    />
  );
}
