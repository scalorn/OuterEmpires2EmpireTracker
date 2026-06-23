# Remediation Tasks — Unified Storage Backend Gaps

## Overview

This remediation addresses two gaps found during verification:
1. **DynamoDbBackend**: 160 methods throw NotImplementedException (per-character CRUD, permissions, intel, audit, baseline)
2. **StorageBackendFactory**: Only JsonMultiFile case is wired; other 4 backends throw NotImplementedException

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "1.5"] },
    { "id": 2, "tasks": ["1.6"] },
    { "id": 3, "tasks": ["2.1"] }
  ]
}
```

## Tasks

- [ ] 1. DynamoDB Backend — Implement remaining methods
  - [ ] 1.1 DynamoDB CRUD: Per-character entities — Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan (Get/GetAll/Upsert/Delete)
    - Satisfies: Req 5, Criteria 1-2
    - Inputs: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs (existing PutItemDataAsync/GetItemDataAsync/DeleteItemAsync/ScanByPrefixAsync helpers)
    - Output: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs (24 methods implemented)
    - Verification: getDiagnostics — compiles cleanly, grep confirms zero NotImplementedException for these methods
  - [ ] 1.2 DynamoDB CRUD: Per-character entities — Ship, ShipTemplate, MarketListing, MarketTransaction, PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain, Asteroid, Station, Faction, ExternalCharacter, WarehouseOverflowRule, MailMessage, BankingTransaction (Get/GetAll/Upsert/Delete)
    - Satisfies: Req 5, Criteria 1-2
    - Inputs: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs (64 methods implemented)
    - Verification: getDiagnostics — compiles cleanly
  - [ ] 1.3 DynamoDB CRUD: Permission entities — all 14 Faction + Character permission types (28 methods)
    - Satisfies: Req 5, Criteria 1-2; Req 1, Criteria 6-8
    - Inputs: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs (28 methods implemented)
    - Verification: getDiagnostics — compiles cleanly
  - [ ] 1.4 DynamoDB CRUD: Intel (8 methods) + Audit (3 methods) + ColonySummary (1 method)
    - Satisfies: Req 5, Criteria 1-2; Req 1, Criteria 7-8
    - Inputs: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs (12 methods implemented)
    - Verification: getDiagnostics — compiles cleanly
  - [ ] 1.5 DynamoDB CRUD: Baseline/global lookup data (16 methods — GameConstants, BlueprintType, ShipClass, TechLevel, Commodity, RefiningRecipe, ResearchTime, PropertyTypeDefinition)
    - Satisfies: Req 5, Criteria 1-2; Req 1, Criterion 9
    - Inputs: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs (16 methods implemented)
    - Verification: getDiagnostics — compiles cleanly
  - [ ] 1.6 Verify DynamoDbBackend has zero NotImplementedException stubs remaining
    - Satisfies: Req 5, Criterion 1 (SHALL implement IStorageBackend)
    - Inputs: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs
    - Output: grep confirms zero NotImplementedException; full solution build passes
    - Verification: `Select-String -Path DynamoDbBackend.cs -Pattern NotImplementedException` returns zero results

- [ ] 2. Wire StorageBackendFactory
  - [ ] 2.1 Update StorageBackendFactory to create all 5 backend types
    - Satisfies: Req 8, Criteria 1-4
    - Inputs: OE2EmpireTracker.Common/Storage/StorageBackendFactory.cs, all 5 backend classes
    - Output: StorageBackendFactory.cs with all switch cases creating real backend instances; update StorageBackendFactoryTests to expect success (not NotImplementedException) for all types
    - Verification: Full solution build + updated factory tests pass

## Notes

- DynamoDB uses a single-table design (PK/SK pattern). The existing helpers (PutItemDataAsync, GetItemDataAsync, DeleteItemAsync, ScanByPrefixAsync) serialize entities as JSON and store them with a composite key. All new methods follow this same pattern — they just need the correct PK/SK prefixes for each entity type.
- The factory wiring is trivial once all backends exist — just replace `throw new NotImplementedException()` with constructor calls.
- Task 1.2 exceeds the 5-file limit on paper (64 methods in 1 file) but it's the same repetitive pattern applied to 16 entity types in a single file. The LOC will be high but the logic is identical for each.
