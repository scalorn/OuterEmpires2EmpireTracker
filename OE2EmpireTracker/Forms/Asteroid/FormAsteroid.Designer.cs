namespace OE2EmpireTracker.Forms.Asteroid
{
    partial class FormAsteroid
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwAsteroids = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtAsteroidName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpSystem = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSystem = new System.Windows.Forms.Label();
            this.txtSystemName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.lblReserves = new System.Windows.Forms.Label();
            this.dgvReserves = new System.Windows.Forms.DataGridView();
            this.colResource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxReserve = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrentReserve = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResetTimestamp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpReserveAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.lblReserveResource = new System.Windows.Forms.Label();
            this.cmbReserveResource = new System.Windows.Forms.ComboBox();
            this.txtReserveResourceFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblReservePurity = new System.Windows.Forms.Label();
            this.cmbReservePurity = new System.Windows.Forms.ComboBox();
            this.lblMaxReserve = new System.Windows.Forms.Label();
            this.txtMaxReserve = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblCurrentReserve = new System.Windows.Forms.Label();
            this.txtCurrentReserve = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdAddReserve = new System.Windows.Forms.Button();
            this.cmdRemoveReserve = new System.Windows.Forms.Button();
            this.lblLinkedSurveys = new System.Windows.Forms.Label();
            this.dgvLinkedSurveys = new System.Windows.Forms.DataGridView();
            this.colSurveyPlayer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSurveyResource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSurveyPurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSurveyRate = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpName.SuspendLayout();
            this.flpSystem.SuspendLayout();
            this.flpReserveAdd.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReserves)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLinkedSurveys)).BeginInit();
            this.SuspendLayout();
            // flpBase
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpDetail);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(950, 620);
            this.flpBase.WrapContents = false;

            // flpSearchList
            this.flpSearchList.Controls.Add(this.flpFilter);
            this.flpSearchList.Controls.Add(this.lvwAsteroids);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 614);
            this.flpSearchList.WrapContents = false;

            // flpFilter
            this.flpFilter.AutoSize = true;
            this.flpFilter.Controls.Add(this.lblFilter);
            this.flpFilter.Controls.Add(this.txtFilter);
            this.flpFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilter.Location = new System.Drawing.Point(3, 3);
            this.flpFilter.Name = "flpFilter";
            this.flpFilter.Size = new System.Drawing.Size(214, 26);

            // lblFilter
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new System.Drawing.Point(3, 5);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(32, 13);
            this.lblFilter.Text = "Filter:";
            this.lblFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // txtFilter
            this.txtFilter.Location = new System.Drawing.Point(41, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(170, 20);

            // lvwAsteroids
            this.lvwAsteroids.FullRowSelect = true;
            this.lvwAsteroids.HideSelection = false;
            this.lvwAsteroids.Location = new System.Drawing.Point(3, 35);
            this.lvwAsteroids.MultiSelect = false;
            this.lvwAsteroids.Name = "lvwAsteroids";
            this.lvwAsteroids.Size = new System.Drawing.Size(214, 520);
            this.lvwAsteroids.UseCompatibleStateImageBehavior = false;
            this.lvwAsteroids.View = System.Windows.Forms.View.Details;

            // flpCommands
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(3, 561);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);

            // cmdNew
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(55, 23);
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;

            // cmdDelete
            this.cmdDelete.Location = new System.Drawing.Point(64, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(55, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;

            // flpDetail
            this.flpDetail.Controls.Add(this.flpName);
            this.flpDetail.Controls.Add(this.flpSystem);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.lblReserves);
            this.flpDetail.Controls.Add(this.dgvReserves);
            this.flpDetail.Controls.Add(this.flpReserveAdd);
            this.flpDetail.Controls.Add(this.cmdRemoveReserve);
            this.flpDetail.Controls.Add(this.lblLinkedSurveys);
            this.flpDetail.Controls.Add(this.dgvLinkedSurveys);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(718, 614);
            this.flpDetail.WrapContents = false;
            // flpName
            this.flpName.AutoSize = true;
            this.flpName.Controls.Add(this.lblName);
            this.flpName.Controls.Add(this.txtAsteroidName);
            this.flpName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpName.Location = new System.Drawing.Point(3, 3);
            this.flpName.Name = "flpName";
            this.flpName.Size = new System.Drawing.Size(712, 26);

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(3, 5);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.Text = "Name:";
            this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // txtAsteroidName
            this.txtAsteroidName.Location = new System.Drawing.Point(47, 3);
            this.txtAsteroidName.Name = "txtAsteroidName";
            this.txtAsteroidName.Size = new System.Drawing.Size(300, 20);

            // flpSystem
            this.flpSystem.AutoSize = true;
            this.flpSystem.Controls.Add(this.lblSystem);
            this.flpSystem.Controls.Add(this.txtSystemName);
            this.flpSystem.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpSystem.Location = new System.Drawing.Point(3, 35);
            this.flpSystem.Name = "flpSystem";
            this.flpSystem.Size = new System.Drawing.Size(712, 26);

            // lblSystem
            this.lblSystem.AutoSize = true;
            this.lblSystem.Location = new System.Drawing.Point(3, 5);
            this.lblSystem.Name = "lblSystem";
            this.lblSystem.Size = new System.Drawing.Size(44, 13);
            this.lblSystem.Text = "System:";
            this.lblSystem.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // txtSystemName
            this.txtSystemName.Location = new System.Drawing.Point(53, 3);
            this.txtSystemName.Name = "txtSystemName";
            this.txtSystemName.Size = new System.Drawing.Size(300, 20);

            // cmdSave
            this.cmdSave.Location = new System.Drawing.Point(3, 67);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;

            // lblReserves
            this.lblReserves.AutoSize = true;
            this.lblReserves.Location = new System.Drawing.Point(3, 96);
            this.lblReserves.Name = "lblReserves";
            this.lblReserves.Size = new System.Drawing.Size(55, 13);
            this.lblReserves.Text = "Reserves:";
            this.lblReserves.Font = new System.Drawing.Font(this.lblReserves.Font, System.Drawing.FontStyle.Bold);

            // dgvReserves
            this.dgvReserves.AllowUserToAddRows = false;
            this.dgvReserves.AllowUserToDeleteRows = false;
            this.dgvReserves.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReserves.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colResource, this.colPurity, this.colMaxReserve, this.colCurrentReserve, this.colResetTimestamp});
            this.dgvReserves.Location = new System.Drawing.Point(3, 115);
            this.dgvReserves.Name = "dgvReserves";
            this.dgvReserves.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvReserves.Size = new System.Drawing.Size(712, 180);

            this.colResource.HeaderText = "Resource";
            this.colResource.Name = "colResource";
            this.colResource.ReadOnly = true;
            this.colResource.Width = 140;

            this.colPurity.HeaderText = "Purity";
            this.colPurity.Name = "colPurity";
            this.colPurity.ReadOnly = true;
            this.colPurity.Width = 80;

            this.colMaxReserve.HeaderText = "Max Reserve";
            this.colMaxReserve.Name = "colMaxReserve";
            this.colMaxReserve.ReadOnly = true;
            this.colMaxReserve.Width = 100;

            this.colCurrentReserve.HeaderText = "Current";
            this.colCurrentReserve.Name = "colCurrentReserve";
            this.colCurrentReserve.Width = 100;

            this.colResetTimestamp.HeaderText = "Reset";
            this.colResetTimestamp.Name = "colResetTimestamp";
            this.colResetTimestamp.Width = 120;
            // flpReserveAdd
            this.flpReserveAdd.AutoSize = true;
            this.flpReserveAdd.Controls.Add(this.lblReserveResource);
            this.flpReserveAdd.Controls.Add(this.txtReserveResourceFilter);
            this.flpReserveAdd.Controls.Add(this.cmbReserveResource);
            this.flpReserveAdd.Controls.Add(this.lblReservePurity);
            this.flpReserveAdd.Controls.Add(this.cmbReservePurity);
            this.flpReserveAdd.Controls.Add(this.lblMaxReserve);
            this.flpReserveAdd.Controls.Add(this.txtMaxReserve);
            this.flpReserveAdd.Controls.Add(this.lblCurrentReserve);
            this.flpReserveAdd.Controls.Add(this.txtCurrentReserve);
            this.flpReserveAdd.Controls.Add(this.cmdAddReserve);
            this.flpReserveAdd.Location = new System.Drawing.Point(3, 301);
            this.flpReserveAdd.Name = "flpReserveAdd";
            this.flpReserveAdd.Size = new System.Drawing.Size(712, 30);

            this.lblReserveResource.AutoSize = true;
            this.lblReserveResource.Text = "Resource:";
            this.lblReserveResource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblReserveResource.Name = "lblReserveResource";

            this.cmbReserveResource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReserveResource.Size = new System.Drawing.Size(120, 21);
            this.cmbReserveResource.Name = "cmbReserveResource";
            this.txtReserveResourceFilter.Size = new System.Drawing.Size(80, 20); this.txtReserveResourceFilter.Name = "txtReserveResourceFilter";

            this.lblReservePurity.AutoSize = true;
            this.lblReservePurity.Text = "Purity:";
            this.lblReservePurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblReservePurity.Name = "lblReservePurity";

            this.cmbReservePurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReservePurity.Size = new System.Drawing.Size(80, 21);
            this.cmbReservePurity.Name = "cmbReservePurity";

            this.lblMaxReserve.AutoSize = true;
            this.lblMaxReserve.Text = "Max:";
            this.lblMaxReserve.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMaxReserve.Name = "lblMaxReserve";

            this.txtMaxReserve.Size = new System.Drawing.Size(60, 20);
            this.txtMaxReserve.Name = "txtMaxReserve";

            this.lblCurrentReserve.AutoSize = true;
            this.lblCurrentReserve.Text = "Current:";
            this.lblCurrentReserve.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCurrentReserve.Name = "lblCurrentReserve";

            this.txtCurrentReserve.Size = new System.Drawing.Size(60, 20);
            this.txtCurrentReserve.Name = "txtCurrentReserve";

            this.cmdAddReserve.Size = new System.Drawing.Size(75, 23);
            this.cmdAddReserve.Text = "Add";
            this.cmdAddReserve.UseVisualStyleBackColor = true;
            this.cmdAddReserve.Name = "cmdAddReserve";

            // cmdRemoveReserve
            this.cmdRemoveReserve.Location = new System.Drawing.Point(3, 337);
            this.cmdRemoveReserve.Size = new System.Drawing.Size(75, 23);
            this.cmdRemoveReserve.Text = "Remove";
            this.cmdRemoveReserve.UseVisualStyleBackColor = true;
            this.cmdRemoveReserve.Name = "cmdRemoveReserve";

            // lblLinkedSurveys
            this.lblLinkedSurveys.AutoSize = true;
            this.lblLinkedSurveys.Location = new System.Drawing.Point(3, 366);
            this.lblLinkedSurveys.Name = "lblLinkedSurveys";
            this.lblLinkedSurveys.Size = new System.Drawing.Size(85, 13);
            this.lblLinkedSurveys.Text = "Linked Surveys:";
            this.lblLinkedSurveys.Font = new System.Drawing.Font(this.lblLinkedSurveys.Font, System.Drawing.FontStyle.Bold);

            // dgvLinkedSurveys
            this.dgvLinkedSurveys.AllowUserToAddRows = false;
            this.dgvLinkedSurveys.AllowUserToDeleteRows = false;
            this.dgvLinkedSurveys.ReadOnly = true;
            this.dgvLinkedSurveys.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLinkedSurveys.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSurveyPlayer, this.colSurveyResource, this.colSurveyPurity, this.colSurveyRate});
            this.dgvLinkedSurveys.Location = new System.Drawing.Point(3, 385);
            this.dgvLinkedSurveys.Name = "dgvLinkedSurveys";
            this.dgvLinkedSurveys.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLinkedSurveys.Size = new System.Drawing.Size(712, 180);

            this.colSurveyPlayer.HeaderText = "Player";
            this.colSurveyPlayer.Name = "colSurveyPlayer";
            this.colSurveyPlayer.ReadOnly = true;
            this.colSurveyPlayer.Width = 140;

            this.colSurveyResource.HeaderText = "Resource";
            this.colSurveyResource.Name = "colSurveyResource";
            this.colSurveyResource.ReadOnly = true;
            this.colSurveyResource.Width = 140;

            this.colSurveyPurity.HeaderText = "Purity";
            this.colSurveyPurity.Name = "colSurveyPurity";
            this.colSurveyPurity.ReadOnly = true;
            this.colSurveyPurity.Width = 80;

            this.colSurveyRate.HeaderText = "Rate/Cycle";
            this.colSurveyRate.Name = "colSurveyRate";
            this.colSurveyRate.ReadOnly = true;
            this.colSurveyRate.Width = 100;

            // FormAsteroid
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(950, 620);
            this.Controls.Add(this.flpBase);
            this.Name = "FormAsteroid";
            this.Text = "Asteroids";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpFilter.ResumeLayout(false);
            this.flpCommands.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpName.ResumeLayout(false);
            this.flpSystem.ResumeLayout(false);
            this.flpReserveAdd.ResumeLayout(false);
            this.flpFilter.PerformLayout();
            this.flpName.PerformLayout();
            this.flpSystem.PerformLayout();
            this.flpCommands.PerformLayout();
            this.flpReserveAdd.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReserves)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLinkedSurveys)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFilter;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.ListView lvwAsteroids;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpName;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtAsteroidName;
        private System.Windows.Forms.FlowLayoutPanel flpSystem;
        private System.Windows.Forms.Label lblSystem;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSystemName;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Label lblReserves;
        private System.Windows.Forms.DataGridView dgvReserves;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxReserve;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrentReserve;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResetTimestamp;
        private System.Windows.Forms.FlowLayoutPanel flpReserveAdd;
        private System.Windows.Forms.Label lblReserveResource;
        private System.Windows.Forms.ComboBox cmbReserveResource;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtReserveResourceFilter;
        private System.Windows.Forms.Label lblReservePurity;
        private System.Windows.Forms.ComboBox cmbReservePurity;
        private System.Windows.Forms.Label lblMaxReserve;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtMaxReserve;
        private System.Windows.Forms.Label lblCurrentReserve;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCurrentReserve;
        private System.Windows.Forms.Button cmdAddReserve;
        private System.Windows.Forms.Button cmdRemoveReserve;
        private System.Windows.Forms.Label lblLinkedSurveys;
        private System.Windows.Forms.DataGridView dgvLinkedSurveys;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSurveyPlayer;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSurveyResource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSurveyPurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSurveyRate;
    }
}