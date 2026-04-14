using OE2EmpireTracker.Services;
using System;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms
{
    public partial class FormHelp : Form
    {
        private readonly string _initialTopic;

        public FormHelp(string initialTopic = null)
        {
            _initialTopic = initialTopic;
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var topics = HelpTopicRegistry.GetAllTopics();
            TreeNode selectNode = null;

            foreach (var topic in topics)
            {
                var node = new TreeNode(topic.DisplayName) { Tag = topic.FileName };
                treeViewTopics.Nodes.Add(node);

                if (_initialTopic != null && topic.FileName == _initialTopic)
                    selectNode = node;
            }

            treeViewTopics.AfterSelect += treeViewTopics_AfterSelect;
            webBrowser.Navigating += webBrowser_Navigating;

            if (selectNode != null)
                treeViewTopics.SelectedNode = selectNode;
            else if (treeViewTopics.Nodes.Count > 0)
                treeViewTopics.SelectedNode = treeViewTopics.Nodes[0];
        }

        private void treeViewTopics_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string fileName = e.Node.Tag as string;
            if (fileName == null) return;

            string html = HelpRenderer.RenderTopic(fileName);
            if (html != null)
                webBrowser.DocumentText = html;
            else
                webBrowser.DocumentText = HelpRenderer.RenderMarkdown("# Topic Not Available\n\nThe requested help topic could not be found.");
        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.ToString();

            // Allow about:blank -- this is how DocumentText works internally
            if (url == "about:blank")
                return;

            // Handle internal .md links
            if (url.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;

                // Extract just the filename from the URL
                string fileName = url;
                int lastSlash = url.LastIndexOf('/');
                if (lastSlash >= 0)
                    fileName = url.Substring(lastSlash + 1);

                string html = HelpRenderer.RenderTopic(fileName);
                if (html != null)
                    webBrowser.DocumentText = html;
                else
                    webBrowser.DocumentText = HelpRenderer.RenderMarkdown("# Topic Not Available\n\nThe requested help topic could not be found.");

                // Select the matching tree node if one exists
                foreach (TreeNode node in treeViewTopics.Nodes)
                {
                    if (string.Equals(node.Tag as string, fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        treeViewTopics.SelectedNode = node;
                        break;
                    }
                }

                return;
            }

            // Block all other navigation (external URLs)
            e.Cancel = true;
        }
    }
}
