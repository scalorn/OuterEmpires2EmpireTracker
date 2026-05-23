import { useState, useRef, useEffect, useCallback } from 'react';

export interface FilteredDropdownProps {
  options: { value: string; label: string }[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  filterFn?: (option: { value: string; label: string }, query: string) => boolean;
}

function defaultFilter(option: { value: string; label: string }, query: string): boolean {
  return option.label.toLowerCase().includes(query.toLowerCase());
}

export function FilteredDropdown({
  options,
  value,
  onChange,
  placeholder = 'Select...',
  filterFn,
}: FilteredDropdownProps) {
  const [query, setQuery] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const filter = filterFn ?? defaultFilter;
  const filtered = query ? options.filter((opt) => filter(opt, query)) : options;
  const selectedOption = options.find((opt) => opt.value === value);

  const handleSelect = useCallback(
    (optionValue: string) => {
      onChange(optionValue);
      setQuery('');
      setIsOpen(false);
    },
    [onChange],
  );

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(e.target.value);
    setIsOpen(true);
  };

  const handleInputFocus = () => {
    setIsOpen(true);
  };

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
        setQuery('');
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div ref={containerRef} className="relative">
      <input
        ref={inputRef}
        type="text"
        value={query || (isOpen ? '' : selectedOption?.label ?? '')}
        onChange={handleInputChange}
        onFocus={handleInputFocus}
        placeholder={placeholder}
        className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
        role="combobox"
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-autocomplete="list"
      />
      {isOpen && (
        <ul
          role="listbox"
          className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded border border-gray-600 bg-gray-700 py-1 shadow-lg"
        >
          {filtered.length === 0 ? (
            <li className="px-3 py-1.5 text-sm text-gray-400">No matches</li>
          ) : (
            filtered.map((opt) => (
              <li
                key={opt.value}
                role="option"
                aria-selected={opt.value === value}
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => handleSelect(opt.value)}
                className={`cursor-pointer px-3 py-1.5 text-sm hover:bg-blue-600 hover:text-white ${
                  opt.value === value ? 'bg-blue-500 text-white' : 'text-gray-200'
                }`}
              >
                {opt.label}
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
}
