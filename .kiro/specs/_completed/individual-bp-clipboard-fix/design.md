# Design Document

## Overview

Fix the individual blueprint clipboard import to work with full-page "Select All + Copy" from the game browser. Three issues were addressed:

1. **Fragment extraction**: `processClipboard` was passing raw clipboard data (including the `Version:0.9` header) to `ProcessHtml`. Fixed by uncommenting the `ExtractHtmlFragmentFromClipboardData` call.
2. **UUID assignment**: `PopulateForm()` returned early when the blueprint had no UUID (e.g. after clicking New). Fixed by assigning a UUID in `cmdImport_Click` if one doesn't exist.
3. **BlueprintType resolution**: `ProcessHtml` had commented-out icon resolution code that used an old image-filename approach. Replaced with icon position extraction from the `ui_icon_base` div's inline `background` style, resolved via `FindBlueprintTypeByIcon()` with `ReclassifyByName` fallback — the same pipeline used by `ProcessMarketHtml`.

## Changes

### BlueprintScanner.processClipboard (BlueprintScanner.cs)

The commented-out line `//string html = ExtractHtmlFragmentFromClipboardData(returnHtmlText);` was uncommented, and `ProcessHtml` now receives the extracted fragment instead of the raw clipboard text.

### BlueprintScanner.ProcessHtml (BlueprintScanner.cs)

Added icon-based BlueprintType resolution: extracts the sprite position from the `ui_icon_base` div's inline `background: url(...) -Xpx -Ypx` style, looks up the BlueprintType via `EmpireContext.FindBlueprintTypeByIcon()`, and applies `ReclassifyByName` for shared-icon types (e.g. OreHopper). Replaces the old commented-out image-filename-based approach.

### FormBlueprint.cmdImport_Click (FormBlueprint.cs)

Added a clipboard check with user feedback ("No HTML found on clipboard") and UUID assignment so `PopulateForm()` doesn't bail out on new/blank blueprints.

## Test Coverage

Test file: `IndividualBlueprintImportTests.cs` with 11 tests using real full-page clipboard captures (`IndivudalBPWSMS-LL9Stats.html` and `IndivudalBPWSMS-LL9Resources.html`).

Tests verify extraction + parsing of: name, tech level, evolution, description, properties, class, BlueprintType (JumpDrive from icon), resources, and specific resource values.
