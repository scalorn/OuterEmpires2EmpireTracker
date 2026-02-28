using OE2EmpireTracker.Baseline;//
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace OE2EmpireTracker
{
    public partial class FormBlueprint : Form
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        public FormBlueprint()
        {
            InitializeComponent();
            empireContext = new EmpireContext();
            playerContext = EmpireContext.PlayerContext;

            cmbBlueprintType.DisplayMember = "Name";
            cmbBlueprintType.ValueMember = "Id";
            cmbBlueprintType.DataSource = empireContext.bindingSourceBlueprintType;
            cmbBlueprintType.SelectedIndex = -1;

            cmbShipClass.DisplayMember = "Name";
            cmbShipClass.ValueMember = "Id";
            cmbShipClass.DataSource = empireContext.bindingSourceShipClass;
            cmbShipClass.SelectedIndex = -1;

            cmbTechLevel.DisplayMember = "Name";
            cmbTechLevel.ValueMember = "Name";
            cmbTechLevel.DataSource = empireContext.bindingSourceTechLevel;
            cmbTechLevel.SelectedIndex = -1;

            dgvStatistics.previousControl = tabDetailedData;

            cmbEvolution.DisplayMember = "Name";
            cmbEvolution.ValueMember = "Name";
            cmbEvolution.DataSource = empireContext.bindingSourceEvolution;
            cmbEvolution.SelectedIndex = 0;

            DataGridViewComboBoxColumn cmbResource = (DataGridViewComboBoxColumn) dgvResources.Columns["Resource"];
            cmbResource.DisplayMember = "Name";
            cmbResource.ValueMember = "Name";
            cmbResource.DataSource = empireContext.bindingSourceResource;

            cmbBaseBlueprint.DisplayMember = "ExtendedName";
            cmbBaseBlueprint.ValueMember = "UUID";
            cmbBaseBlueprint.DataSource = playerContext.bindingSourceBlueprint;
            cmbBaseBlueprint.SelectedIndex = -1;
        }

        private void rtbCopyTarget_TextChanged(object sender, EventArgs e)
        {
            //Debug.Print(e.ToString());
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            String returnHtmlText = null;
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                returnHtmlText = Clipboard.GetText(TextDataFormat.Html);
                string html = ExtractHtmlFragmentFromClipboardData(returnHtmlText);
                //rtbCopyTarget.Text = html;
                //processHTML(html);
            }
        }

        /// https://stackoverflow.com/questions/14604146/standard-class-that-parses-clipboard-functionality-getdatadataformats-html-out
        /// <summary>
        /// Extracts selected Html fragment string from clipboard data by parsing header information 
        /// in htmlDataString
        /// </summary>
        /// <param name="htmlDataString">
        /// String representing Html clipboard data. This includes Html header
        /// </param>
        /// <returns>
        /// String containing only the Html selection part of htmlDataString, without header
        /// </returns>
        internal static string ExtractHtmlFragmentFromClipboardData(string htmlDataString)
        {
            // HTML Clipboard Format
            // (https://msdn.microsoft.com/en-us/library/aa767917(v=vs.85).aspx)

            // The fragment contains valid HTML representing the area the user has selected. This 
            // includes the information required for basic pasting of an HTML fragment, as follows:
            //  - Selected text. 
            //  - Opening tags and attributes of any element that has an end tag within the selected text. 
            //  - End tags that match the included opening tags. 

            // The fragment should be preceded and followed by the HTML comments <!--StartFragment--> and 
            // <!--EndFragment--> (no space allowed between the !-- and the text) to indicate where the 
            // fragment starts and ends. So the start and end of the fragment are indicated by these 
            // comments as well as by the StartFragment and EndFragment byte counts. Though redundant, 
            // this makes it easier to find the start of the fragment (from the byte count) and mark the 
            // position of the fragment directly in the HTML tree.

            // Byte count from the beginning of the clipboard to the start of the fragment.
            int startFragmentIndex = htmlDataString.IndexOf("StartFragment:");
            if (startFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }
            // TODO: We assume that indices represented by strictly 10 zeros ("0123456789".Length),
            // which could be wrong assumption. We need to implement more flrxible parsing here
            startFragmentIndex = Int32.Parse(htmlDataString.Substring(startFragmentIndex + "StartFragment:".Length, 10));
            if (startFragmentIndex < 0 || startFragmentIndex > htmlDataString.Length)
            {
                return "ERROR: Unrecognized html header";
            }

            // Byte count from the beginning of the clipboard to the end of the fragment.
            int endFragmentIndex = htmlDataString.IndexOf("EndFragment:");
            if (endFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }
            // TODO: We assume that indices represented by strictly 10 zeros ("0123456789".Length),
            // which could be wrong assumption. We need to implement more flrxible parsing here
            endFragmentIndex = Int32.Parse(htmlDataString.Substring(endFragmentIndex + "EndFragment:".Length, 10));
            if (endFragmentIndex > htmlDataString.Length)
            {
                endFragmentIndex = htmlDataString.Length;
            }

            // CF_HTML is entirely text format and uses the transformation format UTF-8
            byte[] bytes = Encoding.UTF8.GetBytes(htmlDataString);
            return Encoding.UTF8.GetString(bytes, startFragmentIndex, endFragmentIndex - startFragmentIndex);
        }

        private void processHTML(string inputText)
        {
            StringReader reader = new StringReader(inputText);

            // setup SgmlReader
            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader()
            {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };

            // create document
            XmlDocument doc = new XmlDocument()
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };
            doc.Load(sgmlReader);
            foreach (XmlNode item in doc)
            {
                Debug.Print("T = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children(0, item.ChildNodes);
                }
            }
        }

        private void children(int depth, XmlNodeList nodes)
        {
            foreach (XmlNode item in nodes)
            {
                Debug.Print("C" + depth + " = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children((depth + 1), item.ChildNodes);
                }
            }
        }

        private void cmbBlueprintType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.Print("cmbBlueprintType_SelectedIndexChanged Sender = " + sender + " Event Args " + e);
            BlueprintType bt = cmbBlueprintType.SelectedItem as BlueprintType;
            if (bt != null && bt.Universal == true)
            {
                flpClass.Visible = false;
                cmbShipClass.SelectedIndex = -1;
                flpTechLevel.Visible = false;
                cmbTechLevel.SelectedIndex = -1;
            } else
            {
                flpClass.Visible = true;
                flpTechLevel.Visible = true;
            }

            if (bt != null && bt.Properties != null)
            {
                int row = 0;
                foreach (string property in bt.Properties) {
                    int rowIndex = row;
                    if (dgvStatistics.Rows.Count <= row)
                    {
                        rowIndex = dgvStatistics.Rows.Add();
                    }
                    DataGridViewRow newRow = dgvStatistics.Rows[rowIndex];
                    newRow.Cells["Property"].Value = property;
                    row++;
                }

                if (bt.Properties.Length == 0)
                {
                    dgvStatistics.Rows.Clear();
                }
                else
                while (dgvStatistics.Rows.Count > bt.Properties.Length)
                {
                    dgvStatistics.Rows.RemoveAt(dgvStatistics.Rows.Count - 1);
                }
            }
        }

        private void dgvStatistics_SelectionChanged(object sender, EventArgs e)
        {
            Debug.Print("dgvStatistics_SelectionChanged Sender = " + sender + " Event Args " + e);
        }

        private void txtFilterBlueprintType_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtFilterBlueprintType.Text;
            BindingSource filteredItemsBindingList = empireContext.bindingSourceBlueprintType;

            if (!string.IsNullOrEmpty(searchText))
            {
                BindingList<BlueprintType> blueprintTypes = empireContext.blueprintTypeList;
                var filteredList = blueprintTypes
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                filteredItemsBindingList = new BindingSource();
                // Set the in-memory list as the DataSource for the BindingSource
                filteredItemsBindingList.DataSource = filteredList;
            }

            cmbBlueprintType.DataSource = filteredItemsBindingList;
            cmbBlueprintType.DroppedDown = true;
        }

        private void txtFilterBlueprintType_Enter(object sender, EventArgs e)
        {
            cmbBlueprintType.DroppedDown = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            empireContext = new EmpireContext();

            PlayerContext playerContext = EmpireContext.PlayerContext;

            Blueprint blueprint = new Blueprint();
            Guid myUuid = Guid.NewGuid();
            blueprint.UUID = myUuid.ToString();

            BlueprintType blueprintType = cmbBlueprintType.SelectedItem as BlueprintType;
            blueprint.BluePrintType = blueprintType.Id;

            ShipClass shipClass = cmbShipClass.SelectedItem as ShipClass;
            blueprint.Class = shipClass.Id;

            TechLevel techLevel = cmbTechLevel.SelectedItem as TechLevel;
            blueprint.TechLevel = techLevel.Name;

            string evolution = "0";
            if (cmbEvolution.SelectedItem != null)
            {
                evolution = cmbEvolution.SelectedItem as string;
            } else if (cmbEvolution.Text != null)
            {
                evolution = cmbEvolution.Text;
            }
            blueprint.Evolution = int.Parse(evolution);

            if (cmbBaseBlueprint.SelectedItem != null)
            {
                Blueprint baseBlueprint = cmbBaseBlueprint.SelectedItem as Blueprint;
                blueprint.baseBlueprintUUID = baseBlueprint.UUID;
            }
            else
            {
                blueprint.baseBlueprintUUID = "";
            }

            blueprint.Name = txtName.Text;
            blueprint.NickName = txtNickName.Text;
            blueprint.Description = txtDescription.Text;

            // Now to map grid fields.

            playerContext.blueprintList.Add(blueprint);
            playerContext.writeContext();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void txtFilterBaseBlueprint_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtFilterBaseBlueprint.Text;
            BindingSource filteredItemsBindingList = playerContext.bindingSourceBlueprint;

            if (!string.IsNullOrEmpty(searchText))
            {
                BindingList<Blueprint> blueprints = playerContext.blueprintList;
                var filteredList = blueprints
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                filteredItemsBindingList = new BindingSource();
                // Set the in-memory list as the DataSource for the BindingSource
                filteredItemsBindingList.DataSource = filteredList;
            }

            cmbBaseBlueprint.DataSource = filteredItemsBindingList;
            cmbBaseBlueprint.DroppedDown = true;
        }
    }
}
