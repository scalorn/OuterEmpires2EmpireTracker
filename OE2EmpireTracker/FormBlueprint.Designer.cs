using OE2EmpireTracker.Baseline;
using System;

namespace OE2EmpireTracker
{
    partial class FormBlueprint
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblBlueprintType = new System.Windows.Forms.Label();
            this.cmbBlueprintType = new System.Windows.Forms.ComboBox();
            this.flpBlueprintType = new System.Windows.Forms.FlowLayoutPanel();
            this.flpTechLevel = new System.Windows.Forms.FlowLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbTechLevel = new System.Windows.Forms.ComboBox();
            this.flpBaseDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flpClass = new System.Windows.Forms.FlowLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbShipClass = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbEvolution = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel7 = new System.Windows.Forms.FlowLayoutPanel();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbBaseBlueprint = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.tabDetailedData = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel10 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel11 = new System.Windows.Forms.FlowLayoutPanel();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel12 = new System.Windows.Forms.FlowLayoutPanel();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBlueprintType.SuspendLayout();
            this.flpTechLevel.SuspendLayout();
            this.flpBaseDetails.SuspendLayout();
            this.flpClass.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel7.SuspendLayout();
            this.flowLayoutPanel5.SuspendLayout();
            this.flowLayoutPanel6.SuspendLayout();
            this.tabDetailedData.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.flowLayoutPanel10.SuspendLayout();
            this.flowLayoutPanel11.SuspendLayout();
            this.flowLayoutPanel12.SuspendLayout();
            this.flpBase.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblBlueprintType
            // 
            this.lblBlueprintType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBlueprintType.Location = new System.Drawing.Point(2, 4);
            this.lblBlueprintType.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBlueprintType.Name = "lblBlueprintType";
            this.lblBlueprintType.Size = new System.Drawing.Size(100, 17);
            this.lblBlueprintType.TabIndex = 2;
            this.lblBlueprintType.Text = "Blueprint Type";
            this.lblBlueprintType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbBlueprintType
            // 
            this.cmbBlueprintType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbBlueprintType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbBlueprintType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlueprintType.FormattingEnabled = true;
            this.cmbBlueprintType.Location = new System.Drawing.Point(106, 2);
            this.cmbBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBlueprintType.Name = "cmbBlueprintType";
            this.cmbBlueprintType.Size = new System.Drawing.Size(201, 21);
            this.cmbBlueprintType.TabIndex = 1;
            this.cmbBlueprintType.SelectedIndexChanged += new System.EventHandler(this.cmbBlueprintType_SelectedIndexChanged);
            // 
            // flpBlueprintType
            // 
            this.flpBlueprintType.AutoSize = true;
            this.flpBlueprintType.Controls.Add(this.lblBlueprintType);
            this.flpBlueprintType.Controls.Add(this.cmbBlueprintType);
            this.flpBlueprintType.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintType.Name = "flpBlueprintType";
            this.flpBlueprintType.Size = new System.Drawing.Size(309, 25);
            this.flpBlueprintType.TabIndex = 0;
            // 
            // flpTechLevel
            // 
            this.flpTechLevel.AutoSize = true;
            this.flpTechLevel.Controls.Add(this.label2);
            this.flpTechLevel.Controls.Add(this.cmbTechLevel);
            this.flpTechLevel.Location = new System.Drawing.Point(2, 60);
            this.flpTechLevel.Margin = new System.Windows.Forms.Padding(2);
            this.flpTechLevel.Name = "flpTechLevel";
            this.flpTechLevel.Size = new System.Drawing.Size(309, 25);
            this.flpTechLevel.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(2, 4);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Tech Level";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbTechLevel
            // 
            this.cmbTechLevel.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbTechLevel.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbTechLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTechLevel.FormattingEnabled = true;
            this.cmbTechLevel.Location = new System.Drawing.Point(106, 2);
            this.cmbTechLevel.Margin = new System.Windows.Forms.Padding(2);
            this.cmbTechLevel.Name = "cmbTechLevel";
            this.cmbTechLevel.Size = new System.Drawing.Size(201, 21);
            this.cmbTechLevel.TabIndex = 3;
            // 
            // flpBaseDetails
            // 
            this.flpBaseDetails.AutoSize = true;
            this.flpBaseDetails.Controls.Add(this.flpBlueprintType);
            this.flpBaseDetails.Controls.Add(this.flpClass);
            this.flpBaseDetails.Controls.Add(this.flpTechLevel);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel4);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel7);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel5);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel6);
            this.flpBaseDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBaseDetails.Location = new System.Drawing.Point(2, 2);
            this.flpBaseDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseDetails.Name = "flpBaseDetails";
            this.flpBaseDetails.Size = new System.Drawing.Size(313, 201);
            this.flpBaseDetails.TabIndex = 6;
            // 
            // flpClass
            // 
            this.flpClass.AutoSize = true;
            this.flpClass.Controls.Add(this.label7);
            this.flpClass.Controls.Add(this.cmbShipClass);
            this.flpClass.Location = new System.Drawing.Point(2, 31);
            this.flpClass.Margin = new System.Windows.Forms.Padding(2);
            this.flpClass.Name = "flpClass";
            this.flpClass.Size = new System.Drawing.Size(309, 25);
            this.flpClass.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.Location = new System.Drawing.Point(2, 4);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 17);
            this.label7.TabIndex = 2;
            this.label7.Text = "Class";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbShipClass
            // 
            this.cmbShipClass.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbShipClass.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbShipClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbShipClass.FormattingEnabled = true;
            this.cmbShipClass.Location = new System.Drawing.Point(106, 2);
            this.cmbShipClass.Margin = new System.Windows.Forms.Padding(2);
            this.cmbShipClass.Name = "cmbShipClass";
            this.cmbShipClass.Size = new System.Drawing.Size(201, 21);
            this.cmbShipClass.TabIndex = 1;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.Controls.Add(this.label3);
            this.flowLayoutPanel4.Controls.Add(this.cmbEvolution);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(2, 89);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(309, 25);
            this.flowLayoutPanel4.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(2, 4);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Evolution";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbEvolution
            // 
            this.cmbEvolution.FormattingEnabled = true;
            this.cmbEvolution.Location = new System.Drawing.Point(106, 2);
            this.cmbEvolution.Margin = new System.Windows.Forms.Padding(2);
            this.cmbEvolution.Name = "cmbEvolution";
            this.cmbEvolution.Size = new System.Drawing.Size(201, 21);
            this.cmbEvolution.TabIndex = 4;
            // 
            // flowLayoutPanel7
            // 
            this.flowLayoutPanel7.AutoSize = true;
            this.flowLayoutPanel7.Controls.Add(this.label6);
            this.flowLayoutPanel7.Controls.Add(this.cmbBaseBlueprint);
            this.flowLayoutPanel7.Location = new System.Drawing.Point(2, 118);
            this.flowLayoutPanel7.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel7.Name = "flowLayoutPanel7";
            this.flowLayoutPanel7.Size = new System.Drawing.Size(309, 25);
            this.flowLayoutPanel7.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Location = new System.Drawing.Point(2, 4);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 17);
            this.label6.TabIndex = 2;
            this.label6.Text = "Base Blueprint";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbBaseBlueprint
            // 
            this.cmbBaseBlueprint.FormattingEnabled = true;
            this.cmbBaseBlueprint.Location = new System.Drawing.Point(106, 2);
            this.cmbBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBaseBlueprint.Name = "cmbBaseBlueprint";
            this.cmbBaseBlueprint.Size = new System.Drawing.Size(201, 21);
            this.cmbBaseBlueprint.TabIndex = 5;
            // 
            // flowLayoutPanel5
            // 
            this.flowLayoutPanel5.AutoSize = true;
            this.flowLayoutPanel5.Controls.Add(this.label4);
            this.flowLayoutPanel5.Controls.Add(this.txtName);
            this.flowLayoutPanel5.Location = new System.Drawing.Point(2, 147);
            this.flowLayoutPanel5.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Size = new System.Drawing.Size(309, 24);
            this.flowLayoutPanel5.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(2, 3);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 17);
            this.label4.TabIndex = 2;
            this.label4.Text = "Name";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(106, 2);
            this.txtName.Margin = new System.Windows.Forms.Padding(2);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(201, 20);
            this.txtName.TabIndex = 0;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel6.AutoSize = true;
            this.flowLayoutPanel6.Controls.Add(this.label5);
            this.flowLayoutPanel6.Controls.Add(this.txtDescription);
            this.flowLayoutPanel6.Location = new System.Drawing.Point(2, 175);
            this.flowLayoutPanel6.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            this.flowLayoutPanel6.Size = new System.Drawing.Size(309, 24);
            this.flowLayoutPanel6.TabIndex = 6;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.Location = new System.Drawing.Point(2, 3);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 17);
            this.label5.TabIndex = 2;
            this.label5.Text = "Description";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(106, 2);
            this.txtDescription.Margin = new System.Windows.Forms.Padding(2);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(201, 20);
            this.txtDescription.TabIndex = 7;
            // 
            // tabDetailedData
            // 
            this.tabDetailedData.Controls.Add(this.tabPage1);
            this.tabDetailedData.Controls.Add(this.tabPage2);
            this.tabDetailedData.Location = new System.Drawing.Point(2, 207);
            this.tabDetailedData.Margin = new System.Windows.Forms.Padding(2);
            this.tabDetailedData.Name = "tabDetailedData";
            this.tabDetailedData.SelectedIndex = 0;
            this.tabDetailedData.Size = new System.Drawing.Size(834, 329);
            this.tabDetailedData.TabIndex = 7;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dataGridView1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage1.Size = new System.Drawing.Size(826, 303);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Statistics";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3});
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(826, 300);
            this.dataGridView1.TabIndex = 0;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Property";
            this.Column1.Name = "Column1";
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Base Value";
            this.Column2.Name = "Column2";
            // 
            // Column3
            // 
            this.Column3.HeaderText = "Current Value";
            this.Column3.Name = "Column3";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.flowLayoutPanel10);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage2.Size = new System.Drawing.Size(826, 303);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Required Resources";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel10
            // 
            this.flowLayoutPanel10.Controls.Add(this.flowLayoutPanel11);
            this.flowLayoutPanel10.Controls.Add(this.flowLayoutPanel12);
            this.flowLayoutPanel10.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel10.Location = new System.Drawing.Point(-1, 1);
            this.flowLayoutPanel10.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel10.Name = "flowLayoutPanel10";
            this.flowLayoutPanel10.Size = new System.Drawing.Size(696, 175);
            this.flowLayoutPanel10.TabIndex = 8;
            // 
            // flowLayoutPanel11
            // 
            this.flowLayoutPanel11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel11.AutoSize = true;
            this.flowLayoutPanel11.Controls.Add(this.label8);
            this.flowLayoutPanel11.Controls.Add(this.textBox7);
            this.flowLayoutPanel11.Location = new System.Drawing.Point(2, 2);
            this.flowLayoutPanel11.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel11.Name = "flowLayoutPanel11";
            this.flowLayoutPanel11.Size = new System.Drawing.Size(547, 24);
            this.flowLayoutPanel11.TabIndex = 8;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.Location = new System.Drawing.Point(2, 3);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(133, 17);
            this.label8.TabIndex = 2;
            this.label8.Text = "Manufacture Run Time";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox7
            // 
            this.textBox7.Location = new System.Drawing.Point(139, 2);
            this.textBox7.Margin = new System.Windows.Forms.Padding(2);
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(201, 20);
            this.textBox7.TabIndex = 7;
            // 
            // flowLayoutPanel12
            // 
            this.flowLayoutPanel12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel12.AutoSize = true;
            this.flowLayoutPanel12.Controls.Add(this.label9);
            this.flowLayoutPanel12.Controls.Add(this.textBox8);
            this.flowLayoutPanel12.Controls.Add(this.textBox9);
            this.flowLayoutPanel12.Location = new System.Drawing.Point(2, 30);
            this.flowLayoutPanel12.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel12.Name = "flowLayoutPanel12";
            this.flowLayoutPanel12.Size = new System.Drawing.Size(547, 24);
            this.flowLayoutPanel12.TabIndex = 9;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.Location = new System.Drawing.Point(2, 3);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(133, 17);
            this.label9.TabIndex = 2;
            this.label9.Text = "Mass";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox8
            // 
            this.textBox8.Location = new System.Drawing.Point(139, 2);
            this.textBox8.Margin = new System.Windows.Forms.Padding(2);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(201, 20);
            this.textBox8.TabIndex = 7;
            // 
            // textBox9
            // 
            this.textBox9.Location = new System.Drawing.Point(344, 2);
            this.textBox9.Margin = new System.Windows.Forms.Padding(2);
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new System.Drawing.Size(201, 20);
            this.textBox9.TabIndex = 8;
            // 
            // flpBase
            // 
            this.flpBase.AutoSize = true;
            this.flpBase.Controls.Add(this.flpBaseDetails);
            this.flpBase.Controls.Add(this.tabDetailedData);
            this.flpBase.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(844, 579);
            this.flpBase.TabIndex = 9;
            // 
            // FormBlueprint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(845, 580);
            this.Controls.Add(this.flpBase);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormBlueprint";
            this.Text = "Blueprint";
            this.flpBlueprintType.ResumeLayout(false);
            this.flpTechLevel.ResumeLayout(false);
            this.flpBaseDetails.ResumeLayout(false);
            this.flpBaseDetails.PerformLayout();
            this.flpClass.ResumeLayout(false);
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel7.ResumeLayout(false);
            this.flowLayoutPanel5.ResumeLayout(false);
            this.flowLayoutPanel5.PerformLayout();
            this.flowLayoutPanel6.ResumeLayout(false);
            this.flowLayoutPanel6.PerformLayout();
            this.tabDetailedData.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.flowLayoutPanel10.ResumeLayout(false);
            this.flowLayoutPanel10.PerformLayout();
            this.flowLayoutPanel11.ResumeLayout(false);
            this.flowLayoutPanel11.PerformLayout();
            this.flowLayoutPanel12.ResumeLayout(false);
            this.flowLayoutPanel12.PerformLayout();
            this.flpBase.ResumeLayout(false);
            this.flpBase.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblBlueprintType;
        private System.Windows.Forms.ComboBox cmbBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpTechLevel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbTechLevel;
        private System.Windows.Forms.FlowLayoutPanel flpBaseDetails;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.TabControl tabDetailedData;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel10;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel11;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox7;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel12;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox8;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbBaseBlueprint;
        private System.Windows.Forms.ComboBox cmbEvolution;
        private System.Windows.Forms.FlowLayoutPanel flpClass;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbShipClass;
        private System.Windows.Forms.FlowLayoutPanel flpBase;
    }
}