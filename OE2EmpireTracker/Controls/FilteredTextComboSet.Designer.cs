namespace OE2EmpireTracker.Controls
{
    partial class FilteredTextComboSet
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.flpBlueprintType = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.cmbBlueprintType = new System.Windows.Forms.ComboBox();
            this.flpBlueprintType.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBlueprintType
            // 
            this.flpBlueprintType.AutoSize = true;
            this.flpBlueprintType.Controls.Add(this.textBox1);
            this.flpBlueprintType.Controls.Add(this.cmbBlueprintType);
            this.flpBlueprintType.Location = new System.Drawing.Point(0, 0);
            this.flpBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintType.Name = "flpBlueprintType";
            this.flpBlueprintType.Size = new System.Drawing.Size(311, 26);
            this.flpBlueprintType.TabIndex = 2;
            this.flpBlueprintType.SizeChanged += new System.EventHandler(this.flpBlueprintType_SizeChanged);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.SizeChanged += new System.EventHandler(this.textBox1_SizeChanged);
            // 
            // cmbBlueprintType
            // 
            this.cmbBlueprintType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbBlueprintType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbBlueprintType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlueprintType.FormattingEnabled = true;
            this.cmbBlueprintType.Location = new System.Drawing.Point(108, 2);
            this.cmbBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBlueprintType.Name = "cmbBlueprintType";
            this.cmbBlueprintType.Size = new System.Drawing.Size(201, 21);
            this.cmbBlueprintType.TabIndex = 3;
            this.cmbBlueprintType.SizeChanged += new System.EventHandler(this.cmbBlueprintType_SizeChanged);
            // 
            // FilteredTextComboSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flpBlueprintType);
            this.Name = "FilteredTextComboSet";
            this.Size = new System.Drawing.Size(313, 75);
            this.AutoSizeChanged += new System.EventHandler(this.FilteredTextComboSet_AutoSizeChanged);
            this.SizeChanged += new System.EventHandler(this.FilteredTextComboSet_SizeChanged);
            this.flpBlueprintType.ResumeLayout(false);
            this.flpBlueprintType.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBlueprintType;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox cmbBlueprintType;
    }
}
