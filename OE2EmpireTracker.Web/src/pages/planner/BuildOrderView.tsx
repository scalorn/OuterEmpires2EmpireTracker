import { usePlannerStore } from './plannerStore';
import { BuildOrderList } from '../../components/domain/BuildOrderList';

export function BuildOrderView() {
  const { buildOrder } = usePlannerStore();

  return (
    <div className="rounded border border-gray-700 p-4">
      <h2 className="mb-3 text-lg font-semibold text-gray-200">Build Order</h2>
      {buildOrder ? (
        <>
          <p className="mb-3 text-sm text-gray-400">
            Total estimated time: <span className="text-white">{buildOrder.totalTimeEstimate}</span>
          </p>
          <BuildOrderList steps={buildOrder.steps} />
        </>
      ) : (
        <p className="text-sm text-gray-500">
          Click "Optimize Build Order" to generate a build sequence.
        </p>
      )}
    </div>
  );
}
