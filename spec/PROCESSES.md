# Repeatable Processes

## Icon Position Refresh

When the game updates its sprite sheet or you capture new MarketSample HTML files for previously uncovered BlueprintTypes:

### Prerequisites
1. Capture the market page HTML from the in-game browser
2. Save it as `MarketSample{TypeName}.html` in `OE2EmpireTracker.Tests/TestData/`

### Ask Kiro
> "I've added new MarketSample HTML files to TestData. Run the icon position refresh — add the csproj entries, run the extractor, run all tests, update the backlog, and commit."

### What happens
1. Any new MarketSample HTML files get `<Content Include>` entries added to `OE2EmpireTracker.Tests.csproj`
2. Solution is built
3. The `ExtractAndUpdateIconPositions` test runs — it automatically:
   - Picks up all `MarketSample*.html` files via glob
   - Extracts icon positions from each parsed blueprint
   - Compares against BaselineData.json and updates changed positions
   - Adds new BlueprintType entries for unknown icons
   - Ensures all 14 CommodityFactory per-industry entries exist
   - Writes updated BaselineData.json to both main and test directories
   - Produces a coverage gap report
4. Full test suite runs to verify nothing broke
5. Resolved coverage gaps are removed from `spec/BACKLOG.md`, new gaps are added
6. Everything is committed

### Manual alternative
```
"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug

"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Tests:ExtractAndUpdateIconPositions

"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll
```

### Notes
- The extractor is idempotent — safe to run as many times as needed
- Coverage gaps are tracked in `spec/BACKLOG.md` under "MarketSample Coverage Gaps"
- The extractor writes to source-tree paths, not output directory paths
