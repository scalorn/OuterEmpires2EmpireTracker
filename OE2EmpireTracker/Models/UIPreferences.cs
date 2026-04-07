using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class UIPreferences
    {
        public WindowPosition MainWindow { get; set; }

        public Dictionary<string, Dictionary<string, WindowState>> Forms { get; set; }
            = new Dictionary<string, Dictionary<string, WindowState>>();
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
}
