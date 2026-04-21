using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
            LoadAssemblyInfo();
        }

        private void LoadAssemblyInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var titleAttr = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyTitleAttribute));
            lblAppName.Text = titleAttr != null ? titleAttr.Title : "OE2 Empire Tracker";

            lblVersion.Text = "Version " + assembly.GetName().Version.ToString();

            var copyrightAttr = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyCopyrightAttribute));
            lblCopyright.Text = copyrightAttr != null ? copyrightAttr.Copyright : string.Empty;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnkGame_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://outerempires.net/");
        }

        private void lnkKiro_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://kiro.dev/");
        }

        private void lnkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/scalorn/OuterEmpires2EmpireTracker");
        }
    }
}
