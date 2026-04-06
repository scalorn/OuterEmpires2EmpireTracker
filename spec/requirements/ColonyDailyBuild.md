# Colony Daily Build Form Requirements

## Overview

**REQ-CDB-001** The Colony Daily Build form SHALL display all colonies eligible for building and allow the user to initiate builds.

## Eligibility

**REQ-CDB-010** A colony is eligible when it has at least one staged structure and no currently building structure (see REQ-GM-070 through REQ-GM-073).
**REQ-CDB-011** The form SHALL refresh the eligible colony list when ColonyDataChanged fires.

## Display

**REQ-CDB-020** The form SHALL show a list of eligible colonies with colony name, planet name, and system name.
**REQ-CDB-021** Selecting a colony SHALL display the first staged structure's details.
**REQ-CDB-022** A "Build" button SHALL initiate building on the first staged structure of the selected colony.

## Build Action

**REQ-CDB-030** Building SHALL set the structure's Staged=false, start a BuildCompletionTime timer using the calculated build time (REQ-GM-060/061).
**REQ-CDB-031** After initiating a build, the form SHALL refresh to remove the colony from the eligible list (since it now has a building structure).
**REQ-CDB-032** The form SHALL fire ColonyDataChanged after initiating a build.

## Data Events

**REQ-CDB-040** The form SHALL subscribe to CurrentPlayerChanged and ColonyDataChanged events.
**REQ-CDB-041** The form SHALL unsubscribe from events in OnFormClosed.
