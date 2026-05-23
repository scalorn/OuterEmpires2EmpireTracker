# Implementation Plan: Web UI Forms Parity

## Overview

Bring the OE2EmpireTracker web UI to feature parity with the desktop WinForms application. Implementation uses React 19 + React Router 7 + TanStack React Query 5 + Zustand 5 + Tailwind CSS 4 + ky + Radix UI. Organized in phases: foundation → API layer → form pages → real-time → testing.

## Tasks

- [x] 1. Foundation: Shared Types and Utilities
  - [x] 1.1 Create domain type interfaces (Colony, Blueprint, Survey, PlayerProfile)
    - Create `src/api/types/domain.ts` with Colony, ColonyStructure, WarehouseItem, CommodityRequest, Blueprint, BlueprintResource, Survey, SurveyResource, PlayerProfile, ProfileRanks, RankInfo, SkillGroup, Skill interfaces
    - _Requirements: 1.1–1.10, 2.1–2.9, 3.1–3.9, 4.1–4.9_
    - _Verification: TypeScript compiles with no errors_

  - [x] 1.2 Create domain type interfaces (Delivery, Ship, Market, BuildPlan)
    - Add to `src/api/types/domain.ts`: DeliveryRoute, RouteStop, DeliveryPlan, StopItemSet, DeliveryItem, ShipTemplate, TemplateSlot, MarketListing, MarketTransaction, BuildPlan, BuildPlanItem interfaces
    - _Requirements: 5.1–5.8, 6.1–6.7, 7.1–7.8, 8.1–8.9, 9.1–9.7, 12.1–12.5_
    - _Verification: TypeScript compiles with no errors_

  - [x] 1.3 Create domain type interfaces (SupplyChain, Stock, Pricing, Shared, Supporting, Baseline)
    - Add to `src/api/types/domain.ts`: SupplyChain, SupplyChainStep, StockProfile, StockTargetItem, PricingPlan, PricingPlanItem, SharedDataSummary, ExternalCharacter, Station, Asteroid, BaselineData, ShipClassDef, SlotDefinition, TechLevelDef, CommodityDef interfaces
    - _Requirements: 13.1–13.5, 14.1–14.7, 15.1–15.7, 16.1–16.7, 17.1–17.5_
    - _Verification: TypeScript compiles with no errors_


  - [x] 1.4 Create timer utility functions
    - Create `src/utils/timerUtils.ts` with: computeRemaining(endTime, now), formatCountdown(seconds), isExpired(endTime)
    - Handle edge cases: past timestamps return 0, null/undefined endTime returns null
    - _Requirements: 11.2, 18.3, 18.4_
    - _Verification: Unit tests pass for timerUtils_

  - [x] 1.5 Create filter utility functions
    - Create `src/utils/filterUtils.ts` with: matchesTextFilter(item, fields, query), matchesDropdownFilter(value, selected), applyFilters(items, filters)
    - Generic filter application logic reusable across all entity lists
    - _Requirements: 10.3, 2.2, 3.2_
    - _Verification: Unit tests pass for filterUtils_

  - [x] 1.6 Create reorder utility functions
    - Create `src/utils/reorderUtils.ts` with: moveUp(items, index), moveDown(items, index), resequence(items)
    - Maintains sequential numbering after moves
    - _Requirements: 5.4, 14.5_
    - _Verification: Unit tests pass for reorderUtils_

- [x] 2. Foundation: Shared UI Components (Part 1)
  - [x] 2.1 Implement MasterDetailLayout component
    - Create `src/components/common/MasterDetailLayout.tsx`
    - Responsive: side-by-side on desktop (≥1024px), narrower list on tablet (768–1023px), single-panel with back button on mobile (<768px)
    - Props: listPanel, detailPanel, listWidth, selectedId, onBack
    - _Requirements: 10.2, 10.9_
    - _Verification: Component renders at all breakpoints without errors_

  - [x] 2.2 Implement FilterBar component
    - Create `src/components/common/FilterBar.tsx`
    - Support text search input, dropdown filters, and checkbox filters
    - Emit filter state changes via onChange callback
    - _Requirements: 10.3_
    - _Verification: Component renders and emits filter changes_

  - [x] 2.3 Implement FilteredDropdown component
    - Create `src/components/common/FilteredDropdown.tsx`
    - Searchable dropdown with type-ahead filtering
    - Props: options, value, onChange, placeholder, filterFn
    - _Requirements: 10.6, 1.4, 8.4_
    - _Verification: Component renders, filters options on input_

  - [x] 2.4 Implement EditableGrid component
    - Create `src/components/common/EditableGrid.tsx`
    - Inline cell editing: click to activate, commit on blur or Enter
    - Column types: text, number, select, readonly
    - Props: columns, rows, onRowChange, onRowAdd, onRowRemove, keyExtractor
    - _Requirements: 10.4_
    - _Verification: Component renders, cells editable inline_


  - [x] 2.5 Implement InfiniteScrollList component
    - Create `src/components/common/InfiniteScrollList.tsx`
    - Loads pages of 50 items on scroll using IntersectionObserver
    - Props: queryKey, fetchPage, renderItem, keyExtractor, pageSize, filterValue
    - _Requirements: 10.8_
    - _Verification: Component renders initial page, loads more on scroll_

  - [x] 2.6 Implement TabBar and ConfirmDialog components
    - Create `src/components/common/TabBar.tsx` — tab switching with active indicator
    - Create `src/components/common/ConfirmDialog.tsx` — modal confirmation for delete actions
    - _Requirements: 10.5, 2.9, 3.8, 4.9, 5.8_
    - _Verification: Components render and handle interactions_

  - [x] 2.7 Implement CountdownTimer component
    - Create `src/components/common/CountdownTimer.tsx`
    - Computes remaining time from ISO 8601 end timestamp, decrements each second
    - Handles keepZero prop (stays at 00:00:00 after completion)
    - _Requirements: 11.2, 18.3, 18.5_
    - _Verification: Timer displays and decrements correctly_

  - [x] 2.8 Implement UnsavedChangesGuard hook
    - Create `src/hooks/useUnsavedChanges.ts`
    - Uses react-router useBlocker to prevent navigation when isDirty is true
    - Fail-closed: if hook errors, blocks all navigation
    - _Requirements: 10.7_
    - _Verification: Navigation blocked when dirty flag is true_

  - [x] 2.9 Implement useVisibilityRecovery hook
    - Create `src/hooks/useVisibilityRecovery.ts`
    - Listens to `visibilitychange` event, invalidates timer queries on tab refocus
    - Recalculates countdown from end timestamp, not last displayed value
    - _Requirements: 18.4_
    - _Verification: Timer recalculates correctly after simulated tab background_

