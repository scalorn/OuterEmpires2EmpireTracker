import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import React from 'react';

/**
 * Unit tests for SurveyDetail reserve display states.
 * Validates: Requirements 3.2, 3.4, 4.1, 4.2, 4.5, 4.6
 */

// Mock the API hooks
vi.mock('../../../api/hooks/useSurveys', () => ({
  usePublicSurveys: vi.fn(),
}));

vi.mock('../../../api/hooks/useAsteroids', () => ({
  usePublicAsteroidDetail: vi.fn(),
}));

import { usePublicSurveys } from '../../../api/hooks/useSurveys';
import { usePublicAsteroidDetail } from '../../../api/hooks/useAsteroids';
import { SurveyDetail } from '../SurveyDetail';

const mockedUsePublicSurveys = vi.mocked(usePublicSurveys);
const mockedUsePublicAsteroidDetail = vi.mocked(usePublicAsteroidDetail);

function createQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

function renderSurveyDetail(surveyId: string) {
  const queryClient = createQueryClient();
  return render(
    React.createElement(
      QueryClientProvider,
      { client: queryClient },
      React.createElement(
        MemoryRouter,
        { initialEntries: [`/surveys/${surveyId}`] },
        React.createElement(
          Routes,
          null,
          React.createElement(Route, {
            path: '/surveys/:id',
            element: React.createElement(SurveyDetail),
          }),
        ),
      ),
    ),
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
  } as any;
}

// Sample asteroid survey with resources
function makeAsteroidSurvey(id: string) {
  return {
    surveyID: id,
    surveyType: 'asteroid',
    asteroidUUID: 'ast-uuid-001',
    systemName: 'Alpha Centauri',
    planetName: 'Asteroid Belt 1',
    scannedBy: 'Player1',
    dateTime: '2024-01-15T10:00:00Z',
    resources: {
      r1: { resource: 'Iron', purity: 'High', amount: '50' },
      r2: { resource: 'Copper', purity: 'Medium', amount: '30' },
    },
  };
}

// Sample planet survey with resources
function makePlanetSurvey(id: string) {
  return {
    surveyID: id,
    surveyType: 'planet',
    systemName: 'Sol',
    planetName: 'Earth',
    scannedBy: 'Player2',
    dateTime: '2024-02-20T12:00:00Z',
    resources: {
      r1: { resource: 'Water', purity: 'High', amount: '100' },
      r2: { resource: 'Silicon', purity: 'Low', amount: '25' },
    },
  };
}

beforeEach(() => {
  vi.clearAllMocks();
});


describe('SurveyDetail reserve display states', () => {
  describe('Loading indicator (Req 3.2)', () => {
    it('shows "Loading reserves..." while asteroid data is loading', () => {
      const survey = makeAsteroidSurvey('survey-1');
      mockedUsePublicSurveys.mockReturnValue(
        mockQueryResult([survey]),
      );
      mockedUsePublicAsteroidDetail.mockReturnValue(
        mockQueryResult(undefined, { isLoading: true }),
      );

      renderSurveyDetail('survey-1');

      expect(screen.getByText('Loading reserves...')).toBeInTheDocument();
    });
  });

  describe('Fetch failure (Req 3.4)', () => {
    it('shows resources without error message when asteroid fetch fails', () => {
      const survey = makeAsteroidSurvey('survey-2');
      mockedUsePublicSurveys.mockReturnValue(
        mockQueryResult([survey]),
      );
      mockedUsePublicAsteroidDetail.mockReturnValue(
        mockQueryResult(undefined, { isError: true }),
      );

      renderSurveyDetail('survey-2');

      // Resources should still be displayed
      expect(screen.getByText('Iron')).toBeInTheDocument();
      expect(screen.getByText('Copper')).toBeInTheDocument();
      // No error message should be shown
      expect(screen.queryByText(/failed/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/error/i)).not.toBeInTheDocument();
    });
  });

  describe('Planet survey hides Max Reserve column (Req 4.5)', () => {
    it('does not show "Max Reserve" column header for Planet surveys', () => {
      const survey = makePlanetSurvey('survey-3');
      mockedUsePublicSurveys.mockReturnValue(
        mockQueryResult([survey]),
      );
      mockedUsePublicAsteroidDetail.mockReturnValue(
        mockQueryResult(undefined, { isLoading: false }),
      );

      renderSurveyDetail('survey-3');

      // Resources should be displayed
      expect(screen.getByText('Water')).toBeInTheDocument();
      expect(screen.getByText('Silicon')).toBeInTheDocument();
      // Max Reserve column header should NOT be present
      expect(screen.queryByText('Max Reserve')).not.toBeInTheDocument();
    });
  });


  describe('404 asteroid shows dashes (Req 4.6)', () => {
    it('displays dashes for all reserve cells when asteroid is not found', () => {
      const survey = makeAsteroidSurvey('survey-4');
      mockedUsePublicSurveys.mockReturnValue(
        mockQueryResult([survey]),
      );
      mockedUsePublicAsteroidDetail.mockReturnValue(
        mockQueryResult(undefined, { isError: true, isLoading: false }),
      );

      renderSurveyDetail('survey-4');

      // Max Reserve column should be present (it's an asteroid survey)
      expect(screen.getByText('Max Reserve')).toBeInTheDocument();
      // All reserve cells should show dashes
      const dashCells = screen.getAllByText('-');
      expect(dashCells.length).toBeGreaterThanOrEqual(2); // one per resource row
    });
  });

  describe('Zero maxReserve displays as "0" (Req 4.1)', () => {
    it('displays "0" for a reserve with maxReserve of zero', () => {
      const survey = makeAsteroidSurvey('survey-5');
      mockedUsePublicSurveys.mockReturnValue(
        mockQueryResult([survey]),
      );
      mockedUsePublicAsteroidDetail.mockReturnValue(
        mockQueryResult({
          uuid: 'ast-uuid-001',
          name: 'Test Asteroid',
          reserves: [
            { resourceName: 'Iron', purity: 'High', maxReserve: 0 },
            { resourceName: 'Copper', purity: 'Medium', maxReserve: 5000 },
          ],
        }),
      );

      renderSurveyDetail('survey-5');

      // Zero should display as "0", not as "-"
      expect(screen.getByText('0')).toBeInTheDocument();
      // The other resource should show its formatted value
      expect(screen.getByText('5,000')).toBeInTheDocument();
    });
  });
});
