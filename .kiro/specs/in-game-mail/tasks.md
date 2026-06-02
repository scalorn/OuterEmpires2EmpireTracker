# Implementation Plan: In-Game Mail

## Overview

Implement a local mail archive for Outer Empires 2 players following the established architecture pattern: POCO model, static service class, PlayerContext persistence, and WinForms MDI child form. The system syncs mail incrementally from the game API using a high-water mark strategy, stores messages locally, and presents them in a filterable list/detail MDI child form.

## Tasks

- [ ] 1. Create MailMessage data model
  - [x] 1.1 Create MailMessage POCO class
    - Create `OE2EmpireTracker.Common/Models/MailMessage.cs` with all required fields: MailId (int), CharacterIdFrom (int), FromName (string), CharacterIdTo (int), ToName (string), SentTime (string), Subject (string), MailRead (bool), MailType (string nullable), MailContent (string), LocalRead (bool)
    - Use JsonProperty attributes for JSON serialization
    - Default FromName, ToName, Subject, MailContent to string.Empty
    - Default LocalRead to false
    - _Satisfies: Req 1, Criteria 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_
    - _Inputs: design.md (Data Models section)_
    - _Output: OE2EmpireTracker.Common/Models/MailMessage.cs_
    - _Verification: getDiagnostics on the new file_


  - [-] 1.2 Write unit tests for MailMessage model
    - Create `OE2EmpireTracker.Tests/Models/MailMessageTests.cs`
    - Test JSON serialization round-trip (all fields preserved)
    - Test default values (FromName, ToName, MailContent default to string.Empty; LocalRead defaults to false)
    - Test nullable MailType serialization (null vs "C", "R", "S")
    - _Satisfies: Req 1, Criteria 1.1, 1.5, 1.6_
    - _Inputs: MailMessage.cs_
    - _Output: OE2EmpireTracker.Tests/Models/MailMessageTests.cs_
    - _Verification: vstest.console passes MailMessageTests_

- [ ] 2. Add MailMessage persistence to PlayerContext
  - [-] 2.1 Add MailMessage array to PlayerRoot
    - Add `MailMessage[]` property with JsonProperty("mailMessage") to PlayerRoot
    - Initialize to empty array in constructor
    - _Satisfies: Req 2, Criterion 2.1_
    - _Inputs: PlayerRoot.cs, design.md (PlayerRoot Extension section)_
    - _Output: OE2EmpireTracker.Common/Services/PlayerRoot.cs_
    - _Verification: getDiagnostics on PlayerRoot.cs_

  - [~] 2.2 Add MailMessage list management to PlayerContext
    - Add _mailMessageList field (List<MailMessage>), _mailMessageCache field (Dictionary<int, MailMessage>)
    - Add MailMessageList property (IReadOnlyList<MailMessage>)
    - Add MailDataChanged event
    - Add InitMailMessages method with deduplication by MailId (log error for duplicates)
    - Call InitMailMessages from the existing initialization path
    - _Satisfies: Req 2, Criteria 2.2, 2.3_
    - _Inputs: PlayerContext.cs, design.md (PlayerContext Integration section)_
    - _Output: OE2EmpireTracker.Common/Services/PlayerContext.cs_
    - _Verification: getDiagnostics on PlayerContext.cs_


  - [~] 2.3 Add AddMailMessage and FindMailMessage methods to PlayerContext
    - Implement AddMailMessage with duplicate skip (log warning, no throw)
    - Implement FindMailMessage with dictionary cache lookup
    - Add MailMessage serialization to WriteContext (playerRoot.MailMessage = _mailMessageList.ToArray())
    - _Satisfies: Req 2, Criteria 2.4, 2.5_
    - _Inputs: PlayerContext.cs, design.md (PlayerContext Integration section)_
    - _Output: OE2EmpireTracker.Common/Services/PlayerContext.cs_
    - _Verification: getDiagnostics on PlayerContext.cs_

  - [~] 2.4 Write property test for deduplication invariant
    - **Property 1: Deduplication Invariant**
    - Generate random sequences of AddMailMessage calls with repeated MailIds
    - Assert that MailMessageList never contains duplicate MailIds
    - **Validates: Req 2, Criteria 2.3, 2.4**
    - _Inputs: PlayerContext.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes Property 1 test_