- [x] 3. Foundation: Shared UI Components (Part 2)
  - [x] 3.1 Implement ConnectionBanner component
    - Create `src/components/domain/ConnectionBanner.tsx`
    - Shows "Reconnecting..." during backoff, "Connection lost" after 60s
    - Manual retry button on persistent banner
    - _Requirements: 18.6_
    - _Verification: Banner renders in disconnected state_

  - [x] 3.2 Implement TimerGroup component
    - Create `src/components/domain/TimerGroup.tsx`
    - Groups multiple CountdownTimers by activity type (mining, refining, manufacturing, research, building)
    - _Requirements: 11.1, 11.2_
    - _Verification: Component renders grouped timers_

  - [x] 3.3 Implement loading, error, and empty state components
    - Create `src/components/common/LoadingState.tsx`, `ErrorState.tsx`, `EmptyState.tsx`
    - ErrorState includes retry button
    - Consistent styling across all forms
    - _Requirements: 10.5_
    - _Verification: All three states render correctly_

  - [x] 3.4 Update Sidebar navigation with all form links
    - Modify `src/components/layout/Sidebar.tsx`
    - Add navigation links for all 18 form pages listed in Req 10.1
    - _Requirements: 10.1_
    - _Verification: All nav links render and route correctly_


- [x] 4. Checkpoint - Foundation complete
  - Ensure all foundation components compile, render, and pass unit tests. Ask the user if questions arise.

- [x] 5. API Layer: Endpoint Modules
  - [x] 5.1 Create/extend colony endpoint module
    - Extend `src/api/endpoints/colonies.ts` with sub-resource methods: getStructures, getItems, getCommodityRequests, bootstrap, optimize
    - _Requirements: 1.1–1.10_
    - _Verification: TypeScript compiles, endpoint functions exported_

  - [x] 5.2 Create/extend blueprint and survey endpoint modules
    - Extend `src/api/endpoints/blueprints.ts` with typed responses, save, delete
    - Extend `src/api/endpoints/surveys.ts` with typed responses, save, delete
    - _Requirements: 2.1–2.9, 3.1–3.9_
    - _Verification: TypeScript compiles, endpoint functions exported_

  - [x] 5.3 Create delivery routes and plans endpoint modules
    - Create/extend `src/api/endpoints/delivery-routes.ts` with CRUD + stop reordering
    - Create/extend `src/api/endpoints/delivery-plans.ts` with CRUD + auto-fill
    - _Requirements: 5.1–5.8, 6.1–6.7_
    - _Verification: TypeScript compiles, endpoint functions exported_

  - [x] 5.4 Create ship templates and market endpoint modules
    - Create/extend `src/api/endpoints/ship-templates.ts` with CRUD + order-build
    - Create/extend `src/api/endpoints/market-listings.ts` with CRUD + record-sale
    - Create/extend `src/api/endpoints/market-transactions.ts` with list + record-purchase
    - _Requirements: 8.1–8.9, 9.1–9.7_
    - _Verification: TypeScript compiles, endpoint functions exported_

  - [x] 5.5 Create supply chains, stock, and pricing endpoint modules
    - Create `src/api/endpoints/supply-chains.ts` with CRUD + step management
    - Create `src/api/endpoints/stock-profiles.ts` and `stock-plans.ts` with CRUD
    - _Requirements: 14.1–14.7, 15.1–15.7_
    - _Verification: TypeScript compiles, endpoint functions exported_

  - [x] 5.6 Create pricing plans, shared data, and baseline endpoint modules
    - Create/extend `src/api/endpoints/pricing-plans.ts` with CRUD
    - Create `src/api/endpoints/shared-data.ts` with list + detail
    - Extend `src/api/endpoints/global.ts` with baseline endpoint
    - _Requirements: 16.1–16.7, 17.1–17.5, 10.6_
    - _Verification: TypeScript compiles, endpoint functions exported_

  - [x] 5.7 Create profiles, contacts, stations, asteroids, build-plans endpoint modules
    - Create/extend `src/api/endpoints/profiles.ts` with CRUD
    - Create/extend `src/api/endpoints/build-plans.ts` with CRUD + item management
    - Extend contacts, stations, asteroids endpoints with typed CRUD
    - _Requirements: 4.1–4.9, 12.1–12.5, 13.1–13.5_
    - _Verification: TypeScript compiles, endpoint functions exported_


