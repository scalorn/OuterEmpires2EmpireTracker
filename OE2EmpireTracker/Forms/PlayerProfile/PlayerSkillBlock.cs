using OE2EmpireTracker.Data;
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

        private Data.PlayerSkill _playerSkill;
        public Data.PlayerSkill PlayerSkill
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

        public PlayerSkillBlock()
        {
            InitializeComponent();
        }

        public void PopulateForm()
        { 
          this.txtSkillLevel.Text = PlayerSkill.Level.ToString();
          if (!PlayerSkill.TrainingStarted)
            {
                this.lblCompletion.Visible = false;
                this.txtCompletion.Visible = false;
                this.cmdStart.Visible = CanStartTraining;
                timerCountdown.Stop();
            }
            else
            {
                this.lblCompletion.Visible = true;
                this.txtCompletion.Visible = true;
                this.cmdStart.Visible = false;
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                UpdateCompletion();
            }
        }

        private void UpdateCompletion()
        {
            if (!completionModification)
            {
                TimeSpan span = TimeSpan.FromSeconds(PlayerSkill.CompletionTime.TimeRemaining);
                this.txtCompletion.Text = span.ToString(@"d\d\ h\h\ m\m\ s\s");
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
        private void cmdStart_Click(object sender, EventArgs e)
        {
            PlayerSkill.TrainingStarted = true;
            PlayerSkill.CompletionTime.StartTime = DateTime.Now;
            PlayerSkill.CompletionTime.TimeRemaining = (long) TimeSpan.FromDays(PlayerSkill.Level + 1).TotalSeconds;
            PlayerSkill.CompletionTime.TimeRemaining = 10;

            PopulateForm();
            TrainingStatusChanged?.Invoke(this, e);
        }

        private void timerCountdown_Tick(object sender, EventArgs e)
        {
            UpdateCompletion();
        }

        private void txtCompletion_Enter(object sender, EventArgs e)
        {
            completionModification = true;
        }

        private void txtCompletion_Leave(object sender, EventArgs e)
        {
            completionModification = false;

            var match = Regex.Match(txtCompletion.Text, @"(?:(\d+)d\s*)?(?:(\d+)h\s*)?(?:(\d+)m\s*)?(?:(\d+)s)?");

            int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

            int totalSeconds = ((days * 24 + hours) * 60 + minutes) * 60 + seconds;
            PlayerSkill.CompletionTime.TimeRemaining = totalSeconds;
        }
    }
}
