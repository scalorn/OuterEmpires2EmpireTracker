# Implementation Plan: Code Metrics Integration

## Overview

This plan integrates Visual Studio Code Metrics into the OE2EmpireTracker audit pipeline. The implementation installs the `Microsoft.CodeAnalysis.Metrics` NuGet package, creates a `code-metrics.js` audit tool that parses the generated `CodeMetrics.xml`, and wires it into the existing `audit.js` pipeline. All tool code is JavaScript (Node.js) with zero npm dependencies, following the conventions of the existing 17 audit tools.

## Tasks

- [x] 1. Install NuGet package and configure MSBuild Metrics target
  - [x] 1.1 Add `Microsoft.CodeAnalysis.Metrics` to packages.config
    - Add a `<package>` entry with `developmentDependency="true"` and pinned version
    - Run `nuget restore` to download the package to the `packages/` folder
    - _Requirements: 1.1, 1.3_

  - [x] 1.2 Add MSBuild targets Import to OE2EmpireTracker.csproj
    - Add an `<Import>` element for the package's `.targets` file after `Microsoft.CSharp.targets`, following the existing `System.ValueTuple.targets` pattern
    - Add an `<Error>` condition to the `EnsureNuGetPackageBuildImports` target to verify the targets file exists
    - _Requirements: 1.2_

  - [x] 1.3 Add `*.Metrics.xml` to .gitignore
    - Add a glob pattern `*.Metrics.xml` under a "Code metrics build artifact" comment
    - _Requirements: 2.3_

  - [x] 1.4 Verify MSBuild `/t:Metrics` generates CodeMetrics.xml
    - Run `MSBuild.exe OE2EmpireTracker.sln /t:Metrics` and confirm `OE2EmpireTracker/OE2EmpireTracker.Metrics.xml` is generated
    - _Requirements: 2.1, 2.2_

- [x] 2. Checkpoint - Verify NuGet package installation
  - Ensure the build succeeds with zero errors and zero warnings, and that `MSBuild /t:Metrics` produces the XML file. Ask the user if questions arise.

- [x] 3. Create the code-metrics.js audit tool
  - [x] 3.1 Create the threshold configuration file
    - Create `.kiro/tools/code-metrics-config.json` with default thresholds: `maintainabilityIndexMin: 20`, `cyclomaticComplexityMax: 25`, `classCouplingMax: 80`, `depthOfInheritanceMax: 6`
    - _Requirements: 9.1, 9.2, 9.3_

  - [x] 3.2 Create the baseline file with empty violations array
    - Create `.kiro/tools/code-metrics-baseline.json` with `{ "violations": [] }`
    - _Requirements: 10.1_

  - [x] 3.3 Implement the code-metrics.js tool
    - Create `.kiro/tools/code-metrics.js` as a standalone Node.js script with zero npm dependencies
    - Implement XML existence check: if `OE2EmpireTracker/OE2EmpireTracker.Metrics.xml` does not exist, print skip message and exit 0
    - Implement state-machine XML parser that extracts per-method and per-type metrics from the hierarchical XML structure (Assembly > Namespace > Type > Method)
    - Extract fully qualified names: `Namespace.TypeName` for types, `Namespace.MethodSignature` for methods
    - Extract all six metric values: MaintainabilityIndex, CyclomaticComplexity, ClassCoupling, DepthOfInheritance, SourceLines, ExecutableLines
    - Implement threshold loading from config file with fallback to defaults
    - Implement baseline loading from baseline file with fallback to empty array
    - Implement threshold comparison: MaintainabilityIndex below min = violation, CyclomaticComplexity/ClassCoupling/DepthOfInheritance above max = violation
    - Implement baseline filtering: violations matching baseline entries are marked as `BASELINE` and excluded from findings count
    - Output violations grouped by metric type with format: `VIOLATION: Element.Name METRIC=value (threshold: N) [SL:N EL:N]` for new violations and `BASELINE:` prefix for baselined ones
    - Print summary line `N findings` where N is count of new (non-baselined) violations
    - Exit code 0 when no new violations, exit code 1 when new violations exist
    - Handle malformed XML, malformed config JSON, and malformed baseline JSON gracefully (print warning, use defaults/empty)
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 6.1, 6.2, 7.1, 7.2, 8.1, 8.2, 9.1, 9.2, 10.1, 10.2, 10.3, 10.4, 11.1, 11.2, 11.3, 12.1, 12.2, 12.3, 14.1, 14.2_

  - [ ]* 3.4 Write property test: XML parsing extracts all elements with correct metrics
    - **Property 1: XML parsing extracts all elements with correct metrics**
    - Generate random XML structures with varying namespaces, types, and methods with arbitrary metric values using fast-check
    - Parse the XML and verify one record per NamedType and one record per Method, each with correct fully qualified name and all six metric values
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**

  - [ ]* 3.5 Write property test: Threshold violation detection is correct
    - **Property 2: Threshold violation detection is correct**
    - Generate random metric values and random thresholds using fast-check
    - Verify MaintainabilityIndex flagged iff value < threshold, CyclomaticComplexity flagged iff value > threshold, ClassCoupling flagged iff value > threshold, DepthOfInheritance flagged iff value > threshold
    - **Validates: Requirements 5.1, 5.2, 6.1, 6.2, 7.1, 7.2, 8.1, 8.2**

  - [ ]* 3.6 Write property test: Baseline filtering correctly partitions violations
    - **Property 3: Baseline filtering correctly partitions violations**
    - Generate random violation sets and random baselines (subsets) using fast-check
    - Verify new-violation count equals total violations minus baselined count, and baselined violations are still reported but marked
    - **Validates: Requirements 10.2, 10.3**

  - [ ]* 3.7 Write property test: Violation reports contain all required fields
    - **Property 4: Violation reports contain all required fields**
    - Generate random violations and format them
    - Verify each output line contains metric name, fully qualified element name, actual value, and threshold value; method violations also include SourceLines and ExecutableLines
    - **Validates: Requirements 12.1, 12.2, 14.1**

  - [ ]* 3.8 Write property test: SourceLines and ExecutableLines never cause violations
    - **Property 5: SourceLines and ExecutableLines never cause violations**
    - Generate methods/types with extreme SourceLines/ExecutableLines (0, 100000) but passing values for all threshold-enforced metrics
    - Verify zero violations are produced
    - **Validates: Requirements 14.2**

