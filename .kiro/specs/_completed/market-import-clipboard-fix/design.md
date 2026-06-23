# Design Document: Market Import Clipboard Fix

## Root Cause

`ExtractHtmlFragmentFromClipboardData` in `BlueprintScanner.cs` extracts the HTML fragment using byte offsets from the clipboard header (`StartFragment`/`EndFragment`). It converts the entire .NET string to UTF-8 bytes, then extracts a substring using those byte offsets. However, `Clipboard.GetText(TextDataFormat.Html)` returns a .NET `string` (UTF-16 internally) that was decoded from the clipboard's UTF-8 bytes. The byte offsets in the header refer to positions in the original UTF-8 byte stream, but after the round-trip through `string` → `Encoding.UTF8.GetBytes` → `Encoding.UTF8.GetString`, the offsets can land in the wrong position — specifically, they can truncate the HTML mid-tag.

Diagnostic test confirmed: the extracted fragment throws `SgmlParseException: Unexpected EOF parsing start tag 'div'` because the `EndFragment` offset cuts the HTML in the middle of a `<div>` element. The `ProcessMarketHtml` catch block silently swallows this exception and returns an empty list.

## Fix

Replace the byte-offset extraction with marker-based extraction. The HTML clipboard format always wraps the fragment with `<!--StartFragment-->` and `<!--EndFragment-->` comment markers. These are string-searchable and encoding-safe.

### Changed Method: `ExtractHtmlFragmentFromClipboardData`

**Before:**
```csharp
byte[] bytes = Encoding.UTF8.GetBytes(htmlDataString);
return Encoding.UTF8.GetString(bytes, startFragmentIndex, endFragmentIndex - startFragmentIndex);
```

**After:**
```csharp
const string startMarker = "<!--StartFragment-->";
const string endMarker = "<!--EndFragment-->";
int startPos = htmlDataString.IndexOf(startMarker);
if (startPos < 0) return "ERROR: Missing StartFragment marker";
startPos += startMarker.Length;
int endPos = htmlDataString.IndexOf(endMarker, startPos);
if (endPos < 0) return "ERROR: Missing EndFragment marker";
return htmlDataString.Substring(startPos, endPos - startPos);
```

Falls back to byte-offset extraction if markers are not found (defensive, for edge cases with non-standard clipboard sources).

### Impact

This method is shared by all three clipboard import paths:
- `FormBlueprint.cmdImportMarket_Click` → market blueprint import
- `ColonyParser.processClipboard` → colony HTML import  
- `SurveyParser.processClipboard` → survey HTML import

All three benefit from the fix. The colony and survey parsers haven't hit the bug yet because their clipboard data is typically smaller, but the latent bug exists.

## Testing

### Test: BUGHUNT.html end-to-end
- Load `BUGHUNT.html`, extract fragment, parse with `ProcessMarketHtml`
- Assert results count > 0
- Assert known blueprint names present (Habitation Block Flatpack, Manufactory Flatpack, etc.)

### Test: Existing MarketSample files still work
- Run all existing `MarketBlueprintImporterTests` — must continue passing

### Test: Fragment extraction with markers
- Construct a clipboard string with `<!--StartFragment-->` and `<!--EndFragment-->` markers
- Assert extraction returns content between markers

### Test: Fragment extraction fallback
- Construct a clipboard string WITHOUT markers but WITH byte offsets
- Assert extraction still works via fallback
