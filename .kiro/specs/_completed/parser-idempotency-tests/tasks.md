# Implementation Plan: Parser Idempotency Tests

## Overview

Add idempotency verification tests for `SurveyParser` and `ColonyParser` to confirm that parsing the same HTML into the same model object multiple times does not create duplicate child records. Two new test classes, Compile Include entries in the test csproj, and a build/test verification step.

## Tasks

- [x] 1. Create SurveyParserIdempotencyTests.cs
  - [x] 1.1 Create `OE2EmpireTracker.Tests/Parsers/SurveyParserIdempotencyTests.cs` with test fixture setup
    - Create `[TestFixture]` class with `SurveyParser _parser` field
    - Add `SetUp` method instantiating the parser
    - Add `LoadTestData(filename)` and `ExtractFragment(clipboardData)` helper methods matching existing pattern from `SurveyParserTests.cs`
    - Add private `ParseSurveyFromFile(string filename)` helper that loads test data, extracts fragment, creates Survey, and calls `ProcessHtml`
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 1.2 Implement ZehVazoran idempotency tests
    - `ZehVazoran_ParseTwice_ResourceCountUnchanged` — parse ZehVazoranIIM2.html into same Survey twice, assert `Resources.Count` unchanged
    - `ZehVazoran_ParseThreeTimes_ResourceCountUnchanged` — same but 3 passes
    - `ZehVazoran_ParseTwice_ResourceValuesPreserved` — snapshot resource name/purity/amount after first parse, verify unchanged after second parse
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.3 Implement AllSurveyFiles sweep property tests
    - **Property 1: Survey resource count idempotency**
    - **Property 2: Survey resource values stability**
    - `AllSurveyFiles_ParseTwice_ResourceCountUnchanged` — iterate over `ZehVazoranIIM2.html` and `QuogarV2249II.html`, verify resource count idempotency for each
    - `AllSurveyFiles_ParseTwice_ResourceValuesPreserved` — iterate over `ZehVazoranIIM2.html` and `QuogarV2249II.html`, verify resource values stability for each
    - Include comments: `// Feature: parser-idempotency-tests, Property 1: Survey resource count idempotency` and `// Feature: parser-idempotency-tests, Property 2: Survey resource values stability`
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4**

- [x] 2. Create ColonyParserIdempotencyTests.cs
  - [x] 2.1 Create `OE2EmpireTracker.Tests/Parsers/ColonyParserIdempotencyTests.cs` with test fixture setup
    - Create `[TestFixture]` class with `ColonyParser _parser` and `EmpireContext _empireContext` fields
    - Add `SetUp`/`TearDown` methods matching `ColonyParserTests.cs` pattern (save/restore file paths, `EmpireContext.Reset()`, `TestHelper.SetEmpireFilePath()`)
    - Add `LoadTestData`, `ExtractFragment`, and `ParseColonyFromFile(string filename)` helpers
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4_

  - [x] 2.2 Implement M1 structure idempotency tests
    - `M1_ParseTwice_StructureCountUnchanged` — parse M1 HTML into same Colony twice, assert `Structures.Count` unchanged
    - `M1_ParseThreeTimes_StructureCountUnchanged` — same but 3 passes
    - `M1_ParseTwice_StructureValuesPreserved` — snapshot FlatpackBlueprintUUID and gameSequence for each structure after first parse, verify unchanged after second
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 2.3 Implement M2-2 commodity idempotency tests
    - `M2_2_ParseTwice_CommodityCountUnchanged` — parse M2-2 HTML into same Colony twice, assert `Commodities.Count` unchanged
    - `M2_2_ParseThreeTimes_CommodityCountUnchanged` — same but 3 passes
    - `M2_2_ParseTwice_CommodityValuesPreserved` — snapshot commodity name/requested/fulfilled after first parse, verify unchanged after second
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 2.4 Implement AllColonyFiles sweep property tests
    - **Property 3: Colony structure count idempotency**
    - **Property 4: Colony structure values stability**
    - **Property 5: Colony commodity count idempotency**
    - **Property 6: Colony commodity values stability**
    - `AllColonyFiles_ParseTwice_StructureCountUnchanged` — iterate over all `ClnyHex*.html` files, verify structure count idempotency for each
    - `AllColonyFiles_ParseTwice_StructureValuesPreserved` — iterate over all `ClnyHex*.html` files, verify structure values stability for each
    - `AllColonyFiles_ParseTwice_CommodityCountUnchanged` — iterate over colony files that produce `Commodities.Count > 0`, verify commodity count idempotency for each
    - `AllColonyFiles_ParseTwice_CommodityValuesPreserved` — iterate over colony files that produce `Commodities.Count > 0`, verify commodity values stability for each
    - Include comments referencing design properties
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4**

- [x] 3. Add Compile Include entries to test csproj
  - [x] 3.1 Add `<Compile Include="Parsers\SurveyParserIdempotencyTests.cs" />` and `<Compile Include="Parsers\ColonyParserIdempotencyTests.cs" />` to the `<ItemGroup>` containing other Compile entries in `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - Place them adjacent to the existing `SurveyParserTests.cs` and `ColonyParserTests.cs` entries
    - _Requirements: 1.1, 2.1, 3.1_

- [x] 4. Checkpoint — Build and run all tests
  - Build the solution with MSBuild: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
  - Run tests with vstest: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll`
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The design uses C# with NUnit 4.5.1 — no language selection needed
- Both test classes follow existing patterns from `ColonyParserTests.cs` and `SurveyParserTests.cs`
- Property tests are implemented as parameterized sweep tests over all test data HTML files
- Old-style csproj requires explicit Compile Include entries for new .cs files
