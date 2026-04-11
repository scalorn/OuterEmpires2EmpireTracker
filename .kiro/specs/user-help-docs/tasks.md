# Implementation Plan: User Help Documentation

## Overview

Add an in-app help system to OE2 Empire Tracker. Markdown docs live in `docs/` at the repo root and are embedded into the assembly at build time. A `FormHelp` dialog renders them as HTML via Markdig, with TreeView navigation and a WebBrowser panel. Users access help through Help → Contents (Ctrl+F1) or F1 context-sensitive help.

## Tasks

- [x] 1. Add Markdig NuGet package and create HelpTopicRegistry
  - [x] 1.1 Add Markdig to packages.config and csproj
    - Add `<package id="Markdig" version="0.39.1" targetFramework="net481" />` to `OE2EmpireTracker/packages.config`
    - Run `nuget restore OE2EmpireTracker.sln` to download the package
    - Add a `<Reference Include="Markdig">` with `<HintPath>..\packages\Markdig.0.39.1\lib\net462\Markdig.dll</HintPath>` to `OE2EmpireTracker/OE2EmpireTracker.csproj`
    - _Requirements: 2.1, 2.2_

  - [x] 1.2 Create HelpTopicRegistry static class
    - Create `OE2EmpireTracker/Services/HelpTopicRegistry.cs`
    - Implement `GetTopicForForm(string formTypeName)` returning the mapped filename or `"README.md"` as fallback
    - Implement `GetAllTopics()` returning ordered list of `(string DisplayName, string FileName)` tuples for all 9 topic files (README.md, getting-started.md, colonies.md, blueprints.md, surveys.md, delivery-routes.md, player-profiles.md, background-processing.md, window-state.md)
    - Include all 9 form-type mappings from the design (FormColony→colonies.md, FormBlueprint→blueprints.md, etc.)
    - Add `<Compile Include="Services\HelpTopicRegistry.cs" />` to the csproj
    - _Requirements: 6.3, 6.4, 1.3_

  - [ ]* 1.3 Write property test for HelpTopicRegistry
    - **Property 2: Topic registry returns correct mapping or fallback**
    - Create `OE2EmpireTracker.Tests/Services/HelpTopicRegistryPropertyTests.cs`
    - Generate random strings with FsCheck; verify mapped types return correct file, all others return `"README.md"`
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]`
    - Add `<Compile Include="Services\HelpTopicRegistryPropertyTests.cs" />` to test csproj
    - **Validates: Requirements 6.3, 6.4**

  - [ ]* 1.4 Write unit tests for HelpTopicRegistry
    - Create `OE2EmpireTracker.Tests/Services/HelpTopicRegistryTests.cs`
    - Test `GetAllTopics` returns 9 topics with correct filenames
    - Test `GetTopicForForm` for each of the 9 specific form-type mappings
    - Add `<Compile Include="Services\HelpTopicRegistryTests.cs" />` to test csproj
    - _Requirements: 1.3, 6.3_

- [-] 2. Create HelpRenderer and markdown documentation files
  - [x] 2.1 Create HelpRenderer static class
    - Create `OE2EmpireTracker/Services/HelpRenderer.cs`
    - Implement `RenderMarkdown(string markdownContent)` using Markdig pipeline to produce a full HTML document with `<html>`, `<head>`, `<style>`, and `<body>` elements
    - Implement `RenderTopic(string resourceFileName)` that loads from embedded resources via `Assembly.GetManifestResourceStream` using the `OE2EmpireTracker.docs.{filename}` naming convention, then calls `RenderMarkdown`
    - Return `null` from `RenderTopic` if the resource is not found
    - Include inline CSS for readable formatting (font family, line spacing, heading styles, code block styling, table styling)
    - Add `<Compile Include="Services\HelpRenderer.cs" />` to the csproj
    - _Requirements: 2.3, 3.3, 3.4, 7.1, 7.2, 7.4_

  - [x] 2.2 Create all markdown documentation files in docs/
    - Create `docs/README.md` — Table of Contents with links to all topic pages
    - Create `docs/getting-started.md` — initial setup, creating a player profile, general workflow
    - Create `docs/colonies.md` — colony import, structures, commodities, daily build
    - Create `docs/blueprints.md` — blueprint import, market import, evolution chains, filters
    - Create `docs/surveys.md` — survey import and resource viewing
    - Create `docs/delivery-routes.md` — route planning, auto-fill, delivery execution
    - Create `docs/player-profiles.md` — skills, ranks, profile management
    - Create `docs/background-processing.md` — timer processing, status bar indicators
    - Create `docs/window-state.md` — window position persistence, MDI layout options
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

  - [x] 2.3 Add EmbeddedResource entries to csproj for all docs
    - Add 9 `<EmbeddedResource Include="..\docs\{file}"><Link>docs\{file}</Link></EmbeddedResource>` entries to `OE2EmpireTracker/OE2EmpireTracker.csproj` for each markdown file
    - _Requirements: 3.1, 3.2_

  - [ ]* 2.4 Write property tests for HelpRenderer
    - Create `OE2EmpireTracker.Tests/Services/HelpRendererPropertyTests.cs`
    - **Property 1: Rendered HTML has complete document structure** — generate random non-null strings, verify output contains `<html>`, `<head>`, `<style`, `<body>`
    - **Property 4: Markdown rendering preserves content** — generate random alphanumeric words, embed in markdown, verify word appears in HTML output
    - Use `[FsCheck.NUnit.Property(MaxTest = 100)]`
    - Add `<Compile Include="Services\HelpRendererPropertyTests.cs" />` to test csproj
    - **Validates: Requirements 2.3, 7.1, 7.2, 7.4**

  - [ ]* 2.5 Write unit tests for HelpRenderer
    - Create `OE2EmpireTracker.Tests/Services/HelpRendererTests.cs`
    - Test `RenderTopic` for all registered topics returns non-null (Property 3)
    - Test `RenderTopic` with unknown resource returns null
    - Test `RenderMarkdown` with headings produces `<h1>`, bold produces `<strong>`, code block produces `<pre>`, table produces `<table>`, list produces `<ul>` and `<li>`
    - Add Markdig reference to test csproj: `<Reference Include="Markdig"><HintPath>..\packages\Markdig.0.39.1\lib\net462\Markdig.dll</HintPath></Reference>` and add to test `packages.config`
    - Add `<Compile Include="Services\HelpRendererTests.cs" />` to test csproj
    - _Requirements: 3.3, 3.4, 4.5, 7.4_

- [~] 3. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Create FormHelp and integrate with MainWindow
  - [~] 4.1 Create FormHelp.Designer.cs (hand-written)
    - Create `OE2EmpireTracker/Forms/FormHelp.Designer.cs`
    - Define `SplitContainer` with `TreeView` (left, ~250px) and `WebBrowser` (right)
    - Set form properties: `Text = "Help"`, `Size = 900x600`, `StartPosition = CenterParent`
    - Add `<Compile Include="Forms\FormHelp.Designer.cs"><DependentUpon>FormHelp.cs</DependentUpon></Compile>` to csproj
    - _Requirements: 4.1, 4.2, 4.3, 4.6_

  - [~] 4.2 Create FormHelp.cs
    - Create `OE2EmpireTracker/Forms/FormHelp.cs`
    - Constructor accepts optional `string initialTopic` parameter
    - Populate TreeView from `HelpTopicRegistry.GetAllTopics()` on load
    - On TreeView `AfterSelect`, call `HelpRenderer.RenderTopic()` and set `WebBrowser.DocumentText`
    - Display "Topic not available" message if `RenderTopic` returns null
    - Intercept `WebBrowser.Navigating` to handle internal `.md` links (load from embedded resources) and block external URLs
    - Select the initial topic node if provided, otherwise select Table of Contents
    - Add `<Compile Include="Forms\FormHelp.cs"><SubType>Form</SubType></Compile>` to csproj
    - _Requirements: 4.4, 4.5, 4.6, 7.3, 3.3, 3.4_

  - [~] 4.3 Add Help → Contents menu item and F1 handling to MainWindow
    - Add `contentsToolStripMenuItem` to `MainWindow.Designer.cs` in the Help menu, above About
    - Set `ShortcutKeys = Keys.Control | Keys.F1`
    - Add click handler in `MainWindow.cs` that opens `new FormHelp().ShowDialog(this)`
    - Override `ProcessCmdKey` in `MainWindow.cs` to handle F1: look up `ActiveMdiChild` type name in `HelpTopicRegistry`, open `FormHelp` with that topic
    - _Requirements: 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4_

- [~] 5. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Markdig version should match what's available in the NuGet packages folder after restore
- FormHelp.Designer.cs is hand-written (not generated by WinForms designer) since we're scripting it
- New .cs files need `<Compile Include>` entries in the appropriate csproj
- Embedded resources use `<EmbeddedResource Include="..\docs\{file}"><Link>docs\{file}</Link></EmbeddedResource>` pattern since docs/ is outside the project directory
