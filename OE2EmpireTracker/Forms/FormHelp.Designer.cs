namespace OE2EmpireTracker.Forms
{
    partial class FormHelp
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.treeViewTopics = new System.Windows.Forms.TreeView();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.SplitterDistance = 250;
            this.splitContainer.TabIndex = 0;
            //
            // splitContainer.Panel1
            //
            this.splitContainer.Panel1.Controls.Add(this.treeViewTopics);
            //
            // splitContainer.Panel2
            //
            this.splitContainer.Panel2.Controls.Add(this.webBrowser);
            //
            // treeViewTopics
            //
            this.treeViewTopics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewTopics.HideSelection = false;
            this.treeViewTopics.Location = new System.Drawing.Point(0, 0);
            this.treeViewTopics.Name = "treeViewTopics";
            this.treeViewTopics.Size = new System.Drawing.Size(250, 561);
            this.treeViewTopics.TabIndex = 0;
            //
            // webBrowser
            //
            this.webBrowser.AllowNavigation = true;
            this.webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser.IsWebBrowserContextMenuEnabled = false;
            this.webBrowser.Location = new System.Drawing.Point(0, 0);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(642, 561);
            this.webBrowser.TabIndex = 0;
            //
            // FormHelp
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.splitContainer);
            this.Name = "FormHelp";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Help";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TreeView treeViewTopics;
        private System.Windows.Forms.WebBrowser webBrowser;
    }
}
