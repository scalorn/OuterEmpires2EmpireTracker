using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.PlayerProfile
{
    public partial class PlayerSkillBlock : UserControl
    {
        [Category("Skill Data")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SkillName
        {
            get => lblSkillName.Text;
            set => lblSkillName.Text = value;
        }

        private Models.PlayerSkill _playerSkill;
        public Models.PlayerSkill PlayerSkill
        {
            get
            {
                return _playerSkill;
            }

            set
            {
                _playerSkill = value;
                PopulateForm();
            }
        }

        private bool _canStartTraining;
        public bool CanStartTraining
        {
            get
            {
                return _canStartTraining;
            }

            set
            {
                _canStartTraining = value;
                PopulateForm();
            }
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when skill training starts or ends")]
        public event EventHandler TrainingStatusChanged;

        private bool completionModification = false;

        public CheckBox SkillGroupCheckbox { get; set; }

        public PlayerSkillBlock()
        {
            InitializeComponent();
        }

        public void PopulateForm()
        {
            if (PlayerSkill == null)
            {
                return;
            }

            this.txtSkillLevel.Text = PlayerSkill.Level.ToString();

            bool canStart = CanStartTraining && (SkillGroupCheckbox != null && SkillGroupCheckbox.Checked);
            if (!PlayerSkill.TrainingStarted)
            {
                this.lblCompletion.Visible = false;
                this.txtCompletion.Visible = false;
                this.cmdStart.Visible = canStart;
                timerCountdown.Stop();
            }
            else
            {
                this.lblCompletion.Visible = true;
                this.txtCompletion.Visible = true;
                this.cmdStart.Visible = false;
                int intervalMs = (int)(PreferencesStore.GetInstance().Preferences.Thresholds.CountdownRefreshRateSeconds * 1000);
                timerCountdown.Interval = Math.Max(intervalMs, 1000);
                timerCountdown.Start();
                UpdateCompletion();
            }
        }

        private void UpdateCompletion()
        {
            if (!completionModification)
            {
                TimeSpan span = TimeSpan.FromSeconds(PlayerSkill.CompletionTime.TimeRemaining);
                this.txtCompletion.Text = PlayerSkill.CompletionTime.TimeRemainingString;
                if (span.TotalSeconds <= 0)
                {
                    PlayerSkill.TrainingStarted = false;
                    PlayerSkill.Level += 1;
                    PopulateForm();
                    TrainingStatusChanged?.Invoke(this, EventArgs.Empty);
                    timerCountdown.Stop();
                }
            }
        }

        private void CmdStart_Click(object sender, EventArgs e)
        {
            PlayerSkill.TrainingStarted = true;
            PlayerSkill.CompletionTime.StartTime = SystemClock.UtcNow;
            PlayerSkill.CompletionTime.TimeRemaining = (long) TimeSpan.FromDays(PlayerSkill.Level + 1).TotalSeconds;
            PlayerSkill.CompletionTime.TimeRemaining = 10;

            PopulateForm();
            TrainingStatusChanged?.Invoke(this, e);
        }

        private void TimerCountdown_Tick(object sender, EventArgs e)
        {
            UpdateCompletion();
        }

        private void TxtCompletion_Enter(object sender, EventArgs e)
        {
            completionModification = true;
        }

        private void TxtCompletion_Leave(object sender, EventArgs e)
        {
            completionModification = false;

            PlayerSkill.CompletionTime.TimeRemainingString = txtCompletion.Text;
        }
    }
}
