using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// Builds an RTF document string from colored text runs.
    /// Assign the result to RichTextBox.Rtf to update the control in a single operation.
    /// </summary>
    public class RtfBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();

        private readonly List<Color> _colorTable = new List<Color>();

        private readonly Dictionary<Color, int> _colorIndex = new Dictionary<Color, int>();

        public RtfBuilder()
        {
            // Seed with a placeholder so color indices are 1-based
            _colorTable.Add(Color.Empty);
        }

        /// <summary>
        /// Appends a colony structure status summary (Power, Habitation, Food,
        /// Entertainment, Warehouse) with red/green coloring based on deficit state.
        /// </summary>
        public static void AppendColonyStatus(RtfBuilder builder, ColonyStructureStatus status)
        {
            AppendStatusEntry(
                builder,
                "Power:",
                status.PowerRequired > status.PowerProvided ? Color.Red : Color.Green,
                status.PowerRequired,
                status.PowerProvided);
            AppendStatusEntry(
                builder,
                " Habitation: ",
                status.HabitationProvision < status.HabitationRequired ? Color.Red : Color.Green,
                status.HabitationRequired,
                status.HabitationProvision);
            AppendStatusEntry(
                builder,
                " Food: ",
                status.FoodProvision < status.FoodRequired ? Color.Red : Color.Green,
                status.FoodRequired,
                status.FoodProvision);
            AppendStatusEntry(
                builder,
                " Entertainment: ",
                status.EntertainmentProvided < status.EntertainmentRequired ? Color.Red : Color.Green,
                status.EntertainmentRequired,
                status.EntertainmentProvided);
            AppendStatusEntry(
                builder,
                " Warehouse: ",
                status.WarehouseCapacity < status.WarehouseRequired ? Color.Red : Color.Green,
                status.WarehouseRequired,
                status.WarehouseCapacity);
        }

        public void Append(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            int idx = GetColorIndex(color);
            string escaped = text
                .Replace("\\", "\\\\")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("\n", "\\line ");
            _sb.Append(@"\cf").Append(idx).Append(' ').Append(escaped);
        }

        public string ToRtf()
        {
            var header = new StringBuilder();
            header.Append(@"{\rtf1\ansi{\colortbl;");
            foreach (Color c in _colorTable.Skip(1))
            {
                header.Append($@"\red{c.R}\green{c.G}\blue{c.B};");
            }

            header.Append('}');
            header.Append(_sb);
            header.Append('}');
            return header.ToString();
        }

        private static void AppendStatusEntry(RtfBuilder builder, string name, Color color, decimal required, decimal provided)
        {
            builder.Append(name, Color.Black);
            builder.Append(string.Empty + required, required > provided ? Color.Red : Color.Green);
            builder.Append("/", Color.Black);
            builder.Append(string.Empty + provided, Color.Black);
        }

        private int GetColorIndex(Color color)
        {
            int idx;
            if (!_colorIndex.TryGetValue(color, out idx))
            {
                _colorTable.Add(color);
                idx = _colorTable.Count - 1;
                _colorIndex[color] = idx;
            }

            return idx;
        }
    }
}
