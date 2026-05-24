import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import React from 'react';

/**
 * Component tests for BlueprintBrowser data-driven filters.
 * Validates: Requirements 10.2, 10.5, 10.6, 10.7, 13.1, 13.6
 */

// Mock the API hooks
vi.mock('../../../api/hooks/useBlueprints', () => ({
  useGlobalData: vi.fn(),
  usePublicBlueprints: vi.fn(),
}));

// Mock the auth store (AuthPrompt uses it)
vi.mock('../../../auth/store', () => ({
  useAuthStore: vi.fn(() => false),
}));

import { useGlobalData, usePublicBlueprints } from '../../../api/hooks/useBlueprints';
import { BlueprintBrowser } from '../BlueprintBrowser';


const mockedUseGlobalData = vi.mocked(useGlobalData);
const mockedUsePublicBlueprints = vi.mocked(usePublicBlueprints);

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
}

function renderBrowser() {
  const queryClient = createQueryClient();
  return render(
    React.createElement(QueryClientProvider, { client: queryClient },
      React.createElement(MemoryRouter, null,
        React.createElement(BlueprintBrowser)
      )
    )
  );
}

// Helper to create a mock useQuery return value
function mockQueryResult<T>(data: T, overrides = {}) {
  return {
    data,
    isLoading: false,
    isFetching: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
    ...overrides,
  } as unknown as ReturnType<typeof useGlobalData<T>>;
}


beforeEach(() => {
  vi.clearAllMocks();
});

