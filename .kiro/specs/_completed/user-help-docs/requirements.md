# Requirements Document

## Introduction

End-user help documentation for OE2 Empire Tracker. Markdown files live in a `docs/` folder at the repository root so they render natively on GitHub. The same files are embedded in the application assembly and rendered at runtime via Markdig (markdown-to-HTML) inside a dedicated help form with tree navigation and a WebBrowser panel. Users access help through a Help → Contents menu item or F1 context-sensitive help that opens the page for the currently active form.

## Glossary

- **Help_System**: The subsystem responsible for loading, rendering, and navigating embedded markdown help content at runtime.
- **Help_Form**: A non-MDI dialog window containing a TreeView (left panel) for navigation and a WebBrowser control (right panel) for displaying rendered HTML.
- **Markdown_Renderer**: The component that converts markdown content to HTML using the Markdig library.
- **Help_Topic_Registry**: A static mapping from form type names to their corresponding documentation resource names.
- **Table_of_Contents**: The `docs/README.md` file that serves as the root help page, listing and linking to all topic pages.
- **Embedded_Resource**: A markdown file compiled into the application assembly so help content is available without external file dependencies.
- **Context_Sensitive_Help**: The mechanism that determines which help topic to display based on the currently active MDI child form.
- **MainWindow**: The MDI container form with the application menu bar.

## Requirements

### Requirement 1: Markdown Documentation Files

**User Story:** As an end user, I want to browse the application documentation on GitHub, so that I can read help content without running the application.

#### Acceptance Criteria

1. THE Help_System SHALL provide markdown documentation files in a `docs/` folder at the repository root.
2. THE Table_of_Contents SHALL be a `docs/README.md` file containing links to all topic pages.
3. THE Help_System SHALL provide one markdown file per application area: getting-started, colonies, blueprints, surveys, delivery-routes, player-profiles, background-processing, and window-state.
4. WHEN a user views the `docs/` folder on GitHub, THE Help_System SHALL render all documentation using standard markdown syntax compatible with GitHub rendering.
5. THE Help_System SHALL target end-user audiences only and SHALL NOT include contributor or developer documentation.

### Requirement 2: Markdig NuGet Integration

**User Story:** As a developer, I want Markdig added as a NuGet dependency, so that the application can convert markdown to HTML at runtime.

#### Acceptance Criteria

1. THE Help_System SHALL reference the Markdig NuGet package in `packages.config`.
2. THE Help_System SHALL include the Markdig assembly reference in the `.csproj` file.
3. THE Markdown_Renderer SHALL use the Markdig library to convert markdown content to HTML.

### Requirement 3: Embedded Resource Packaging

**User Story:** As an end user, I want help content available offline, so that I can access documentation without an internet connection or external files.

#### Acceptance Criteria

1. THE Help_System SHALL include all markdown files from the `docs/` folder as embedded resources in the application assembly.
2. THE Help_System SHALL declare each markdown file as an `EmbeddedResource` in the `.csproj` file with a link path to the `docs/` folder.
3. WHEN the application loads a help topic, THE Help_System SHALL read the markdown content from the embedded resource stream.
4. IF an embedded resource is not found for a requested topic, THEN THE Help_System SHALL display a message indicating the topic is unavailable.

### Requirement 4: Help Form Layout

**User Story:** As an end user, I want a help window with a navigation tree and content panel, so that I can browse and read documentation within the application.

#### Acceptance Criteria

1. THE Help_Form SHALL display a TreeView control in the left panel for topic navigation.
2. THE Help_Form SHALL display a WebBrowser control in the right panel for rendering HTML content.
3. THE Help_Form SHALL use a SplitContainer to divide the TreeView and WebBrowser panels.
4. WHEN the Help_Form opens, THE Help_Form SHALL populate the TreeView with all available help topics.
5. WHEN a user selects a TreeView node, THE Help_Form SHALL render the corresponding markdown file as HTML in the WebBrowser panel.
6. THE Help_Form SHALL open as a non-MDI dialog window (not an MDI child).

### Requirement 5: Help Menu Integration

**User Story:** As an end user, I want a Contents item in the Help menu, so that I can open the help system from the menu bar.

#### Acceptance Criteria

1. THE MainWindow SHALL include a "Contents" menu item in the Help menu, positioned above the existing "About" item.
2. WHEN the user clicks Help → Contents, THE MainWindow SHALL open the Help_Form displaying the Table_of_Contents.
3. THE "Contents" menu item SHALL have the keyboard shortcut Ctrl+F1.

### Requirement 6: F1 Context-Sensitive Help

**User Story:** As an end user, I want to press F1 to get help about the form I am currently using, so that I can quickly find relevant documentation.

#### Acceptance Criteria

1. WHEN the user presses F1 while an MDI child form is active, THE MainWindow SHALL open the Help_Form at the help topic mapped to that form type.
2. WHEN the user presses F1 while no MDI child form is active, THE MainWindow SHALL open the Help_Form at the Table_of_Contents.
3. THE Help_Topic_Registry SHALL map each form type to a documentation file:
   - FormColony → `colonies.md`
   - FormBlueprint → `blueprints.md`
   - FormSurvey → `surveys.md`
   - FormDeliveryRoute → `delivery-routes.md`
   - FormDeliveryExecution → `delivery-routes.md`
   - FormAutoFill → `delivery-routes.md`
   - FormPlayerProfile → `player-profiles.md`
   - FormColonyActivity → `colonies.md`
   - FormColonyDailyBuild → `colonies.md`
4. IF the active form type has no mapping in the Help_Topic_Registry, THEN THE Help_System SHALL open the Help_Form at the Table_of_Contents.

### Requirement 7: Markdown-to-HTML Rendering

**User Story:** As an end user, I want documentation rendered as formatted HTML, so that I can read well-structured content with headings, lists, and links.

#### Acceptance Criteria

1. THE Markdown_Renderer SHALL convert markdown content to a complete HTML document with a `<head>` and `<body>` element.
2. THE Markdown_Renderer SHALL include a CSS stylesheet in the HTML output for readable formatting (font family, line spacing, heading styles, code block styling).
3. WHEN the markdown contains internal links to other `.md` files, THE Help_Form SHALL intercept navigation and load the linked topic from embedded resources instead of attempting file system or web navigation.
4. THE Markdown_Renderer SHALL support standard markdown features: headings, bold, italic, code blocks, lists, tables, and links.

### Requirement 8: Documentation Content Coverage

**User Story:** As an end user, I want documentation covering all major application features, so that I can learn how to use the full application.

#### Acceptance Criteria

1. THE Help_System SHALL provide a getting-started page covering initial setup, creating a player profile, and the general workflow.
2. THE Help_System SHALL provide a colonies page covering colony import, structures, commodities, and daily build operations.
3. THE Help_System SHALL provide a blueprints page covering blueprint import, market import, evolution chains, and filters.
4. THE Help_System SHALL provide a surveys page covering survey import and resource viewing.
5. THE Help_System SHALL provide a delivery-routes page covering route planning, auto-fill, and delivery execution.
6. THE Help_System SHALL provide a player-profiles page covering skills, ranks, and profile management.
7. THE Help_System SHALL provide a background-processing page covering automatic timer processing and the status bar indicators.
8. THE Help_System SHALL provide a window-state page covering window position persistence and MDI layout options.
