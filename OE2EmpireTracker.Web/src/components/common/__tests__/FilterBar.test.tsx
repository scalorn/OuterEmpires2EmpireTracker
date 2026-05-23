import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { FilterBar } from '../FilterBar';
import type { FilterDefinition, FilterValues } from '../FilterBar';

describe('FilterBar (new interface)', () => {
  it('renders a text filter with placeholder', () => {
    const filters: FilterDefinition[] = [
      { type: 'text', key: 'search', placeholder: 'Search items...' },
    ];
    const values: FilterValues = { search: '' };
    render(<FilterBar filters={filters} values={values} onChange={() => {}} />);
    const input = screen.getByPlaceholderText('Search items...');
    expect(input).toBeInTheDocument();
  });

  it('renders a dropdown filter with options', () => {
    const filters: FilterDefinition[] = [
      {
        type: 'dropdown',
        key: 'type',
        label: 'Type',
        options: [
          { value: 'planet', label: 'Planet' },
          { value: 'asteroid', label: 'Asteroid' },
        ],
      },
    ];
    const values: FilterValues = { type: '' };
    render(<FilterBar filters={filters} values={values} onChange={() => {}} />);
    const select = screen.getByLabelText('Type');
    expect(select).toBeInTheDocument();
    expect(screen.getByText('Planet')).toBeInTheDocument();
    expect(screen.getByText('Asteroid')).toBeInTheDocument();
  });

  it('renders a checkbox filter with label', () => {
    const filters: FilterDefinition[] = [
      { type: 'checkbox', key: 'active', label: 'Active only' },
    ];
    const values: FilterValues = { active: false };
    render(<FilterBar filters={filters} values={values} onChange={() => {}} />);
    const checkbox = screen.getByLabelText('Active only');
    expect(checkbox).toBeInTheDocument();
    expect(checkbox).not.toBeChecked();
  });

  it('emits updated values when text input changes', () => {
    const handleChange = vi.fn();
    const filters: FilterDefinition[] = [
      { type: 'text', key: 'search', placeholder: 'Search...' },
    ];
    const values: FilterValues = { search: '' };
    render(<FilterBar filters={filters} values={values} onChange={handleChange} />);
    fireEvent.change(screen.getByPlaceholderText('Search...'), {
      target: { value: 'hello' },
    });
    expect(handleChange).toHaveBeenCalledWith({ search: 'hello' });
  });

  it('emits updated values when dropdown changes', () => {
    const handleChange = vi.fn();
    const filters: FilterDefinition[] = [
      {
        type: 'dropdown',
        key: 'type',
        label: 'Type',
        options: [
          { value: 'planet', label: 'Planet' },
          { value: 'asteroid', label: 'Asteroid' },
        ],
      },
    ];
    const values: FilterValues = { type: '' };
    render(<FilterBar filters={filters} values={values} onChange={handleChange} />);
    fireEvent.change(screen.getByLabelText('Type'), {
      target: { value: 'asteroid' },
    });
    expect(handleChange).toHaveBeenCalledWith({ type: 'asteroid' });
  });

  it('emits updated values when checkbox is toggled', () => {
    const handleChange = vi.fn();
    const filters: FilterDefinition[] = [
      { type: 'checkbox', key: 'active', label: 'Active only' },
    ];
    const values: FilterValues = { active: false };
    render(<FilterBar filters={filters} values={values} onChange={handleChange} />);
    fireEvent.click(screen.getByLabelText('Active only'));
    expect(handleChange).toHaveBeenCalledWith({ active: true });
  });

  it('renders Clear button when onClear is provided', () => {
    const handleClear = vi.fn();
    const filters: FilterDefinition[] = [
      { type: 'text', key: 'search' },
    ];
    const values: FilterValues = { search: 'test' };
    render(
      <FilterBar filters={filters} values={values} onChange={() => {}} onClear={handleClear} />,
    );
    const clearBtn = screen.getByText('Clear');
    fireEvent.click(clearBtn);
    expect(handleClear).toHaveBeenCalled();
  });

  it('renders multiple filter types together', () => {
    const filters: FilterDefinition[] = [
      { type: 'text', key: 'search', placeholder: 'Search...' },
      { type: 'dropdown', key: 'category', label: 'Category', options: [{ value: 'a', label: 'A' }] },
      { type: 'checkbox', key: 'flagged', label: 'Flagged' },
    ];
    const values: FilterValues = { search: '', category: '', flagged: false };
    render(<FilterBar filters={filters} values={values} onChange={() => {}} />);
    expect(screen.getByPlaceholderText('Search...')).toBeInTheDocument();
    expect(screen.getByLabelText('Category')).toBeInTheDocument();
    expect(screen.getByLabelText('Flagged')).toBeInTheDocument();
  });
});

