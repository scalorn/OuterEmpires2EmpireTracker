import { useMemo } from 'react';

export interface EvolutionDataPoint {
  evolution: number;
  properties: Record<string, number>;
}

export interface EvolutionChartProps {
  data: EvolutionDataPoint[];
  propertyNames: string[];
}

/**
 * SVG-based line chart showing property percentage changes across evolution levels.
 *
 * X-axis: evolution levels (1, 2, 3, etc.)
 * Y-axis: property values (percentage change from base evolution)
 * One line per property, each with a different color.
 * Responsive width via viewBox.
 *
 * Requirements: 2.6
 */

const CHART_COLORS = [
  '#3b82f6', // blue
  '#10b981', // emerald
  '#f59e0b', // amber
  '#ef4444', // red
  '#8b5cf6', // violet
  '#06b6d4', // cyan
  '#f97316', // orange
  '#ec4899', // pink
  '#14b8a6', // teal
  '#6366f1', // indigo
];

const PADDING = { top: 30, right: 20, bottom: 50, left: 60 };
const CHART_WIDTH = 600;
const CHART_HEIGHT = 300;

function getColor(index: number): string {
  return CHART_COLORS[index % CHART_COLORS.length];
}

export function EvolutionChart({ data, propertyNames }: EvolutionChartProps) {
  const chartData = useMemo(() => {
    if (data.length === 0 || propertyNames.length === 0) {
      return null;
    }

    // Sort data by evolution level
    const sorted = [...data].sort((a, b) => a.evolution - b.evolution);

    // Compute percentage changes from the base (first evolution level)
    const base = sorted[0].properties;
    const percentages = sorted.map((point) => {
      const pctProps: Record<string, number> = {};
      for (const prop of propertyNames) {
        const baseVal = base[prop] ?? 0;
        const curVal = point.properties[prop] ?? 0;
        if (baseVal === 0) {
          pctProps[prop] = curVal === 0 ? 0 : 100;
        } else {
          pctProps[prop] = ((curVal - baseVal) / Math.abs(baseVal)) * 100;
        }
      }
      return { evolution: point.evolution, properties: pctProps };
    });

    // Compute Y-axis range
    let minY = 0;
    let maxY = 0;
    for (const point of percentages) {
      for (const prop of propertyNames) {
        const val = point.properties[prop] ?? 0;
        if (val < minY) minY = val;
        if (val > maxY) maxY = val;
      }
    }

    // Add padding to Y range
    const yRange = maxY - minY;
    const yPadding = yRange === 0 ? 10 : yRange * 0.1;
    minY = minY - yPadding;
    maxY = maxY + yPadding;

    return { sorted, percentages, minY, maxY };
  }, [data, propertyNames]);

  if (!chartData || data.length < 2) {
    return (
      <div className="flex items-center justify-center p-8 text-gray-400">
        <p>Not enough evolution data to display a chart. At least 2 evolution levels are needed.</p>
      </div>
    );
  }

  const { percentages, minY, maxY } = chartData;
  const plotWidth = CHART_WIDTH - PADDING.left - PADDING.right;
  const plotHeight = CHART_HEIGHT - PADDING.top - PADDING.bottom;

  // Scale functions
  const xScale = (index: number): number => {
    if (percentages.length <= 1) return PADDING.left;
    return PADDING.left + (index / (percentages.length - 1)) * plotWidth;
  };

  const yScale = (value: number): number => {
    const range = maxY - minY;
    if (range === 0) return PADDING.top + plotHeight / 2;
    return PADDING.top + plotHeight - ((value - minY) / range) * plotHeight;
  };

  // Build polyline paths for each property
  const lines = propertyNames.map((prop, propIndex) => {
    const points = percentages.map((point, i) => {
      const x = xScale(i);
      const y = yScale(point.properties[prop] ?? 0);
      return `${x},${y}`;
    });
    return {
      prop,
      color: getColor(propIndex),
      path: points.join(' '),
      dataPoints: percentages.map((point, i) => ({
        x: xScale(i),
        y: yScale(point.properties[prop] ?? 0),
        value: point.properties[prop] ?? 0,
      })),
    };
  });

  // Y-axis tick marks
  const yTickCount = 5;
  const yTicks: number[] = [];
  const yRange = maxY - minY;
  for (let i = 0; i <= yTickCount; i++) {
    yTicks.push(minY + (yRange * i) / yTickCount);
  }

  return (
    <div className="w-full">
      <svg
        viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`}
        className="w-full"
        role="img"
        aria-label="Evolution property percentage change chart"
      >
        {/* Grid lines */}
        {yTicks.map((tick) => (
          <line
            key={`grid-${tick}`}
            x1={PADDING.left}
            y1={yScale(tick)}
            x2={CHART_WIDTH - PADDING.right}
            y2={yScale(tick)}
            stroke="#374151"
            strokeWidth="0.5"
          />
        ))}

        {/* Zero line */}
        {minY <= 0 && maxY >= 0 && (
          <line
            x1={PADDING.left}
            y1={yScale(0)}
            x2={CHART_WIDTH - PADDING.right}
            y2={yScale(0)}
            stroke="#6b7280"
            strokeWidth="1"
            strokeDasharray="4,4"
          />
        )}

        {/* Y-axis labels */}
        {yTicks.map((tick) => (
          <text
            key={`ylabel-${tick}`}
            x={PADDING.left - 8}
            y={yScale(tick)}
            textAnchor="end"
            dominantBaseline="middle"
            className="fill-gray-400 text-[10px]"
          >
            {tick.toFixed(0)}%
          </text>
        ))}

        {/* X-axis labels */}
        {percentages.map((point, i) => (
          <text
            key={`xlabel-${point.evolution}`}
            x={xScale(i)}
            y={CHART_HEIGHT - PADDING.bottom + 20}
            textAnchor="middle"
            className="fill-gray-400 text-[10px]"
          >
            Evo {point.evolution}
          </text>
        ))}

        {/* Axis lines */}
        <line
          x1={PADDING.left}
          y1={PADDING.top}
          x2={PADDING.left}
          y2={CHART_HEIGHT - PADDING.bottom}
          stroke="#6b7280"
          strokeWidth="1"
        />
        <line
          x1={PADDING.left}
          y1={CHART_HEIGHT - PADDING.bottom}
          x2={CHART_WIDTH - PADDING.right}
          y2={CHART_HEIGHT - PADDING.bottom}
          stroke="#6b7280"
          strokeWidth="1"
        />

        {/* Data lines */}
        {lines.map((line) => (
          <g key={line.prop}>
            <polyline
              points={line.path}
              fill="none"
              stroke={line.color}
              strokeWidth="2"
              strokeLinejoin="round"
              strokeLinecap="round"
            />
            {/* Data point dots */}
            {line.dataPoints.map((dp, i) => (
              <circle
                key={`${line.prop}-${i}`}
                cx={dp.x}
                cy={dp.y}
                r="3"
                fill={line.color}
              />
            ))}
          </g>
        ))}

        {/* Y-axis title */}
        <text
          x={15}
          y={CHART_HEIGHT / 2}
          textAnchor="middle"
          dominantBaseline="middle"
          transform={`rotate(-90, 15, ${CHART_HEIGHT / 2})`}
          className="fill-gray-400 text-[11px]"
        >
          % Change from Base
        </text>
      </svg>

      {/* Legend */}
      <div className="mt-3 flex flex-wrap gap-4 px-2">
        {lines.map((line) => (
          <div key={line.prop} className="flex items-center gap-1.5">
            <span
              className="inline-block h-3 w-3 rounded-sm"
              style={{ backgroundColor: line.color }}
            />
            <span className="text-xs text-gray-300">{line.prop}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
