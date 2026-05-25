import type { PlannedStructure } from './computeColonyStatus';

export interface DisableFlags {
  isMoveUpDisabled: boolean;
  isMoveDownDisabled: boolean;
}

/**
 * Computes move-up/move-down disable flags for each structure in the list.
 * Structures are sorted by buildQueuePosition ascending before computing.
 *
 * Rules:
 * - Move Up disabled: structure is CC, OR at position 1, OR at position 2 with CC at position 1, OR single item
 * - Move Down disabled: structure is CC, OR at last position, OR single item
 */
export function computeDisableFlags(structures: PlannedStructure[]): DisableFlags[] {
  const sorted = [...structures].sort((a, b) => a.buildQueuePosition - b.buildQueuePosition);

  return sorted.map((structure, index) => {
    const isCC = structure.subType === 'ColonyCommandCentre';
    const isFirst = index === 0;
    const isLast = index === sorted.length - 1;
    const isSingleItem = sorted.length === 1;
    const ccAtFirst = sorted[0]?.subType === 'ColonyCommandCentre';
    const isSecondWithCCFirst = index === 1 && ccAtFirst;

    const isMoveUpDisabled = isCC || isFirst || isSecondWithCCFirst || isSingleItem;
    const isMoveDownDisabled = isCC || isLast || isSingleItem;

    return { isMoveUpDisabled, isMoveDownDisabled };
  });
}
