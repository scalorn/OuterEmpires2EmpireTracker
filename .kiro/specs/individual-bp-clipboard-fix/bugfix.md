# Bugfix Requirements Document

## Introduction

When the user clicks "Import Clipboard" on the Blueprint form to import an individual blueprint from the game browser, the app reads HTML from the clipboard and passes it to `ProcessHtml` to extract blueprint properties and resources. The import silently does nothing because `processClipboard` passes the raw clipboard data (including the `Version:0.9` / `StartHTML` / `EndHTML` header) directly to `ProcessHtml` instead of first extracting the HTML fragment.

The market import (`cmdImportMarket_Click`) correctly calls `ExtractHtmlFragmentFromClipboardData` before parsing, but the individual import (`cmdImport_Click` → `processClipboard`) had this extraction commented out.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the user copies a blueprint detail page from the game browser (Select All + Copy) and clicks Import Clipboard THEN `processClipboard` passes the raw clipboard text (including `Version:0.9\r\nStartHTML:...` header) to `ProcessHtml` instead of extracting the HTML fragment first

1.2 WHEN raw clipboard data with header is passed to the SGML parser THEN parsing may silently fail or produce incorrect results depending on the clipboard content structure

### Expected Behavior (Correct)

2.1 WHEN the user copies a blueprint detail page and clicks Import Clipboard THEN `processClipboard` SHALL extract the HTML fragment using `ExtractHtmlFragmentFromClipboardData` before passing it to `ProcessHtml`

2.2 WHEN the extracted HTML fragment contains `ShipComponentProperty` divs (Statistics tab) THEN `ProcessHtml` SHALL extract all blueprint properties including name, tech level, evolution, description, and class

2.3 WHEN the extracted HTML fragment contains `ScanDetailOutputResourceName` divs (Resources tab) THEN `ProcessHtml` SHALL extract all resource names and quantities

### Unchanged Behavior (Regression Prevention)

3.1 WHEN clipboard data contains market HTML THEN the market import flow SHALL CONTINUE TO work correctly via its own `ExtractHtmlFragmentFromClipboardData` call

3.2 WHEN `ProcessHtml` is called directly with clean HTML (as in existing tests) THEN it SHALL CONTINUE TO parse correctly
