# BL-124 Design: Read-Only Display Forms

## Overview

BL-124 ensures that read-only display forms consistently use ReadOnly wrapper types. No new services, ViewModels, or DTOs are needed.

## Architecture

These forms are consumers of data, not mutators. They read from PlayerContext and display results. The migration ensures they use ReadOnly wrapper types consistently.

### FormColonyActivity

Uses ColonyActivityCollector to gather activity data from colonies. Migration ensures all colony data access uses ReadOnly types.

### FormColonyDailyBuild

Uses delivery routes and colony data to compute daily build schedules. Migration ensures route selection uses ReadOnlyDeliveryRoute and colony data uses ReadOnlyColony.

### FormAutoFill

Already clean --- only accesses PreferencesStore. No entity data access.

## Components

No new components. Only modifications to existing forms to use ReadOnly types consistently.

## Testing Strategy

Test file: OE2EmpireTracker.Tests/Services/ReadOnlyDisplayFormVerificationTests.cs
