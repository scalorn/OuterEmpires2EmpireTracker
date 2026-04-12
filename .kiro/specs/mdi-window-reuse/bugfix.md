# Bugfix Requirements Document

## Introduction

BL-067: MDI child windows always receive the next highest window number instead of reusing gaps left by closed windows. When a user opens windows #1, #2, #3, closes #2, then opens a new window, it gets #4 instead of reusing #2. This causes window numbers to grow unboundedly and breaks the user's expectation that saved window state (position, size, filters) associated with a number will be reused when a gap is filled.

The root cause is `MainWindow._windowNumberCounters`, a simple incrementing counter per form type that never checks for gaps from closed windows.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a user closes an MDI child window and then opens a new window of the same form type THEN the system assigns the next highest number (counter + 1) instead of reusing the closed window's number

1.2 WHEN multiple MDI child windows have been closed leaving gaps in the numbering sequence THEN the system ignores all gaps and continues incrementing from the highest counter value

1.3 WHEN the first window of a form type is opened after previous windows of that type were all closed THEN the system continues from the last counter value rather than restarting from 1

### Expected Behavior (Correct)

2.1 WHEN a user opens a new MDI child window of a given form type THEN the system SHALL assign the lowest positive integer not currently in use by any open window of that same form type

2.2 WHEN multiple gaps exist in the numbering sequence of open windows THEN the system SHALL fill the lowest gap first (e.g., if #1 and #3 are open, the next window SHALL be #2)

2.3 WHEN all windows of a form type have been closed and a new one is opened THEN the system SHALL assign window number 1

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a new MDI child window is opened THEN the system SHALL CONTINUE TO set the window's `Tag` property to the assigned window number

3.2 WHEN a new MDI child window is opened THEN the system SHALL CONTINUE TO format the title bar as `"#N - FormTitle"` where N is the assigned window number

3.3 WHEN a new MDI child window is opened THEN the system SHALL CONTINUE TO call `WindowStateHelper.RestoreState` with the assigned window number, restoring any previously saved position, size, and control state for that number

3.4 WHEN multiple windows of the same form type are open simultaneously THEN the system SHALL CONTINUE TO assign each a unique window number

3.5 WHEN windows of different form types are open THEN the system SHALL CONTINUE TO number each form type independently (e.g., Blueprint #1 and Colony #1 can coexist)
