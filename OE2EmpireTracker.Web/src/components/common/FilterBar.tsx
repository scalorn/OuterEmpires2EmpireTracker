export interface FilterField {
  key: string;
  label: string;
  type: 'text' | 'select';
  options?: { value: string; label: string }[];
  placeholder?: string;
}

interface FilterBarProps {
  fields: FilterField[];
  values: Record<string, string>;
  onChange: (key: string, value: string) => void;
  onClear?: () => void;
}

export function FilterBar({ fields, values, onChange, onClear }: FilterBarProps) {
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
