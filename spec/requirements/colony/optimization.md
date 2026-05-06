# Colony Optimization

## Flatpack Build Order Optimization

**REQ-COL-095** The colony form SHALL provide an "Optimize Build Order" action that reorders the colony's structures to satisfy resource constraints at every build step. The optimizer reorders ALL structures (built and unbuilt) because the importer groups them by flatpack type, not by the order they were actually built.

**REQ-COL-095a** Structures SHALL be classified as either Support (Power, Habitation, Food, Entertainment providers) or Primary (all others). The Colony Command Centre is classified as Support for ordering purposes.

**REQ-COL-095b** The algorithm SHALL use a "fix-before-place" approach:
1. Bootstrap: seed the build order with CC, Reactor, Hab Block, Hydroponics Bay, Entertainment Centre from the support pool. This ensures no deficits from the start.
2. For each Primary structure in order:
   a. Fix any existing deficits by placing support (priority: Power > Hab > Food > Entertainment).
   b. If the primary itself would cause a deficit, fix it before placing.
   c. Look ahead: simulate placing the primary + a Hab + Hydro. If that would cause hab/food/ent deficits, pre-place the needed support.
   d. Final check: after all look-ahead placements, verify the primary won't cause any deficit. Fix if needed.
   e. Place the primary.
3. Append leftover support, fixing deficits as each is added.

**REQ-COL-095c** When a support structure would itself cause a new deficit (e.g. Entertainment Centre needs 2 power and has a worker), the algorithm SHALL recursively place prerequisites first. The cascade chain is bounded: Hydro -> Ent -> Reactor -> done (max depth 3).

**REQ-COL-095d** When the support pool is exhausted, the algorithm SHALL create new support structures from player blueprints, respecting MaxPerColony limits.

**REQ-COL-095e** The resulting sequence SHALL guarantee that from the first primary structure onwards, Power >= PowerRequired, HabitationProvision >= HabitationRequired, FoodProvision >= FoodRequired, and EntertainmentProvided >= EntertainmentRequired at every position.

**REQ-COL-095f** Entertainment required per worker is 2 (not 1 like habitation and food). The ColonyStatusCalculator computes `EntertainmentRequired = sum(workers) * 2`.

**REQ-COL-095g** Support structures that are not consumed during optimization SHALL be appended at the end, with deficit checks for each (e.g. leftover Entertainment Centres may need Hab/Hydro for their workers).

## Colony Bootstrap (Future)

**REQ-COL-096** The colony form SHALL provide a "Bootstrap Colony" action that generates a foundation set of planned structures based on the surveys available for the colony's planet.

**REQ-COL-096a** Survey selection: for each unique resource found across all surveys for the planet, the algorithm SHALL select the survey entry that yields the highest refined output rate. Refined output rate = `Amount * (1 + ExtractionFocusLevel * 0.01) * RefiningOutputPerHour(purity)` where RefiningOutputPerHour is 1 for Low, 3 for Medium, 5 for High purity.

**REQ-COL-096b** Mining rigs: one MiningRig flatpack (BlueprintType = `Flatpacks/MiningRig`) SHALL be added per unique resource, configured with the best survey and resource selected in REQ-COL-096a.

**REQ-COL-096c** Refiners: for each resource, calculate the raw mining rate per hour as `Amount * (1 + ExtractionFocusLevel * 0.01)`. Each refiner consumes 25 raw resources per hour. The number of refiners required is `ceil(miningRatePerHour / 25)`. One Refiner flatpack SHALL be added per required refiner.

**REQ-COL-096d** Fixed structure sequence: the output structure list SHALL always begin with:
1. Command Centre (`Flatpacks/ColonyCommandCentre`)
2. Remote Operations Array (`Flatpacks/RemoteOperationsArray`)
3. Mining rigs (one per resource, in resource name order)
4. Refiners (grouped by resource, in resource name order)

**REQ-COL-096e** After the bootstrap structures are added, the build order optimization algorithm (REQ-COL-095) SHALL be applied automatically to insert Power, Habitation, Food, and Entertainment structures as needed.

**REQ-COL-096f** The bootstrap action SHALL NOT remove any existing structures from the colony. It SHALL only add new planned structures. The user can then modify the list via the UI and re-run the optimization.

**REQ-COL-096g** The ExtractionFocusLevel used in calculations SHALL come from the owning player's Extraction Focus skill (REQ-ARCH-072). Until multi-player support is implemented (REQ-ARCH-070), a level of 0 SHALL be used as the default.

## Colony Timed Processing Order

**REQ-COL-100** Colony.ProcessColony() SHALL process structures in the following order within each cycle:
1. Structure building (build completion timers)
2. Mining
3. Refining base resources
4. Refining S1 synthetics
5. Refining S2 synthetics
6. Manufacturing
7. Research

This ordering ensures that resources mined in a cycle are available for refining in the same cycle, and refined resources are available for manufacturing.
