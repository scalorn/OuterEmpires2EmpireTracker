# Design: Colony Form UI Tweaks

## Dynamic Title
`UpdateTitle()` reads `playerContext.CurrentPlayer.Name` and colony count. Called from constructor, save, delete, player change.

## Sortable List
`ListViewItemComparer` implements `IComparer` for column-based string sorting. `_sortColumn` and `_sortOrder` track state. `ColumnClick` handler toggles order.

## Filterable List
`txtColonyListFilter_TextChanged` filters `GetCurrentPlayerColonies()` by planet/colony name, clears and repopulates the ListView.

## Tab Headers
`UpdateTabTitles()` sets tab text with counts. Structures count from `selectedColony.Structures.Count`. Workers count filters `Commodities` excluding fulfilled and expired (NeedBy past now).

## Deferred Structure Loading
`PopulateForm` checks `tabDetailedData.SelectedTab == tabPStructures`. If not active, assigns data to controls but skips `UpdateData()`, sets `_structuresDirty = true`. Tab switch handler calls `UpdateData()` on visible controls when dirty.

## Parser Worker Fix
`ParseBuilding` maps `detailsRequired[].name` to `WorkerPrefix + index` keys (e.g. "BlueCollar1") with boolean values via `MapDetailNameToWorkerPrefix`. `MergeStructure` copies `AssignedWorkers` from parsed to existing.
