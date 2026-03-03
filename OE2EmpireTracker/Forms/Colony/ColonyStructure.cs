using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class ColonyStructure : UserControl
    {
        public ColonyStructure()
        {
            InitializeComponent();
            populateStats();
        }

        void populateStats()
        {
            rtbStatus.Text = "";
            // Power
            AppendColoredText(rtbStatus, "Power: ", Color.Black);
            AppendColoredText(rtbStatus, "12.1", Color.Red);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, "Habitation: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, "Food: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, "Entertainment: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, "Warehouse: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);
        }

        public static void AppendColoredText(RichTextBox box, string text, Color color)
        {
            // Set the selection point to the end of the existing text
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            // Set the color for the text to be appended
            box.SelectionColor = color;

            // Append the new text
            box.AppendText(text);

            // Reset the selection color to the default (e.g., black) for future user input
            box.SelectionColor = box.ForeColor;
        }
    }
}
