using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class UIPreferences
    {
        public WindowPosition MainWindow { get; set; }

        public Dictionary<string, Dictionary<string, WindowState>> Forms { get; set; }
            = new Dictionary<string, Dictionary<string, WindowState>>();

        public List<string> OpenForms { get; set; } = new List<string>();
        public List<OpenFormEntry> OpenFormEntries { get; set; } = new List<OpenFormEntry>();

        public ThresholdPreferences Thresholds { get; set; } = new ThresholdPreferences();
    }

    public class ThresholdPreferences
    {
        public int StructureCountYellow { get; set; } = 60;
        public int StructureCountRed { get; set; } = 66;
        public long WorkerRequestYellowSeconds { get; set; } = 172800;  // 2 days
        public long WorkerRequestRedSeconds { get; set; } = 86400;      // 1 day
        public long ColonyImportStalenessYellowSeconds { get; set; } = 432000; // 5 days
        public long ColonyImportStalenessRedSeconds { get; set; } = 518400;    // 6 days
        public long BackgroundProcessingIntervalSeconds { get; set; } = 60;
        public long AdminRefreshIntervalSeconds { get; set; } = 60;
        public long CountdownRefreshRateSeconds { get; set; } = 1;
    }

    public class OpenFormEntry
    {
        public string TypeName { get; set; }
        public int WindowNumber { get; set; }
    }

    public class WindowPosition
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WindowState
    {
        public WindowPosition Position { get; set; }
        public FormControlState FormState { get; set; }
    }

    public class FormControlState
    {
        public Dictionary<string, string> FilterTexts { get; set; }
            = new Dictionary<string, string>();

        public Dictionary<string, bool> CheckStates { get; set; }
            = new Dictionary<string, bool>();

        public Dictionary<string, ComboState> ComboSelections { get; set; }
            = new Dictionary<string, ComboState>();

        public Dictionary<string, GridState> Grids { get; set; }
            = new Dictionary<string, GridState>();

        public Dictionary<string, ListViewState> ListViews { get; set; }
            = new Dictionary<string, ListViewState>();
    }

    public class ComboState
    {
        public string SelectedValue { get; set; }
        public int SelectedIndex { get; set; }
    }

    public class GridState
    {
        public Dictionary<string, GridColumnState> Columns { get; set; }
            = new Dictionary<string, GridColumnState>();

        public string SortColumnName { get; set; }
        public string SortDirection { get; set; }
    }

    public class GridColumnState
    {
        public int Width { get; set; }
        public int DisplayIndex { get; set; }
    }

    public class ListViewState
    {
        public Dictionary<int, int> ColumnWidths { get; set; }
            = new Dictionary<int, int>();

        public int SortColumn { get; set; } = -1;
        public string SortDirection { get; set; }
    }
}
