import { useShipBuilderStore } from './shipBuilderStore';
import type { WeaponSustainEntry, MiningSustainEntry } from './computeShipStats';

/** Format a number to 2 decimal places for derived stats, or as integer if whole */
function fmt(value: number): string {
  if (Number.isInteger(value)) return value.toString();
  return value.toFixed(2);
}

/** Display "∞" for infinite values (-1), otherwise format the number */
function fmtUptime(value: number): string {
  if (value === -1) return '∞';
  return fmt(value);
}

interface StatRowProps {
  label: string;
  value: string;
  warning?: boolean;
}

function StatRow({ label, value, warning }: StatRowProps) {
  return (
    <div className="flex justify-between items-center py-0.5">
      <span className="text-gray-400 text-sm">{label}</span>
      <span className={`text-sm font-mono ${warning ? 'text-red-400 font-bold' : 'text-gray-100'}`}>
        {value}
      </span>
    </div>
  );
}

interface StatCategoryProps {
  title: string;
  children: React.ReactNode;
}

function StatCategory({ title, children }: StatCategoryProps) {
  return (
    <div className="mb-3">
      <h4 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 border-b border-gray-700 pb-0.5">
        {title}
      </h4>
      {children}
    </div>
  );
}

function WeaponSustainSection({ entries }: { entries: WeaponSustainEntry[] }) {
  if (entries.length === 0) return null;
  return (
    <div className="mt-1 ml-2">
      <span className="text-xs text-gray-500">Sustain by type:</span>
      {entries.map((entry) => (
        <div key={entry.weaponType} className="flex justify-between items-center py-0.5 ml-2">
          <span className="text-gray-400 text-xs">{entry.weaponType}</span>
          <span className="text-xs font-mono text-gray-100">
            {entry.count} installed, {fmt(entry.sustainableCount)} sustainable
          </span>
        </div>
      ))}
    </div>
  );
}

function MiningSustainSection({ entries }: { entries: MiningSustainEntry[] }) {
  if (entries.length === 0) return null;
  return (
    <div className="mt-1 ml-2">
      <span className="text-xs text-gray-500">Sustain by type:</span>
      {entries.map((entry) => (
        <div key={entry.laserType} className="flex justify-between items-center py-0.5 ml-2">
          <span className="text-gray-400 text-xs">{entry.laserType}</span>
          <span className="text-xs font-mono text-gray-100">
            {entry.count} installed, {fmt(entry.sustainableCount)} sustainable
          </span>
        </div>
      ))}
    </div>
  );
}

export function ShipStatsDisplay() {
  const stats = useShipBuilderStore((s) => s.stats);

  if (!stats) return null;

  const engOverrun = stats.engCapacityUsed > stats.engCapacityAvailable;

  return (
    <div className="bg-gray-800 rounded-lg p-4 space-y-1">
      <h3 className="text-sm font-semibold text-gray-200 mb-2">Ship Stats</h3>

      {/* Engineering */}
      <StatCategory title="Engineering">
        <StatRow
          label="Eng Capacity"
          value={`${fmt(stats.engCapacityUsed)} / ${fmt(stats.engCapacityAvailable)}`}
          warning={engOverrun}
        />
      </StatCategory>

      {/* Capacity */}
      <StatCategory title="Capacity">
        <StatRow label="Cargo" value={fmt(stats.cargoCapacity)} />
        <StatRow label="Fuel" value={fmt(stats.fuelCapacity)} />
        <StatRow label="Hopper" value={fmt(stats.hopperCapacity)} />
      </StatCategory>

      {/* Defence */}
      <StatCategory title="Defence">
        <StatRow label="Total Health" value={fmt(stats.totalHealth)} />
        <StatRow label="Shield HP" value={fmt(stats.shieldHitpoints)} />
        <StatRow label="Shield Regen" value={fmt(stats.shieldRegen)} />
        <StatRow label="Energy Defence" value={fmt(stats.energyDefence)} />
        <StatRow label="Kinetic Defence" value={fmt(stats.kineticDefence)} />
        <StatRow label="Missile Defence" value={fmt(stats.missileDefence)} />
      </StatCategory>

      {/* Propulsion */}
      <StatCategory title="Propulsion">
        <StatRow label="Acceleration Factor" value={fmt(stats.accelerationFactor)} />
        <StatRow label="Turn Rate" value={fmt(stats.turnRate)} />
      </StatCategory>

      {/* Jump */}
      <StatCategory title="Jump">
        <StatRow label="Max Distance" value={fmt(stats.maxJumpDistance)} />
        <StatRow label="Fuel / JAS" value={fmt(stats.jumpFuelPerJAS)} />
        <StatRow label="Fuel Range" value={fmt(stats.jumpFuelRange)} />
        <StatRow label="Charge Time" value={fmt(stats.jumpChargeTime)} />
      </StatCategory>

      {/* Power */}
      <StatCategory title="Power">
        <StatRow label="Power Provided" value={fmt(stats.powerProvided)} />
        <StatRow label="Regen Rate" value={fmt(stats.powerRegenRate)} />
        <StatRow label="Shield Draw" value={fmt(stats.shieldPowerDraw)} />
        <StatRow label="Shield Uptime" value={fmtUptime(stats.shieldUptime)} />
        <StatRow label="Weapon Draw" value={fmt(stats.totalWeaponPowerDraw)} />
        <StatRow label="Weapon Sustain" value={fmtUptime(stats.weaponSustainTime)} />
      </StatCategory>

      {/* Mining */}
      <StatCategory title="Mining">
        <StatRow label="Mining Yield" value={fmt(stats.miningYield)} />
        <StatRow label="Cycle Time" value={fmt(stats.miningCycleTime)} />
        <MiningSustainSection entries={stats.miningSustainByType} />
      </StatCategory>

      {/* Weapons */}
      <StatCategory title="Weapons">
        <StatRow label="Small Weapons" value={String(stats.smallWeaponsInstalled)} />
        <StatRow label="Medium Weapons" value={String(stats.mediumWeaponsInstalled)} />
        <StatRow label="Large Weapons" value={String(stats.largeWeaponsInstalled)} />
        <WeaponSustainSection entries={stats.weaponSustainByType} />
      </StatCategory>

      {/* Scanning */}
      <StatCategory title="Scanning">
        <StatRow label="Scan Level" value={String(stats.scanLevel)} />
      </StatCategory>
    </div>
  );
}
