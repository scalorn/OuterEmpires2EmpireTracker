import type { BuildOrderStep } from '../../api/types/generated';

interface BuildOrderListProps {
  steps: BuildOrderStep[];
}

export function BuildOrderList({ steps }: BuildOrderListProps) {
  if (steps.length === 0) {
    return <p className="text-sm text-gray-500">No build steps to display.</p>;
  }

  return (
    <ol className="space-y-2">
      {steps.map((step) => (
        <li
          key={step.sequence}
          className="rounded border border-gray-700 p-3"
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="flex h-6 w-6 items-center justify-center rounded-full bg-blue-600 text-xs font-bold text-white">
                {step.sequence}
              </span>
              <div>
                <p className="font-medium text-white">{step.structureName}</p>
                <p className="text-xs text-gray-400">{step.blueprintType}</p>
              </div>
            </div>
            <span className="text-sm text-gray-400">{step.timeEstimate}</span>
          </div>
          {step.resourcesRequired.length > 0 && (
            <ul className="mt-2 flex flex-wrap gap-2">
              {step.resourcesRequired.map((res, i) => (
                <li key={i} className="rounded bg-gray-800 px-2 py-0.5 text-xs text-gray-300">
                  {res.resourceName}: {res.quantity}
                </li>
              ))}
            </ul>
          )}
        </li>
      ))}
    </ol>
  );
}
