# Project Structure

```
OE2EmpireTracker.sln              # Visual Studio solution (2 projects)
│
├── OE2EmpireTracker/             # Main WinForms application
│   ├── Program.cs                # Entry point — launches MainWindow
│   ├── BaselineData.json         # Shared game data (blueprint types, ship classes, tech levels)
│   ├── PlayerData.json           # Player-specific data (profiles, colonies, blueprints, surveys)
│   ├── NLog.config               # Logging configuration
│   │
│   ├── Models/                   # All POCOs, data types, enums, interfaces (33 files)
│   │   ├── Item.cs / ItemBag.cs  # Generic item and inventory container
│   │   ├── Blueprint.cs          # Blueprint data (extends Item)
│   │   ├── Commodity.cs          # Commodity definitions with construction recipes
│   │   ├── Resource.cs           # Resource definitions
│   │   ├── PlayerProfile.cs      # Player profile with skills
│   │   ├── CountDownTime.cs      # Timer/countdown tracking
│   │   ├── PropertyBag.cs        # Key-value property storage
│   │   ├── Colony.cs             # Colony data and processing logic
│   │   ├── ColonyStructure.cs    # Individual structure within a colony
│   │   ├── DeliveryRoute.cs      # Delivery route definitions
│   │   ├── DeliveryPlan.cs       # Delivery plan data
│   │   ├── Survey.cs             # Survey data
│   │   └── ...                   # ShipClass, TechLevel, BlueprintType, enums, etc.
│   │
│   ├── Services/                 # Singletons, processing logic, business rules (11 files)
│   │   ├── EmpireContext.cs      # Singleton — loads/manages shared game data
│   │   ├── PlayerContext.cs      # Singleton — loads/manages player data, persistence
│   │   ├── ColonyStatusCalculator.cs  # Computes built/ideal status for colony structures
│   │   ├── ColonyActivityCollector.cs # Collects colony activity data
│   │   ├── ColonyBuildEligibility.cs  # Build eligibility checks
│   │   ├── ColonyBootstrap.cs    # Colony initialization
│   │   ├── BuildOrderOptimizer.cs # Build order optimization
│   │   ├── BuildTimeCalculator.cs # Build time calculations
│   │   ├── BackgroundProcessor.cs # Background processing tasks
│   │   ├── DeliveryFulfillment.cs # Delivery fulfillment logic
│   │   └── PreferencesStore.cs   # User preferences persistence
│   │
│   ├── Parsers/                  # HTML and data import parsers (2 files)
│   │   ├── ColonyParser.cs       # Colony HTML parsing
│   │   └── SurveyParser.cs       # Survey HTML parsing
│   │
│   ├── Persistence/              # File I/O and window-state helpers (3 files)
│   │   ├── SafeFileWriter.cs     # Safe atomic file writing
│   │   ├── WindowStateHelper.cs  # Window position/size persistence
│   │   └── BoundsValidator.cs    # Screen bounds validation
│   │
│   ├── Constants/                # Game constants and static lookup tables
│   │   ├── GameConstants.cs      # Numeric/string constants (rates, keys, purities)
│   │   ├── BlueprintTypes.cs     # Blueprint type string constants
│   │   ├── RefiningRecipes.cs    # Synthetic refining recipe definitions
│   │   ├── ResearchTimeLookup.cs # Research time calculations
│   │   └── BlueprintPropertyValidation.cs
│   │
│   ├── ViewModels/               # ViewModel layer wrapping data for UI binding
│   │   ├── ColonyViewModel.cs
│   │   ├── ColonyStructureViewModel.cs
│   │   ├── BlueprintViewModel.cs
│   │   ├── PlayerProfileViewModel.cs
│   │   ├── DeliveryRouteViewModel.cs
│   │   ├── DeliveryPlanViewModel.cs
│   │   └── SurveyViewModel.cs
│   │
│   ├── Controls/                 # Reusable WinForms custom controls
│   │   ├── DataEntryGridView.cs
│   │   ├── FilteredComboBox.cs
│   │   ├── ValidatedTextBox.cs
│   │   └── ...
│   │
│   ├── Forms/                    # WinForms UI (one subfolder per feature area)
│   │   ├── MainWindow.cs         # Main application window
│   │   ├── Blueprint/            # Blueprint management form
│   │   ├── Colony/               # Colony management form + structure control
│   │   ├── Delivery/             # Delivery route form
│   │   ├── PlayerProfile/        # Player profile form + skill blocks
│   │   └── Survey/               # Survey form
│   │
│   └── specs/                    # In-project design notes (informal)
│
├── OE2EmpireTracker.Tests/       # NUnit test project
│   ├── Models/                   # Tests for data types, colony processing, structures, surveys
│   ├── Services/                 # Tests for singletons, calculators, fulfillment, context
│   ├── Parsers/                  # Tests for colony and survey HTML parsers
│   ├── Persistence/              # Tests for safe file writing, bounds validation
│   ├── Blueprint/                # Blueprint scanner tests
│   ├── Constants/                # Constants validation tests
│   ├── Controls/                 # Custom control tests
│   ├── Forms/                    # Form tests
│   └── TestData/                 # HTML fixtures for survey/blueprint parsing
│
└── spec/                         # External specification documents
    ├── requirements/             # Feature requirements (Colony, Delivery, DataModel, etc.)
    └── ...                       # Goals, recommendations, ambiguities
```

## Architecture Pattern
- Layered: Models (POCOs) → Services (domain logic) → ViewModels → Forms (UI)
- Parsers handle data import (HTML scraping) into Models
- Persistence handles file I/O and window-state management
- Singletons (`EmpireContext`, `PlayerContext`) act as in-memory repositories
- ViewModels wrap domain objects and expose typed operations for UI binding
- Forms use WinForms Designer (`.Designer.cs` + `.resx` pairs) — do not hand-edit Designer files
- Test project mirrors the main project's folder structure
