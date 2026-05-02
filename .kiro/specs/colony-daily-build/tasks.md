# Implementation Plan: Colony Daily Build

## Overview

Implement the Colony Daily Build feature in incremental steps: first the pure domain logic (build time calculator, eligibility filtering, ProcessColony build completion), then the new FormColonyDailyBuild form, then the ColonyStructure control changes for build UI, and finally wiring into MainWindow. Each step builds on the previous and ends with integration.

## Tasks

- [x] 1. Implement BuildTimeCalculator and ColonyBuildEligibility static classes
  - [x] 1.1 Create `Baseline/BuildTimeCalculator.cs` with `Calculate(int builderSkillLevel)` returning `Math.Max(1, (long)(86400.0 * (1.0 - builderSkillLevel * 0.02)))`
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 5.1, 5.2_

  - [x]* 1.2 Write property test for BuildTimeCalculator (Property 1: Build time calculation)
    - **Property 1: Build time calculation**
    - **Validates: Requirements 5.1, 5.2**
    - Loop over skill levels 0–50, verify `Calculate(level) == Math.Max(1, (long)(86400.0 * (1.0 - level * 0.02)))`
    - Also verify `Calculate(0) == 86400`, `Calculate(50) == 1`, `Calculate(25) == 43200`
    - Create test file `OE2EmpireTracker.Tests/Baseline/BuildTimeCalculatorTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`

  - [x] 1.3 Create `Baseline/ColonyBuildEligibility.cs` with static methods: `IsStagedStructure`, `IsBuildingStructure`, `IsEligible`, `GetFirstStagedStructure`
    - Uses `ColonyStructureViewModel` internally to read `IsStaged`/`IsBuilt`
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.2_

  - [x]* 1.4 Write property test for structure state predicates (Property 2: Structure state predicates are mutually consistent)
    - **Property 2: Structure state predicates are mutually consistent**
    - **Validates: Requirements 3.2, 3.3**
    - Generate structures with combinations of IsStaged (true/false), IsBuilt (true/false), BuildCompletionTime (null, expired, active)
    - Verify `IsStagedStructure` and `IsBuildingStructure` return correct values and are never both true
    - Add tests to `OE2EmpireTracker.Tests/Baseline/ColonyBuildEligibilityTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`

  - [x]* 1.5 Write property test for colony eligibility filtering (Property 3: Colony eligibility filtering)
    - **Property 3: Colony eligibility filtering**
    - **Validates: Requirements 3.1, 3.4, 3.5, 8.1, 8.2**
    - Generate colonies with varying numbers of structures in staged/building/built/online states
    - Verify `IsEligible` returns true iff at least one staged AND zero building

  - [x]* 1.6 Write property test for first staged structure selection (Property 4: First staged structure selection)
    - **Property 4: First staged structure selection**
    - **Validates: Requirements 4.2**
    - Generate colonies with multiple structures, random staged positions
    - Verify `GetFirstStagedStructure` returns the staged structure with the lowest index

- [x] 2. Implement build completion in Colony.ProcessColony()
  - [x] 2.1 Add build completion as the new first step in `Colony.ProcessColony()` before the existing mining/refining loop
    - Iterate `Structures`, check `BuildCompletionTime != null && BuildCompletionTime.TimeRemaining <= 0`
    - Set `Properties["Built"] = true`, `Properties["Staged"] = false`, `BuildCompletionTime = null`
    - _Requirements: 7.1, 7.2, 7.3_

  - [x]* 2.2 Write property test for build completion in ProcessColony (Property 6: Build completion in ProcessColony)
    - **Property 6: Build completion in ProcessColony**
    - **Validates: Requirements 7.1, 7.2**
    - Generate colonies with structures having expired/active BuildCompletionTime
    - Verify expired timers result in `IsBuilt=true`, `BuildCompletionTime=null`; active timers unchanged
    - Add tests to `OE2EmpireTracker.Tests/Baseline/ColonyBuildCompletionTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`

  - [x]* 2.3 Write unit tests for ProcessColony build completion edge cases
    - Test: expired BuildCompletionTime sets Built=true, clears timer
    - Test: active BuildCompletionTime (TimeRemaining > 0) is unchanged
    - Test: build completion runs before mining (newly-built mining rig can process in same call)
    - Test: colony with no structures — ProcessColony does not throw
    - Add tests to same `ColonyBuildCompletionTests.cs` file

