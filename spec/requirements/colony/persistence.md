# Colony Persistence

**REQ-COL-080** Clicking Save SHALL write PlanetName and ColonyName from the form fields to the colony, add the colony to PlayerContext if not already present, and call PlayerContext.WriteContext().  
**REQ-COL-081** The colony list SHALL be populated from PlayerContext.ColonyList on form load.  
**REQ-COL-082** After saving, the colony list SHALL reflect the updated PlanetName and ColonyName.

## MVVM Pattern

**REQ-COL-090** All direct PropertyBag access for Built, Staged, Online, and worker assignment SHALL go through ColonyStructureViewModel, not directly from the UI.  
**REQ-COL-091** All direct Colony.Structures, Colony.Items, and Colony.Commodities list manipulation SHALL go through ColonyViewModel, not directly from the UI.  
**REQ-COL-092** ColonyViewModel.RecalculateStatus() SHALL call both CalculateBuilt() and CalculateIdeal().  
**REQ-COL-093** ColonyViewModel.Save() SHALL ensure UUID is set, add to PlayerContext if absent, and call WriteContext().
