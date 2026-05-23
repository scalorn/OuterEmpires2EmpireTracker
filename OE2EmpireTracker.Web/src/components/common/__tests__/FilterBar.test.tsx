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

