# Implementation Plan: BL-124 Read-Only Display Forms

## Overview

Ensure FormColonyActivity, FormColonyDailyBuild, and FormAutoFill consistently use ReadOnly wrapper types. No services or ViewModels needed.

## Tasks

- [x] 1. Audit FormColonyActivity for mutable entity references
  - [x] 1.1 Review FormColonyActivity.cs for direct mutable entity usage
    - Replace with ReadOnly wrapper types where needed
    - _Requirements: 1.1, 1.2_
  - [x] 1.2 Verify ColonyActivityCollector compatibility with ReadOnly types
    - _Requirements: 1.3_

- [x] 2. Audit FormColonyDailyBuild for mutable entity references
  - [x] 2.1 Review FormColonyDailyBuild.cs for direct mutable entity usage
    - Replace route selection with ReadOnlyDeliveryRoute
    - Replace colony data access with ReadOnly types
    - _Requirements: 2.1, 2.2, 2.3_

- [x] 3. Verify FormAutoFill is clean
  - [x] 3.1 Confirm FormAutoFill does not access mutable entity data
    - _Requirements: 3.1, 3.2_

- [x] 4. Checkpoint --- Verify changes compile and existing tests pass

- [x] 5. Add verification test
  - [x] 5.1 Write verification test for read-only display forms
    - Create OE2EmpireTracker.Tests/Services/ReadOnlyDisplayFormVerificationTests.cs
    - Grep forms for mutable entity references
    - Add Compile Include to test csproj
    - _Requirements: 4.1_

- [-] 6. Final checkpoint --- Full build, all tests pass, audit clean

## Notes

- No services, ViewModels, or DTOs needed
- No unsaved changes prompts needed
- FormAutoFill is already clean
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