- [x] 6. API Layer: React Query Hook Modules
  - [x] 6.1 Create useBaseline and extend queryKeys
    - Create `src/api/hooks/useBaseline.ts` with staleTime: Infinity
    - Extend `src/api/hooks/queryKeys.ts` with keys for all new entity types
    - _Requirements: 10.6_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.2 Create useColonies hook module (extend)
    - Extend `src/api/hooks/useColonies.ts` with useColonyDetail, useColonyMutations (save, remove, bootstrap, optimize)
    - _Requirements: 1.1–1.10_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.3 Create useBlueprints and useSurveys hook modules (extend)
    - Extend `src/api/hooks/useBlueprints.ts` with useBlueprintDetail, useBlueprintMutations
    - Extend `src/api/hooks/useSurveys.ts` with useSurveyDetail, useSurveyMutations
    - _Requirements: 2.1–2.9, 3.1–3.9_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.4 Create useProfiles and useDeliveryRoutes hook modules
    - Create `src/api/hooks/useProfiles.ts` with useProfiles, useProfileDetail, useProfileMutations
    - Create `src/api/hooks/useDeliveryRoutes.ts` with useDeliveryRoutes, useRouteDetail, useRouteMutations
    - _Requirements: 4.1–4.9, 5.1–5.8_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.5 Create useDeliveryPlans and useShipTemplates hook modules
    - Create `src/api/hooks/useDeliveryPlans.ts` with usePlans, usePlanDetail, usePlanMutations
    - Create `src/api/hooks/useShipTemplates.ts` with useShipTemplates, useTemplateDetail, useTemplateMutations
    - _Requirements: 6.1–6.7, 8.1–8.9_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.6 Create useMarketListings and useMarketTransactions hook modules
    - Create `src/api/hooks/useMarketListings.ts` with useListings, useListingMutations (save, remove, recordSale)
    - Create `src/api/hooks/useMarketTransactions.ts` with useTransactions, useRecordPurchase
    - _Requirements: 9.1–9.7_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.7 Create useSupplyChains, useStockProfiles, useStockPlans hook modules
    - Create `src/api/hooks/useSupplyChains.ts` with CRUD hooks
    - Create `src/api/hooks/useStockProfiles.ts` and `useStockPlans.ts` with CRUD hooks
    - _Requirements: 14.1–14.7, 15.1–15.7_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.8 Create usePricingPlans, useSharedData, useBuildPlans hook modules
    - Create `src/api/hooks/usePricingPlans.ts` with CRUD hooks
    - Create `src/api/hooks/useSharedData.ts` with list + detail hooks (read-only)
    - Create `src/api/hooks/useBuildPlans.ts` with CRUD hooks
    - _Requirements: 16.1–16.7, 17.1–17.5, 12.1–12.5_
    - _Verification: TypeScript compiles, hooks exported_

  - [x] 6.9 Create useContacts, useStations, useAsteroids hook modules
    - Create `src/api/hooks/useContacts.ts` with CRUD hooks
    - Create `src/api/hooks/useStations.ts` with CRUD hooks
    - Create `src/api/hooks/useAsteroids.ts` with CRUD hooks
    - _Requirements: 13.1–13.5_
    - _Verification: TypeScript compiles, hooks exported_

- [~] 7. Checkpoint - API layer complete
  - Ensure all endpoint modules and hook modules compile with no TypeScript errors. Ask the user if questions arise.


