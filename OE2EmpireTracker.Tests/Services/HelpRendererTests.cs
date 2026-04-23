using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class HelpRendererTests
    {
        // Feature: user-help-docs, Property 3: All registered topics render successfully
        /// <summary>
        /// Iterates all topics from HelpTopicRegistry.GetAllTopics(), calls
        /// RenderTopic for each, and asserts the result is non-null and non-empty.
        /// **Validates: Requirements 3.3, 4.5**
        /// </summary>
        [Test]
        public void RenderTopic_AllRegisteredTopics_ReturnNonNull()
        {
            var topics = HelpTopicRegistry.GetAllTopics();

            foreach (var (displayName, fileName) in topics)
            {
                var html = HelpRenderer.RenderTopic(fileName);
                Assert.That(
                    html,
                    Is.Not.Null,
                    $"RenderTopic returned null for topic '{displayName}' ({fileName})");
                Assert.That(
                    html,
                    Is.Not.Empty,
                    $"RenderTopic returned empty string for topic '{displayName}' ({fileName})");
            }
        }

        /// <summary>
        /// Passing a non-existent resource filename returns null.
        /// **Validates: Requirements 3.4**
        /// </summary>
        [Test]
        public void RenderTopic_UnknownResource_ReturnsNull()
        {
            var result = HelpRenderer.RenderTopic("nonexistent-topic.md");
            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// A markdown heading (# Heading) renders to an &lt;h1&gt; tag.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void RenderMarkdown_Headings_ProducesHTag()
        {
            var html = HelpRenderer.RenderMarkdown("# Heading");
            Assert.That(html, Does.Contain("<h1"));
        }

        /// <summary>
        /// Bold markdown (**bold**) renders to a &lt;strong&gt; tag.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void RenderMarkdown_Bold_ProducesStrongTag()
        {
            var html = HelpRenderer.RenderMarkdown("**bold**");
            Assert.That(html, Does.Contain("<strong>"));
        }

        /// <summary>
        /// A fenced code block renders to a &lt;pre&gt; tag.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void RenderMarkdown_CodeBlock_ProducesPreTag()
        {
            var markdown = "```\nvar x = 1;\n```";
            var html = HelpRenderer.RenderMarkdown(markdown);
            Assert.That(html, Does.Contain("<pre>"));
        }

        /// <summary>
        /// A pipe table renders to a &lt;table&gt; tag.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void RenderMarkdown_Table_ProducesTableTag()
        {
            var markdown = "| Col1 | Col2 |\n|------|------|\n| A    | B    |";
            var html = HelpRenderer.RenderMarkdown(markdown);
            Assert.That(html, Does.Contain("<table>"));
        }

        /// <summary>
        /// An unordered list renders to &lt;ul&gt; and &lt;li&gt; tags.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void RenderMarkdown_List_ProducesListTags()
        {
            var markdown = "- item one\n- item two";
            var html = HelpRenderer.RenderMarkdown(markdown);
            Assert.That(html, Does.Contain("<ul>"));
            Assert.That(html, Does.Contain("<li>"));
        }
    }
}
