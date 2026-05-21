import { usePlannerStore } from './plannerStore';
import { ColonyStatusBar } from '../../components/domain/ColonyStatusBar';

export function StatusDisplay() {
  const { status, isComputing } = usePlannerStore();

  if (isComputing) {
    return (
      <div className="rounded border border-gray-700 p-4">
        <h2 className="mb-3 text-lg font-semibold text-gray-200">Colony Status</h2>
        <p className="text-sm text-gray-400">Computing...</p>
      </div>
    );
  }

  if (!status) {
    return (
      <div className="rounded border border-gray-700 p-4">
        <h2 className="mb-3 text-lg font-semibold text-gray-200">Colony Status</h2>
        <p className="text-sm text-gray-500">Add structures to see colony status.</p>
      </div>
    );
  }

  return (
    <div className="rounded border border-gray-700 p-4">
      <h2 className="mb-3 text-lg font-semibold text-gray-200">Colony Status</h2>
      <ColonyStatusBar
        label="Power"
        provided={status.powerProvided}
        required={status.powerRequired}
        color="yellow"
      />
      <ColonyStatusBar
        label="Habitation"
        provided={status.habitationProvision}
        required={status.habitationRequired}
        color="blue"
      />
      <ColonyStatusBar
        label="Food"
        provided={status.foodProvision}
        required={status.foodRequired}
        color="green"
      />
      <ColonyStatusBar
        label="Entertainment"
        provided={status.entertainmentProvided}
        required={status.entertainmentRequired}
        color="purple"
      />
      <ColonyStatusBar
        label="Warehouse"
        provided={status.warehouseCapacity}
        required={status.warehouseRequired}
        color="blue"
      />
    </div>
  );
}
