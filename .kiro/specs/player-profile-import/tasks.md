# Implementation Plan: Player Profile Import

## Overview

Implement a clipboard-based HTML parser for importing player profile data from the OE2 game UI. The parser follows the established SgmlReader → XmlDocument → XPath pattern used by ColonyParser and SurveyParser. The implementation extends the PlayerProfile and PlayerRank models with new fields, creates the PlayerProfileParser class with section-specific extraction methods, adds an Import button to FormPlayerProfile, and includes unit tests and property-based tests.

## Tasks

- [x] 1. Extend data models with new properties
  - [x] 1.1 Add CitizenId, RegistrationDate, and ActiveTime string properties to PlayerProfile.cs (default string.Empty)
    - _Requirements: 9.1, 9.2, 9.3, 9.7_
  - [x] 1.2 Add Title string property to PlayerRank.cs (default string.Empty)
    - _Requirements: 9.4, 9.7_
  - [x] 1.3 Write property test for serialization backward compatibility
    - **Property 6: Serialization backward compatibility**
    - Serialize PlayerProfile/PlayerRank to JSON, remove new fields, deserialize, assert defaults are string.Empty (not null)
    - **Validates: Requirements 9.7**

- [x] 2. Implement PlayerProfileParser core and identity/credits parsing
  - [x] 2.1 Create OE2EmpireTracker/Parsers/PlayerProfileParser.cs with class skeleton, ProcessHtml, ProcessClipboard, ParseFormattedNumber, and NormalizeWhitespace utility methods
    - Reuse ColonyParser.ParseHtmlToXml for HTML→XML conversion
    - Reuse ClipboardHelper.ExtractHtmlFragment for clipboard extraction
    - Add `<Compile Include>` entry to OE2EmpireTracker.csproj
    - _Requirements: 10.1, 10.2, 10.3, 10.4_
  - [x] 2.2 Implement ParseCharacterIdentity — extract name from ui_text_white div, faction from ui_text_purple div (strip brackets)
    - _Requirements: 1.1, 1.2, 1.3_
  - [x] 2.3 Implement ParseCredits — extract decimal from data-ui-tooltip attribute on ui_credit_detail element
    - _Requirements: 2.1, 2.2_
  - [x] 2.4 Write property test for formatted number round-trip
    - **Property 1: Formatted number round-trip**
    - Generate random non-negative decimals, format with commas, parse with ParseFormattedNumber, assert equality
    - **Validates: Requirements 2.1, 3.2**

- [x] 3. Implement rank and skill point parsing
  - [x] 3.1 Implement ParseHeadlineFields — extract CitizenId, RegistrationDate, ActiveTime from ProfileHeadlineRow elements
    - _Requirements: 9.5_
  - [x] 3.2 Implement ParseRankTracks — identify Public/Private/Military tracks by Bar_Public/Bar_Private/Bar_Military CSS classes, extract rank level, title, current XP, and next XP
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 9.6_
  - [x] 3.3 Implement ParseSkillPoints — extract available SP from Profile_Skills_BankedContainer_Available element
    - _Requirements: 4.1, 4.2_

- [x] 4. Implement skill group and individual skill parsing
  - [x] 4.1 Implement ParseSkillGroups — iterate Profile_Skill_Group elements, match display names to SkillGroupName enum via reverse lookup dictionary, set locked/unlocked state based on Profile_Skill_Group_Disabled presence
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  - [x] 4.2 Implement ParseSkills — iterate individual skill elements within each group, extract level from completed level boxes, detect training-in-progress from training box, parse training time remaining into CountDownTime
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  - [x] 4.3 Write property test for enum display name round-trip
    - **Property 2: Enum display name round-trip**
    - For all SkillName and SkillGroupName values, assert ToDisplayName → reverse lookup = identity
    - **Validates: Requirements 5.1, 5.4, 6.4**
  - [x] 4.4 Write property test for training time parsing
    - **Property 3: Training time parsing**
    - Generate random (days 0–99, hours 0–23) pairs, format as game string, parse, assert correct total seconds within 1s tolerance
    - **Validates: Requirements 6.3**

- [x] 5. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Add Import button and form integration
  - [x] 6.1 Add cmdImport button to FormPlayerProfile.Designer.cs in the flpCommands panel (before cmdNew)
    - _Requirements: 8.1_
  - [x] 6.2 Implement cmdImport_Click handler in FormPlayerProfile.cs — read clipboard, invoke PlayerProfileParser, match existing profile by name (case-insensitive) or create new with generated UUID, refresh form and list view, show MessageBox on error or missing clipboard data
    - _Requirements: 7.1, 7.2, 7.3, 8.2, 8.3, 8.4_
  - [x] 6.3 Write property test for profile update by case-insensitive name match
    - **Property 4: Profile update by case-insensitive name match**
    - Generate profile lists and a parsed profile with matching name (case-shuffled), assert UUID preserved and data updated
    - **Validates: Requirements 7.1, 7.3**
  - [x] 6.4 Write property test for profile creation when no name match exists
    - **Property 5: Profile creation when no name match exists**
    - Generate profile lists and a parsed profile with unique name, assert list grows by one with non-empty UUID
    - **Validates: Requirements 7.2**

- [-] 7. Unit tests for PlayerProfileParser
  - [x] 7.1 Create OE2EmpireTracker.Tests/Parsers/PlayerProfileParserTests.cs with integration tests against PlayerProfileScalorn.html — assert name, faction, credits, all three ranks (level, title, currentXP, nextXP), skill points, skill group states, individual skill levels, training status, CitizenId, RegistrationDate, ActiveTime
    - Add `<Compile Include>` and `<Content Include>` entries to test csproj
    - _Requirements: 1.1, 1.2, 2.1, 3.1, 3.2, 3.3, 4.1, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_
  - [x] 7.2 Add edge case tests — empty HTML, missing ui_character_detail, missing rank sections, malformed numbers, unknown skill names
    - _Requirements: 1.3, 2.2, 3.4, 4.2, 6.5_

- [ ] 8. Update csproj files and final wiring
  - [~] 8.1 Ensure OE2EmpireTracker.csproj has `<Compile Include="Parsers\PlayerProfileParser.cs" />` entry
    - _Requirements: 10.1_
  - [~] 8.2 Ensure OE2EmpireTracker.Tests.csproj has `<Compile Include>` entries for all new test files and `<Content Include>` for PlayerProfileScalorn.html with CopyToOutputDirectory
    - _Requirements: 10.1_

- [~] 9. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases using the sample HTML fixture
- The parser reuses ColonyParser.ParseHtmlToXml (internal static) and ClipboardHelper.ExtractHtmlFragment
- New .cs files require `<Compile Include>` entries in the old-style csproj files
- Test HTML file requires `<Content Include>` with CopyToOutputDirectory in the test csproj