- [ ] 3. Implement MailService core methods
  - [~] 3.1 Create MailService static class with filter logic
    - Create `OE2EmpireTracker.Common/Services/MailService.cs`
    - Implement GetFilteredMessages with AND logic: readFilter (All/Unread/Read), typeFilter (All/Player-System/Colony/Research/Skill), searchText (case-insensitive substring on FromName OR Subject OR MailContent)
    - Implement GetUnreadCount
    - Implement GetHighWaterMark (max MailId, or 0 if empty)
    - Add MailDataChanged event and OnMailDataChanged method
    - _Satisfies: Req 5, Criteria 5.3, 5.4, 5.6; Req 6, Criterion 6.1_
    - _Inputs: design.md (MailService section, Filter Logic section)_
    - _Output: OE2EmpireTracker.Common/Services/MailService.cs_
    - _Verification: getDiagnostics on MailService.cs_


  - [~] 3.2 Implement MailService.MarkAsRead method
    - Implement MarkAsRead: find message by MailId, set LocalRead = true, persist via WriteContext
    - LocalRead is independent of MailRead (never modify MailRead)
    - _Satisfies: Req 6, Criteria 6.1, 6.3_
    - _Inputs: MailService.cs, PlayerContext.cs_
    - _Output: OE2EmpireTracker.Common/Services/MailService.cs_
    - _Verification: getDiagnostics on MailService.cs_

  - [~] 3.3 Write property test for filter completeness
    - **Property 3: Filter Completeness**
    - Generate random mail sets and random filter combinations
    - Assert GetFilteredMessages result equals manual predicate evaluation
    - **Validates: Req 5, Criterion 5.4**
    - _Inputs: MailService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes Property 3 test_

  - [~] 3.4 Write property test for LocalRead independence
    - **Property 4: LocalRead Independence**
    - Generate mail messages, call MarkAsRead
    - Assert MailRead field is never modified when LocalRead changes
    - **Validates: Req 6, Criteria 6.1, 6.2**
    - _Inputs: MailService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes Property 4 test_

