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

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class FormColony : Form
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private Baseline.Colony selectedColony;
        public FormColony()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            ColonyStructure colonyStructure = new ColonyStructure();
            flpColonyStructure.Controls.Add(colonyStructure);
            ColonyStructure colonyStructure2 = new ColonyStructure();
            flpColonyStructure.Controls.Add(colonyStructure2);
            ColonyStructure colonyStructure3 = new ColonyStructure();
            flpColonyStructure.Controls.Add(colonyStructure3);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Baseline.Colony colony;

            if (selectedColony != null)
            {
                colony = selectedColony;
            }
            else
            {
                colony = new Baseline.Colony();
                Guid myUuid = Guid.NewGuid();
                colony.UUID = myUuid.ToString();
            }

            colony.PlanetName = txtPlanetName.Text;
            colony.ColonyName = txtColonyName.Text;


            if (selectedColony == null)
            {
                playerContext.colonyList.Add(colony);
            }
            playerContext.writeContext();
        }
    }
}
