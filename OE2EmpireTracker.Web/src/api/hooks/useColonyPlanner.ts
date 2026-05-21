import { useMutation } from '@tanstack/react-query';
import { colonyPlannerApi } from '../endpoints/colony-planner';
import type { ColonyPlannerRequest } from '../types/generated';

export function useColonyPlannerStatus() {
  return useMutation({
    mutationFn: (request: ColonyPlannerRequest) =>
      colonyPlannerApi.computeStatus(request),
  });
}

export function useColonyPlannerBuildOrder() {
  return useMutation({
    mutationFn: (request: ColonyPlannerRequest) =>
      colonyPlannerApi.optimizeBuildOrder(request),
  });
}

export function useColonyPlannerEligibility() {
  return useMutation({
    mutationFn: (request: ColonyPlannerRequest) =>
      colonyPlannerApi.checkEligibility(request),
  });
}
