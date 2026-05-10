# Remote Faction Service — Open Items

Items that need further design and discussion before implementation.

## OI-001: Faction Logistics Coordination

**Status:** Open

**Problem:** Factions need to coordinate logistics among members — requesting resources, manufacturing jobs, and deliveries. The current design supports data sharing (read-only visibility) but has no mechanism for members to make requests of each other or track fulfillment.

**Scenarios:**
- A faction leader sees the faction needs 500 Steel Plates for a station build. They want to post a request that any member can claim and fulfill.
- A member has excess Titanium Ore and wants to offer it to the faction. Others should be able to see the offer and arrange pickup.
- A leader wants to request that a specific member manufacture 20 Reactor Mk3s using their blueprints.
- A delivery pilot wants to see all pending resource requests across faction colonies and plan a route to fulfill them.

**Questions to resolve:**
1. What is the backing data model? (Request entity with type, quantity, source/destination, status, assignee?)
2. Are requests faction-wide (anyone can claim) or directed (assigned to a specific member)?
3. How do requests relate to existing DeliveryRoutes/DeliveryPlans? Are they a higher-level abstraction that generates delivery plans?
4. What states does a request go through? (Open → Claimed → In Transit → Fulfilled → Closed?)
5. Who can create requests? (Leaders only, or any member?)
6. How does fulfillment get confirmed? (Manual acknowledgment, or automatic when inventory changes are detected via server-side processing?)
7. Should there be a "faction board" UI showing all open requests, or does it integrate into existing forms (colony view shows "3 pending requests for this colony")?
8. How do requests interact with the sharing model? (If I fulfill a request by delivering to someone's colony, do I need write access to their colony data?)
9. Priority/urgency levels on requests?
10. Expiry on unfulfilled requests?

**Dependencies:** Requires the core sharing and membership model to be implemented first.

---

## OI-002: Game API Integration Specifics

**Status:** Open — blocked on game API documentation

**Problem:** The server needs to call the game API on behalf of characters (for background processing, data import, etc.). The credential storage mechanism is designed (Section 25), but the actual API endpoints, authentication flow, rate limits, and data formats are unknown.

**Questions to resolve:**
1. What authentication does the game API use? (OAuth2, API key, session cookie?)
2. What endpoints are available? (Colony data, market data, player stats?)
3. What are the rate limits? (Per-account, per-IP, global?)
4. Is there a webhook/push mechanism, or is polling required?
5. How does the server handle token refresh if the game uses OAuth2?
6. What happens when game API credentials expire — notify the character via WebSocket?

**Dependencies:** Game API documentation from the game developers.

---

## OI-003: Multi-Server Federation

**Status:** Future consideration

**Problem:** If two factions run separate servers, can they share data across servers for alliance coordination?

**Not currently planned** — noted for future consideration if cross-faction alliances become common.

---

## OI-004: Audit Log

**Status:** Open — requirements clarified, needs detailed design

**Problem:** For faction governance and future write-delegation scenarios, all mutations need a full audit trail with diffs showing what changed, who changed it, and when.

**Resolved decisions:**
1. **What events:** Every mutation (create, update, delete) on any entity. Full diff (before/after JSON) stored per change.
2. **Retention:** Configurable at three levels — Owner sets a server-wide maximum (they pay for storage), Faction Leaders can set a faction-level retention within that maximum, Characters can set per-entity retention within the faction limit. Hierarchy: Owner max >= Faction Leader setting >= Character setting.
3. **Who can view:** You can view the audit history of anything you can view. Same sharing/access rules apply to audit records as to the underlying data.
4. **Who did it:** Every audit record includes the character UUID and token ID of who performed the mutation. This is forward-looking — when write delegation is added, the audit trail shows who actually made the change vs who owns the data.
5. **Queryable:** Yes — filter by character, entity UUID, entity type, date range, event type (create/update/delete).

**Questions remaining:**
1. What is the diff format? (JSON Patch RFC 6902, or full before/after snapshots, or both?)
2. ~~How is retention enforced?~~ **Resolved:** TTL for DynamoDB, background pruning job for JSON files/SQLite/PostgreSQL.
3. API shape for querying audit records? (GET /api/v1/audit?entity={uuid}&from={date}&to={date}?)
4. Should audit records be included in data exports?
5. Storage impact — full diffs on every colony tick (60s) could be large. Should timer ticks be batched or summarized differently from user-initiated mutations?

**Dependencies:** Core CRUD endpoints must be implemented first. Audit is a cross-cutting concern added to the mutation pipeline.

---

## OI-005: ~~Notification Preferences~~

**Status:** Closed — not needed

Per the design (Section 7.3), event filtering already limits pushes to only data the character has access to. The volume is inherently bounded by what's shared with you. Additionally, push events are lightweight notifications (event type, entity type, UUID, timestamp) — not full entity payloads. The client only fetches the full record via REST if the user is actively viewing that entity. No additional filtering mechanism needed.
