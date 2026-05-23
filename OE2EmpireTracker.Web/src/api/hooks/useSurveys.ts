import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { surveysApi } from '../endpoints/surveys';
import { publicApi, type SurveyFilters } from '../endpoints/public';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { Survey } from '../types/domain';

export function useSurveys(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Surveys'),
    queryFn: () => surveysApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useSurveyDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'Surveys', entityUUID ?? ''),
    queryFn: () => surveysApi.getOne(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function usePublicSurveys(filters?: SurveyFilters) {
  return useQuery({
    queryKey: queryKeys.publicSurveys(filters),
    queryFn: () => publicApi.getPublicSurveys(filters),
  });
}

export function useSurveyMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: Partial<Survey> }) =>
      surveysApi.update(charUUID!, entityUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Surveys') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      surveysApi.remove(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Surveys') });
    },
  });

  return { save, remove };
}
