# Desktop Common Deduplication

## Problem

The Desktop (Avalonia) project has 27 files that duplicate logic now in Common:
- 23 services with the same names as Common services
- 4 parsers identical to Common parsers

These were written as Avalonia-specific rewrites during the initial port (before the Common library had the shared logic). Now that Common has the canonical implementations, the Desktop project should use them instead of maintaining parallel copies.

## Key Differences Between Desktop and Common Versions

| Aspect | Common | Desktop |
|--------|--------|---------|
| DI | Singletons (PlayerContext.GetInstance()) | Constructor injection (ILogger, DataService) |
| Logging | NLog (LogManager.GetCurrentClassLogger()) | Microsoft.Extensions.Logging (ILogger<T>) |
| Data access | PlayerContext/EmpireContext singletons | DataService wrapper |
| Persistence | PlayerContext.WriteContext() | DataService.SaveAsync() |
| Events | C# events (PlayerContext.*Changed) | CommunityToolkit.Mvvm Messenger |
| Async | Synchronous | Some async patterns |

## Strategy

The Desktop services fall into three categories:

### Category 1: Delete and use Common directly (pure logic, no DI needed)
These Desktop services are thin wrappers that just call the same logic Common now has:
- CountdownFormatParser
- YieldDistributionService  
- PriceCalculator
- SafeFileWriter
- SystemRepository

**Action:** Delete Desktop copies. Update Desktop ViewModels to call Common types directly.

### Category 2: Thin adapter over Common (CRUD services)
These Desktop services do CRUD operations that Common services now handle. The Desktop version adds DataService/Messenger integration:
- AsteroidService, BlueprintService, BuildPlanService, ColonyService
- ContactsService, DeliveryPlanService, DeliveryRouteService
- MarketService, PlayerProfileService, PricingPlanService
- ShipService, ShipTemplateService, StationService
- StockTargetService, SupplyChainService, SurveyService

**Action:** Refactor each to delegate to Common's service for the actual mutation, keeping only the Avalonia-specific wrapper (DataService save, Messenger notification). This may mean:
1. Desktop service holds a reference to Common's service
2. Desktop service calls Common service for the mutation
3. Desktop service calls DataService.SaveAsync() and sends Messenger notification

### Category 3: Platform-specific (keep Desktop version)
- BackgroundProcessor (uses Avalonia DispatcherTimer)
- PreferencesStore (different storage path/format for cross-platform)

### Parsers: Delete and use Common directly
All 4 parsers (BlueprintScanner, ColonyParser, PlayerProfileParser, SurveyParser) are duplicates. The Common versions are now clipboard-free and can be used directly.

**Action:** Delete Desktop parser copies. Update Desktop ViewModels to instantiate Common parsers.

## Risks

- **Namespace conflicts:** Common uses `OE2EmpireTracker.Services`, Desktop uses `OE2EmpireTracker.Desktop.Services`. Both are visible when Desktop references Common. Need to qualify or alias.
- **Singleton vs DI:** Common services use singletons. Desktop uses DI. May need adapter pattern or refactor Common to support both.
- **Event model:** Common fires C# events. Desktop uses Messenger. Adapters needed.
- **Testing:** Desktop has no test project currently. Changes must be manually verified.

## Tasks

### Phase 1: Parsers (low risk, no DI issues)
- [ ] 1.1 Delete Desktop/Parsers/BlueprintScanner.cs — update callers to use Common's
- [ ] 1.2 Delete Desktop/Parsers/ColonyParser.cs — update callers to use Common's
- [ ] 1.3 Delete Desktop/Parsers/PlayerProfileParser.cs — update callers to use Common's
- [ ] 1.4 Delete Desktop/Parsers/SurveyParser.cs — update callers to use Common's
- [ ] 1.5 Fix any namespace ambiguities (qualify with full namespace where needed)

### Phase 2: Pure logic services (no DI, no events)
- [ ] 2.1 Delete Desktop/Services/CountdownFormatParser.cs
- [ ] 2.2 Delete Desktop/Services/YieldDistributionService.cs
- [ ] 2.3 Delete Desktop/Services/PriceCalculator.cs
- [ ] 2.4 Delete Desktop/Services/SafeFileWriter.cs
- [ ] 2.5 Delete Desktop/Services/SystemRepository.cs
- [ ] 2.6 Fix callers to use Common types directly

### Phase 3: CRUD services — DEFERRED (not true duplicates)

Analysis revealed these Desktop services are NOT duplicating Common's logic. They're thin
CRUD wrappers around Desktop's `DataService` (the Avalonia data layer), while Common's
services operate on `PlayerContext`/`EmpireContext` singletons. They do the same thing
conceptually but against different data stores.

To truly unify them would require either:
- Making Desktop use PlayerContext/EmpireContext (abandon DataService) — huge rewrite
- Making Common services accept an interface for data access — significant refactor

Neither is worth doing now. The real shared logic (parsing, calculations, validation,
background processing) is already in Common. These 60-130 line CRUD wrappers are
platform-specific and should stay.

**Status: Deferred — not cost-effective to unify at this time.**

### Phase 4: Cleanup
- [ ] 4.1 Remove any dead code / unused usings
- [ ] 4.2 Verify Desktop builds with zero warnings
- [ ] 4.3 Manual testing on Windows
- [ ] 4.4 Manual testing on Linux (if available)
