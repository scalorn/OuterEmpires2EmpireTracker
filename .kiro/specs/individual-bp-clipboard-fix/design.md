# Design Document

## Overview

Fix the individual blueprint clipboard import by uncommenting the `ExtractHtmlFragmentFromClipboardData` call in `BlueprintScanner.processClipboard`, aligning it with the market import flow.

## Changes

### BlueprintScanner.processClipboard (BlueprintScanner.cs)

The commented-out line `//string html = ExtractHtmlFragmentFromClipboardData(returnHtmlText);` was uncommented, and `ProcessHtml` now receives the extracted fragment instead of the raw clipboard text.

This matches the pattern used by `cmdImportMarket_Click` in FormBlueprint.cs, which already calls `ExtractHtmlFragmentFromClipboardData` before parsing.

## Test Coverage

New test file: `IndividualBlueprintImportTests.cs` with 10 tests using real full-page clipboard captures (`IndivudalBPWSMS-LL9Stats.html` and `IndivudalBPWSMS-LL9Resources.html`).

Tests verify extraction + parsing of: name, tech level, evolution, description, properties, class, resources, and specific resource values.
