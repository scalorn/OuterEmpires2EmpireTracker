import { useState, useRef, useEffect, type ReactNode, type KeyboardEvent } from 'react';

export interface GridColumn<T> {
  key: string;
  header: string;
  type: 'text' | 'number' | 'select' | 'readonly';
  options?: { value: string; label: string }[];
  render?: (row: T) => ReactNode;
}

export interface EditableGridProps<T> {
  columns: GridColumn<T>[];
  rows: T[];
  onRowChange: (index: number, row: T) => void;
  onRowAdd: () => void;
  onRowRemove: (index: number) => void;
  keyExtractor: (row: T) => string;
}

interface EditingCell {
  rowIndex: number;
  colKey: string;
}

export function EditableGrid<T>({
  columns,
  rows,
  onRowChange,
  onRowAdd,
  onRowRemove,
  keyExtractor,
}: EditableGridProps<T>) {
  const [editingCell, setEditingCell] = useState<EditingCell | null>(null);
  const [editValue, setEditValue] = useState<string>('');
  const inputRef = useRef<HTMLInputElement | HTMLSelectElement | null>(null);

  useEffect(() => {
    if (editingCell && inputRef.current) {
      inputRef.current.focus();
    }
  }, [editingCell]);

  const startEdit = (rowIndex: number, colKey: string, currentValue: string) => {
    setEditingCell({ rowIndex, colKey });
    setEditValue(currentValue);
  };

  const commitEdit = () => {
    if (!editingCell) return;
    const { rowIndex, colKey } = editingCell;
    const row = rows[rowIndex];
    const col = columns.find((c) => c.key === colKey);
    if (!row || !col) {
      setEditingCell(null);
      return;
    }

    let finalValue: string | number = editValue;
    if (col.type === 'number') {
      const parsed = Number(editValue);
      finalValue = Number.isNaN(parsed) ? 0 : parsed;
    }

    const updatedRow = { ...row, [colKey]: finalValue } as T;
    setEditingCell(null);
    onRowChange(rowIndex, updatedRow);
  };

  const cancelEdit = () => {
    setEditingCell(null);
  };

  const handleKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      commitEdit();
    } else if (e.key === 'Escape') {
      e.preventDefault();
      cancelEdit();
    }
  };

  const getCellValue = (row: T, colKey: string): string => {
    const value = (row as Record<string, unknown>)[colKey];
    return value == null ? '' : String(value);
  };

  const renderCell = (row: T, col: GridColumn<T>, rowIndex: number) => {
    const isEditing =
      editingCell?.rowIndex === rowIndex && editingCell?.colKey === col.key;

    if (col.type === 'readonly') {
      if (col.render) return col.render(row);
      return <span>{getCellValue(row, col.key)}</span>;
    }

    if (isEditing) {
      if (col.type === 'select') {
        return (
          <select
            ref={(el) => { inputRef.current = el; }}
            value={editValue}
            onChange={(e) => {
              setEditValue(e.target.value);
            }}
            onBlur={commitEdit}
            onKeyDown={handleKeyDown}
            className="w-full rounded border border-blue-500 bg-gray-700 px-2 py-1 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
          >
            <option value="">—</option>
            {col.options?.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        );
      }

      return (
        <input
          ref={(el) => { inputRef.current = el; }}
          type={col.type === 'number' ? 'number' : 'text'}
          value={editValue}
          onChange={(e) => setEditValue(e.target.value)}
          onBlur={commitEdit}
          onKeyDown={handleKeyDown}
          className="w-full rounded border border-blue-500 bg-gray-700 px-2 py-1 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
        />
      );
    }

    // Read mode — click to edit
    const displayValue = col.render
      ? col.render(row)
      : col.type === 'select'
        ? col.options?.find((o) => o.value === getCellValue(row, col.key))?.label ??
          getCellValue(row, col.key)
        : getCellValue(row, col.key);

    return (
      <button
        type="button"
        onClick={() => startEdit(rowIndex, col.key, getCellValue(row, col.key))}
        className="w-full cursor-pointer rounded px-2 py-1 text-left hover:bg-gray-600"
        aria-label={`Edit ${col.header}`}
      >
        {displayValue || <span className="text-gray-500">—</span>}
      </button>
    );
  };

  return (
    <div className="overflow-x-auto rounded border border-gray-700">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-gray-700 bg-gray-800">
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                className="px-4 py-3 font-medium text-gray-300"
              >
                {col.header}
              </th>
            ))}
            <th className="px-4 py-3 font-medium text-gray-300 w-16">
              <span className="sr-only">Actions</span>
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-700">
          {rows.map((row, rowIndex) => (
            <tr key={keyExtractor(row)} className="hover:bg-gray-800/50">
              {columns.map((col) => (
                <td key={col.key} className="px-4 py-2 text-gray-200">
                  {renderCell(row, col, rowIndex)}
                </td>
              ))}
              <td className="px-4 py-2">
                <button
                  type="button"
                  onClick={() => onRowRemove(rowIndex)}
                  className="rounded px-2 py-1 text-sm text-red-400 hover:bg-red-900/30 hover:text-red-300"
                  aria-label="Remove row"
                >
                  ✕
                </button>
              </td>
            </tr>
          ))}
          {rows.length === 0 && (
            <tr>
              <td
                colSpan={columns.length + 1}
                className="px-4 py-6 text-center text-sm text-gray-500"
              >
                No rows. Click "Add Row" to get started.
              </td>
            </tr>
          )}
        </tbody>
      </table>
      <div className="border-t border-gray-700 bg-gray-800 px-4 py-2">
        <button
          type="button"
          onClick={onRowAdd}
          className="rounded border border-gray-600 px-3 py-1.5 text-sm text-gray-300 hover:bg-gray-700 hover:text-white"
        >
          + Add Row
        </button>
      </div>
    </div>
  );
}
