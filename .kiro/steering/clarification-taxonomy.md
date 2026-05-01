---
inclusion: manual
---
# Feature Clarification Taxonomy

When starting a new feature, systematically check each category below for ambiguities before writing requirements. For each category, mark status: Clear / Partial / Missing. Address Partial and Missing items before proceeding to design.

## Categories

### 1. Functional Scope & Behavior
- What are the core user goals?
- What is explicitly out of scope?
- Are there distinct user roles or personas?
- What does success look like for the user?

### 2. Domain & Data Model
- What entities are involved? What are their attributes?
- What are the identity/uniqueness rules (UUIDs, natural keys)?
- What lifecycle/state transitions exist?
- What are the data volume assumptions?

### 3. Interaction & UX Flow
- What are the critical user journeys (step by step)?
- What happens on error, empty state, loading?
- Are there keyboard shortcuts or accessibility concerns?
- What existing forms/controls does this interact with?

### 4. Non-Functional Quality
- Performance targets (acceptable latency for operations)?
- Data scale (how many items in lists, grids)?
- Reliability (what happens if the app crashes mid-operation)?
- Observability (what should be logged at Info/Debug/Warn)?

### 5. Integration & Dependencies
- What existing services/contexts does this touch?
- What events does it fire or subscribe to?
- What data does it read from PlayerContext/EmpireContext?
- Does it affect background processing?

### 6. Edge Cases & Failure Handling
- What happens with empty/null/missing data?
- What happens with duplicate entries?
- What happens during concurrent access (background processor vs UI)?
- What are the boundary conditions (zero items, max items)?

### 7. Constraints & Tradeoffs
- Are there game-mechanic constraints that limit design choices?
- Are there performance vs. correctness tradeoffs?
- Are there UI space constraints (form size, grid columns)?

### 8. Terminology & Consistency
- Are we using the same terms as the game?
- Are there synonyms we should standardize on?
- Do any terms conflict with existing code/spec usage?

## Usage

Reference this file (#clarification-taxonomy) when starting a new feature spec. Work through each category and document findings in the requirements or in spec/Ambiguities.md with AMB-xxx IDs for unresolved items.
