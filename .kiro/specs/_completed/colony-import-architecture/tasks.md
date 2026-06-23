# Colony Import Architecture Cleanup

## Problem

`ColonyService.Import()` currently orchestrates the import workflow:
1. Receives a partially-parsed temp colony + raw HTML
2. Does dedup (find existing colony by planet name)
3. Calls `ColonyParser.ProcessHtml()` to do the full parse into the target colony
4. Persists and fires events

This means the service calls the parser, which is backwards. The correct flow is:

```
Form → Parser (orchestrates) → Service (pure data)
```

## Desired Architecture

```
Form → Parser.ImportFromClipboard():
  1. Read clipboard, extract HTML
  2. Parse identity (planet name, system name)
  3. Call ColonyService.DedupeOrCreate(planetName, systemName) → target colony
  4. Parse full HTML into target colony
  5. Return fully-parsed colony

Form → ColonyService.Persist(colony):
  1. Write context
  2. Fire ColonyDataChanged event
```

## Changes Required

1. **ColonyService** — split `Import()` into:
   - `DedupeOrCreate(string planetName, string systemName, string ownerUUID)` → returns mutable Colony (existing or new)
   - `Persist(Colony colony)` → WriteContext + fire event
   - Remove `Import()` method (or deprecate)

2. **ColonyParser** — add orchestration method:
   - `ImportFromHtml(string html, EmpireContext ec, ColonyService svc)` → Colony
   - Calls `ProcessHtml` on the deduped target from the service

3. **FormColonyV2** — simplify to:
   ```csharp
   var colony = parser.ImportFromHtml(htmlFragment, empireContext, colonyService);
   colonyService.Persist(colony);
   ```

## Status

- [ ] Implement DedupeOrCreate in ColonyService
- [ ] Implement Persist in ColonyService  
- [ ] Add ImportFromHtml to ColonyParser
- [ ] Update FormColonyV2 to use new flow
- [ ] Remove old Import() method
- [ ] Update tests
