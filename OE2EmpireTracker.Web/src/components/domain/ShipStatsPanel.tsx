export interface ShipStatsPanelProps {
  mass: number;
  powerBalance: number;
  cargoCapacity: number;
  defenceRating: number;
  propulsion: number;
}

/**
 * Displays a compact grid of live-updating ship stats.
 * Values update reactively as slots are assigned in the ShipTemplateForm.
 */
export function ShipStatsPanel({
  mass,
  powerBalance,
  cargoCapacity,
  defenceRating,
  propulsion,
}: ShipStatsPanelProps) {
  return (
    <div className="rounded border border-gray-700 bg-gray-800 p-4">
      <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-400">
        Ship Stats
      </h3>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        <StatItem label="Mass" value={mass} unit="t" />
        <StatItem
          label="Power"
          value={powerBalance}
          variant={powerBalance < 0 ? 'danger' : powerBalance === 0 ? 'neutral' : 'success'}
        />
        <StatItem label="Cargo" value={cargoCapacity} unit="m³" />
        <StatItem label="Defence" value={defenceRating} />
        <StatItem label="Propulsion" value={propulsion} />
      </div>
    </div>
  );
}

type StatVariant = 'neutral' | 'success' | 'danger';

function StatItem({
  label,
  value,
  unit,
  variant = 'neutral',
}: {
  label: string;
  value: number;
  unit?: string;
  variant?: StatVariant;
}) {
  const colorClass =
    variant === 'danger'
      ? 'text-red-400'
      : variant === 'success'
        ? 'text-green-400'
        : 'text-white';

  return (
    <div className="flex flex-col">
      <span className="text-xs text-gray-500">{label}</span>
      <span className={`text-lg font-bold ${colorClass}`}>
        {value.toLocaleString()}
        {unit && <span className="ml-0.5 text-xs font-normal text-gray-500">{unit}</span>}
      </span>
    </div>
  );
}
