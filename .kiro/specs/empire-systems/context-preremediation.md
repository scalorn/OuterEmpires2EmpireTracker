# Empire Systems ? Iteration Context: Pre-Remediation

## What This Phase Does
Fix existing bugs and add missing logging/PERF before new feature work begins.

## Prerequisites
None ? this is the first phase.

## Key Files to Read
- .kiro/steering/empire-patterns.md (implementation patterns)
- .kiro/steering/forms.md (WinForms UI patterns)
- .kiro/specs/empire-systems/tasks.md (tasks 1-3)

## Tasks Summary
1. Fix FormPricingPlan cross-thread bug (BeginInvoke in OnCurrentPlayerChanged)
2. Fix FormColonyActivity event leak (unsubscribe in OnFormClosed)
3. Create DeliveryRouteReferenceCounter (counts DeliveryPlans, WarehouseOverflowRules, SupplyChainStages)
4. Fix FormSurvey resource filter combo (cmbResource not wired to GetFilteredSurveys)
5. Add NLog Logger to forms and services missing it
6. Add PERF Stopwatch timing to all forms

## Existing Patterns to Follow
- BlueprintReferenceCounter in Services/ (reference counter pattern)
- FormColonyV2 (event subscribe/unsubscribe, BeginInvoke, PERF timing)
- ColonyReferenceCounter (multi-source reference counting)

## What Gets Built
- Services/DeliveryRouteReferenceCounter.cs + DeliveryRouteReferenceReport
- Refs column + delete protection on FormDeliveryRoute
- NLog + PERF additions to existing forms/services
- FormSurvey resource filter fix

## Completion Criteria
- Build passes
- All 1463+ existing tests pass
- No new tests required (these are fixes to existing code)
