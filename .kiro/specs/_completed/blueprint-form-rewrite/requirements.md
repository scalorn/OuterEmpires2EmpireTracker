# Requirements: Blueprint Form Rewrite

## Introduction

Rewrite FormBlueprint from scratch to produce cleaner, more performant, and less complex code while preserving all existing functionality. The current form is 2143 lines of accumulated features with interaction bugs (e.g. save wiping imported properties). The rewrite uses the existing spec documents as the authoritative source of requirements.

## Requirement 1: Blueprint List with Filtering

As a player, I want to browse and filter my blueprints so I can find specific items quickly.

### Acceptance Criteria
1. The form SHALL display a ListView of all blueprints (global + current player) with columns: Type, Name, TechLevel, Evolution, NickName, Refs.
2. A text filter SHALL perform case-insensitive substring matching on ExtendedName and BluePrintType.
3. Structured filters SHALL support: BlueprintType (combo), ShipClass (combo), TechLevel (combo), Evolution (combo), and "Evolution And Above" (checkbox).
4. All filters SHALL use AND semantics. Clearing a filter removes that constraint.
5. A "Clear Filters" button SHALL reset all structured filters to unselected.
6. The list SHALL refresh on CurrentPlayerChanged, BlueprintDataChanged, and filter changes.
7. The Refs column SHALL show the TotalCount from BlueprintReferenceCounter for each blueprint.
8. The title bar SHALL show "Blueprints - Global: N Player: M" with live counts.

## Requirement 2: Blueprint CRUD

As a player, I want to create, edit, save, and delete blueprints.

### Acceptance Criteria
1. New SHALL reset the form to a blank blueprint with no UUID.
2. Save SHALL persist via BlueprintViewModel.Save(isGlobal). The "Global Blueprint" checkbox determines routing.
3. Save SHALL NOT do ClearProperties + write-all-from-grid. Instead, the PropertyBag is the source of truth (see Requirement 4).
4. Delete SHALL be disabled with "In Use (N)" text when BlueprintReferenceCounter.TotalCount > 0.
5. Delete SHALL prompt for confirmation and remove from the correct list (global or player).
6. Save SHALL validate: empty/whitespace name rejected with message.

## Requirement 3: Detail Panel — Identity Fields

As a player, I want to edit blueprint metadata so I can organize my collection.

### Acceptance Criteria
1. The form SHALL display editable fields: Name, NickName, Description, CopyCost, BlueprintType (combo), ShipClass (combo), TechLevel (combo), Evolution (combo), BaseBlueprintUUID (filtered combo), Global checkbox.
2. All editable fields SHALL write-through to the ViewModel immediately on change (TextChanged, SelectedIndexChanged, CheckedChanged).
3. The ProgrammaticUpdateGuard SHALL suppress write-through during programmatic population.
4. ShipClass and TechLevel panels SHALL be hidden for Universal blueprint types.
5. The BaseBlueprintUUID combo SHALL show candidates filtered by matching BluePrintType, Name, Class, TechLevel, and lower Evolution.

## Requirement 4: Statistics Grid — PropertyBag as Source of Truth

As a player, I want to view and edit blueprint properties with the PropertyBag always being the authoritative data source.