- [x] 8. Colony Form
  - [x] 8.1 Implement ColonyForm list panel and selection
    - Create `src/pages/authenticated/ColonyForm.tsx`
    - Searchable, sortable colony list in left panel using InfiniteScrollList
    - Selection updates URL param, detail panel shows/hides based on selection
    - _Requirements: 1.1, 1.2_
    - _Verification: Colony list renders, selection shows detail panel_

  - [x] 8.2 Implement ColonyForm Structures tab
    - Add Structures tab to ColonyForm detail panel
    - Display structures with blueprint type, status, workers, processing timer (CountdownTimer)
    - Colony status summary (power, habitation, food, entertainment, warehouse capacity, workers)
    - _Requirements: 1.3, 1.8_
    - _Verification: Structures tab renders with status summary_

  - [x] 8.3 Implement ColonyForm structure actions (Add, Optimize, Bootstrap)
    - Add flatpack FilteredDropdown + Add button to append structure
    - Optimize button calls POST /colony-planner/build-order then persists reorder
    - Bootstrap button calls POST /colonies/{id}/bootstrap
    - _Requirements: 1.4, 1.5, 1.9_
    - _Verification: Add/Optimize/Bootstrap actions call correct endpoints_

  - [x] 8.4 Implement ColonyForm Warehousing and Commodity Requests tabs
    - Warehousing tab: inventory grouped by item type with name, purity, quantity columns
    - Commodity Requests tab: commodity name, quantity, need-by date, fulfilled status
    - _Requirements: 1.6, 1.7_
    - _Verification: Both tabs render with correct data_

  - [x] 8.5 Implement ColonyForm Administration tab
    - Import staleness indicators: no color (0–4 days), yellow (5–6 days), red (6+ days)
    - Colony status report display
    - _Requirements: 1.10_
    - _Verification: Staleness colors render correctly based on lastImportUtc_

- [x] 9. Blueprint Form
  - [x] 9.1 Implement BlueprintForm list panel with filters
    - Create `src/pages/authenticated/BlueprintForm.tsx`
    - Filterable list with columns: type, name, tech level, evolution, nick name, reference count
    - Filter controls: text search, blueprint type, ship class, tech level, evolution level
    - _Requirements: 2.1, 2.2_
    - _Verification: Blueprint list renders with working filters_

  - [x] 9.2 Implement BlueprintForm detail panel and editing
    - Editable detail fields: name, type, ship class, tech level, evolution, nick name, global flag
    - Save/New/Delete buttons with confirmation dialog on delete
    - _Requirements: 2.3, 2.7, 2.8, 2.9_
    - _Verification: Detail fields editable, CRUD operations work_

  - [x] 9.3 Implement BlueprintForm Statistics and Resources tabs
    - Statistics tab: editable grid of numeric properties
    - Resources tab: editable grid of manufacturing resource requirements
    - _Requirements: 2.4, 2.5_
    - _Verification: Both grids render and allow inline editing_

  - [x] 9.4 Implement BlueprintForm Evolution Graph tab
    - Create `src/components/domain/EvolutionChart.tsx`
    - Line chart showing property percentage changes across evolution levels
    - _Requirements: 2.6_
    - _Verification: Chart renders with evolution data_


- [ ] 10. Survey Form
  - [x] 10.1 Implement SurveyForm list panel with filters
    - Create `src/pages/authenticated/SurveyForm.tsx`
    - Filterable list: planet name, system name, survey type, scan date
    - Filters: text search, survey type (Planet/Asteroid/All), purity level, minimum yield
    - _Requirements: 3.1, 3.2_
    - _Verification: Survey list renders with working filters_

  - [x] 10.2 Implement SurveyForm detail panel and resource grid
    - Detail fields: planet name, system name, survey ID, nick name, scanned by, scan date, sensor abundance, purity modifier, scan level, scanner blueprint
    - Resource grid: editable rows with resource name, purity, amount, max reserve
    - _Requirements: 3.3, 3.4, 3.5_
    - _Verification: Detail renders, resource grid allows add/edit/remove_

  - [x] 10.3 Implement SurveyForm CRUD actions
    - Save/New/Delete buttons
    - Delete disabled with tooltip when survey has assigned mining rigs (assignedRigCount > 0)
    - _Requirements: 3.6, 3.7, 3.8, 3.9_
    - _Verification: CRUD works, delete disabled when rigs assigned_

- [x] 11. Player Profile Form
  - [x] 11.1 Implement ProfileForm list and basic details
    - Create `src/pages/authenticated/ProfileForm.tsx`
    - Profile list in left panel, editable fields: name, faction, total credits, skill points
    - Save/New/Delete with confirmation
    - _Requirements: 4.1, 4.2, 4.7, 4.8, 4.9_
    - _Verification: Profile list renders, detail fields editable_

  - [x] 11.2 Implement ProfileForm ranks and skills sections
    - Three rank sections (Public, Private, Military): level, current XP, XP to next
    - Skill groups as collapsible sections (Radix Collapsible) with enable/disable toggle
    - Individual skills: name, level, training status — changes update immediately
    - _Requirements: 4.3, 4.4, 4.5, 4.6_
    - _Verification: Ranks display, skill groups collapse/expand, skill changes persist_

