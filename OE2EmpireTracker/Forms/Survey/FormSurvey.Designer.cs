namespace OE2EmpireTracker.Forms.Survey
{
    partial class FormSurvey
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
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBlueprintSearch = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanetFilter = new System.Windows.Forms.Label();
            this.txtPlanetFilter = new System.Windows.Forms.TextBox();
            this.flpResource = new System.Windows.Forms.FlowLayoutPanel();
            this.lblResource = new System.Windows.Forms.Label();
            this.cmbResource = new System.Windows.Forms.ComboBox();
            this.lvwSurveys = new System.Windows.Forms.ListView();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBaseDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBlueprintType = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprintType = new System.Windows.Forms.Label();
            this.txtFilterBlueprintType = new System.Windows.Forms.TextBox();
            this.flpBaseBlueprint = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBaseBlueprint = new System.Windows.Forms.Label();
            this.txtFilterBaseBlueprint = new System.Windows.Forms.TextBox();
            this.cmbBaseBlueprint = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.flpNickName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNickName = new System.Windows.Forms.Label();
            this.txtNickName = new System.Windows.Forms.TextBox();
            this.dgvResources = new System.Windows.Forms.DataGridView();
            this.Resource = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Amount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpBlueprintSearch.SuspendLayout();
            this.flpResource.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flpBaseDetails.SuspendLayout();
            this.flpBlueprintType.SuspendLayout();
            this.flpBaseBlueprint.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flpNickName.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).BeginInit();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.AutoSize = true;
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flowLayoutPanel1);
            this.flpBase.Location = new System.Drawing.Point(-150, -98);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1100, 647);
            this.flpBase.TabIndex = 10;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpBlueprintSearch);
            this.flpSearchList.Controls.Add(this.flpResource);
            this.flpSearchList.Controls.Add(this.lvwSurveys);
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(427, 641);
            this.flpSearchList.TabIndex = 10;
            // 
            // flpBlueprintSearch
            // 
            this.flpBlueprintSearch.AutoSize = true;
            this.flpBlueprintSearch.Controls.Add(this.lblPlanetFilter);
            this.flpBlueprintSearch.Controls.Add(this.txtPlanetFilter);
            this.flpBlueprintSearch.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintSearch.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintSearch.Name = "flpBlueprintSearch";
            this.flpBlueprintSearch.Size = new System.Drawing.Size(210, 26);
            this.flpBlueprintSearch.TabIndex = 5;
            // 
            // lblPlanetFilter
            // 
            this.lblPlanetFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPlanetFilter.Location = new System.Drawing.Point(2, 4);
            this.lblPlanetFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPlanetFilter.Name = "lblPlanetFilter";
            this.lblPlanetFilter.Size = new System.Drawing.Size(100, 17);
            this.lblPlanetFilter.TabIndex = 2;
            this.lblPlanetFilter.Text = "Planet";
            this.lblPlanetFilter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlanetFilter
            // 
            this.txtPlanetFilter.Location = new System.Drawing.Point(107, 3);
            this.txtPlanetFilter.Name = "txtPlanetFilter";
            this.txtPlanetFilter.Size = new System.Drawing.Size(100, 20);
            this.txtPlanetFilter.TabIndex = 0;
            // 
            // flpResource
            // 
            this.flpResource.AutoSize = true;
            this.flpResource.Controls.Add(this.lblResource);
            this.flpResource.Controls.Add(this.cmbResource);
            this.flpResource.Location = new System.Drawing.Point(2, 32);
            this.flpResource.Margin = new System.Windows.Forms.Padding(2);
            this.flpResource.Name = "flpResource";
            this.flpResource.Size = new System.Drawing.Size(309, 25);
            this.flpResource.TabIndex = 7;
            // 
            // lblResource
            // 
            this.lblResource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblResource.Location = new System.Drawing.Point(2, 4);
            this.lblResource.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblResource.Name = "lblResource";
            this.lblResource.Size = new System.Drawing.Size(100, 17);
            this.lblResource.TabIndex = 2;
            this.lblResource.Text = "Resource";
            this.lblResource.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbResource
            // 
            this.cmbResource.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbResource.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbResource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResource.FormattingEnabled = true;
            this.cmbResource.Location = new System.Drawing.Point(106, 2);
            this.cmbResource.Margin = new System.Windows.Forms.Padding(2);
            this.cmbResource.Name = "cmbResource";
            this.cmbResource.Size = new System.Drawing.Size(201, 21);
            this.cmbResource.TabIndex = 1;
            // 
            // lvwSurveys
            // 
            this.lvwSurveys.FullRowSelect = true;
            this.lvwSurveys.HideSelection = false;
            this.lvwSurveys.Location = new System.Drawing.Point(3, 62);
            this.lvwSurveys.MultiSelect = false;
            this.lvwSurveys.Name = "lvwSurveys";
            this.lvwSurveys.Size = new System.Drawing.Size(412, 566);
            this.lvwSurveys.TabIndex = 6;
            this.lvwSurveys.UseCompatibleStateImageBehavior = false;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.flpBaseDetails);
            this.flowLayoutPanel1.Controls.Add(this.flpCommands);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(436, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(661, 641);
            this.flowLayoutPanel1.TabIndex = 10;
            // 
            // flpBaseDetails
            // 
            this.flpBaseDetails.AutoSize = true;
            this.flpBaseDetails.Controls.Add(this.flpBlueprintType);
            this.flpBaseDetails.Controls.Add(this.flpBaseBlueprint);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel4);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel3);
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel2);
            this.flpBaseDetails.Controls.Add(this.flpNickName);
            this.flpBaseDetails.Controls.Add(this.dgvResources);
            this.flpBaseDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBaseDetails.Location = new System.Drawing.Point(2, 2);
            this.flpBaseDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseDetails.Name = "flpBaseDetails";
            this.flpBaseDetails.Size = new System.Drawing.Size(831, 481);
            this.flpBaseDetails.TabIndex = 6;
            // 
            // flpBlueprintType
            // 
            this.flpBlueprintType.AutoSize = true;
            this.flpBlueprintType.Controls.Add(this.lblBlueprintType);
            this.flpBlueprintType.Controls.Add(this.txtFilterBlueprintType);
            this.flpBlueprintType.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintType.Name = "flpBlueprintType";
            this.flpBlueprintType.Size = new System.Drawing.Size(210, 26);
            this.flpBlueprintType.TabIndex = 0;
            // 
            // lblBlueprintType
            // 
            this.lblBlueprintType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBlueprintType.Location = new System.Drawing.Point(2, 4);
            this.lblBlueprintType.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBlueprintType.Name = "lblBlueprintType";
            this.lblBlueprintType.Size = new System.Drawing.Size(100, 17);
            this.lblBlueprintType.TabIndex = 2;
            this.lblBlueprintType.Text = "Planet Name";
            this.lblBlueprintType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFilterBlueprintType
            // 
            this.txtFilterBlueprintType.Location = new System.Drawing.Point(107, 3);
            this.txtFilterBlueprintType.Name = "txtFilterBlueprintType";
            this.txtFilterBlueprintType.Size = new System.Drawing.Size(100, 20);
            this.txtFilterBlueprintType.TabIndex = 0;
            // 
            // flpBaseBlueprint
            // 
            this.flpBaseBlueprint.AutoSize = true;
            this.flpBaseBlueprint.Controls.Add(this.lblBaseBlueprint);
            this.flpBaseBlueprint.Controls.Add(this.txtFilterBaseBlueprint);
            this.flpBaseBlueprint.Controls.Add(this.cmbBaseBlueprint);
            this.flpBaseBlueprint.Location = new System.Drawing.Point(2, 32);
            this.flpBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseBlueprint.Name = "flpBaseBlueprint";
            this.flpBaseBlueprint.Size = new System.Drawing.Size(415, 26);
            this.flpBaseBlueprint.TabIndex = 4;
            // 
            // lblBaseBlueprint
            // 
            this.lblBaseBlueprint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBaseBlueprint.Location = new System.Drawing.Point(2, 4);
            this.lblBaseBlueprint.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBaseBlueprint.Name = "lblBaseBlueprint";
            this.lblBaseBlueprint.Size = new System.Drawing.Size(100, 17);
            this.lblBaseBlueprint.TabIndex = 2;
            this.lblBaseBlueprint.Text = "Scanner Blueprint";
            this.lblBaseBlueprint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFilterBaseBlueprint
            // 
            this.txtFilterBaseBlueprint.Location = new System.Drawing.Point(107, 3);
            this.txtFilterBaseBlueprint.Name = "txtFilterBaseBlueprint";
            this.txtFilterBaseBlueprint.Size = new System.Drawing.Size(100, 20);
            this.txtFilterBaseBlueprint.TabIndex = 0;
            // 
            // cmbBaseBlueprint
            // 
            this.cmbBaseBlueprint.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbBaseBlueprint.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbBaseBlueprint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBaseBlueprint.FormattingEnabled = true;
            this.cmbBaseBlueprint.Location = new System.Drawing.Point(212, 2);
            this.cmbBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBaseBlueprint.Name = "cmbBaseBlueprint";
            this.cmbBaseBlueprint.Size = new System.Drawing.Size(201, 21);
            this.cmbBaseBlueprint.TabIndex = 1;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.Controls.Add(this.label3);
            this.flowLayoutPanel4.Controls.Add(this.textBox3);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(2, 62);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(827, 24);
            this.flowLayoutPanel4.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(2, 3);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Scan DateTime";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(106, 2);
            this.textBox3.Margin = new System.Windows.Forms.Padding(2);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(201, 20);
            this.textBox3.TabIndex = 7;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.Controls.Add(this.label2);
            this.flowLayoutPanel3.Controls.Add(this.textBox2);
            this.flowLayoutPanel3.Location = new System.Drawing.Point(2, 90);
            this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(827, 24);
            this.flowLayoutPanel3.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(2, 3);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Sensor Abundance Factor";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(106, 2);
            this.textBox2.Margin = new System.Windows.Forms.Padding(2);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(201, 20);
            this.textBox2.TabIndex = 7;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.Controls.Add(this.label1);
            this.flowLayoutPanel2.Controls.Add(this.textBox1);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(2, 118);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(827, 24);
            this.flowLayoutPanel2.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(2, 3);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Purity Modifier";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(106, 2);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(201, 20);
            this.textBox1.TabIndex = 7;
            // 
            // flpNickName
            // 
            this.flpNickName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpNickName.AutoSize = true;
            this.flpNickName.Controls.Add(this.lblNickName);
            this.flpNickName.Controls.Add(this.txtNickName);
            this.flpNickName.Location = new System.Drawing.Point(2, 146);
            this.flpNickName.Margin = new System.Windows.Forms.Padding(2);
            this.flpNickName.Name = "flpNickName";
            this.flpNickName.Size = new System.Drawing.Size(827, 24);
            this.flpNickName.TabIndex = 6;
            // 
            // lblNickName
            // 
            this.lblNickName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNickName.Location = new System.Drawing.Point(2, 3);
            this.lblNickName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblNickName.Name = "lblNickName";
            this.lblNickName.Size = new System.Drawing.Size(100, 17);
            this.lblNickName.TabIndex = 2;
            this.lblNickName.Text = "Scan Level";
            this.lblNickName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtNickName
            // 
            this.txtNickName.Location = new System.Drawing.Point(106, 2);
            this.txtNickName.Margin = new System.Windows.Forms.Padding(2);
            this.txtNickName.Name = "txtNickName";
            this.txtNickName.Size = new System.Drawing.Size(201, 20);
            this.txtNickName.TabIndex = 7;
            // 
            // dgvResources
            // 
            this.dgvResources.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResources.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Resource,
            this.Amount});
            this.dgvResources.Location = new System.Drawing.Point(3, 175);
            this.dgvResources.Name = "dgvResources";
            this.dgvResources.Size = new System.Drawing.Size(825, 303);
            this.dgvResources.TabIndex = 0;
            // 
            // Resource
            // 
            this.Resource.HeaderText = "Resource";
            this.Resource.Name = "Resource";
            // 
            // Amount
            // 
            this.Amount.HeaderText = "Amount";
            this.Amount.Name = "Amount";
            // 
            // flpCommands
            // 
            this.flpCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.btnSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.btnCancel);
            this.flpCommands.Location = new System.Drawing.Point(2, 487);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(831, 29);
            this.flpCommands.TabIndex = 9;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(3, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(84, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.TabIndex = 11;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(165, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // FormSurvey
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 704);
            this.Controls.Add(this.flpBase);
            this.Name = "FormSurvey";
            this.Text = "FormSurvey";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpBlueprintSearch.ResumeLayout(false);
            this.flpBlueprintSearch.PerformLayout();
            this.flpResource.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flpBaseDetails.ResumeLayout(false);
            this.flpBaseDetails.PerformLayout();
            this.flpBlueprintType.ResumeLayout(false);
            this.flpBlueprintType.PerformLayout();
            this.flpBaseBlueprint.ResumeLayout(false);
            this.flpBaseBlueprint.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flpNickName.ResumeLayout(false);
            this.flpNickName.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).EndInit();
            this.flpCommands.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintSearch;
        private System.Windows.Forms.Label lblPlanetFilter;
        private System.Windows.Forms.TextBox txtPlanetFilter;
        private System.Windows.Forms.FlowLayoutPanel flpResource;
        private System.Windows.Forms.Label lblResource;
        private System.Windows.Forms.ComboBox cmbResource;
        private System.Windows.Forms.ListView lvwSurveys;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flpBaseDetails;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintType;
        private System.Windows.Forms.Label lblBlueprintType;
        private System.Windows.Forms.TextBox txtFilterBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpBaseBlueprint;
        private System.Windows.Forms.Label lblBaseBlueprint;
        private System.Windows.Forms.TextBox txtFilterBaseBlueprint;
        private System.Windows.Forms.ComboBox cmbBaseBlueprint;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.FlowLayoutPanel flpNickName;
        private System.Windows.Forms.Label lblNickName;
        private System.Windows.Forms.TextBox txtNickName;
        private System.Windows.Forms.DataGridView dgvResources;
        private System.Windows.Forms.DataGridViewComboBoxColumn Resource;
        private System.Windows.Forms.DataGridViewTextBoxColumn Amount;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button btnCancel;
    }
}