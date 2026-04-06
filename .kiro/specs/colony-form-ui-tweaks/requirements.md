# Requirements: Colony Form UI Tweaks

## Overview
Improve the colony form's usability and performance with dynamic titles, sortable/filterable colony list, informative tab headers, and deferred structure loading.

## US-1: Dynamic Form Title
Title shows "Manage Colonies - <Player Name> : <Colony Count>", updating on save, delete, and player change.

## US-2: Sortable Colony List
Clicking any column header sorts the list ascending/descending. Sort order is preserved after save.

## US-3: Filterable Colony List
The filter text box above the colony list filters by planet name or colony name (case-insensitive).

## US-4: Informative Tab Headers
- Structures tab: "Structures : <count>"
- Workers tab: "Workers : <count>" where count = active commodity requests (not fulfilled, not expired)

## US-5: Deferred Structure Loading
Structure controls skip expensive UpdateData() when the Structures tab is not active. Updates are deferred until the tab is selected, improving colony switch performance.

## US-6: Colony Parser Worker Assignment Fix
Imported colonies now correctly mark workers as assigned. Parser maps JSON detail names to the UI key format (e.g. "BlueCollar1" = true). MergeStructure also updates worker assignments on re-import.

## US-7: Control Rename
Renamed txtBlueprintListFilter to txtColonyListFilter for clarity.