- [x] 12. Delivery Route Form
  - [x] 12.1 Implement RouteForm list and route details
    - Create `src/pages/authenticated/RouteForm.tsx`
    - Filterable route list, detail panel with route name and ordered stops
    - Each stop shows colony name, planet name, system name
    - _Requirements: 5.1, 5.2_
    - _Verification: Route list renders, stops display in order_

  - [x] 12.2 Implement RouteForm stop management
    - Add Stop: colony dropdown + Add button appends stop (works on empty routes)
    - Reorder: Up/Down controls per stop
    - Remove: delete individual stops
    - Save/New/Delete with confirmation
    - _Requirements: 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
    - _Verification: Stops can be added, reordered, removed, and persisted_

  - [x] 12.3 Implement RouteForm Plan tab
    - Plan tab with dropdown to select/create delivery plans
    - New Plan button creates plan with default name (route name + date)
    - Stop selection shows drop-off and pick-up item lists
    - _Requirements: 6.1, 6.2, 6.3_
    - _Verification: Plan tab renders, plans selectable, items display per stop_

  - [x] 12.4 Implement RouteForm Plan item management
    - Add items to drop-off/pick-up: item type, name, purity, quantity fields
    - Remove items from lists
    - Auto-Fill button populates from colony requests/flatpacks/resources
    - Save plan and all item assignments
    - _Requirements: 6.4, 6.5, 6.6, 6.7_
    - _Verification: Items addable/removable, auto-fill populates, save persists_


- [ ] 13. Delivery Execution Form
  - [x] 13.1 Implement ExecutionForm route/plan selection and load list
    - Create `src/pages/authenticated/ExecutionForm.tsx`
    - Route and plan selection dropdowns with text filters
    - Consolidated load list showing all items with total quantity and volume
    - _Requirements: 7.1, 7.2_
    - _Verification: Dropdowns filter, load list aggregates correctly_

  - [x] 13.2 Implement ExecutionForm stop-by-stop execution
    - Each stop as a section with checkable drop-off and pick-up items
    - Checking commodity drop-off marks colony request as fulfilled via API
    - Checking flatpack drop-off marks colony structure as staged via API
    - _Requirements: 7.3, 7.4, 7.5_
    - _Verification: Checkboxes trigger correct API calls_

  - [x] 13.3 Implement ExecutionForm stop completion and plan completion
    - Complete Stop button appears when all items checked; visual completion only after click
    - When all stops complete, mark plan as completed via API
    - _Requirements: 7.6, 7.7, 7.8_
    - _Verification: Complete Stop gates visual state, plan marked done_

- [ ] 14. Ship Template Form
  - [x] 14.1 Implement ShipTemplateForm list and hull selection
    - Create `src/pages/authenticated/ShipTemplateForm.tsx`
    - Template list in left panel, hull dropdown from BaselineData ship classes
    - Hull selection displays available component slots grouped by type
    - _Requirements: 8.1, 8.2, 8.3_
    - _Verification: Template list renders, hull selection shows slots_

  - [x] 14.2 Implement ShipTemplateForm slot assignment and stats
    - Filtered dropdown per slot showing compatible blueprints
    - Create `src/components/domain/ShipStatsPanel.tsx` — live-updating stats (mass, power, cargo, defence, propulsion)
    - _Requirements: 8.4, 8.5_
    - _Verification: Slots assignable, stats update on change_

  - [x] 14.3 Implement ShipTemplateForm pricing and actions
    - Pricing section: pricing plan dropdown, total estimated build cost display
    - Save/New/Order Build buttons
    - Order Build generates manufacturing items via build-plans API
    - _Requirements: 8.6, 8.7, 8.8, 8.9_
    - _Verification: Pricing displays, CRUD works, Order Build calls API_

- [ ] 15. Market Form
  - [x] 15.1 Implement MarketForm Listings tab
    - Create `src/pages/authenticated/MarketForm.tsx`
    - Listings tab: active listings with station, item, quantity, price, condition columns
    - Create new listing form: station, item, quantity, price, condition, max-repair
    - Record Sale: decrements quantity, creates transaction, removes at zero
    - _Requirements: 9.1, 9.2, 9.3_
    - _Verification: Listings display, create works, Record Sale decrements_

  - [-] 15.2 Implement MarketForm Transactions tab
    - Filterable transaction history: type, item, counterparty, faction, station, date range
    - Record purchase form: item, quantity, price, station, counterparty
    - _Requirements: 9.4, 9.5, 9.6_
    - _Verification: Transactions display with filters, purchase recording works_

  - [-] 15.3 Implement MarketForm Summary tab
    - Profit/loss totals for filtered period
    - Per-item breakdown
    - _Requirements: 9.7_
    - _Verification: Summary calculates correctly from filtered transactions_


- [ ] 16. Supply Chain Form
  - [x] 16.1 Implement SupplyChainForm list and detail
    - Create `src/pages/authenticated/SupplyChainForm.tsx`
    - Filterable list, detail panel: name, source colony, destination colony
    - Create new chain: name, source colony dropdown, destination colony dropdown
    - _Requirements: 14.1, 14.2, 14.3_
    - _Verification: List renders, detail shows chain info, create works_

  - [-] 16.2 Implement SupplyChainForm step management
    - Ordered list of steps with resource/commodity, quantity, processing type
    - Add/edit/remove/reorder steps
    - Save/Delete with confirmation
    - _Requirements: 14.4, 14.5, 14.6, 14.7_
    - _Verification: Steps addable, reorderable, removable, persist on save_

