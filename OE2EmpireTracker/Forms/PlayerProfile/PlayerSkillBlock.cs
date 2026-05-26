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
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.PlayerProfile
{
    public partial class PlayerSkillBlock : UserControl
    {
        private LocalSkillData _skillData;

        private bool _canStartTraining;

        private bool completionModification = false;

        public PlayerSkillBlock()
        {
            InitializeComponent();
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when skill training starts or ends")]
        public event EventHandler TrainingStatusChanged;

        [Category("Skill Data")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SkillName
        {
            get => lblSkillName.Text;
            set => lblSkillName.Text = value;
        }

        public LocalSkillData SkillData
        {
            get
            {
                return _skillData;
            }

            set
            {
                _skillData = value;
                PopulateForm();
            }
        }

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

        public CheckBox SkillGroupCheckbox { get; set; }

        public void PopulateForm()
        {
            if (_skillData == null)
            {
                return;
            }

            this.txtSkillLevel.Text = _skillData.Level.ToString();

            bool canStart = CanStartTraining && (SkillGroupCheckbox != null && SkillGroupCheckbox.Checked);
            if (!_skillData.TrainingStarted)
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

            // Effect description (Req 11.1)
            bool hasEffect = !string.IsNullOrEmpty(_skillData.EffectDescription);
            this.lblEffectDescription.Text = _skillData.EffectDescription;
            this.lblEffectDescription.Visible = hasEffect;

            // Amount per level (Req 11.2)
            bool hasAmount = _skillData.AmountPerLevel > 0;
            this.lblAmountPerLevel.Text = string.Format("+{0}% per level", _skillData.AmountPerLevel);
            this.lblAmountPerLevel.Visible = hasAmount;

            // Training percentage progress bar (Req 4)
            bool hasTrainingPct = _skillData.TrainingPercentageComplete > 0;
            this.pnlTrainingProgress.Visible = hasTrainingPct;
            if (hasTrainingPct)
            {
                this.pnlTrainingProgress.Invalidate();
            }

            // Remaining time (Req 11.4)
            bool hasRemaining = _skillData.RemainingMinutes > 0;
            this.lblRemainingTime.Text = FormatMinutesAsCountdown(_skillData.RemainingMinutes);
            this.lblRemainingTime.Visible = hasRemaining;

            // Dynamic height based on metadata presence (Req 3)
            bool hasMetadata = hasEffect || hasAmount || hasTrainingPct || hasRemaining;
            int targetHeight = hasMetadata ? 42 : 24;
            if (this.Height != targetHeight)
            {
                this.MinimumSize = new Size(450, targetHeight);
                this.MaximumSize = new Size(450, targetHeight);
                this.Size = new Size(450, targetHeight);
            }
        }

        private static string FormatMinutesAsCountdown(int totalMinutes)
        {
            int days = totalMinutes / (24 * 60);
            int hours = (totalMinutes % (24 * 60)) / 60;
            int minutes = totalMinutes % 60;

            string result = string.Empty;
            if (days > 0)
            {
                result += string.Format("{0}d ", days);
            }

            if (days > 0 || hours > 0)
            {
                result += string.Format("{0}h ", hours);
            }

            result += string.Format("{0}m", minutes);
            result += " 0s";
            return result.Trim();
        }

        private void PnlTrainingProgress_Paint(object sender, PaintEventArgs e)
        {
            int pct = _skillData != null ? _skillData.TrainingPercentageComplete : 0;
            int clamped = Math.Max(0, Math.Min(100, pct));
            int fillWidth = (int)((clamped / 100.0) * 60);

            Graphics g = e.Graphics;

            // Background is handled by Panel.BackColor = SystemColors.ControlLight
            // Draw fill bar
            if (fillWidth > 0)
            {
                using (var brush = new SolidBrush(SystemColors.Highlight))
                {
                    g.FillRectangle(brush, 0, 0, fillWidth, 12);
                }
            }

            // Draw centered percentage text
            string text = string.Format("{0}%", clamped);
            using (var font = new Font(this.Font.FontFamily, 7f, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    text,
                    font,
                    new Rectangle(0, 0, 60, 12),
                    SystemColors.ControlText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void UpdateCompletion()
        {
            if (!completionModification && _skillData != null)
            {
                this.txtCompletion.Text = _skillData.TimeRemainingString;
                if (_skillData.TimeRemaining <= 0)
                {
                    _skillData.TrainingStarted = false;
                    _skillData.Level += 1;
                    PopulateForm();
                    TrainingStatusChanged?.Invoke(this, EventArgs.Empty);
                    timerCountdown.Stop();
                }
            }
        }

        private void CmdStart_Click(object sender, EventArgs e)
        {
            _skillData.TrainingStarted = true;
            _skillData.CompletionStartTime = SystemClock.UtcNow;
            _skillData.CompletionEndTime = SystemClock.UtcNow.AddSeconds(10);

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

            // Parse the edited text and update local CompletionEndTime
            var match = Regex.Match(
                txtCompletion.Text,
                @"(?:(\d+)d\s*)?(?:(\d+)h\s*)?(?:(\d+)m\s*)?(?:(\d+)s)?");

            int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

            long totalSeconds = ((((long)days * 24) + hours) * 60 * 60) + (minutes * 60) + seconds;
            _skillData.CompletionStartTime = SystemClock.UtcNow;
            _skillData.CompletionEndTime = SystemClock.UtcNow.AddSeconds(totalSeconds);
        }
    }
}