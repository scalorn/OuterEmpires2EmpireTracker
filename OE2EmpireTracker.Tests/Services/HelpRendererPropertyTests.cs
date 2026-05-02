using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class HelpRendererPropertyTests
    {
        // Feature: user-help-docs, Property 1: Rendered HTML has complete document structure
        /// <summary>
        /// Property 1: For any non-null markdown string, RenderMarkdown() produces
        /// an HTML string containing &lt;html&gt;, &lt;head&gt;, &lt;style, and &lt;body&gt;.
        /// **Validates: Requirements 2.3, 7.1, 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property RenderMarkdown_AlwaysProducesCompleteHtmlStructure()
        {
            return Prop.ForAll(
                Arb.From<NonNull<string>>(),
                input =>
                {
                    var html = HelpRenderer.RenderMarkdown(input.Get);

                    return (html.Contains("<html>")
                            && html.Contains("<head>")
                            && html.Contains("<style")
                            && html.Contains("<body>"))
                        .Label($"HTML structure incomplete for input length {input.Get.Length}");
                });
        }

        // Feature: user-help-docs, Property 4: Markdown rendering preserves content
        /// <summary>
        /// Property 4: For any alphanumeric word embedded in a markdown paragraph,
        /// the rendered HTML output contains that same word.
        /// **Validates: Requirements 2.3, 7.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property RenderMarkdown_PreservesPlainTextContent()
        {
            // Generate random alphanumeric words (1-50 chars) to avoid HTML encoding issues
            var alphanumGen = Gen.ArrayOf(
                    Gen.Elements(
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
                            .ToCharArray()))
                .Where(arr => arr.Length > 0)
                .Select(arr => new string(arr));

            return Prop.ForAll(
                alphanumGen.ToArbitrary(),
                word =>
                {
                    var markdown = $"This is a paragraph with {word} in it.";
                    var html = HelpRenderer.RenderMarkdown(markdown);

                    return html.Contains(word)
                        .Label($"Word '{word}' not found in rendered HTML");
                });
        }
    }
}