- [ ] 17. Stock Target Form
  - [x] 17.1 Implement StockTargetForm list and profile detail
    - Create `src/pages/authenticated/StockTargetForm.tsx`
    - Stock profile list, detail: profile name, assigned colony
    - Create new profile with name and colony assignment
    - _Requirements: 15.1, 15.2, 15.3_
    - _Verification: Profile list renders, detail shows items, create works_

  - [-] 17.2 Implement StockTargetForm item management
    - Stock target items: item type, name, purity, target quantity, current quantity
    - Add/edit/remove items via EditableGrid
    - Save/Delete with confirmation
    - _Requirements: 15.4, 15.5, 15.6, 15.7_
    - _Verification: Items addable/editable/removable, persist on save_

- [ ] 18. Pricing Plan Form
  - [x] 18.1 Implement PricingPlanForm list and detail
    - Create `src/pages/authenticated/PricingPlanForm.tsx`
    - Plan list, detail: plan name, item prices grid (item name, item type, unit price)
    - Create new plan with name
    - _Requirements: 16.1, 16.2, 16.3_
    - _Verification: Plan list renders, detail shows prices, create works_

  - [-] 18.2 Implement PricingPlanForm item price management
    - Add item price: filtered dropdown for item selection + unit price input
    - Edit/remove item prices via EditableGrid
    - Save/Delete with confirmation
    - _Requirements: 16.4, 16.5, 16.6, 16.7_
    - _Verification: Prices addable/editable/removable, persist on save_

- [ ] 19. Shared Data View
  - [x] 19.1 Implement SharedDataView character list and tabs
    - Create `src/pages/authenticated/SharedDataView.tsx`
    - List of characters who shared data, tabs per shared type (blueprints, surveys, colonies)
    - Visual badge distinguishing shared from owned data
    - _Requirements: 17.1, 17.2, 17.3, 17.5_
    - _Verification: Character list renders, tabs switch, badge visible_

  - [x] 19.2 Implement SharedDataView read-only entity display
    - Shared entities in read-only mode using same list/detail layout
    - No edit/save/delete controls rendered for any shared entity
    - _Requirements: 17.4_
    - _Verification: No mutation controls present in rendered output_


- [ ] 20. Colony Activity and Daily Build Pages
  - [x] 20.1 Implement ColonyActivityPage with grouped timers
    - Create `src/pages/authenticated/ColonyActivityPage.tsx`
    - Active timers grouped by activity type (mining, refining, manufacturing, research, building)
    - Timers only display when active; show zero until user navigates away
    - Inactivity mode toggle showing idle structures
    - _Requirements: 11.1, 11.2, 11.3_
    - _Verification: Timers render grouped, inactivity toggle works_

  - [-] 20.2 Implement DailyBuildPage
    - Create `src/pages/authenticated/DailyBuildPage.tsx`
    - Colony selector dropdown
    - Resource requirements and time estimates display only after colony selection triggers colony-planner API
    - _Requirements: 11.4, 11.5_
    - _Verification: Colony selection triggers API, results display_

- [ ] 21. Build Planner Form
  - [~] 21.1 Implement BuildPlannerForm list and detail
    - Create `src/pages/authenticated/BuildPlannerForm.tsx`
    - Build plan list, detail: items with blueprint name, quantity, status, assigned colony
    - Add items by selecting blueprint + quantity
    - _Requirements: 12.1, 12.2, 12.3_
    - _Verification: Plan list renders, items display, add works_

  - [~] 21.2 Implement BuildPlannerForm item status and resource aggregation
    - Mark items as allocated or complete
    - Resource requirements aggregated across pending items
    - _Requirements: 12.4, 12.5_
    - _Verification: Status changes persist, resource totals calculate_

- [ ] 22. Supporting Entity Forms
  - [~] 22.1 Implement ContactsForm
    - Create `src/pages/authenticated/ContactsForm.tsx`
    - List: name, faction, notes with create/edit/delete
    - Inline edit form with all relevant fields
    - _Requirements: 13.1, 13.4, 13.5_
    - _Verification: CRUD operations work for contacts_

  - [~] 22.2 Implement StationsForm
    - Create `src/pages/authenticated/StationsForm.tsx`
    - List: name, system, type with create/edit/delete
    - Inline edit form with all relevant fields
    - _Requirements: 13.2, 13.4, 13.5_
    - _Verification: CRUD operations work for stations_

  - [~] 22.3 Implement AsteroidsForm
    - Create `src/pages/authenticated/AsteroidsForm.tsx`
    - List: name, system, linked survey with create/edit/delete
    - Inline edit form with all relevant fields
    - _Requirements: 13.3, 13.4, 13.5_
    - _Verification: CRUD operations work for asteroids_

- [~] 23. Checkpoint - All form pages complete
  - Ensure all form pages compile, render with mock data, and pass basic interaction tests. Ask the user if questions arise.


