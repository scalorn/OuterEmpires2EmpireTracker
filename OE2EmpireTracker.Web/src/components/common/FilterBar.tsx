/**
 * FilterBar component — renders a configurable set of filter controls.
 * Supports text search inputs, dropdown (select) filters, and checkbox filters.
 * Generic and reusable across all entity list pages.
 *
 * Requirements: 10.3
 */

// --- New generic filter definition types ---

export interface TextFilterDef {
  type: 'text';
  key: string;
  placeholder?: string;
}

export interface DropdownFilterDef {
  type: 'dropdown';
  key: string;
  label: string;
  options: { value: string; label: string }[];
}

export interface CheckboxFilterDef {
  type: 'checkbox';
  key: string;
  label: string;
}

export type FilterDefinition = TextFilterDef | DropdownFilterDef | CheckboxFilterDef;

export type FilterValues = Record<string, string | boolean>;

export interface FilterBarProps {
  filters: FilterDefinition[];
  values: FilterValues;
  onChange: (values: FilterValues) => void;
  onClear?: () => void;
}

// --- Legacy interface for backward compatibility ---

export interface FilterField {
  key: string;
  label: string;
  type: 'text' | 'select';
  options?: { value: string; label: string }[];
  placeholder?: string;
}

interface LegacyFilterBarProps {
  fields: FilterField[];
  values: Record<string, string>;
  onChange: (key: string, value: string) => void;
  onClear?: () => void;
}


function isLegacyProps(
  props: FilterBarProps | LegacyFilterBarProps,
): props is LegacyFilterBarProps {
  return 'fields' in props;
}

/**
 * FilterBar supports two calling conventions:
 * 1. New: filters + values + onChange(values)
 * 2. Legacy: fields + values + onChange(key, value)
 */
export function FilterBar(props: FilterBarProps | LegacyFilterBarProps) {
  if (isLegacyProps(props)) {
    return <LegacyFilterBar {...props} />;
  }
  return <NewFilterBar {...props} />;
}

function LegacyFilterBar({ fields, values, onChange, onClear }: LegacyFilterBarProps) {
  return (
    <div className="mb-4 flex flex-wrap items-end gap-3">
      {fields.map((field) => (
        <div key={field.key} className="flex flex-col">
          <label
            htmlFor={`filter-${field.key}`}
            className="mb-1 text-xs font-medium text-gray-400"
          >
            {field.label}
          </label>
          {field.type === 'select' ? (
            <select
              id={`filter-${field.key}`}
              value={values[field.key] ?? ''}
              onChange={(e) => onChange(field.key, e.target.value)}
              className="rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white"
            >
              <option value="">All</option>
              {field.options?.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          ) : (
            <input
              id={`filter-${field.key}`}
              type="text"
              value={values[field.key] ?? ''}
              onChange={(e) => onChange(field.key, e.target.value)}
              placeholder={field.placeholder ?? `Filter by ${field.label.toLowerCase()}`}
              className="rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500"
            />
          )}
        </div>
      ))}
      {onClear && (
        <button
          onClick={onClear}
          className="rounded border border-gray-600 px-3 py-1.5 text-sm text-gray-300 hover:bg-gray-700"
        >
          Clear
        </button>
      )}
    </div>
  );
}

function NewFilterBar({ filters, values, onChange, onClear }: FilterBarProps) {
  const handleChange = (key: string, value: string | boolean) => {
    onChange({ ...values, [key]: value });
  };

  return (
    <div className="mb-4 flex flex-wrap items-end gap-3">
      {filters.map((filter) => {
        switch (filter.type) {
          case 'text':
            return (
              <div key={filter.key} className="flex flex-col">
                <label
                  htmlFor={`filter-${filter.key}`}
                  className="mb-1 text-xs font-medium text-gray-400"
                >
                  Search
                </label>
                <input
                  id={`filter-${filter.key}`}
                  type="text"
                  value={(values[filter.key] as string) ?? ''}
                  onChange={(e) => handleChange(filter.key, e.target.value)}
                  placeholder={filter.placeholder ?? 'Search...'}
                  className="rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500"
                />
              </div>
            );
          case 'dropdown':
            return (
              <div key={filter.key} className="flex flex-col">
                <label
                  htmlFor={`filter-${filter.key}`}
                  className="mb-1 text-xs font-medium text-gray-400"
                >
                  {filter.label}
                </label>
                <select
                  id={`filter-${filter.key}`}
                  value={(values[filter.key] as string) ?? ''}
                  onChange={(e) => handleChange(filter.key, e.target.value)}
                  className="rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white"
                >
                  <option value="">All</option>
                  {filter.options.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </div>
            );
          case 'checkbox':
            return (
              <div key={filter.key} className="flex items-center gap-2 pb-1">
                <input
                  id={`filter-${filter.key}`}
                  type="checkbox"
                  checked={Boolean(values[filter.key])}
                  onChange={(e) => handleChange(filter.key, e.target.checked)}
                  className="h-4 w-4 rounded border-gray-600 bg-gray-700 text-blue-500 focus:ring-blue-500"
                />
                <label
                  htmlFor={`filter-${filter.key}`}
                  className="text-sm text-gray-300"
                >
                  {filter.label}
                </label>
              </div>
            );
          default:
            return null;
        }
      })}
      {onClear && (
        <button
          onClick={onClear}
          className="rounded border border-gray-600 px-3 py-1.5 text-sm text-gray-300 hover:bg-gray-700"
        >
          Clear
        </button>
      )}
    </div>
  );
}