- [x] 3. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Create FormColonyDailyBuild form
  - [x] 4.1 Create `Forms/ColonyDailyBuild/FormColonyDailyBuild.Designer.cs` with layout matching FormDeliveryExecution pattern
    - `flpBase` (Dock=Fill, WrapContents=false) containing `flpSelectors` (220px, TopDown) and `pnlContent` (AutoScroll, TopDown, WrapContents=false)
    - `flpSelectors` contains: `lblRoute` (bold "Route"), `txtRouteFilter` (ValidatedTextBox), `cmbRoute` (DropDownList)
    - Add `Compile Include` entries for `.cs`, `.Designer.cs` and `EmbeddedResource` for `.resx` to `OE2EmpireTracker.csproj`
    - _Requirements: 9.1, 9.2, 9.3_

  - [x] 4.2 Create `Forms/ColonyDailyBuild/FormColonyDailyBuild.resx` (empty resource file)
    - _Requirements: 9.3_

  - [x] 4.3 Create `Forms/ColonyDailyBuild/FormColonyDailyBuild.cs` implementing the form logic
    - Implement `IProgrammaticUpdateSource` with `ProgrammaticUpdateGuard` pattern
    - Route selector: populate from `playerContext.GetCurrentPlayerRoutes()`, filter by `txtRouteFilter.Text`
    - On route selection: iterate route stops, find colonies via `playerContext.FindColony()`, filter with `ColonyBuildEligibility.IsEligible()`
    - For each eligible colony: get `ColonyBuildEligibility.GetFirstStagedStructure()`, display `"{PlanetName} - {ColonyName}"` label + blueprint `ExtendedName` label + "Build" button
    - Build button click: look up owner's Builder skill, call `BuildTimeCalculator.Calculate()`, create `ColonyStructureViewModel` to set `IsStaged = false`, create `CountDownTime` with `TimeRemaining = buildSeconds`, assign to `structure.BuildCompletionTime`, call `playerContext.writeContext()`, fire `playerContext.OnColonyDataChanged()`, remove colony panel
    - Subscribe to `playerContext.CurrentPlayerChanged` (repopulate routes, clear content) and `playerContext.ColonyDataChanged` (rebuild content)
    - Unsubscribe in `OnFormClosed`
    - Layout handlers for resize
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 3.1, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 6.5, 8.1, 8.2_

  - [ ]* 4.4 Write property test for build initiation state transition (Property 5: Build initiation state transition)
    - **Property 5: Build initiation state transition**
    - **Validates: Requirements 6.1, 6.2**
    - Generate staged structures with random owner skill levels (0–50)
    - Simulate build initiation (set IsStaged=false, assign BuildCompletionTime)
    - Verify post-state: IsStaged=false, IsBuilt=false, BuildCompletionTime != null, TimeRemaining within 2s of `BuildTimeCalculator.Calculate()`
    - Verify colony is no longer eligible
    - Add tests to `OE2EmpireTracker.Tests/Baseline/ColonyBuildEligibilityTests.cs`

- [x] 5. Add ColonyStructure control build UI
  - [x] 5.1 Add building state handling to `ColonyStructure.UpdateData()` in `Forms/Colony/ColonyStructure.cs`
    - Before blueprint-type-specific handlers, check `BuildCompletionTime != null && TimeRemaining > 0`
    - Show `flpCompletionTime` with `txtCompletionTime` displaying `BuildCompletionTime.TimeRemainingString`
    - Show `cmdDone`, hide `cmdStart`, hide `flpSelection`, `flpSubSelection`, `flpManufacturingControls`
    - Start `timerCountdown` if not already running
    - Return early (skip blueprint-type handlers)
    - _Requirements: 10.3, 10.4, 10.5_

  - [x] 5.2 Add Build button visibility logic to `ColonyStructure.UpdateData()`
    - When structure is staged (`ViewModel.IsStaged && !ViewModel.IsBuilt`) and no sibling structure has active `BuildCompletionTime`, show `cmdStart` with text "Build"
    - When not eligible, hide `cmdStart`
    - _Requirements: 10.1, 10.6_

  - [x] 5.3 Add build initiation click handler in `Forms/Colony/ColonyStructure.cs`
    - Check single-build constraint (no other structure on colony has active BuildCompletionTime)
    - Look up owner's Builder skill level via `Colony.OwnerUUID`
    - Call `BuildTimeCalculator.Calculate(builderLevel)` for build duration
    - Set `ViewModel.IsStaged = false`, create `CountDownTime` with `TimeRemaining = buildSeconds`, assign to `ColonyStructureData.BuildCompletionTime`
    - Fire `ColonyStructureDataChanged`
    - _Requirements: 10.2, 10.7_

  - [ ]* 5.4 Write property test for colony form build button visibility (Property 7: Colony form build button visibility)
    - **Property 7: Colony form build button visibility**
    - **Validates: Requirements 10.1, 10.3, 10.5, 10.6**
    - Generate colonies with various structure state combinations
    - Verify build button visibility logic: visible iff staged AND no sibling building
    - Verify building state shows completion time, hides build button
    - Add tests to `OE2EmpireTracker.Tests/Baseline/ColonyBuildEligibilityTests.cs`

- [x] 6. Wire FormColonyDailyBuild into MainWindow
  - [x] 6.1 Add "Colony Daily Build" menu item to MainWindow Edit menu
    - Add click handler that creates `FormColonyDailyBuild` as MDI child
    - Update `MainWindow.Designer.cs` to add the menu item
    - Add `using OE2EmpireTracker.Forms.ColonyDailyBuild;` to `MainWindow.cs`
    - _Requirements: 1.1_

- [x] 7. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests are implemented as regular NUnit `[Test]` methods with parameterized/loop-based assertions (FsCheck is not available)
- The .csproj uses explicit `Compile Include` entries — new files must be added manually to both main and test projects
