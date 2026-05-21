import { useQuery } from '@tanstack/react-query';
import { factionsApi } from '../endpoints/factions';
import { queryKeys } from './queryKeys';

export function useFaction(uuid: string | null) {
  return useQuery({
    queryKey: queryKeys.faction(uuid ?? ''),
    queryFn: () => factionsApi.getFaction(uuid!),
    enabled: !!uuid,
  });
}

export function useFactionMembers(factionUUID: string | null) {
  return useQuery({
    queryKey: queryKeys.factionMembers(factionUUID ?? ''),
    queryFn: () => factionsApi.getMembers(factionUUID!),
    enabled: !!factionUUID,
  });
}

export function useFactionSharedData(factionUUID: string | null, dataType: string) {
  return useQuery({
    queryKey: queryKeys.factionShared(factionUUID ?? '', dataType),
    queryFn: () => factionsApi.getSharedData(factionUUID!, dataType),
    enabled: !!factionUUID,
  });
}
