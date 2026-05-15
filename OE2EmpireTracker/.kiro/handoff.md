# Session Handoff — 2026-05-15 (Avalonia Port)

## Branch: net8maui

## Current State

The Avalonia Desktop client (`OE2EmpireTracker.Desktop`) is substantially functional with:

### Infrastructure (Phase A) — Complete
- A1: SafeFileWriter, WriteContext, File New/Open/Save/SaveAs, auto-open, title bar, exit confirmation
- A2: 16 data change message types, WeakReferenceMessenger, auto-refresh on all views
- A3: BackgroundProcessor with colony timer advancement, status bar countdown
- A5: 17 service classes (CRUD for all entity types)
- A6: ColonyParser, SurveyParser (clipboard import), HtmlClipboardHelper, BlueprintScanner/PlayerProfileParser stubs
- A7: ReferenceCountService (colony, blueprint, station reference counting)
- A8: ValidationService (name validation, duplicate checking, positive value validation)

### Main Window (Phase B) — Complete
- File menu: New, Open, Save, Save As, Preferences, Exit
- Manage menu: All 16 feature items, alphabetically sorted
- Help menu: Help Topics, About
- Player selector combo in toolbar
- Status bar: Next Process countdown, Memory usage

### Preferences (Phase C) — Complete
- ThresholdPreferences model (9 values)
- CountdownFormatParser (Xd Yh Zm Ws ↔ seconds)
- PreferencesStore (load/save UIPreferences.json)
- Full form with validation, OK/Cancel/Reset

### Feature Views — All have CRUD + editable grids:
- Colony: List, header editing, structures display, items add/remove, overflow rules, clipboard import
- Blueprint: List, detail editing, stats editing (add/remove/inline), resources editing, evolution placeholder
- Survey: List, detail editing, resources editing (add/remove/inline), clipboard import
- Player Profile: List, detail editing, skills editing (inline level changes)
- Delivery Routes: List, detail editing, stops management (add/remove)
- Delivery Execution: Plan selection, load items display, mark delivered (fires ColonyDataChanged)
- Market: Listings CRUD
- Ship Templates: CRUD
- Ship Instances: CRUD
- Stations: CRUD
- Build Planner: CRUD
- Stock Targets: CRUD
- Supply Chains: CRUD
- Contacts: Factions + Characters CRUD
- Asteroids: CRUD
- Pricing Plans: CRUD
- Systems: Display (read-only)
- Colony Activity: Display
- Colony Daily Build: Display

## What's Still Missing (for full parity with WinForms)

### High Priority
- Full BlueprintScanner implementation (property remapping, market import)
- Full PlayerProfileParser implementation (HTML → skills/ranks)
- Colony structure editing (add/remove structures, assign mining/refining/manufacturing)
- Ship template slot grid (hull selection → generate slots → assign components)
- Ship instance cargo/hopper management
- Station hold management (add/remove items)
- Build planner item management (add items, auto-assign, generate delivery)
- Stock target evaluation (check & generate orders)
- Supply chain stage management
- Pricing plan resource price grid (inline editing with clear = remove)
- Price calculator integration (commodity/blueprint price computation)

### Medium Priority
- A4: UI State Persistence (window position, grid columns, filter text)
- ScottPlot chart integration (evolution graphs, yield distribution)
- Help system (HelpTopicRegistry, markdown rendering, F1 context help)
- Unsaved changes prompt on navigation/close
- Colony admin reports tab
- Colony daily build display

### Low Priority
- Full validation on all forms (duplicate names, required fields)
- Reference counting display (Refs column in lists)
- Delete protection dialogs (show what references the entity)
- Keyboard shortcuts (F5 refresh, Ctrl+Tab switch tabs)
- Accessibility pass

## To Run
```
dotnet run --project OE2EmpireTracker.Desktop
```

## To Publish
```
bash OE2EmpireTracker.Desktop/publish.sh
```

## All work committed and backed up to all 3 remotes.