### Acceptance Criteria
1. The statistics grid SHALL display one row per property defined by the selected BlueprintType's Properties array.
2. The statistics grid SHALL ALSO display rows for any additional properties in the blueprint's PropertyBag that are NOT in the BlueprintType's Properties array (unknown/new properties from game updates). These extra rows SHALL appear after the defined properties.
3. Unknown properties SHALL be logged at WARN level (so they're noticed and incorporated into BlueprintPropertyValidation) but still displayed in the grid with no validation (free-form text, same as PropertyValueType.Unknown).
4. Property values SHALL be read from the blueprint's PropertyBag on population. Missing properties show as empty cells.
5. Cell edits SHALL write-through to the PropertyBag immediately via CellValueChanged (not deferred to Save).
6. Empty cell values SHALL NOT be written to the PropertyBag. Clearing a cell SHALL remove the property from the PropertyBag.
7. Boolean/CheckBox properties SHALL render as DataGridViewCheckBoxColumn cells.
8. CommodityIndustry SHALL render as a DataGridViewComboBoxColumn.
9. Cell validation SHALL use BlueprintPropertyValidation patterns (Integer, Decimal, Time, Boolean). Unknown properties have no validation.
10. The grid structure SHALL be rebuilt when the BlueprintType changes OR when the set of extra PropertyBag keys changes (e.g. after import adds a new property). Changing the selected blueprint within the same type only updates values.
11. Save SHALL NOT call ClearProperties(). It SHALL only call WriteContext() since all edits are already written through.
12. Properties starting with underscore (e.g. `_IconPosition`) SHALL be hidden from the grid — these are internal metadata, not game properties.

## Requirement 5: Resources Grid

As a player, I want to view and edit blueprint resource requirements.

### Acceptance Criteria
1. The resources grid SHALL display Resource (combo from Resource list) and Amount (validated integer) columns.
2. Resource edits SHALL write-through to Blueprint.Resources immediately.
3. Save SHALL NOT call ClearResources() + write-all. Resources are already current via write-through.
4. Adding a row SHALL add to Blueprint.Resources. Deleting a row SHALL remove from Blueprint.Resources.

## Requirement 6: Individual Blueprint Import

As a player, I want to import a blueprint from the game's clipboard HTML so I can track blueprints I discover.

### Acceptance Criteria
1. The Import button SHALL read clipboard HTML, validate content type, and parse via BlueprintScanner.ParseClipboardToTemp().
2. Import logic SHALL be extracted into a BlueprintImportHandler service with clear methods: ClassifyImport(), FindTarget(), MergeAndPersist().
3. Resources-only imports (0 properties, N resources) SHALL merge into the selected blueprint without touching properties.
4. Full imports SHALL match against the selected blueprint first (relaxed: Name + Evolution, BluePrintType matches or existing type is empty).
5. If no selected match, SHALL route via FindByDedupKey in the appropriate list (global for Evo 0, player otherwise).
6. If no dedup match, SHALL create a new blueprint with deterministic UUID (global) or random UUID (player).
7. UpdateExisting SHALL merge properties additively (incoming overwrites existing keys, but empty incoming properties are skipped). SHALL NOT clear existing properties.
8. Name-based type classification SHALL handle iconless blueprints: "Ore Hopper" -> OreHopper, "Mining Laser" -> MiningLaser, "Grapple" -> AsteroidGrapple.
9. Name parsing SHALL occur BEFORE icon detection so ReclassifyByName has the name available.
10. All import decisions SHALL be logged at INFO level with parsed data, match results, and routing decisions.

## Requirement 7: Market Bulk Import

As a player, I want to import multiple blueprints from the game's market listing HTML.

### Acceptance Criteria
1. The Import Market button SHALL parse market HTML via BlueprintScanner.ProcessMarketHtml().
2. Each parsed blueprint SHALL be routed: Government seller -> global, player seller -> current player.
3. Dedup by Name + Evolution + BluePrintType + Class + TechLevel. Existing blueprints updated, new ones created.
4. Protected fields (NickName, CopyCost, BaseBlueprintUUID) SHALL be preserved on existing blueprints.
5. Import SHALL be idempotent — re-importing the same data SHALL NOT create duplicates.

## Requirement 8: Evolution Graph

As a player, I want to see how blueprint properties change across evolution levels.

### Acceptance Criteria
1. An "Evolution Graph" tab SHALL appear after Statistics and Resources tabs.
2. The chart SHALL resolve the evolution chain via BaseBlueprintUUID links, sorted by Evolution ascending.
3. Only numeric properties (Integer, Decimal, Time) that changed across the chain SHALL be plotted.
4. Values SHALL be normalized as percentage of Ev0 base value. Zero base values excluded.
5. X-axis: 0-15 (evolution levels). Y-axis: 50%-150% with 10% gridlines.
6. Solid lines for consecutive levels, dashed for gaps. Colorblind-friendly palette.
7. Property checkboxes SHALL toggle line visibility. All checked by default.
8. The graph SHALL refresh on blueprint selection change and BlueprintDataChanged events.

## Requirement 9: Pricing Integration

As a player, I want to see computed prices for the selected blueprint.

### Acceptance Criteria
1. A pricing plan combo SHALL list the current player's pricing plans.
2. When a plan is selected, the computed price SHALL be displayed using PriceCalculator.ComputeBlueprintPrice().
3. Incomplete prices SHALL show a visual indicator.
4. The combo SHALL refresh on PricingDataChanged events.

## Requirement 10: Window State and Data Events

As a player, I want the form to remember its state and respond to external data changes.

### Acceptance Criteria
1. Position, size, grid columns, filter selections, and combo states SHALL persist via WindowStateHelper.
2. The form SHALL subscribe to CurrentPlayerChanged, BlueprintDataChanged, and PricingDataChanged in the constructor.
3. The form SHALL unsubscribe in OnFormClosed.
4. All event handlers SHALL check IsDisposed before accessing controls.
5. BlueprintDataChanged SHALL refresh the list and re-populate the form if the changed blueprint is selected.
6. CurrentPlayerChanged SHALL clear selections, reset the ViewModel, and refresh the list.

## Requirement 11: Clipboard Content Validation

As a player, I want clear feedback when I try to import the wrong content type.

### Acceptance Criteria
1. Before parsing, the import handler SHALL validate clipboard content via ClipboardContentDetector.Detect().
2. If the content is not Blueprint or Survey type, a message SHALL explain what was found vs expected.
3. Survey content SHALL be allowed through for resources-only imports.
