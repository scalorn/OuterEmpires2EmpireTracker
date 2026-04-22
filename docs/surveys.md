# Surveys

The Survey form lets you import and view planet resource surveys. Surveys tell you what resources are available on a planet, their purity levels, and abundance — essential information for planning colony placement and mining operations.

## Opening the Survey Form

Open **Forms → Surveys** from the menu bar. The form has two main areas:

- **Left panel** — A list of all surveys for the current player, plus a resource filter.
- **Right panel** — Detailed survey information and a resource grid.

## Importing a Survey

To import survey data from the game:

1. In the game browser, navigate to the survey results page for a planet.
2. Select and copy the survey HTML (Ctrl+C).
3. In the Survey form, click **Import**.
4. The parser extracts the planet name, system, survey ID, scanner information, and all resource data.
5. If a survey with the same planet name and survey ID already exists, it will be updated (merged). Otherwise a new survey is created.
6. The imported survey is automatically selected in the list.

### Deduplication

The import process uses the combination of planet name and survey ID to detect duplicates. When a duplicate is found, the existing survey is updated with the new data rather than creating a duplicate entry.

## Survey Details

The right panel shows the following fields:

| Field | Description |
|-------|-------------|
| Planet Name | The planet that was surveyed |
| System Name | The star system containing the planet |
| Survey ID | The unique identifier for this survey |
| Nick Name | An optional friendly name for the survey |
| Scanned By | The player who conducted the survey |
| Scan Date/Time | When the survey was performed, shown in the game's format (e.g. `27JUL24-11:44p`). Use the calendar picker next to the field to change the date — the text box is read-only. |
| Sensor Abundance | The sensor's abundance reading |
| Purity Modifier | The purity modifier applied to the scan |
| Scan Level | The level of the scan |
| Scanner Blueprint | The blueprint of the scanner used |

## Resource Grid

The resource grid at the bottom of the form shows all resources found in the survey:

| Column | Description |
|--------|-------------|
| Resource | The resource type (e.g., Iron, Copper, Titanium) |
| Purity | The purity level of the resource deposit |
| Amount | The estimated quantity available |
| Max Reserve | For asteroid surveys, the maximum reserve amount for each resource. Empty for planet surveys. |

You can manually add or edit resource rows if needed.

## Managing Surveys

- **New** — Clear the form to create a new survey manually.
- **Save** — Save the current survey data.
- **Delete** — Remove the selected survey after confirmation.

## Default Surveys

When you import a colony with active miners but haven't yet imported a real survey for that planet, the tracker creates a temporary "default survey" so miners can still function. Default surveys are identified by a Survey ID of "DEFAULT" and are created automatically — you don't need to do anything.

When you later import a real survey for the same planet, miners will automatically upgrade to the real survey on the next colony reimport. Default surveys are cleaned up automatically: resources that are no longer being mined are removed, and if no resources remain, the default survey is deleted entirely.

## Sorting by Date

Click the DateTime column header in the survey list to sort surveys chronologically. The list sorts by the actual date, not the display text, so surveys from different months and years sort correctly.

## Filtering Surveys

Use the text filter at the top of the left panel to search surveys by planet name or other fields.

Additional filters below the text filter:
- **Type** — Filter by Planet, Asteroid, or All (default: All)
- **Purity** — Filter to surveys containing a specific purity level (Low, Medium, High, etc.)
- **Min** — Minimum amount per hour (planet) or per cycle (asteroid). Only surveys with at least one resource meeting this threshold are shown.

## Asteroid Surveys

Asteroid surveys are auto-detected when you import them — the parser recognizes the "/cycle" rate format and MaxReserve fields. When imported, the tracker extracts each resource's max reserve value and stores it on the linked asteroid entity. The Max Reserve column in the resource grid shows these values so you can see how much of each resource the asteroid holds. Asteroid surveys are stored alongside planet surveys with a SurveyType of Asteroid. Use the Type filter to view only asteroid or planet surveys.

## Related Topics

- [Colonies](colonies.md) — Use survey data to decide where to place colonies
- [Blueprints](blueprints.md) — Scanner blueprints determine survey quality
- [Getting Started](getting-started.md) — Overview of the application workflow
