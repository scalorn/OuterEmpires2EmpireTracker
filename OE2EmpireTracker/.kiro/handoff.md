# Session Handoff — 2026-05-15 (Avalonia Port)

## Branch: net8maui (39 commits, all backed up to 3 remotes)

## Current State: Substantially Complete

The Avalonia Desktop client is functional with full CRUD, event-driven refresh, background processing, clipboard import, pricing, help system, and UI state persistence.

### To Run
```
dotnet run --project OE2EmpireTracker.Desktop
```

### What's Implemented

**Infrastructure:**
- SafeFileWriter, WriteContext, File New/Open/Save/SaveAs, auto-open, exit confirmation
- 16 data change message types with auto-refresh on all views
- BackgroundProcessor (colony timer advancement, status bar countdown)
- 17 CRUD service classes for all entity types
- ColonyParser + SurveyParser (clipboard import), BlueprintScanner (full HTML parsing)
- ReferenceCountService (colony, blueprint, station)
- ValidationService (name, duplicate, positive value)
- PriceCalculator (commodity + blueprint price computation)
- PreferencesStore (9 thresholds, UIPreferences.json)
- CountdownFormatParser (Xd Yh Zm Ws ↔ seconds)
- HelpTopicRegistry + HelpRenderer (markdown topic loading)
- Window state persistence (position/size save/restore)
- Dock layout persistence (save/restore on close/open)

**Main Window:**
- File menu: New, Open, Save, Save As, Preferences, Exit
- Manage menu: All 16 feature items alphabetically sorted
- Help menu: Help Topics, About
- Player selector combo
- Status bar: Next Process countdown, Memory usage

**Feature Views (all have CRUD + editable grids):**
- Colony: List, header, structures (add/remove), items (add/remove), overflow rules, clipboard import
- Blueprint: List, detail, stats editing, resources editing, price display, market import
- Survey: List, detail, resources editing, clipboard import
- Player Profile: List, detail, skills editing (inline levels)
- Delivery Routes: List, detail, stops management (add/remove)
- Delivery Execution: Plan selection, mark delivered (fires colony events)
- Market: Listings CRUD
- Ship Templates: List, slot grid (add/remove/edit)
- Ship Instances: CRUD
- Stations: List, hold items management (add/remove)
- Build Planner: List, build items (add/remove/edit), save
- Stock Targets: List, targets display, Check & Generate Orders
- Supply Chains: List, stages management (add/remove/edit)
- Contacts: Factions + Characters CRUD
- Asteroids: CRUD
- Pricing Plans: List, detail, resource price grid (inline editing)
- Systems: Display
- Colony Activity: Display
- Help: Topic tree + content display
- Preferences: 9 thresholds with validation

### What Remains (Low Priority)

- PlayerProfileParser full implementation (HTML → skills/ranks)
- ScottPlot chart integration (evolution graphs, yield distribution)
- DataGrid column width/sort persistence
- F1 context-sensitive help
- Unsaved changes prompt on all navigation
- Colony admin reports tab
- Colony daily build display
- Full delivery plan auto-fill dialog
- Build planner auto-assign and delivery generation
- Keyboard shortcuts (F5 refresh, Ctrl+Tab)
- Accessibility pass

### All work committed and backed up to all 3 remotes.