- [ ] 4. Implement incremental sync algorithm
  - [~] 4.1 Implement SyncMailAsync method
    - Implement the incremental sync algorithm in MailService.SyncMailAsync
    - Call GetMailListAsync with offset/limit pagination (page size 50)
    - Early-stop when all mailIds on a page already exist locally
    - For new mailIds, call GetMailDetailAsync for full content
    - Map API response fields to MailMessage model (use detail endpoint's FromName)
    - Set LocalRead = false on all new messages
    - Fire MailDataChanged event when new messages are added
    - _Satisfies: Req 3, Criteria 3.3, 3.4, 3.5, 3.6, 3.7, 3.11; Req 6, Criterion 6.2_
    - _Inputs: design.md (Sync Algorithm section), GameApiClient.cs_
    - _Output: OE2EmpireTracker.Common/Services/MailService.cs_
    - _Verification: getDiagnostics on MailService.cs_


  - [~] 4.2 Implement sync error handling
    - Handle HTTP 401/403: log "Authentication failed for mail sync" at Error level, stop sync cycle
    - Handle HTTP 429: log "Rate limited during mail sync" at Warning level, stop sync cycle
    - Handle detail endpoint failure: log error, store message with empty MailContent, continue sync
    - Handle network failure: log at Error level, stop sync cycle
    - Return -1 on error, newCount on success
    - _Satisfies: Req 3, Criterion 3.9; Req 8, Criteria 8.1, 8.2, 8.3_
    - _Inputs: MailService.cs, design.md (Sync Algorithm section)_
    - _Output: OE2EmpireTracker.Common/Services/MailService.cs_
    - _Verification: getDiagnostics on MailService.cs_

  - [~] 4.3 Write property test for high-water mark monotonicity
    - **Property 2: High-Water Mark Monotonicity**
    - Generate sequences of AddMailMessage calls
    - Assert GetHighWaterMark never decreases after additions
    - **Validates: Req 3, Criteria 3.3, 3.4**
    - _Inputs: MailService.cs, PlayerContext.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes Property 2 test_

  - [~] 4.4 Write property test for sync idempotency
    - **Property 5: Sync Idempotency**
    - Add a set of messages, record state, add same messages again
    - Assert list state is identical after second pass (no duplicates, no losses)
    - **Validates: Req 3, Criteria 3.3, 3.4, 3.5**
    - _Inputs: MailService.cs, PlayerContext.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes Property 5 test_

- [~] 5. Checkpoint - Ensure model and service compile and pass tests
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 6. Add sync interval preference
  - [~] 6.1 Add MailSyncIntervalMinutes to PreferencesStore
    - Add MailSyncIntervalMinutes property (default 5, min 1, max 60) to the preferences model
    - Add GetMailSyncIntervalMs() static helper method that clamps and converts to milliseconds
    - _Satisfies: Req 7, Criterion 7.1_
    - _Inputs: PreferencesStore.cs, design.md (Preferences Integration section)_
    - _Output: OE2EmpireTracker.Common/Services/PreferencesStore.cs_
    - _Verification: getDiagnostics on PreferencesStore.cs_

- [ ] 7. Create FormMail Designer layout
  - [~] 7.1 Create FormMail form files with split layout
    - Create `OE2EmpireTracker/Forms/Mail/FormMail.cs`, `FormMail.Designer.cs`, `FormMail.resx`
    - Layout: filter panel (top), SplitContainer with ListView (left) and detail panel (right), status bar (bottom)
    - Filter panel controls: cboReadFilter, cboTypeFilter, txtSearch, lblMessageCount
    - ListView columns: From, Subject, Date
    - Detail panel: lblDetailFrom, lblDetailDate, lblDetailSubject, txtDetailContent (multiline readonly)
    - Status bar: lblSyncStatus, lblPlaceholder
    - _Satisfies: Req 4, Criteria 4.1, 4.2, 4.3_
    - _Inputs: design.md (FormMail section, Layout diagram)_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs, FormMail.Designer.cs, FormMail.resx_
    - _Verification: getDiagnostics on FormMail.cs and FormMail.Designer.cs_


  - [~] 7.2 Implement FormMail filter initialization and event wiring
    - Populate cboReadFilter (All, Unread, Read) and cboTypeFilter (All, Player/System, Colony, Research, Skill) in constructor
    - Implement IProgrammaticUpdateSource interface
    - Subscribe to PlayerContext.MailDataChanged and CurrentPlayerChanged events
    - Wire filter change handlers (cboReadFilter, cboTypeFilter, txtSearch)
    - Wire lvwMail.SelectedIndexChanged handler
    - Unsubscribe events in OnFormClosed
    - _Satisfies: Req 5, Criteria 5.1, 5.2; Req 4, Criterion 4.1_
    - _Inputs: FormMail.Designer.cs, design.md (Event subscriptions table)_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs_
    - _Verification: getDiagnostics on FormMail.cs_

  - [~] 7.3 Implement mail list display and refresh logic
    - Implement RefreshMailList: call MailService.GetFilteredMessages, populate ListView
    - Sort by SentTime descending (newest first); use epoch for null/empty SentTime, display "(no date)"
    - Bold text for unread messages (LocalRead = false)
    - Display message count label ("N messages")
    - Update title bar with unread count ("Mail (N unread)" or "Mail")
    - _Satisfies: Req 4, Criteria 4.4, 4.6, 4.8, 4.9; Req 6, Criteria 6.4, 6.5; Req 8, Criterion 8.4_
    - _Inputs: FormMail.cs, MailService.cs_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs_
    - _Verification: getDiagnostics on FormMail.cs_


  - [~] 7.4 Implement message selection and detail display
    - OnMailSelected: display full MailContent in txtDetailContent, FromName in lblDetailFrom, formatted date in lblDetailDate, Subject in lblDetailSubject
    - Call MailService.MarkAsRead on selection to set LocalRead = true
    - Update row styling from bold to normal after marking read
    - When no messages exist, show "No messages" placeholder; when messages exist but none selected, detail panel is empty
    - _Satisfies: Req 4, Criteria 4.5, 4.7, 4.8; Req 6, Criterion 6.3_
    - _Inputs: FormMail.cs, MailService.cs_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs_
    - _Verification: getDiagnostics on FormMail.cs_

  - [~] 7.5 Implement filter application logic in FormMail
    - OnFilterChanged: call MailService.GetFilteredMessages with current filter values
    - Combine read-status filter, type filter, and search text with AND logic
    - Map dropdown values to MailType: "Player/System" → null, "Colony" → "C", "Research" → "R", "Skill" → "S"
    - Ensure refresh completes within 500ms for up to 5000 messages
    - _Satisfies: Req 5, Criteria 5.3, 5.4, 5.5, 5.6_
    - _Inputs: FormMail.cs, MailService.cs_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs_
    - _Verification: getDiagnostics on FormMail.cs_

- [ ] 8. Implement background sync timer in FormMail
  - [~] 8.1 Add background sync timer and sync execution
    - Create System.Threading.Timer with one-shot mode in FormMail
    - Trigger immediate sync on form open (Task.Run + ExecuteSyncAsync)
    - Reschedule timer after each sync completes using GetMailSyncIntervalMs
    - Use _isSyncing flag to prevent re-entrant sync
    - Marshal UI updates back via BeginInvoke
    - _Satisfies: Req 3, Criteria 3.1, 3.2, 3.8_
    - _Inputs: FormMail.cs, design.md (Background Sync Timer section)_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs_
    - _Verification: getDiagnostics on FormMail.cs_


  - [~] 8.2 Implement sync status display and timer lifecycle
    - Display "Syncing..." status label while sync is in progress
    - Display sync result on completion ("Synced: N new messages" or "Sync complete: up to date")
    - Display last successful sync timestamp ("Last sync: yyyy-MM-dd HH:mm" or "Never")
    - Display error status on sync failure ("Sync failed: auth error", "Sync failed: rate limited", "Sync failed: network error")
    - Apply new sync interval on next tick when preference changes (no form restart needed)
    - Stop and dispose timer on FormClosed
    - _Satisfies: Req 3, Criterion 3.10; Req 7, Criteria 7.2, 7.3; Req 8, Criteria 8.5, 8.6_
    - _Inputs: FormMail.cs, PreferencesStore.cs_
    - _Output: OE2EmpireTracker/Forms/Mail/FormMail.cs_
    - _Verification: getDiagnostics on FormMail.cs_

- [ ] 9. Wire FormMail into main menu
  - [~] 9.1 Add Mail menu item to MainWindow
    - Add "Mail" menu item to the existing main menu in MainWindow
    - Open FormMail as MDI child when clicked (single instance pattern matching existing forms)
    - _Satisfies: Req 4, Criterion 4.1_
    - _Inputs: MainWindow.cs, design.md (Modified Files section)_
    - _Output: OE2EmpireTracker/Forms/MainWindow.cs (or MainWindow.Designer.cs)_
    - _Verification: getDiagnostics on MainWindow files_

- [~] 10. Checkpoint - Ensure full solution compiles and all tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 11. Write remaining property and unit tests
  - [~] 11.1 Write property test for sort stability
    - **Property 6: Sort Stability**
    - Generate messages with null/empty SentTime mixed with valid SentTime
    - Assert null/empty SentTime sorts to the end (oldest position)
    - Assert valid SentTime messages sort by parsed datetime descending
    - **Validates: Req 4, Criterion 4.4; Req 8, Criterion 8.4**
    - _Inputs: MailService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes Property 6 test_

  - [~] 11.2 Write unit tests for MailService filter logic
    - Test read-status filter: All shows everything, Unread shows LocalRead=false, Read shows LocalRead=true
    - Test type filter mapping: "Player/System" → null, "Colony" → "C", "Research" → "R", "Skill" → "S"
    - Test search: case-insensitive substring match on FromName, Subject, MailContent (OR logic)
    - Test AND combination of all three filters
    - _Satisfies: Req 5, Criteria 5.3, 5.4, 5.6_
    - _Inputs: MailService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes filter unit tests_

  - [~] 11.3 Write unit tests for MailService sync and error handling
    - Test mark-as-read sets LocalRead=true without touching MailRead
    - Test GetHighWaterMark returns max MailId (or 0 when empty)
    - Test GetUnreadCount returns count of LocalRead=false messages
    - _Satisfies: Req 6, Criteria 6.1, 6.3; Req 3, Criterion 3.3_
    - _Inputs: MailService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MailServiceTests.cs_
    - _Verification: vstest.console passes sync unit tests_

- [~] 12. Final checkpoint - Ensure all tests pass and solution builds clean
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The design uses C# with .NET Framework 4.8.1, Newtonsoft.Json, NLog, and FsCheck 2.16.6 for property tests
- FormMail follows the same MDI child pattern as existing forms (IProgrammaticUpdateSource, ProgrammaticUpdateGuard, event subscription/unsubscription)


## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["2.2"] },
    { "id": 3, "tasks": ["2.3"] },
    { "id": 4, "tasks": ["2.4", "3.1", "6.1"] },
    { "id": 5, "tasks": ["3.2", "3.3", "3.4"] },
    { "id": 6, "tasks": ["4.1"] },
    { "id": 7, "tasks": ["4.2", "4.3", "4.4"] },
    { "id": 8, "tasks": ["7.1"] },
    { "id": 9, "tasks": ["7.2"] },
    { "id": 10, "tasks": ["7.3", "7.4", "7.5"] },
    { "id": 11, "tasks": ["8.1"] },
    { "id": 12, "tasks": ["8.2", "9.1"] },
    { "id": 13, "tasks": ["11.1", "11.2", "11.3"] }
  ]
}
```
