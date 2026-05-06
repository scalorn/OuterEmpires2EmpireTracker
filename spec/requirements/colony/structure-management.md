# Colony Structure Management

## Structure Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Staged : Flatpack delivered
    Staged --> Building : Build started (timer)
    Building --> Built : Timer expires (86400s base)
    Built --> Online : Set online

    state "Worker Assignment" as WA {
        [*] --> Unassigned
        Unassigned --> Assigned : Assign worker
        Assigned --> Unassigned : Unassign worker
    }

    note right of Online : Resources only counted\nwhen Online
    note right of Staged : Background color: Yellow
    note right of Built : Background color: PaleVioletRed (offline)
    note right of Online : Background color: Green
```

**REQ-COL-030** The colony form SHALL display a list of all saved colonies filtered by a name search field.  
**REQ-COL-031** Selecting a colony from the list SHALL populate the form with that colony's data.  
**REQ-COL-032** The user SHALL be able to add a flatpack structure to the colony by selecting from a filtered list of Flatpack blueprints and clicking Add.  
**REQ-COL-033** Each structure in the colony SHALL be displayed as a ColonyStructure control showing its blueprint name, sequence number, and Actual/Ideal resource status. The sequence number (displaySequence) is a per-blueprint-type counter matching the game UI convention — structures of the same type are numbered independently (e.g. two Power Plants are #1 and #2; a Habitation is also #1).  
**REQ-COL-034** The user SHALL be able to reorder structures using Up and Down buttons; the display order SHALL update immediately.  
**REQ-COL-035** The user SHALL be able to delete a structure using a Delete button; the structure SHALL be removed from the colony and the display SHALL update immediately.  
**REQ-COL-036** Pressing the Delete key when a structure control is focused SHALL delete that structure.  
**REQ-COL-037** After any structure change (add, reorder, delete, worker assignment, state change), CalculateBuilt() and CalculateIdeal() SHALL be called and all structure controls SHALL be refreshed.  
**REQ-COL-037a** The status display SHALL color each resource label red when Required > Provided (or Capacity for Warehouse), and green otherwise. This applies to all five resources: Power, Habitation, Food, Entertainment, and Warehouse.  
**REQ-COL-038** The structure background color SHALL indicate state: Yellow=Staged, PaleVioletRed=Built but Offline, LightGreen=Online with missing workers, Green=Online with all workers.  
**REQ-COL-038a** The Structures tab SHALL display a structure type filter (lvwStructureTypes) as a checkbox ListView showing all flatpack blueprint types from BaselineData.json, sorted alphabetically by name. All types SHALL be checked by default. Unchecking a type SHALL hide all structure controls of that type. The list is static (all flatpack types, not just those in the current colony) so that unchecked selections persist reliably across colony switches and application restarts via UIPreferences.json.

**REQ-COL-040** Each structure SHALL have Built, Staged, and Online boolean state stored in its Properties PropertyBag.  
**REQ-COL-041** Setting Online=true SHALL also set Built=true and Staged=false.  
**REQ-COL-042** Setting Built=true SHALL also set Staged=false.  
**REQ-COL-043** Setting Staged=true SHALL also set Built=false and Online=false.  
**REQ-COL-044** Worker assignment checkboxes SHALL reflect the current AssignedWorkers state when the control is loaded.  
**REQ-COL-045** Changing a worker assignment checkbox SHALL immediately update the AssignedWorkers PropertyBag and trigger recalculation.