- [x] 4. Checkpoint - Verify code-metrics.js works correctly
  - Run `node .kiro/tools/code-metrics.js` against the generated `OE2EmpireTracker.Metrics.xml` and verify output format is correct. Test with missing XML (should skip gracefully). Ensure all tests pass. Ask the user if questions arise.

- [x] 5. Integrate into audit pipeline and generate initial baseline
  - [x] 5.1 Add Code Metrics entry to audit.js tools array
    - Add `{ name: 'Code Metrics', script: 'code-metrics.js' }` to the `tools` array in `.kiro/tools/audit.js`
    - _Requirements: 13.1, 13.2, 13.3_

  - [x] 5.2 Generate initial baseline from current codebase
    - Run `node .kiro/tools/code-metrics.js` against the current `OE2EmpireTracker.Metrics.xml`
    - Capture all violations and populate `.kiro/tools/code-metrics-baseline.json` with the current violations so the audit passes cleanly
    - _Requirements: 10.1, 10.2_

  - [ ]* 5.3 Write integration test: audit.js includes Code Metrics in summary
    - Run `node .kiro/tools/audit.js` and verify "Code Metrics" appears in the audit summary output
    - _Requirements: 13.1, 13.2, 13.3_

- [x] 6. Update documentation
  - [x] 6.1 Add code metrics documentation to docs/
    - Document the full MSBuild command for generating CodeMetrics.xml: `"D:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe" OE2EmpireTracker.sln /t:Metrics`
    - Describe the purpose of the Metrics target and the output file location (`OE2EmpireTracker/OE2EmpireTracker.Metrics.xml`)
    - Note that CodeMetrics.xml is a build artifact excluded from source control via `.gitignore`
    - _Requirements: 15.1, 15.2, 15.3_

- [x] 7. Final checkpoint - Verify complete integration
  - Build the solution with zero errors and zero warnings. Run `node .kiro/tools/audit.js` and verify Code Metrics appears in the summary with zero new findings. Ensure all tests pass. Ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using fast-check
- The code-metrics.js tool uses only Node.js built-ins (fs, path) with zero npm dependencies
- All file writes during implementation must use `.kiro/tools/fwrite.js` per project conventions
- The tool handles missing XML gracefully (exit 0) so the audit pipeline never crashes when metrics have not been generated
