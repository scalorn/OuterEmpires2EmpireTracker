using System.Drawing;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Shared colorblind-friendly chart palette (extended Wong palette).
    /// Used by evolution graphs and yield distribution graphs.
    /// </summary>
    public static class ChartColors
    {
        /// <summary>
        /// Extended 16-color Wong palette (8 base + 8 lighter tints) for colorblind-friendly chart lines.
        /// </summary>
        public static readonly Color[] WongPalette = new Color[]
        {
            ColorTranslator.FromHtml("#E69F00"), // orange
            ColorTranslator.FromHtml("#56B4E9"), // sky blue
            ColorTranslator.FromHtml("#009E73"), // bluish green
            ColorTranslator.FromHtml("#B8860B"), // dark goldenrod
            ColorTranslator.FromHtml("#0072B2"), // blue
            ColorTranslator.FromHtml("#D55E00"), // vermillion
            ColorTranslator.FromHtml("#CC79A7"), // reddish purple
            ColorTranslator.FromHtml("#000000"), // black
            ColorTranslator.FromHtml("#808080"), // grey
            ColorTranslator.FromHtml("#F2CF80"), // light orange
            ColorTranslator.FromHtml("#ABD9F4"), // light sky blue
            ColorTranslator.FromHtml("#80CEB9"), // light bluish green
            ColorTranslator.FromHtml("#DAA520"), // goldenrod
            ColorTranslator.FromHtml("#80B8D8"), // light blue
            ColorTranslator.FromHtml("#EAAF80"), // light vermillion
            ColorTranslator.FromHtml("#E5BCD3"), // light reddish purple
        };
    }
}
