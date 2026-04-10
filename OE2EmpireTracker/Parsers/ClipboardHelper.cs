namespace OE2EmpireTracker.Parsers
{
    public static class ClipboardHelper
    {
        /// <summary>
        /// Extracts the HTML fragment from clipboard data by parsing the clipboard header.
        /// Delegates to BlueprintScanner.ExtractHtmlFragmentFromClipboardData.
        /// </summary>
        public static string ExtractHtmlFragment(string clipboardData)
        {
            return OE2EmpireTracker.Forms.Blueprint.BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboardData);
        }
    }
}
