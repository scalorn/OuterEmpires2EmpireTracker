import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { EditableGrid, type GridColumn } from '../EditableGrid';

interface TestRow {
  id: string;
  name: string;
  quantity: number;
  category: string;
}

const columns: GridColumn<TestRow>[] = [
  { key: 'name', header: 'Name', type: 'text' },
  { key: 'quantity', header: 'Quantity', type: 'number' },
  {
    key: 'category',
    header: 'Category',
    type: 'select',
    options: [
      { value: 'ore', label: 'Ore' },
      { value: 'gas', label: 'Gas' },
    ],
  },
  { key: 'id', header: 'ID', type: 'readonly' },
];

const rows: TestRow[] = [
  { id: '1', name: 'Iron', quantity: 100, category: 'ore' },
  { id: '2', name: 'Helium', quantity: 50, category: 'gas' },
];

describe('EditableGrid', () => {
  it('renders column headers', () => {
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={vi.fn()}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Quantity')).toBeInTheDocument();
    expect(screen.getByText('Category')).toBeInTheDocument();
    expect(screen.getByText('ID')).toBeInTheDocument();
  });

  it('renders row data in read mode', () => {
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={vi.fn()}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    expect(screen.getByText('Iron')).toBeInTheDocument();
    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.getByText('Ore')).toBeInTheDocument();
    // Readonly column renders value directly
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('enters edit mode on cell click', () => {
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={vi.fn()}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    fireEvent.click(screen.getByText('Iron'));
    const input = screen.getByDisplayValue('Iron');
    expect(input).toBeInTheDocument();
    expect(input.tagName).toBe('INPUT');
  });

  it('commits edit on blur and calls onRowChange', () => {
    const onRowChange = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={onRowChange}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    fireEvent.click(screen.getByText('Iron'));
    const input = screen.getByDisplayValue('Iron');
    fireEvent.change(input, { target: { value: 'Copper' } });
    fireEvent.blur(input);

    expect(onRowChange).toHaveBeenCalledWith(0, {
      id: '1',
      name: 'Copper',
      quantity: 100,
      category: 'ore',
    });
  });

  it('commits edit on Enter key', () => {
    const onRowChange = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={onRowChange}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    fireEvent.click(screen.getByText('Iron'));
    const input = screen.getByDisplayValue('Iron');
    fireEvent.change(input, { target: { value: 'Copper' } });
    fireEvent.keyDown(input, { key: 'Enter' });

    expect(onRowChange).toHaveBeenCalledWith(0, {
      id: '1',
      name: 'Copper',
      quantity: 100,
      category: 'ore',
    });
  });

  it('cancels edit on Escape key', () => {
    const onRowChange = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={onRowChange}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    fireEvent.click(screen.getByText('Iron'));
    const input = screen.getByDisplayValue('Iron');
    fireEvent.change(input, { target: { value: 'Copper' } });
    fireEvent.keyDown(input, { key: 'Escape' });

    expect(onRowChange).not.toHaveBeenCalled();
    // Should return to read mode showing original value
    expect(screen.getByText('Iron')).toBeInTheDocument();
  });

  it('parses number type cells as numbers', () => {
    const onRowChange = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={onRowChange}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    fireEvent.click(screen.getByText('100'));
    const input = screen.getByDisplayValue('100');
    fireEvent.change(input, { target: { value: '250' } });
    fireEvent.blur(input);

    expect(onRowChange).toHaveBeenCalledWith(0, {
      id: '1',
      name: 'Iron',
      quantity: 250,
      category: 'ore',
    });
  });

  it('calls onRowAdd when Add Row button is clicked', () => {
    const onRowAdd = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={vi.fn()}
        onRowAdd={onRowAdd}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    fireEvent.click(screen.getByText('+ Add Row'));
    expect(onRowAdd).toHaveBeenCalledTimes(1);
  });

  it('calls onRowRemove when remove button is clicked', () => {
    const onRowRemove = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={vi.fn()}
        onRowAdd={vi.fn()}
        onRowRemove={onRowRemove}
        keyExtractor={(r) => r.id}
      />,
    );

    const removeButtons = screen.getAllByLabelText('Remove row');
    fireEvent.click(removeButtons[0]);
    expect(onRowRemove).toHaveBeenCalledWith(0);
  });

  it('shows empty state when no rows', () => {
    render(
      <EditableGrid
        columns={columns}
        rows={[]}
        onRowChange={vi.fn()}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r: TestRow) => r.id}
      />,
    );

    expect(screen.getByText(/No rows/)).toBeInTheDocument();
  });

  it('does not allow editing readonly cells', () => {
    const onRowChange = vi.fn();
    render(
      <EditableGrid
        columns={columns}
        rows={rows}
        onRowChange={onRowChange}
        onRowAdd={vi.fn()}
        onRowRemove={vi.fn()}
        keyExtractor={(r) => r.id}
      />,
    );

    // Readonly cell renders as a span, not a button
    const readonlyCells = screen.getAllByText('1');
    // Click on the readonly cell — it should not enter edit mode
    fireEvent.click(readonlyCells[0]);
    expect(screen.queryByDisplayValue('1')).not.toBeInTheDocument();
  });
});
