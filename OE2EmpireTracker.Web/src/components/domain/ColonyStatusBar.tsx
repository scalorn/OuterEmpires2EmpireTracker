interface ColonyStatusBarProps {
  label: string;
  provided: number;
  required: number;
  color?: string;
}

export function ColonyStatusBar({ label, provided, required, color = 'blue' }: ColonyStatusBarProps) {
  const percentage = required > 0 ? Math.min((provided / required) * 100, 100) : 100;
  const isDeficit = provided < required;

  const colorClasses: Record<string, string> = {
    blue: 'bg-blue-500',
    green: 'bg-green-500',
    yellow: 'bg-yellow-500',
    red: 'bg-red-500',
    purple: 'bg-purple-500',
  };

  const barColor = isDeficit ? 'bg-red-500' : (colorClasses[color] ?? 'bg-blue-500');

  return (
    <div className="mb-3">
      <div className="mb-1 flex justify-between text-sm">
        <span className="text-gray-300">{label}</span>
        <span className={isDeficit ? 'text-red-400' : 'text-gray-400'}>
          {provided} / {required}
        </span>
      </div>
      <div className="h-2 w-full rounded-full bg-gray-700">
        <div
          className={`h-2 rounded-full transition-all ${barColor}`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}
