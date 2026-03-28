using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.PlayerProfile
{
    public partial class FormPlayerProfile : Form
    {
        private Data.PlayerProfile selectedProfile;

        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();


        public FormPlayerProfile()
        {
            InitializeComponent();
            selectedProfile = new Data.PlayerProfile();
            PopulateForm();
        }

        public void PopulateForm()
        {
            txtPlayerName.Text = selectedProfile.Name;
            configureSkillBlock(pskHumanResources, "Human Resources");
            configureSkillBlock(pskForeman, "Foreman");

            bool isTraining = false;
            foreach (KeyValuePair<string, PlayerSkill> skillEntry in selectedProfile.Skills)
            {
                if (skillEntry.Value.TrainingStarted)
                {
                    isTraining = true;
                    break;
                }
            }
            foreach (KeyValuePair<string, PlayerSkillBlock> skillBlockEntry in skillBlocks)
            {
                skillBlockEntry.Value.CanStartTraining = !isTraining;
            }
        }

        private void configureSkillBlock(PlayerSkillBlock skillBlock, string skillName)
        {
            skillBlock.SkillName = skillName;
            skillBlock.PlayerSkill = selectedProfile.GetSkill(skillName);
            skillBlocks[skillName] = skillBlock;
            skillBlock.TrainingStatusChanged += TrainingStatusChanged;
        }


        private void TrainingStatusChanged (object sender, EventArgs e)
        {
            PopulateForm();
        }

    }
}
