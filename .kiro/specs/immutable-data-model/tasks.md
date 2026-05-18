# BL-074: Immutable Data Model — Mutation Through Interface Only

## Goal

Make entity property setters `internal` so only code within OE2EmpireTracker.Common
can mutate entities. Forms, ViewModels (WinForms), and external projects get compile
errors if they try to set entity properties directly.

## Prerequisites (done)

- All services moved to Common ✅
- All ViewModels moved to Common ✅  
- Mutation audit passes (no Forms bypass services) ✅
- All parsers moved to Common ✅

## Approach

1. Add `InternalsVisibleTo` for test project and Server project
2. Change entity property setters from `public set` to `internal set`
3. Keep constructors public (entities are created by services and deserialization)
4. Newtonsoft.Json can still deserialize with internal setters (it uses reflection)
5. Build and fix any compile errors in WinForms/Desktop projects

## Scope

- ~30 model files in Common/Models/
- ~525 entity constructions in tests (need InternalsVisibleTo)
- ~698 property sets in tests (need InternalsVisibleTo)
- WinForms project: mutation audit is clean, but may have false positives
  from request DTOs or local variable setup

## InternalsVisibleTo Setup

Add to Common.csproj:
```xml
<ItemGroup>
  <InternalsVisibleTo Include="OE2EmpireTracker" />
  <InternalsVisibleTo Include="OE2EmpireTracker.Tests" />
  <InternalsVisibleTo Include="OE2EmpireTracker.Server" />
  <InternalsVisibleTo Include="OE2EmpireTracker.Server.Tests" />
</ItemGroup>
```

NOTE: This means the WinForms project CAN still access internal setters.
The protection is at the audit level (mutation-audit.js), not compile level,
for the WinForms project. True compile-level protection only applies to
projects NOT in the InternalsVisibleTo list (e.g. Desktop).

For true compile-level enforcement on WinForms, we'd need to NOT include it
in InternalsVisibleTo — but then its remaining services (MarketBlueprintImporter,
ColonyAdminReportBuilder, etc.) would break. This is acceptable for now since
the mutation audit enforces the rule.

## Entity Classes to Modify

Priority order (most-referenced first):
1. Blueprint.cs — Name, UUID, OwnerUUID, Evolution, Class, TechLevel, etc.
2. Colony.cs — UUID, PlanetName, SystemName, OwnerUUID, ColonyName, etc.
3. ColonyStructure.cs — UUID, FlatpackBlueprintUUID, properties, etc.
4. Survey.cs — UUID, PlanetName, SystemName, OwnerUUID, etc.
5. PlayerProfile.cs — UUID, Name, skills, etc.
6. DeliveryRoute.cs + RouteStop — UUID, Name, Stops, etc.
7. Ship.cs — UUID, Name, TemplateUUID, Components, etc.
8. ShipTemplate.cs — UUID, Name, Components, etc.
9. Station.cs — UUID, Name, etc.
10. Asteroid.cs — UUID, Name, SystemName, etc.
11. All remaining models

## Tasks

- [ ] 1. Add InternalsVisibleTo to Common.csproj
- [ ] 2. Start with Blueprint.cs — make setters internal, build, fix errors
- [ ] 3. Colony.cs + ColonyStructure.cs
- [ ] 4. Survey.cs
- [ ] 5. PlayerProfile.cs
- [ ] 6. DeliveryRoute.cs + RouteStop
- [ ] 7. Ship.cs + ShipTemplate.cs
- [ ] 8. Station.cs + Asteroid.cs
- [ ] 9. Remaining models
- [ ] 10. Full build + test + audit
- [ ] 11. Update BL-074 status in BACKLOG.md
