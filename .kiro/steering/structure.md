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
│   ├── Baseline/                 # Core domain logic and context singletons
│   │   ├── EmpireContext.cs      # Singleton — loads/manages shared game data
│   │   ├── PlayerContext.cs      # Singleton — loads/manages player data, persistence
│   │   ├── Colony.cs             # Colony processing (mining, refining, research, manufacturing)
│   │   ├── ColonyStructure.cs    # Individual structure within a colony
│   │   ├── ColonyStatusCalculator.cs  # Computes built/ideal status for colony structures
│   │   ├── DeliveryRoute.cs      # Delivery route definitions
│   │   ├── DeliveryPlan.cs       # Delivery plan logic
│   │   ├── Survey.cs / SurveyParser.cs  # Survey data and HTML parsing
│   │   └── ...                   # ShipClass, TechLevel, BlueprintType, etc.
│   │
│   ├── Data/                     # Data model classes (POCOs)
│   │   ├── Item.cs / ItemBag.cs  # Generic item and inventory container
│   │   ├── Blueprint.cs          # Blueprint data (extends Item)
│   │   ├── Commodity.cs          # Commodity definitions with construction recipes
│   │   ├── Resource.cs           # Resource definitions
│   │   ├── PlayerProfile.cs      # Player profile with skills
│   │   ├── CountDownTime.cs      # Timer/countdown tracking
│   │   ├── PropertyBag.cs        # Key-value property storage
│   │   └── ...                   # Supporting types (enums, sub-resources, etc.)
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
│   ├── Baseline/                 # Tests for colony processing, structures, surveys
│   ├── Blueprint/                # Blueprint scanner tests
│   ├── Constants/                # Constants validation tests
│   ├── Controls/                 # Custom control tests
│   ├── Data/                     # Data model tests
│   └── TestData/                 # HTML fixtures for survey/blueprint parsing
│
└── spec/                         # External specification documents
    ├── requirements/             # Feature requirements (Colony, Delivery, DataModel, etc.)
    └── ...                       # Goals, recommendations, ambiguities
```

## Architecture Pattern
- Layered: Data → Baseline (domain logic) → ViewModels → Forms (UI)
- Singletons (`EmpireContext`, `PlayerContext`) act as in-memory repositories
- ViewModels wrap domain objects and expose typed operations for UI binding
- Forms use WinForms Designer (`.Designer.cs` + `.resx` pairs) — do not hand-edit Designer files
- Test project mirrors the main project's folder structure
