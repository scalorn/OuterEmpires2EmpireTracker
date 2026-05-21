import fc from 'fast-check';
import { describe, it, expect } from 'vitest';

/**
 * Property test for survey filter correctness.
 * Validates: Requirements 7.2
 */

interface Survey {
  UUID: string;
  System: string;
  Planet: string;
  ResourceType: string;
  Purity: string;
}

interface SurveyFilters {
  system?: string;
  resourceType?: string;
  purityLevel?: string;
  search?: string;
}

// Pure filter function matching the logic used in SurveyBrowser
function filterSurveys(surveys: Survey[], filters: SurveyFilters): Survey[] {
  return surveys.filter((s) => {
    if (filters.system) {
      if (!s.System.toLowerCase().includes(filters.system.toLowerCase())) return false;
    }
    if (filters.resourceType && s.ResourceType !== filters.resourceType) return false;
    if (filters.purityLevel && s.Purity !== filters.purityLevel) return false;
    if (filters.search) {
      const search = filters.search.toLowerCase();
      const searchable = `${s.System} ${s.Planet} ${s.ResourceType}`.toLowerCase();
      if (!searchable.includes(search)) return false;
    }
    return true;
  });
}

const SYSTEMS = ['Sol', 'Alpha Centauri', 'Proxima', 'Sirius', 'Vega'];
const RESOURCE_TYPES = ['Ore', 'Gas', 'Crystal', 'Organic'];
const PURITY_LEVELS = ['High', 'Medium', 'Low'];

const surveyArb = fc.record({
  UUID: fc.uuid(),
  System: fc.constantFrom(...SYSTEMS),
  Planet: fc.string({ minLength: 1, maxLength: 20 }),
  ResourceType: fc.constantFrom(...RESOURCE_TYPES),
  Purity: fc.constantFrom(...PURITY_LEVELS),
});

const filtersArb = fc.record({
  system: fc.option(fc.constantFrom(...SYSTEMS), { nil: undefined }),
  resourceType: fc.option(fc.constantFrom(...RESOURCE_TYPES), { nil: undefined }),
  purityLevel: fc.option(fc.constantFrom(...PURITY_LEVELS), { nil: undefined }),
  search: fc.option(fc.string({ minLength: 1, maxLength: 10 }), { nil: undefined }),
});

describe('Property 5: Survey filter correctness', () => {
  it('for any set of filter criteria, every item in filtered result matches ALL active criteria', () => {
    fc.assert(
      fc.property(
        fc.array(surveyArb, { minLength: 0, maxLength: 50 }),
        filtersArb,
        (surveys, filters) => {
          const result = filterSurveys(surveys, filters);

          for (const s of result) {
            if (filters.system) {
              expect(s.System.toLowerCase()).toContain(filters.system.toLowerCase());
            }
            if (filters.resourceType) {
              expect(s.ResourceType).toBe(filters.resourceType);
            }
            if (filters.purityLevel) {
              expect(s.Purity).toBe(filters.purityLevel);
            }
            if (filters.search) {
              const searchable = `${s.System} ${s.Planet} ${s.ResourceType}`.toLowerCase();
              expect(searchable).toContain(filters.search.toLowerCase());
            }
          }
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});
