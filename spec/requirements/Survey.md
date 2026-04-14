# Survey Requirements

## Survey Data

**REQ-SRV-001** A Survey SHALL have UUID, PlanetName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, a Properties dictionary, and a Resources dictionary keyed by resource name.  
**REQ-SRV-002** Each SurveyResource SHALL have Resource (name), Purity, and Amount string fields.  
**REQ-SRV-003** Survey SHALL serialize to and deserialize from JSON, preserving all fields including the Resources dictionary.  
**REQ-SRV-004** Survey.ExtendedName SHALL return `PlanetName (SurveyID)` with `[NickName]` appended when NickName is non-empty.

## Survey Form — List

**REQ-SRV-010** The survey form SHALL display a list of all saved surveys with UUID, PlanetName, NickName, and DateTime columns.  
**REQ-SRV-011** The list SHALL be filterable by PlanetName and by Resource type.  
**REQ-SRV-012** Selecting a survey from the list SHALL populate all form fields with that survey's data.

## Survey Form — Fields

**REQ-SRV-020** The form SHALL display and allow editing of: PlanetName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID (via filtered combo), and scanner properties (SensorAbundanceFactor, PurityModifier, ScanLevel).  
**REQ-SRV-021** The scanner blueprint combo SHALL be filtered to SystemObjectScanner blueprint type only.  
**REQ-SRV-022** The scanner blueprint filter textbox SHALL re-filter the combo on every keystroke.

## Survey Form — Resources Grid

**REQ-SRV-030** The resources grid SHALL have three columns: Resource (combo), Purity (combo), Amount (text).  
**REQ-SRV-031** The Resource combo SHALL be populated from the empire context resource list.  
**REQ-SRV-032** The Purity combo SHALL be populated from the resource purity list.  
**REQ-SRV-033** When a survey is selected, the grid SHALL be cleared and repopulated from the survey's Resources dictionary.

## Survey Form — Save / Delete / Cancel

**REQ-SRV-040** Clicking Save SHALL write all form fields to the survey, assign a UUID if absent, add to SurveyList if new, persist all resource rows from the grid to the survey's Resources dictionary, and call PlayerContext.WriteContext().  
**REQ-SRV-041** Clicking Delete SHALL remove the survey from SurveyList and call WriteContext().  
**REQ-SRV-042** Clicking Cancel SHALL reset the form to a blank state.  
**REQ-SRV-043** After a successful save, the form SHALL be cleared for new input.

## Blueprint Scanner

**REQ-SRV-050** BlueprintScanner.processHtml() SHALL parse a blueprint HTML fragment and populate a Blueprint object with Name, TechLevel, Evolution, Description, Properties, and Resources.  
**REQ-SRV-051** The evolution number SHALL be stripped from the title string before setting Name.  
**REQ-SRV-052** TechLevel SHALL be extracted from parentheses at the end of the title, e.g. `Name (TechLevel)`.  
**REQ-SRV-053** Property values SHALL have delta indicators (e.g. `(▲ 435)`) stripped before storage.  
**REQ-SRV-054** Property keys SHALL be remapped using the PropertyRemap dictionary to normalize game HTML labels to canonical spaced form (e.g. "Health (Hitpoints)" → "Health", "Eng. Capacity Required" → "Eng Capacity Required", "Blue Collar Detail(s)" → "Blue Collar Detail", "Warehousing Capacity" → "Warehouse Capacity"). Keys not in the remap table are used as-is.  
**REQ-SRV-055** Resource quantities SHALL have non-digit characters (commas, spaces) stripped, leaving only digits.  
**REQ-SRV-056** The Class property SHALL be extracted from the "Class" property key and stored as Blueprint.Class (int).  
**REQ-SRV-057** processHtml() SHALL not throw on malformed or empty HTML input.
