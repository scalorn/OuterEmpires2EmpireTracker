---
inclusion: manual
---
# Empire Systems ? Implementation Patterns

Quick reference for all cross-cutting patterns required when implementing empire-systems tasks.
Load this file into context when starting any implementation task.

## Build & Test Commands

```
# Build
& "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug /v:minimal

# Run tests
& "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx

# Parse test results
node .kiro/tools/trxparse.js

# Commit (avoids PowerShell quoting issues)
node .kiro/tools/commit.js "Summary" "Body" "Prompt: user said this" --files "path1,path2"

# Backup (run after every commit)
& D:\projects\OuterEmpires2\OE2EmpireTracker\oebackup.ps1
```

## File Writing

Always use fwrite.js for file operations larger than ~10 lines:
```powershell
# Write
@"content"@ | node .kiro/tools/fwrite.js write path/to/file.cs

# Append
@"content"@ | node .kiro/tools/fwrite.js append path/to/file.cs

# Replace (via temp files)
@"old text"@ | Out-File -NoNewline -Encoding utf8 _old.tmp
@"new text"@ | Out-File -NoNewline -Encoding utf8 _new.tmp
node .kiro/tools/fwrite.js replace path/to/file.cs _old.tmp _new.tmp
```

## Service Pattern

Every new service must:
1. private static readonly Logger Log = LogManager.GetCurrentClassLogger();
2. Log entry at Debug, result at Info, validation failures at Warn, exceptions at Error
3. PERF Stopwatch timing on any computation iterating collections
4. Never acquire locks ? caller is responsible
5. Accept data via parameters (Func delegates, IEnumerable), not singletons
6. Return results, don't mutate shared state directly
7. XML doc comments on all public methods

Example:
```csharp
public static class MyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Does the thing.</summary>
    public static Result DoThing(BuildPlan plan, Func<string, Colony> colonyFinder)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Log.Debug("DoThing: plan={0}", plan.Name);
        // ... logic ...
        sw.Stop();
        Log.Info("DoThing PERF: {0}ms, result={1}", sw.ElapsedMilliseconds, result);
        return result;
    }
}
```

## Form Pattern

Every new form must:
1. NLog Logger
2. IProgrammaticUpdateSource with ProgrammaticUpdateGuard
3. Named event handlers (not lambdas) for PlayerContext events
4. Unsubscribe all events in OnFormClosed
5. BeginInvoke for cross-thread UI updates with IsDisposed check
6. PERF timing on PopulateForm, PopulateList, grid rebuilds
7. Reference counting (Refs column, disabled Delete) where applicable
8. Preserve selection across list rebuilds
9. Write-through on all editable controls
10. CancellationTokenSource for background computations

Event handler pattern:
```csharp
private void OnDataChanged(object sender, EventArgs e)
{
    if (IsDisposed) return;
    if (InvokeRequired)
    {
        try { BeginInvoke(new Action(() => OnDataChanged(sender, e))); }
        catch (ObjectDisposedException) { }
        return;
    }
    // ... refresh logic ...
}
```

## Model Pattern

- All properties have defaults (empty string, 0, false)
- DefaultValueHandling.Ignore omits defaults from JSON
- StringEnumConverter on all enum properties
- UUID as string (not Guid)
- OwnerUUID for player-scoped entities
- Deterministic UUID for shared entities (Station, Faction, ExternalCharacter, Asteroid)
- Damage fields (CurrentHP/MaxHP/MaxRepairPercent) default to 0 = undamaged
- IsActive defaults to true on automation entities

## Reference Counter Pattern

```csharp
public class EntityReferenceCounter
{
    private readonly IEnumerable<ReferencingType> _sources;

    public EntityReferenceCounter(IEnumerable<ReferencingType> sources) { _sources = sources ?? Enumerable.Empty<ReferencingType>(); }

    public EntityReferenceReport CountReferences(string uuid)
    {
        if (string.IsNullOrEmpty(uuid)) return EntityReferenceReport.Empty;
        int count = _sources.Count(s => s.ReferenceField == uuid);
        return new EntityReferenceReport(count);
    }
}
```

## Thread Safety

- Lock ordering: _listLock -> ColonyLock -> _syncRoot (never reversed)
- Events fired OUTSIDE all locks
- WriteContext called OUTSIDE ColonyLock
- UI reads: ReadLock -> snapshot -> release -> populate
- UI writes: WriteLock -> mutate -> release -> WriteContext -> fire events
- New entity lists use List<T> (not BindingList<T>), protected by _listLock
- Snapshot methods: SnapshotXxxList() returns copy under _listLock

## Naming Conventions

- Services: {Domain}Service (static, stateless)
- Reference counters: {Entity}ReferenceCounter + {Entity}ReferenceReport
- ViewModels: {Entity}ViewModel
- Forms: Form{Feature} (one per directory under Forms/)
- Constants: Constants/{Domain}Constants.cs or Constants/SlotTypes.cs
- Event args: {Event}EventArgs
- Booleans: Is/Has/Can prefix

## Code Quality

- 80 line max per method
- Max 3 levels of nesting
- XML doc comments on all public members
- ArgumentNullException for null required parameters
- string.IsNullOrEmpty (not just null checks)
- Snapshot iteration for PlayerContext collections
- Zero divisor checks on decimal division
- Grid cell reads: ?.ToString() ?? ""

## Key Design References

- Full design: .kiro/specs/empire-systems/design.md
- Tasks: .kiro/specs/empire-systems/tasks.md
- Forms steering: .kiro/steering/forms.md
- Tech steering: .kiro/steering/tech.md
