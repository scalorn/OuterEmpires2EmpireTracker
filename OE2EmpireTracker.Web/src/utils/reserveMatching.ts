import { AsteroidReserve } from '../api/types/domain';

export function matchReserveToResource(
  resource: { resourceName: string; purity: string },
  reserves: AsteroidReserve[]
): AsteroidReserve | undefined {
  return reserves.find(
    (r) =>
      r.resourceName.toLowerCase() === resource.resourceName.toLowerCase() &&
      r.purity.toLowerCase() === resource.purity.toLowerCase()
  );
}
