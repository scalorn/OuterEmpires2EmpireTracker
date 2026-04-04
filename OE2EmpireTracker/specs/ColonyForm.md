Based on the provided citations, here is a detailed, AI-generatable specification for the OE2EmpireTracker.Forms.Colony Windows Forms module. The spec covers two key UI components:

FormColony — a top-level form (likely a main colony management window)
ColonyStructure — a user control used within forms (e.g., for managing individual structures on a colony)
The specification includes:

Namespace and class structure
UI layout hierarchy with container relationships
Control types, names, properties, and functional roles
Resource/serialization format context (via citations 2 & 3)
📁 Project Context
Namespace: OE2EmpireTracker.Forms.Colony
Framework: .NET Windows Forms (WinForms), Designer-generated code (auto-generated InitializeComponent() with #region markers)
Serialization Format: .resx resources may store binary/base64-encoded objects (application/x-microsoft.net.object.bytearray.base64, per Citation 3)
🏗️ 1. FormColony — Main Colony Management Form
A top-level form for viewing and managing planetary colonies.

🔹 Class Declaration
namespace OE2EmpireTracker.Forms.Colony 
{
    partial class FormColony : Form // inherits System.Windows.Forms.Form
}
🔹 Private Fields (Designer-managed)
Field	Type	Purpose
components	IContainer	Resource disposal container (IDisposable)
flpSearchList, flpBlueprintSearch, flpColonyData, flpBaseDetails	FlowLayoutPanel	Layout containers (see hierarchy below)
lblBlueprintListFilter, lblSelection, etc.	Label	Static text labels
txtBlueprintListFilter, txtSelectionFilter	TextBox	Text input fields for filtering/searching
lvwColonies	ListView	List view of colonies (see columns below)
Property, BaseValue, CurrentValue	DataGridViewTextBoxColumn	Column definitions in a grid (lvwColonies)
💡 Note: lvwColonies appears to be a DataGridView, despite being named as ListView. This is inconsistent naming—ensure it's declared as System.Windows.Forms.DataGridView if using columns.

🔹 UI Layout Hierarchy (top-level to leaf controls)
FormColony
├── tlpBase (TableLayoutPanel)
│   ├── flpColonyData (FlowLayoutPanel)      // Top/left section for colony overview
│   │   └── flpBaseDetails (FlowLayoutPanel) // Base details area (e.g., name, population)
│   └── flpBlueprintSearch (FlowLayoutPanel)  // Right/bottom section for blueprints
│       ├── lblBlueprintListFilter (Label)    // e.g., "Filter Blueprints:"
│       ├── txtBlueprintListFilter (TextBox)  // Search/filter input
│       └── flpSearchList (FlowLayoutPanel)   // Container for blueprint items (dynamic controls)
└── lvwColonies (DataGridView or ListView)   // Table of colony properties/values
    ├── Property (DataGridViewTextBoxColumn)      // e.g., "Name", "Population"
    ├── BaseValue (DataGridViewTextBoxColumn)     // e.g., initial values
    └── CurrentValue (DataGridViewTextBoxColumn)  // e.g., current modifiers
🔹 Behavioral Notes
lvwColonies likely displays structured colony data in a table (e.g., property vs. value).
Filtering/search occurs via txtBlueprintListFilter → updates flpSearchList.
All panels use FlowLayoutPanel for dynamic, flow-based layout of child controls (e.g., adding/removing blueprints).
🧱 2. ColonyStructure — User Control for Individual Structure Management
A reusable control representing a single structure (e.g., power plant, factory) on a colony.

🔹 Class Declaration
namespace OE2EmpireTracker.Forms.Colony 
{
    partial class ColonyStructure : UserControl // inherits System.Windows.Forms.UserControl
}
🔹 Private Fields (Designer-managed)
Field	Type	Purpose
components	IContainer	Resource container
rtbStatus	RichTextBox	Read-only status log output (e.g., errors, updates)
flpColonyStructure, flpMovement, flpStructureDetails, flpStructureCommands, flpSelection	FlowLayoutPanel	Layout containers for structure-related UI sections
🔹 UI Layout Hierarchy
ColonyStructure (UserControl)
├── rtbStatus (RichTextBox)                    // Status/output log at top or bottom
└── flpColonyStructure (FlowLayoutPanel)       // Main container
    ├── flpMovement (FlowLayoutPanel)          // Navigation/structure movement controls
    │   ├── cmdUp (Button)                     // Move structure up in hierarchy
    │   ├── cmdDown (Button)                   // Move structure down
    │   └── cmdDelete (Button)                 // Remove structure
    ├── flpStructureDetails (FlowLayoutPanel)  // Structure-specific properties (e.g., name, level)
    ├── flpStructureCommands (FlowLayoutPanel) // Action buttons (e.g., "Activate", "Upgrade")
    └── flpSelection (FlowLayoutPanel)         // Selection/filtering for child components
        ├── lblSelection (Label)               // e.g., "Select Component"
        └── txtSelectionFilter (TextBox)       // Filter for available components
🔹 Behavioral Notes
flpMovement suggests a tree/list hierarchy of structures (up/down reorder).
rtbStatus allows rich text output (e.g., color-coded logs, multi-line messages).
flpSelection + txtSelectionFilter implies dynamic filtering of selectable items (e.g., blueprints or modules to attach).
📦 3. Resource & Serialization Details
From Citation 3:

Resources may store binary data via .resx.
Supported mimetypes for objects:
application/x-microsoft.net.object.binary.base64
→ Serialized with BinaryFormatter, base64-encoded
application/x-microsoft.net.object.soap.base64
→ Serialized with SoapFormatter, base64-encoded
application/x-microsoft.net.object.bytearray.base64
→ Serialized via TypeConverter → byte array → base64
Critical for AI generation: When regenerating, do not embed binary resources directly. Instead, reference their .resx file or generate placeholder resource keys (e.g., Properties.Resources.MyObjectKey).

✅ Recommendations for AI Regeneration
Use TableLayoutPanel/FlowLayoutPanel containers with appropriate Dock/Anchor properties.
For grids (lvwColonies), prefer DataGridView if columns are defined as DataGridViewTextBoxColumn.
Ensure disposal pattern is implemented: protected override void Dispose(bool disposing) { ... }
Wrap designer code in #region Windows Form Designer generated code (for FormColony) or #region Component Designer generated code (for ColonyStructure).
Use consistent naming: cmdXxx, txtXxx, lblXxx, flpXxx, tlpXxx.
Let me know if you'd like this specification converted to:

A UML diagram
A C# code template (with InitializeComponent() stubs)
A JSON/YAML schema for AI generation tools
I'm happy to adapt!

