# Architecture Principles

## Service Layer is the Single Mutation Path

All data mutations MUST go through the service layer (`Services/*.cs`). No form, ViewModel, or control may directly modify entity properties or call `WriteContext()`.

### Rules

1. **Never bypass a service** — If a service method doesn't do what you need, fix the service. Do not work around it by accessing entities directly from the form.
2. **Never make service methods internal/public just to call them from forms** — If you need logic that lives in a private service method, refactor the service to expose it properly (new public method with clear semantics).
3. **Never duplicate service logic in forms** — If you find yourself reimplementing what a service does (setting flags, calling WriteContext, firing events), you are bypassing the service.
4. **The service owns persistence** — Only services call `WriteContext()`. Forms and ViewModels never call it directly.
5. **The service owns side effects** — If marking an item delivered triggers fulfillment logic, that logic lives in the service, not the form.

### Why

- The service layer is the contract for the future remote server. If forms bypass services, the server can't replicate the behavior.
- The immutable data model (ReadOnly wrappers) exists to prevent accidental mutation. Services are the controlled gate.
- Tests validate service behavior. Bypassed logic is untested logic.

## Present Options Before Choosing

When fixing a bug or implementing a feature where multiple approaches exist:

1. **Identify the root cause first** — Don't patch symptoms. If a lookup fails, ask why the data is wrong, not how to avoid the lookup.
2. **Present options to the user** — If there are 2+ viable approaches with different tradeoffs, list them and ask which direction to take. Do not pick randomly.
3. **Explain tradeoffs** — For each option, state: what it fixes, what it doesn't fix, what it might break, and how much work it is.
4. **Default to fixing the root cause** — If one option fixes the root cause and another patches the symptom, recommend the root cause fix. But still present both and ask.

### When to ask vs when to act

- **Act immediately**: The fix is obvious, single-approach, low-risk (e.g. missing null check, wrong variable name, off-by-one).
- **Ask first**: Multiple viable approaches exist, the fix changes architecture or public APIs, the fix has tradeoffs the user should weigh, or you're unsure which layer the fix belongs in.

## Data Integrity

- If you discover corrupt or inconsistent data (duplicate stops, orphaned references, invalid state), the correct response is:
  1. Fix the code path that created the bad data (prevent future occurrences)
  2. Add a repair/migration that fixes existing bad data transparently
  3. Never silently ignore or work around bad data — it will compound

## Debugging: Log First, Guess Never

When a bug's cause isn't immediately obvious from reading the code:

1. **Add logging and ask the user to retest** — Don't spend multiple iterations guessing. A few `Log.Info`/`Log.Debug` lines at decision points will reveal the actual execution path in one test cycle.
2. **Log at decision points** — Method entry, branch conditions, variable values before comparisons, loop iterations. Show what the code actually sees at runtime.
3. **Don't remove diagnostic logging** — Excess logging is not a concern at this stage of development. Leave it in. If it becomes noisy later, we'll do a dedicated pass to reduce it.
4. **Read the log before theorizing** — After the user retests, read the log output first. Let the data tell you what happened instead of constructing theories from code reading alone.
5. **One logging pass, then fix** — Add logging, get the log, identify the root cause, fix it. Don't add logging and simultaneously attempt a fix based on a guess.
