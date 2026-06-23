# Requirements: Align Form Management Pattern

## Overview
Align Colony, Blueprint, Survey, and PlayerProfile forms to follow the same management pattern used by the Delivery Routes form: New/Save/Delete buttons, no Cancel button, Save does not clear the form.

## User Stories

### US-1: Consistent Button Layout
As a user, I want all management forms to have the same New/Save/Delete button pattern so the UI is predictable across the application.

**Acceptance Criteria:**
- AC-1.1: Each form has New, Save, and Delete buttons
- AC-1.2: Cancel button is removed from all forms
- AC-1.3: Button order is consistent: New → Save → Delete

### US-2: Save Preserves Selection
As a user, I want Save to keep the current record displayed so I can continue editing after saving.

**Acceptance Criteria:**
- AC-2.1: Clicking Save persists the record and refreshes the list view
- AC-2.2: The form fields remain populated with the saved record's data
- AC-2.3: The list view selection is maintained

### US-3: New Clears Form
As a user, I want a New button that clears the form for creating a new record.

**Acceptance Criteria:**
- AC-3.1: Clicking New clears all form fields
- AC-3.2: Clicking New deselects the list view selection
- AC-3.3: A fresh empty record is ready for data entry

### US-4: Delete Confirms
As a user, I want Delete to ask for confirmation before removing a record.

**Acceptance Criteria:**
- AC-4.1: Clicking Delete shows a Yes/No confirmation dialog
- AC-4.2: Confirming Yes removes the record and clears the form
- AC-4.3: Confirming No cancels the deletion and preserves the form state
