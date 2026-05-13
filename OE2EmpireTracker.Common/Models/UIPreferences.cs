using System.Collections.Generic;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Models
{
    public class UIPreferences
    {
        public WindowPosition MainWindow { get; set; }

        public Dictionary<string, Dictionary<string, WindowState>> Forms { get; set; }
            = new Dictionary<string, Dictionary<string, WindowState>>();

        public List<OpenFormEntry> OpenFormEntries { get; set; } = new List<OpenFormEntry>();

        public ThresholdPreferences Thresholds { get; set; } = new ThresholdPreferences();

        /// <summary>
        /// Gets or sets the server connection settings (URL, token, thumbprint, mode).
        /// </summary>
        public ServerConnectionSettings ServerConnection { get; set; } = new ServerConnectionSettings();

        /// <summary>
        /// Time horizon in hours for flatpack auto-fill. 0 = include all unbuilt structures.
        /// When > 0, only includes structures whose build will complete within this many hours.
        /// </summary>
        public int FlatpackTimeHorizonHours { get; set; } = 0;
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

        /// <summary>
        /// Validates a ThresholdPreferences instance. Returns true with error=null if valid,
        /// false with a descriptive error message if invalid.
        /// </summary>
        public static bool Validate(ThresholdPreferences prefs, out string error)
        {
            if (prefs.StructureCountYellow <= 0)
            {
                error = "Structure Count Yellow threshold must be a positive number.";
                return false;
            }

            if (prefs.StructureCountRed <= 0)
            {
                error = "Structure Count Red threshold must be a positive number.";
                return false;
            }

            if (prefs.WorkerRequestYellowSeconds <= 0)
            {
                error = "Worker Request Yellow threshold must be a positive number of seconds.";
                return false;
            }

            if (prefs.WorkerRequestRedSeconds <= 0)
            {
                error = "Worker Request Red threshold must be a positive number of seconds.";
                return false;
            }

            if (prefs.ColonyImportStalenessYellowSeconds <= 0)
            {
                error = "Colony Import Staleness Yellow threshold must be a positive number of seconds.";
                return false;
            }

            if (prefs.ColonyImportStalenessRedSeconds <= 0)
            {
                error = "Colony Import Staleness Red threshold must be a positive number of seconds.";
                return false;
            }

            if (prefs.BackgroundProcessingIntervalSeconds <= 0)
            {
                error = "Background Processing Interval must be a positive number of seconds.";
                return false;
            }

            if (prefs.AdminRefreshIntervalSeconds <= 0)
            {
                error = "Admin Refresh Interval must be a positive number of seconds.";
                return false;
            }

            if (prefs.CountdownRefreshRateSeconds <= 0)
            {
                error = "Countdown Refresh Rate must be a positive number of seconds.";
                return false;
            }

            if (prefs.StructureCountYellow >= prefs.StructureCountRed)
            {
                error = "Structure Count Yellow threshold must be less than Red threshold.";
                return false;
            }

            if (prefs.WorkerRequestYellowSeconds <= prefs.WorkerRequestRedSeconds)
            {
                error = "Worker Request Yellow threshold must be greater than Red threshold (yellow triggers at a larger remaining window).";
                return false;
            }

            if (prefs.ColonyImportStalenessYellowSeconds >= prefs.ColonyImportStalenessRedSeconds)
            {
                error = "Colony Import Staleness Yellow threshold must be less than Red threshold.";
                return false;
            }

            if (prefs.CountdownRefreshRateSeconds < 1)
            {
                error = "Countdown Refresh Rate must be at least 1 second.";
                return false;
            }

            error = null;
            return true;
        }
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

        public Dictionary<string, int> SplitterDistances { get; set; }
            = new Dictionary<string, int>();

        public Dictionary<string, int> TabSelectedIndices { get; set; }
            = new Dictionary<string, int>();
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

        /// <summary>
        /// For CheckBoxes ListViews: Tag values of unchecked items.
        /// Empty list means all checked (the default).
        /// </summary>
        public List<string> UncheckedItems { get; set; }
            = new List<string>();
    }
}
