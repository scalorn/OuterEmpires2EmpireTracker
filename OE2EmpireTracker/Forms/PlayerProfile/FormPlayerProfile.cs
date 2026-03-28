using OE2EmpireTracker.Baseline;
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
        /// <summary>
        /// Gets or sets the empire context instance for accessing empire-wide data.
        /// </summary>
        private EmpireContext empireContext;

        /// <summary>
        /// Gets or sets the player context instance for accessing player-specific data.
        /// </summary>
        private PlayerContext playerContext;

        private Data.PlayerProfile selectedProfile;

        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();


        public FormPlayerProfile()
        {
            InitializeComponent();

            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            selectedProfile = new Data.PlayerProfile();
            PopulateForm();
        }

        public void PopulateForm()
        {
            txtPlayerName.Text = selectedProfile.Name;
            configureSkillBlock(chkColonyDirector, pskHumanResources, "Human Resources");
            configureSkillBlock(chkColonyDirector, pskForeman, "Foreman");
            configureSkillBlock(chkColonyFounder, pskFounder, "Founder");
            configureSkillBlock(chkColonyFounder, pskEnergyEfficiency, "Energy Efficiency");
            configureSkillBlock(chkColonyFounder, pskBuilder, "Builder");
            configureSkillBlock(chkColonyOperations, pskRefiningFocus, "Refining Focus");
            configureSkillBlock(chkColonyOperations, pskProductionFocus, "Production Focus");
            configureSkillBlock(chkColonyOperations, pskExtractionFocus, "Extraction Focus");
            configureSkillBlock(chkCommander, pskDamageControl, "Damage Control");
            configureSkillBlock(chkEngineer, pskEngineeringCapacity, "Engineering Capacity");
            configureSkillBlock(chkEntrepeneur, pskSoundAsAPound, "Sounds As A Pound");
            configureSkillBlock(chkEntrepeneur, pskSelfMadeMillionaire, "Self Made Millionaire");
            configureSkillBlock(chkEntrepeneur, pskAAAHealthcare, "AAA Healthcare");
            configureSkillBlock(chkJobManagement, pskJobOpportunities, "Job Opportunities");
            configureSkillBlock(chkJobManagement, pskContractManagement, "Contract Management");
            configureSkillBlock(chkResearcher, pskResearchReview, "Research Review");
            configureSkillBlock(chkResearcher, pskResearchMethods, "Research Methods");
            configureSkillBlock(chkResearcher, pskResearchFocus, "Research Focus");
            configureSkillBlock(chkSurveyor, pskSurveyingMethods, "Surveying Methods");
            configureSkillBlock(chkSurveyor, pskScanningMethods, "Scanning Methods");
            configureSkillBlock(chkSurveyor, pskQuartermaster, "Quartermaster");
            configureSkillBlock(chkTrader, pskBroker, "Broker");

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

        private void configureSkillBlock(CheckBox skillGroup, PlayerSkillBlock skillBlock, string skillName)
        {
            skillBlock.SkillGroupCheckbox = skillGroup;
            skillBlock.SkillName = skillName;
            skillBlock.PlayerSkill = selectedProfile.GetSkill(skillName);
            skillBlocks[skillName] = skillBlock;
            skillBlock.TrainingStatusChanged += TrainingStatusChanged;
            skillBlock.PopulateForm();
        }


        private void TrainingStatusChanged (object sender, EventArgs e)
        {
            PopulateForm();
        }

        private void chkColonyDirector_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Colony Director", chkColonyDirector.Checked);
            PopulateForm();
        }

        private void chkColonyFounder_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Colony Founder", chkColonyFounder.Checked);
            PopulateForm();
        }

        private void chkCommander_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Commander", chkCommander.Checked);
            PopulateForm();
        }

        private void chkEngineer_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Engineer", chkEngineer.Checked);
            PopulateForm();
        }

        private void chkEntrepeneur_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Entrepeneur", chkEntrepeneur.Checked);
            PopulateForm();
        }

        private void chkJobManagement_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Job Management", chkJobManagement.Checked);
            PopulateForm();
        }

        private void chkResearcher_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Researcher", chkResearcher.Checked);
            PopulateForm();
        }

        private void chkSurveyor_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Surveyor", chkSurveyor.Checked);
            PopulateForm();
        }

        private void chkTrader_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Trader", chkTrader.Checked);
            PopulateForm();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {

            // Add or update in player context
            playerContext.playerProfileList.Add(selectedProfile);

            // Persist changes to player context
            playerContext.writeContext();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {

        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {

        }
    }
}