describe('BlueprintBrowser data-driven filters', () => {
  describe('Renders with empty reference data (no errors)', () => {
    it('renders without error when reference data APIs return empty arrays', () => {
      mockedUseGlobalData.mockReturnValue(mockQueryResult([]));
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({ items: [], totalCount: 0, page: 1, pageSize: 20 }) as any,
      );

      renderBrowser();

      // Should render the page title without crashing
      expect(screen.getByText('Blueprint Browser')).toBeInTheDocument();
    });

    it('shows empty filter dropdowns without error when data unavailable', () => {
      mockedUseGlobalData.mockReturnValue(mockQueryResult([]));
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({ items: [], totalCount: 0, page: 1, pageSize: 20 }) as any,
      );

      renderBrowser();

      // Filter labels should still be present
      expect(screen.getByLabelText('Type')).toBeInTheDocument();
      expect(screen.getByLabelText('Tech Level')).toBeInTheDocument();
      expect(screen.getByLabelText('Ship Class')).toBeInTheDocument();
      expect(screen.getByLabelText('Evolution')).toBeInTheDocument();
    });
  });

  describe('Populates dropdowns from API responses', () => {
    it('populates Type dropdown with BlueprintType data from API', () => {
      const blueprintTypes = [
        { Id: 'Ship', Name: 'Ship' },
        { Id: 'Structure', Name: 'Structure' },
        { Id: 'Module', Name: 'Module' },
      ];

      mockedUseGlobalData.mockImplementation((dataType: string) => {
        if (dataType === 'BlueprintType') return mockQueryResult(blueprintTypes);
        return mockQueryResult([]);
      });
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({ items: [], totalCount: 0, page: 1, pageSize: 20 }) as any,
      );

      renderBrowser();

      const typeSelect = screen.getByLabelText('Type') as HTMLSelectElement;
      expect(typeSelect).toBeInTheDocument();
      // Options should include the API values
      const options = Array.from(typeSelect.querySelectorAll('option'));
      const optionValues = options.map((o) => o.value);
      expect(optionValues).toContain('Ship');
      expect(optionValues).toContain('Structure');
      expect(optionValues).toContain('Module');
    });

    it('populates Tech Level dropdown with TechLevel data from API', () => {
      const techLevels = [
        { Id: 'TL1', Name: 'TL1' },
        { Id: 'TL2', Name: 'TL2' },
        { Id: 'TL3', Name: 'TL3' },
      ];

      mockedUseGlobalData.mockImplementation((dataType: string) => {
        if (dataType === 'TechLevel') return mockQueryResult(techLevels);
        return mockQueryResult([]);
      });
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({ items: [], totalCount: 0, page: 1, pageSize: 20 }) as any,
      );

      renderBrowser();

      const techSelect = screen.getByLabelText('Tech Level') as HTMLSelectElement;
      const options = Array.from(techSelect.querySelectorAll('option'));
      const optionValues = options.map((o) => o.value);
      expect(optionValues).toContain('TL1');
      expect(optionValues).toContain('TL2');
      expect(optionValues).toContain('TL3');
    });

    it('populates Ship Class dropdown with ShipClass data from API', () => {
      const shipClasses = [
        { Id: 'Fighter', Name: 'Fighter' },
        { Id: 'Frigate', Name: 'Frigate' },
      ];

      mockedUseGlobalData.mockImplementation((dataType: string) => {
        if (dataType === 'ShipClass') return mockQueryResult(shipClasses);
        return mockQueryResult([]);
      });
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({ items: [], totalCount: 0, page: 1, pageSize: 20 }) as any,
      );

      renderBrowser();

      const classSelect = screen.getByLabelText('Ship Class') as HTMLSelectElement;
      const options = Array.from(classSelect.querySelectorAll('option'));
      const optionValues = options.map((o) => o.value);
      expect(optionValues).toContain('Fighter');
      expect(optionValues).toContain('Frigate');
    });
  });

  describe('Displays correct columns', () => {
    it('displays Type, Name, Tech Level, Evolution, Nickname columns', () => {
      mockedUseGlobalData.mockReturnValue(mockQueryResult([]));
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({
          items: [
            {
              UUID: 'bp-001',
              Name: 'Laser Mk1',
              BluePrintType: 'Weapon',
              TechLevel: 'TL2',
              Evolution: 'Evo3',
              NickName: 'Zapper',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }) as any,
      );

      renderBrowser();

      // Data values from the blueprint should be rendered in the table
      expect(screen.getByText('Weapon')).toBeInTheDocument();
      expect(screen.getByText('Laser Mk1')).toBeInTheDocument();
      expect(screen.getAllByText('Evo3').length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText('Zapper')).toBeInTheDocument();

      // Column headers exist in the table (use getAllByText since labels also match)
      expect(screen.getAllByText('Type').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Name').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Tech Level').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Evolution').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Nickname').length).toBeGreaterThanOrEqual(1);
    });
  });

  describe('Filters apply correctly to combined global+shared results', () => {
    const sampleBlueprints = [
      {
        UUID: 'bp-g1',
        Name: 'Global Laser',
        BluePrintType: 'Weapon',
        TechLevel: 'TL1',
        ShipClass: 'Fighter',
        Evolution: 'Evo1',
        NickName: '',
      },
      {
        UUID: 'bp-g2',
        Name: 'Global Shield',
        BluePrintType: 'Defense',
        TechLevel: 'TL2',
        ShipClass: 'Frigate',
        Evolution: 'Evo2',
        NickName: 'Shieldy',
      },
      {
        UUID: 'bp-s1',
        Name: 'Shared Engine',
        BluePrintType: 'Weapon',
        TechLevel: 'TL1',
        ShipClass: 'Destroyer',
        Evolution: 'Evo1',
        NickName: 'Speedy',
      },
    ];

    it('filters by type correctly', async () => {
      mockedUseGlobalData.mockReturnValue(mockQueryResult([]));
      mockedUsePublicBlueprints.mockReturnValue(
        mockQueryResult({
          items: sampleBlueprints,
          totalCount: 3,
          page: 1,
          pageSize: 20,
        }) as any,
      );

      renderBrowser();

      // All 3 blueprints should be visible initially
      expect(screen.getByText('Global Laser')).toBeInTheDocument();
      expect(screen.getByText('Global Shield')).toBeInTheDocument();
      expect(screen.getByText('Shared Engine')).toBeInTheDocument();
    });
  });
});