- [ ] 24. Real-Time Features: WebSocket Integration
  - [~] 24.1 Extend WebSocket event handler for all entity types
    - Extend `handleServerEvent` in useWebSocket hook
    - Add cases for: colony, blueprint, survey, deliveryRoute, deliveryPlan, shipTemplate, marketListing, marketTransaction, buildPlan, playerProfile, station, asteroid, externalCharacter, supplyChain, stockProfile, stockPlan, pricingPlan
    - Each case invalidates the correct React Query cache key
    - _Requirements: 18.1, 18.2_
    - _Verification: All entity types have handlers that invalidate correct keys_

  - [~] 24.2 Implement connection loss handling and reconnection
    - Exponential backoff reconnection (1s, 2s, 4s, 8s, max 30s)
    - Wire ConnectionBanner to WebSocket state
    - On reconnection: invalidate all active queries
    - After 60s disconnected: show persistent "Connection lost" with manual retry
    - _Requirements: 18.6_
    - _Verification: Banner shows on disconnect, reconnection invalidates cache_

  - [~] 24.3 Implement tab visibility recovery for timers
    - Wire useVisibilityRecovery into ColonyActivityPage and ColonyForm
    - On tab refocus: recalculate all active countdowns from end timestamps
    - Compensates for throttled setInterval in background tabs
    - _Requirements: 18.3, 18.4, 18.5_
    - _Verification: Timers show correct remaining time after simulated background_

- [ ] 25. Real-Time Features: Optimistic Updates
  - [~] 25.1 Implement optimistic update pattern for entity mutations
    - Add optimistic cache updates to save/delete mutations across all hook modules
    - On mutation failure: rollback to pre-mutation cache state
    - Display error toast on rollback
    - _Requirements: 10.5_
    - _Verification: Cache updates optimistically, rolls back on failure_

- [~] 26. Checkpoint - Real-time features complete
  - Ensure WebSocket handlers, connection recovery, timer visibility, and optimistic updates all work. Ask the user if questions arise.

- [ ] 27. Route Registration and Wiring
  - [~] 27.1 Register all form page routes in React Router
    - Update router configuration with all 18 authenticated routes
    - Wire each route to its page component
    - Ensure navigation from Sidebar links to correct pages
    - _Requirements: 10.1_
    - _Verification: All routes resolve to correct components_


- [ ] 28. Testing: Property-Based Tests
  - [ ]* 28.1 Write property test for filter determinism
    - **Property 1: Filter Determinism**
    - Create `src/utils/__tests__/filterUtils.property.test.ts`
    - Generate random entity lists and filter parameter combinations
    - Verify output is deterministic and all items match all active filter criteria
    - **Validates: Requirements 1.1, 2.1, 2.2, 3.1, 3.2, 5.1, 10.3, 14.1**

  - [ ]* 28.2 Write property test for reorder invariant
    - **Property 2: Reorder Invariant**
    - Create `src/utils/__tests__/reorderUtils.property.test.ts`
    - Generate random ordered lists and move sequences
    - Verify result is valid permutation with correct sequential numbering
    - **Validates: Requirements 5.4, 14.5**

  - [ ]* 28.3 Write property test for unsaved changes detection
    - **Property 3: Unsaved Changes Detection**
    - Create `src/hooks/__tests__/useUnsavedChanges.property.test.ts`
    - Generate random edit sequences, verify dirty flag blocks navigation
    - Verify fail-closed behavior when hook unavailable
    - **Validates: Requirements 10.7**

  - [ ]* 28.4 Write property test for timer computation accuracy
    - **Property 4: Timer Computation Accuracy**
    - Create `src/utils/__tests__/timerUtils.property.test.ts`
    - Generate random end timestamps and current times
    - Verify countdown equals max(0, end - now), verify recalculation after background
    - **Validates: Requirements 11.2, 18.3, 18.4**

  - [ ]* 28.5 Write property test for shared data read-only invariant
    - **Property 5: Shared Data Read-Only Invariant**
    - Create `src/pages/authenticated/__tests__/SharedDataView.property.test.tsx`
    - Generate random shared entities, render, verify no mutation controls
    - **Validates: Requirements 17.4**

  - [ ]* 28.6 Write property test for WebSocket event completeness
    - **Property 6: WebSocket Event Completeness**
    - Create `src/api/hooks/__tests__/useWebSocket.property.test.ts`
    - Enumerate all entity types, verify each has handler invalidating correct cache key
    - **Validates: Requirements 18.1, 18.2**

  - [ ]* 28.7 Write property test for optimistic update rollback
    - **Property 7: Optimistic Update Rollback**
    - Create `src/api/hooks/__tests__/optimisticRollback.property.test.ts`
    - Generate random entity states and failed mutations
    - Verify rollback restores original state exactly
    - **Validates: Requirements 10.5**

  - [ ]* 28.8 Write property test for import staleness classification
    - **Property 8: Import Staleness Classification**
    - Create `src/utils/__tests__/staleness.property.test.ts`
    - Generate random day counts, verify: 0–4 → no color, 5–6 → yellow, >6 → red
    - **Validates: Requirements 1.10**


