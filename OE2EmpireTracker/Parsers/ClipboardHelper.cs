using System;
using System.Text;

namespace OE2EmpireTracker.Parsers
{
    /// <summary>
    /// Shared clipboard HTML extraction utilities used by all parsers.
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// Extracts the HTML fragment from clipboard data by parsing the clipboard header.
        /// Supports both marker-based (StartFragment/EndFragment comments) and byte-offset extraction.
        /// </summary>
        public static string ExtractHtmlFragment(string htmlDataString)
        {
            // HTML Clipboard Format:
            // (https://msdn.microsoft.com/en-us/library/aa767917(v=vs.85).aspx)
            // Prefer marker-based extraction -- encoding-safe and avoids byte-offset mismatch
            const string startMarker = "<!--StartFragment-->";
            const string endMarker = "<!--EndFragment-->";
            int startPos = htmlDataString.IndexOf(startMarker);
            if (startPos >= 0)
            {
                startPos += startMarker.Length;
                int endPos = htmlDataString.IndexOf(endMarker, startPos);
                if (endPos >= 0)
                    return htmlDataString.Substring(startPos, endPos - startPos);
            }

            // Fallback: byte-offset extraction for non-standard clipboard sources
            int startFragmentIndex = htmlDataString.IndexOf("StartFragment:");
            if (startFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }

            startFragmentIndex = Int32.Parse(htmlDataString.Substring(startFragmentIndex + "StartFragment:".Length, 10));
            if (startFragmentIndex < 0 || startFragmentIndex > htmlDataString.Length)
            {
                return "ERROR: Unrecognized html header";
            }

            int endFragmentIndex = htmlDataString.IndexOf("EndFragment:");
            if (endFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }

            endFragmentIndex = Int32.Parse(htmlDataString.Substring(endFragmentIndex + "EndFragment:".Length, 10));
            if (endFragmentIndex > htmlDataString.Length)
            {
                endFragmentIndex = htmlDataString.Length;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(htmlDataString);
            return Encoding.UTF8.GetString(bytes, startFragmentIndex, endFragmentIndex - startFragmentIndex);
        }
    }
}
