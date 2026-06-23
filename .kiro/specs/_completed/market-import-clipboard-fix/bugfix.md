# Bugfix Requirements Document

## Introduction

When the user clicks "Import Market" on the Blueprint form, the app reads HTML from the clipboard (copied from the Outer Empires 2 game's market page in a browser), extracts the HTML fragment using byte offsets from the clipboard header, and passes it to the SGML parser to find MarketListingRow elements. The parser returns zero results even though the clipboard data contains MarketListingRow elements with blueprint data (e.g. "Habitation Block Flatpack", "Entertainment Centre Flatpack", "Manufactory Flatpack", etc.).

The `ExtractHtmlFragmentFromClipboardData` method uses byte offsets from the clipboard header (StartFragment/EndFragment) to extract the HTML fragment. The clipboard data from the browser includes the full game UI HTML (top bar, hex menus, filter dropdowns) before the actual market listing table. The resulting HTML fragment is ~384KB with massive inline styles on every element. The SGML parser either fails to build a proper DOM tree from this HTML, or the fragment extraction produces subtly malformed HTML that prevents correct parsing. The existing test data files (MarketSampleHulls.html, etc.) work correctly because they are passed directly to `ProcessMarketHtml` without going through `ExtractHtmlFragmentFromClipboardData`, and/or because they contain smaller, cleaner HTML.

The real clipboard data is saved at `OE2EmpireTracker.Tests/TestData/BUGHUNT.html`.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN clipboard data contains full game UI HTML (~384KB) with MarketListingRow elements deep in the DOM AND the data is processed through `ExtractHtmlFragmentFromClipboardData` then `ProcessMarketHtml` THEN the system returns zero blueprint results despite valid MarketListingRow elements being present in the HTML

1.2 WHEN the extracted HTML fragment is extremely large with massive inline styles on every element THEN the SGML parser fails to construct a correct DOM tree, causing XPath queries for `//tr` elements with class `MarketListingRow` to return no matches

### Expected Behavior (Correct)

2.1 WHEN clipboard data contains full game UI HTML with MarketListingRow elements AND the data is processed through `ExtractHtmlFragmentFromClipboardData` then `ProcessMarketHtml` THEN the system SHALL extract and return all blueprint listings (names, evolution, properties, resources) from the MarketListingRow elements

2.2 WHEN the clipboard HTML fragment is large with inline styles THEN the system SHALL successfully parse the HTML and find all MarketListingRow and MarketListingRowDetail elements regardless of document size or style complexity

### Unchanged Behavior (Regression Prevention)

3.1 WHEN clipboard data contains smaller market HTML fragments (like the existing MarketSample*.html test files) THEN the system SHALL CONTINUE TO correctly parse and extract all blueprint listings with their properties and resources

3.2 WHEN clipboard data contains non-market HTML or no HTML THEN the system SHALL CONTINUE TO return an empty result list or appropriate error message

3.3 WHEN the clipboard header contains valid StartFragment/EndFragment byte offsets THEN `ExtractHtmlFragmentFromClipboardData` SHALL CONTINUE TO correctly extract the HTML fragment for non-market clipboard data (colony parsing, survey parsing)
