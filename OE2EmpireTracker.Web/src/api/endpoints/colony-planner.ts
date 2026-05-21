import { apiClient } from '../client';
import type {
  ColonyPlannerRequest,
  ColonyStatusResult,
  BuildOrderResult,
  EligibilityResult,
} from '../types/generated';

export const colonyPlannerApi = {
  computeStatus: (request: ColonyPlannerRequest) =>
    apiClient.post('api/v1/colony-planner/status', { json: request }).json<ColonyStatusResult>(),

  optimizeBuildOrder: (request: ColonyPlannerRequest) =>
    apiClient.post('api/v1/colony-planner/build-order', { json: request }).json<BuildOrderResult>(),

  checkEligibility: (request: ColonyPlannerRequest) =>
    apiClient.post('api/v1/colony-planner/eligibility', { json: request }).json<EligibilityResult>(),
};
