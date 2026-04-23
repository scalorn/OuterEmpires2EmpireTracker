using System.IO;
using System.Reflection;
using Markdig;
using NLog;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Loads markdown content from embedded resources and converts it to
    /// a complete HTML document using Markdig.
    /// </summary>
    public static class HelpRenderer
    {
        private const string CssStyle = @"
            body {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                font-size: 14px;
                line-height: 1.6;
                color: #222;
                background: #fff;
                margin: 16px 24px;
                max-width: 960px;
            }

            h1 { font-size: 1.8em; border-bottom: 1px solid #ccc; padding-bottom: 6px; margin-top: 24px; }
            h2 { font-size: 1.4em; border-bottom: 1px solid #eee; padding-bottom: 4px; margin-top: 20px; }
            h3 { font-size: 1.15em; margin-top: 16px; }
            a { color: #0366d6; text-decoration: none; }
            a:hover { text-decoration: underline; }
            code {
                background: #f4f4f4;
                padding: 2px 5px;
                border-radius: 3px;
                font-family: Consolas, 'Courier New', monospace;
                font-size: 0.92em;
            }

            pre {
                background: #f4f4f4;
                padding: 12px;
                border-radius: 4px;
                overflow-x: auto;
                font-family: Consolas, 'Courier New', monospace;
                font-size: 0.92em;
                line-height: 1.45;
            }

            pre code { background: none; padding: 0; }
            table {
                border-collapse: collapse;
                width: 100%;
                margin: 12px 0;
            }

            th, td {
                border: 1px solid #ddd;
                padding: 8px 12px;
                text-align: left;
            }

            th { background: #f0f0f0; font-weight: 600; }
            tr:nth-child(even) { background: #fafafa; }
            blockquote {
                border-left: 4px solid #ddd;
                margin: 12px 0;
                padding: 4px 16px;
                color: #555;
            }

            ul, ol { padding-left: 28px; }
            li { margin-bottom: 4px; }
            hr { border: none; border-top: 1px solid #ddd; margin: 20px 0; }
        ";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly MarkdownPipeline Pipeline =
            new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

        /// <summary>
        /// Converts raw markdown text to a full HTML document string.
        /// </summary>
        public static string RenderMarkdown(string markdownContent)
        {
            string htmlBody = Markdown.ToHtml(markdownContent ?? string.Empty, Pipeline);
            return $"<html><head><style>{CssStyle}</style></head><body>{htmlBody}</body></html>";
        }

        /// <summary>
        /// Loads the embedded resource and converts to a full HTML document string.
        /// Returns null if the resource is not found.
        /// </summary>
        public static string RenderTopic(string resourceFileName)
        {
            string resourceName = "OE2EmpireTracker.docs." + resourceFileName;
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Log.Warn("Embedded resource not found: {0}", resourceName);
                    return null;
                }

                using (var reader = new StreamReader(stream))
                {
                    string markdown = reader.ReadToEnd();
                    return RenderMarkdown(markdown);
                }
            }
        }
    }
}