- [ ] 29. Testing: Unit and Integration Tests
  - [ ]* 29.1 Write unit tests for shared components (MasterDetailLayout, FilterBar, EditableGrid)
    - Create `src/components/common/__tests__/MasterDetailLayout.test.tsx`
    - Create `src/components/common/__tests__/FilterBar.test.tsx`
    - Create `src/components/common/__tests__/EditableGrid.test.tsx`
    - Test render, responsive behavior, user interactions
    - _Requirements: 10.2, 10.3, 10.4, 10.9_

  - [ ]* 29.2 Write unit tests for shared components (FilteredDropdown, InfiniteScrollList, CountdownTimer)
    - Create `src/components/common/__tests__/FilteredDropdown.test.tsx`
    - Create `src/components/common/__tests__/InfiniteScrollList.test.tsx`
    - Create `src/components/common/__tests__/CountdownTimer.test.tsx`
    - Test filtering, scroll loading, timer decrement
    - _Requirements: 10.6, 10.8, 11.2, 18.3_

  - [ ]* 29.3 Write unit tests for utility functions
    - Create `src/utils/__tests__/timerUtils.test.ts`
    - Create `src/utils/__tests__/filterUtils.test.ts`
    - Create `src/utils/__tests__/reorderUtils.test.ts`
    - Test edge cases: zero values, empty arrays, boundary dates, null inputs
    - _Requirements: 5.4, 11.2, 14.5, 18.3_

  - [ ]* 29.4 Write integration tests for ColonyForm
    - Create `src/pages/authenticated/__tests__/ColonyForm.test.tsx`
    - Full render with mocked API: select colony → view tabs → add structure → save
    - Test staleness indicator colors
    - _Requirements: 1.1–1.10_

  - [ ]* 29.5 Write integration tests for BlueprintForm and SurveyForm
    - Create `src/pages/authenticated/__tests__/BlueprintForm.test.tsx`
    - Create `src/pages/authenticated/__tests__/SurveyForm.test.tsx`
    - Full render with mocked API: select → edit → save, delete with confirmation
    - _Requirements: 2.1–2.9, 3.1–3.9_

  - [ ]* 29.6 Write integration tests for DeliveryExecution flow
    - Create `src/pages/authenticated/__tests__/ExecutionForm.test.tsx`
    - Test step-by-step execution: check items → Complete Stop → plan completion
    - Verify Complete Stop gates visual completion state
    - _Requirements: 7.1–7.8_

  - [ ]* 29.7 Write integration tests for WebSocket and connection handling
    - Create `src/api/hooks/__tests__/useWebSocket.test.ts`
    - Simulate WebSocket events, verify cache invalidation
    - Simulate disconnect/reconnect, verify banner and query refresh
    - _Requirements: 18.1–18.6_

  - [ ]* 29.8 Write integration tests for responsive layout
    - Create `src/components/common/__tests__/MasterDetailLayout.responsive.test.tsx`
    - Test at mobile/tablet/desktop breakpoints
    - Verify single-panel collapse on mobile with back button
    - _Requirements: 10.9_

- [~] 30. Final Checkpoint - All tests pass
  - Ensure all property-based tests, unit tests, and integration tests pass. Run `vitest --run`. Ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at phase boundaries
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- All code is TypeScript (React 19 + Vite stack)
- Task sizing: max 5 files modified, max 200 LOC, max 3 acceptance criteria per task
- Foundation tasks (1–4) must complete before form pages (8–23)
- API layer tasks (5–7) must complete before form pages that consume them
- Real-time tasks (24–26) can proceed after foundation but before final testing

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4", "1.5", "1.6"] },
    { "id": 1, "tasks": ["2.1", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7", "2.8", "2.9"] },
    { "id": 2, "tasks": ["3.1", "3.2", "3.3", "3.4"] },
    { "id": 3, "tasks": ["5.1", "5.2", "5.3", "5.4", "5.5", "5.6", "5.7"] },
    { "id": 4, "tasks": ["6.1", "6.2", "6.3", "6.4", "6.5", "6.6", "6.7", "6.8", "6.9"] },
    { "id": 5, "tasks": ["8.1", "9.1", "10.1", "11.1", "12.1", "13.1", "14.1", "14.2"] },
    { "id": 6, "tasks": ["8.2", "8.3", "9.2", "10.2", "11.2", "12.2", "13.2", "14.3"] },
    { "id": 7, "tasks": ["8.4", "8.5", "9.3", "9.4", "10.3", "12.3", "13.3"] },
    { "id": 8, "tasks": ["12.4", "15.1", "16.1", "17.1", "18.1", "19.1", "20.1"] },
    { "id": 9, "tasks": ["15.2", "15.3", "16.2", "17.2", "18.2", "19.2", "20.2"] },
    { "id": 10, "tasks": ["21.1", "22.1", "22.2", "22.3"] },
    { "id": 11, "tasks": ["21.2", "24.1", "24.2", "24.3"] },
    { "id": 12, "tasks": ["25.1", "27.1"] },
    { "id": 13, "tasks": ["28.1", "28.2", "28.3", "28.4", "28.5", "28.6", "28.7", "28.8"] },
    { "id": 14, "tasks": ["29.1", "29.2", "29.3", "29.4", "29.5", "29.6", "29.7", "29.8"] }
  ]
}
```
