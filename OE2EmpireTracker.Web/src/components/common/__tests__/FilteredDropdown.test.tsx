import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { FilteredDropdown } from '../FilteredDropdown';

const options = [
  { value: 'iron', label: 'Iron Ore' },
  { value: 'copper', label: 'Copper Ore' },
  { value: 'gold', label: 'Gold Ingot' },
  { value: 'titanium', label: 'Titanium Plate' },
];

describe('FilteredDropdown', () => {
  it('renders with placeholder when no value selected', () => {
    render(<FilteredDropdown options={options} value="" onChange={() => {}} placeholder="Pick one" />);
    const input = screen.getByRole('combobox');
    expect(input).toHaveAttribute('placeholder', 'Pick one');
  });

  it('displays selected option label when value is set', () => {
    render(<FilteredDropdown options={options} value="gold" onChange={() => {}} />);
    const input = screen.getByRole('combobox') as HTMLInputElement;
    expect(input.value).toBe('Gold Ingot');
  });

  it('opens dropdown on focus and shows all options', () => {
    render(<FilteredDropdown options={options} value="" onChange={() => {}} />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    const listbox = screen.getByRole('listbox');
    expect(listbox).toBeInTheDocument();
    const items = screen.getAllByRole('option');
    expect(items).toHaveLength(4);
  });

  it('filters options as user types', () => {
    render(<FilteredDropdown options={options} value="" onChange={() => {}} />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'ore' } });
    const items = screen.getAllByRole('option');
    expect(items).toHaveLength(2);
    expect(items[0]).toHaveTextContent('Iron Ore');
    expect(items[1]).toHaveTextContent('Copper Ore');
  });

  it('shows no matches message when filter yields nothing', () => {
    render(<FilteredDropdown options={options} value="" onChange={() => {}} />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'zzz' } });
    expect(screen.getByText('No matches')).toBeInTheDocument();
  });

  it('calls onChange when an option is clicked', () => {
    const handleChange = vi.fn();
    render(<FilteredDropdown options={options} value="" onChange={handleChange} />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.click(screen.getByText('Gold Ingot'));
    expect(handleChange).toHaveBeenCalledWith('gold');
  });

  it('closes dropdown after selection', () => {
    const handleChange = vi.fn();
    render(<FilteredDropdown options={options} value="" onChange={handleChange} />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.click(screen.getByText('Iron Ore'));
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('uses custom filterFn when provided', () => {
    const customFilter = (opt: { value: string; label: string }, q: string) =>
      opt.value.startsWith(q);
    render(
      <FilteredDropdown options={options} value="" onChange={() => {}} filterFn={customFilter} />,
    );
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'go' } });
    const items = screen.getAllByRole('option');
    expect(items).toHaveLength(1);
    expect(items[0]).toHaveTextContent('Gold Ingot');
  });

  it('filter is case-insensitive by default', () => {
    render(<FilteredDropdown options={options} value="" onChange={() => {}} />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'IRON' } });
    const items = screen.getAllByRole('option');
    expect(items).toHaveLength(1);
    expect(items[0]).toHaveTextContent('Iron Ore');
  });
});
