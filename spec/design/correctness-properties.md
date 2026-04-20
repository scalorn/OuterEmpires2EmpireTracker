<!-- Extracted from .kiro/specs/empire-systems/design.md, lines 3118-3155 — Correctness Properties -->
# Correctness Properties

## Property 1: Build item quantity validation
For any integer, a build item accepts it as quantity if and only if it is ≥ 1.

## Property 2: Queue calculator — manufactory
For any blueprint with a known manufacturing time and any positive target duration, the computed quantity × manufacturing time ≥ target duration.

## Property 3: Queue calculator — commodity
For any positive target duration, the computed runs × CommodityCycleSeconds ≥ target duration.

## Property 4: Resource shortfall computation
For any build item allocated to a build location (colony, ship, or station) with an associated delivery route, the shortfall for each resource equals max(0, required - available), where available = build location warehouse/hold stock + current player's station hold stock at stations on the route. Currently only Colony build locations are supported.

## Property 5: Delivery plan generation covers all shortfalls
For any build plan with shortfalls, the generated delivery plan's drop-off items cover every shortfall quantity.

## Property 6: Ship class assembly validation
For any ship class and station type, ValidateAssemblyLocation returns null iff the class/type combination is permitted (2-5 any, 6 Station+Starbase, 7-8 Starbase only).

## Property 7: Stock target shortfall computation
For any stock target within a stock plan, the shortfall equals max(0, target - current quantity) where current quantity is scoped correctly (empire-wide sums all colony warehouses + current player's station holds, colony scope checks one warehouse, station scope checks one player hold). IsCritical is true iff current quantity < CriticalThreshold. Within a plan, overlapping component requirements use max (OR pooling). Across plans, requirements are summed (AND/dedicated).

## Property 8: Market sale decrements listing
For any sell transaction linked to a listing, the listing quantity after recording equals the listing quantity before minus the transaction quantity.

## Property 9: Market purchase adds to station hold
For any buy transaction at a station, the station hold quantity of the purchased item after recording equals the hold quantity before plus the transaction quantity.

## Property 10: Serialization round-trip
For all new entity types (BuildPlan, ShipTemplate, Ship, Station, MarketListing, MarketTransaction, StockPlan, SupplyChain, Faction, ExternalCharacter, Asteroid, WarehouseOverflowRule, StockProfile), serializing then deserializing produces equivalent objects.

## Property 11: RouteStop migration preserves destinations
For any existing RouteStop with ColonyUUID, after migration DestinationUUID equals ColonyUUID and DestinationType equals Colony.

## Property 12: Build item status cascade monotonicity
For any build item, the cascade processor only advances status forward (Staged < Delivering < Ready). It never sets InProgress or Completed, and never decreases the status ordinal. The result is max(currentStatus, computedStatus).
