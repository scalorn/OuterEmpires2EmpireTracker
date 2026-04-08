using NUnit.Framework;
using OE2EmpireTracker.Forms.Blueprint;

namespace OE2EmpireTracker.Tests.Blueprint
{
    [TestFixture]
    public class FragmentExtractionTests
    {
        [Test]
        public void ExtractHtmlFragment_WithMarkers_ReturnsContentBetweenMarkers()
        {
            string clipboard =
                "Version:0.9\r\n" +
                "StartHTML:0000000105\r\n" +
                "EndHTML:0000000250\r\n" +
                "StartFragment:0000000141\r\n" +
                "EndFragment:0000000210\r\n" +
                "<html>\r\n" +
                "<body>\r\n" +
                "<!--StartFragment--><p>Hello World</p><!--EndFragment-->\r\n" +
                "</body>\r\n" +
                "</html>";

            string result = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboard);

            Assert.That(result, Is.EqualTo("<p>Hello World</p>"));
        }

        [Test]
        public void ExtractHtmlFragment_WithoutMarkers_FallsBackToByteOffsets()
        {
            // Build a clipboard string WITHOUT <!--StartFragment--> / <!--EndFragment--> markers
            // but WITH valid StartFragment/EndFragment byte-offset headers.
            // For ASCII content, byte offsets == character offsets.
            string header =
                "Version:0.9\r\n" +
                "StartHTML:0000000097\r\n" +
                "EndHTML:0000000153\r\n" +
                "StartFragment:0000000113\r\n" +
                "EndFragment:0000000139\r\n";
            string body =
                "<html>\r\n" +
                "<body>\r\n" +
                "<p>Fallback Test</p>\r\n" +
                "</body>\r\n" +
                "</html>";
            string clipboard = header + body;

            // Compute the actual byte offsets for the substring we want extracted.
            // Target: "<p>Fallback Test</p>"
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(clipboard);
            int startByte = System.Text.Encoding.UTF8.GetByteCount(header + "<html>\r\n<body>\r\n");
            int endByte = startByte + System.Text.Encoding.UTF8.GetByteCount("<p>Fallback Test</p>");

            // Rebuild header with correct offsets
            string correctedHeader =
                "Version:0.9\r\n" +
                "StartHTML:0000000097\r\n" +
                "EndHTML:0000000153\r\n" +
                $"StartFragment:{startByte.ToString("D10")}\r\n" +
                $"EndFragment:{endByte.ToString("D10")}\r\n";
            clipboard = correctedHeader + body;

            string result = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboard);

            Assert.That(result, Is.EqualTo("<p>Fallback Test</p>"));
        }

        [Test]
        public void ExtractHtmlFragment_NoMarkersNoHeader_ReturnsError()
        {
            string clipboard = "<html><body><p>No clipboard header at all</p></body></html>";

            string result = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboard);

            Assert.That(result, Does.StartWith("ERROR"));
        }
    }
}
