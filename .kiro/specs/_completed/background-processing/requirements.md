# Requirements Document

## Introduction

Background Processing adds a background thread to the OE2EmpireTracker application that automatically processes colony timers without requiring the Colony Form to be open. The thread periodically scans all colonies for the current player, identifies expired timers, runs Colony.ProcessColony() for those colonies, notifies open forms via events, and persists changes only when processing actually occurs. This replaces the need for manual intervention on each colony while keeping the existing manual Start/Done buttons intact.

## Glossary

- **Background_Processor**: The component that runs on a dedicated non-UI thread, periodically checking for and processing expired colony timers.
- **Colony_Activity_Collector**: The existing ColonyActivityCollector class that scans all colonies and returns ActivityRow instances with countdown information.
- **Colony**: A player-owned colony containing structures with active timers (mining, refining, research, manufacturing, building).
- **Expired_Timer**: A CountDownTime on a ColonyStructure where TimeRemaining has reached 0 or IntervalsPassed is greater than 0, indicating work is ready to be processed.
- **Player_Context**: The PlayerContext singleton that holds all player data, fires data-change events, and handles persistence via writeContext().
- **Processing_Cycle**: A single pass of the Background_Processor where all colonies are scanned and those with expired timers are processed.
- **Tick_Interval**: The time between Processing_Cycles, set to 60 seconds.

## Requirements

### Requirement 1: Background Thread Lifecycle

**User Story:** As a player, I want colony processing to happen automatically in the background, so that I do not need to keep the Colony Form open on each colony for timers to complete.

#### Acceptance Criteria

1. WHEN the application starts and a current player is selected, THE Background_Processor SHALL start a background thread that executes Processing_Cycles at the configured Tick_Interval of 60 seconds.
2. WHEN the current player changes, THE Background_Processor SHALL continue running without interruption — it processes all players' colonies regardless of which player is currently selected.
3. WHEN the application is shutting down, THE Background_Processor SHALL stop the background thread and wait for any in-progress Processing_Cycle to complete before allowing shutdown to proceed.
4. THE Background_Processor SHALL run on a non-UI thread so that the WinForms message loop is never blocked by colony processing.
5. IF an unhandled exception occurs during a Processing_Cycle, THEN THE Background_Processor SHALL log the exception at Error level and continue running subsequent Processing_Cycles.

### Requirement 2: Colony Scanning and Filtering

**User Story:** As a player, I want only colonies with expired timers to be processed each cycle, so that unnecessary work and persistence writes are avoided.

#### Acceptance Criteria

1. WHEN a Processing_Cycle begins, THE Background_Processor SHALL scan all colonies across all players (the full PlayerContext.colonyList), not just the current player's colonies.
2. THE Background_Processor SHALL identify colonies that have at least one Expired_Timer by checking each colony's structures for expired BuildCompletionTime or ProcessCompletionTime.
3. WHEN no colonies have expired timers, THE Background_Processor SHALL skip processing and persistence for that Processing_Cycle.
4. THE Background_Processor SHALL process only the distinct set of colonies that have at least one Expired_Timer, not all colonies.

### Requirement 3: Colony Processing Execution

**User Story:** As a player, I want each colony with expired timers to be fully processed (mining, refining, manufacturing, research, building), so that resources and items are updated automatically.

#### Acceptance Criteria

1. WHEN a colony has at least one Expired_Timer, THE Background_Processor SHALL call Colony.ProcessColony() on that colony.
2. THE Background_Processor SHALL process colonies one at a time in sequence, not concurrently, to avoid data contention on shared PlayerContext state.
3. WHEN Colony.ProcessColony() completes for a colony, THE Background_Processor SHALL fire Player_Context.OnColonyDataChanged() with the processed colony's UUID so that open forms refresh their display.

### Requirement 4: Cross-Thread UI Safety

**User Story:** As a player, I want open forms to update correctly when background processing fires data-change events, so that the UI reflects the latest colony state without crashes.

#### Acceptance Criteria

1. THE Background_Processor SHALL fire Player_Context.OnColonyDataChanged() from the background thread after processing each colony.
2. WHEN a form receives a ColonyDataChanged event on a non-UI thread, THE form event handler SHALL use Control.Invoke to marshal UI updates back to the UI thread.
3. WHEN a form has been disposed before the event fires, THE form event handler SHALL check IsDisposed and return without updating the UI.

### Requirement 5: Conditional Persistence

**User Story:** As a player, I want data to be saved to disk only when background processing actually changes something, so that unnecessary file writes are avoided.

#### Acceptance Criteria

1. WHEN at least one colony was processed during a Processing_Cycle, THE Background_Processor SHALL call Player_Context.writeContext() once after all colonies in that cycle have been processed.
2. WHEN no colonies were processed during a Processing_Cycle, THE Background_Processor SHALL NOT call Player_Context.writeContext().
3. THE Background_Processor SHALL call Player_Context.writeContext() at most once per Processing_Cycle, not once per colony.

### Requirement 6: Coexistence with Manual Controls

**User Story:** As a player, I want the existing manual Start/Done buttons on ColonyStructure controls to continue working alongside background processing, so that I can still manually trigger processing when needed.

#### Acceptance Criteria

1. THE Background_Processor SHALL NOT interfere with or disable the existing Start and Done buttons on ColonyStructure controls.
2. WHEN a user manually clicks Done on a ColonyStructure control while the Background_Processor is between Processing_Cycles, THE manual processing SHALL execute as it does today.
3. IF the Background_Processor and a manual Done click attempt to process the same colony simultaneously, THEN THE Background_Processor SHALL use a per-colony lock to ensure only one processing path executes Colony.ProcessColony() at a time.

### Requirement 7: Logging

**User Story:** As a developer, I want background processing activity to be logged, so that I can diagnose issues and verify correct operation.

#### Acceptance Criteria

1. WHEN the Background_Processor starts, THE Background_Processor SHALL log an Info-level message indicating the thread has started and the Tick_Interval.
2. WHEN a Processing_Cycle processes one or more colonies, THE Background_Processor SHALL log an Info-level message listing the count of colonies processed.
3. WHEN the Background_Processor stops, THE Background_Processor SHALL log an Info-level message indicating the thread has stopped.
4. IF an exception occurs during colony processing, THEN THE Background_Processor SHALL log the exception details at Error level including the colony name and UUID.

### Requirement 8: Next Process Countdown Display

**User Story:** As a player, I want to see when the next background processing cycle will run, so that I know when my colony timers will be checked.

#### Acceptance Criteria

1. THE MainWindow SHALL display a label to the left of the player selection dropdown showing "Next Process: " followed by a countdown in the standard "Xd Yh Zm Ws" format.
2. THE MainWindow SHALL update the countdown label once per second using a System.Windows.Forms.Timer.
3. WHEN a Processing_Cycle completes, THE countdown SHALL reset to the Tick_Interval (60 seconds).
4. WHEN the Background_Processor is not running (no player selected), THE label SHALL display "Next Process: --".
5. WHEN an exception occurs during a Processing_Cycle, THE MainWindow SHALL set the Next Process label's foreground color to red.
6. WHEN the next Processing_Cycle completes without an exception, THE MainWindow SHALL reset the Next Process label's foreground color to the default.
