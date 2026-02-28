using OE2EmpireTracker.Baseline;//
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace OE2EmpireTracker
{
    public partial class FormBlueprint : Form
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private Blueprint selectedBlueprint;
        public FormBlueprint()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
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

            lvwBlueprints.View = View.Details;
            lvwBlueprints.Columns.Add("UUID", 0);
            lvwBlueprints.Columns.Add("Type", 50);
            lvwBlueprints.Columns.Add("Name", 100);
            lvwBlueprints.Columns.Add("Tech Level", 60);
            lvwBlueprints.Columns.Add("Evolution", 30);
            lvwBlueprints.Columns.Add("Nick Name", 100);
            populateListView(new List<Blueprint>(playerContext.blueprintList));

            //lvwBlueprints.Height = flpSearchList.Height - flpBlueprintSearch.Height;
            //lvwBlueprints.Width = flpSearchList.Width;

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
            updateBlueprintTypeList();
        }

        public void updateBlueprintTypeList()
        {
            BlueprintType bt = cmbBlueprintType.SelectedItem as BlueprintType;
            if (bt != null && bt.Universal == true)
            {
                flpClass.Visible = false;
                cmbShipClass.SelectedIndex = -1;
                flpTechLevel.Visible = false;
                cmbTechLevel.SelectedIndex = -1;
            }
            else
            {
                flpClass.Visible = true;
                flpTechLevel.Visible = true;
            }

            if (bt != null && bt.Properties != null)
            {
                int row = 0;
                foreach (string property in bt.Properties)
                {
                    int rowIndex = row;
                    if (dgvStatistics.Rows.Count <= row)
                    {
                        rowIndex = dgvStatistics.Rows.Add();
                    }
                    DataGridViewRow newRow = dgvStatistics.Rows[rowIndex];
                    newRow.Cells["Property"].Value = property;
                    newRow.Cells["Property"].Tag = property;
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
                filteredList.Insert(0, new BlueprintType());
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

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void txtFilterBaseBlueprint_TextChanged(object sender, EventArgs e)
        {
            updateBaseBlueprintList();
            cmbBaseBlueprint.DroppedDown = true;
        }

        private void updateBaseBlueprintList()
        {
            string searchText = txtFilterBaseBlueprint.Text;
            List<Blueprint> filteredList = new List<Blueprint>(playerContext.blueprintList);
            if (cmbBlueprintType.SelectedItem != null)
            {
                filteredList = filteredList
                    .Where(item => item.BluePrintType == (cmbBlueprintType.SelectedItem as BlueprintType).Id)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            if (selectedBlueprint != null)
            {
                filteredList = filteredList
                    .Where(item => item.UUID != selectedBlueprint.UUID)
                    .ToList();
            }

            // Add an empty to allow to select no base blueprint.

            filteredList.Insert(0, new Blueprint());
            BindingSource filteredItemsBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemsBindingList.DataSource = filteredList;


            cmbBaseBlueprint.DataSource = filteredItemsBindingList;
        }

        private void txtBlueprintListFilter_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtBlueprintListFilter.Text;
            //BindingSource filteredItemsBindingList = playerContext.bindingSourceBlueprint;
            List<Blueprint> blueprints = new List<Blueprint>(playerContext.blueprintList);

            if (!string.IsNullOrEmpty(searchText))
            {
                blueprints = blueprints
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                //filteredItemsBindingList = new BindingSource();
                // Set the in-memory list as the DataSource for the BindingSource
                //filteredItemsBindingList.DataSource = filteredList;
            }

            //lvwBlueprints.DataSource = filteredItemsBindingList;
            populateListView(blueprints);
        }

        void populateListView(List<Blueprint> blueprints)
        {
            if (blueprints == null)
            {
                return;
            }
            lvwBlueprints.Items.Clear();

            Dictionary<string, ListViewItem> viewableBlueprints = new Dictionary<string, ListViewItem>();

            // First index what is viewable.
            foreach (ListViewItem item in lvwBlueprints.Items)
            {
                viewableBlueprints[(item.Tag as Blueprint).UUID] = item;
            }

            // Now add or update what is viewable.
            foreach (Blueprint blueprint in blueprints)
            {
                ListViewItem item;
                bool found = viewableBlueprints.TryGetValue(blueprint.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(blueprint.UUID); // Main item text (first column)
                }
                item.Tag = blueprint;
                item.SubItems[0].Tag = blueprint;
                item.SubItems.Add(blueprint.BluePrintType); // Subitem for the second column
                item.SubItems.Add(blueprint.Name); // Subitem for the second column
                item.SubItems.Add(blueprint.TechLevel); // Subitem for the third column
                item.SubItems.Add("" + blueprint.Evolution); // Subitem for the third column
                item.SubItems.Add(blueprint.NickName); // Subitem for the third column

                if (!found)
                {
                    lvwBlueprints.Items.Add(item); // Add the item to the ListView
                } else
                {
                    viewableBlueprints.Remove(blueprint.UUID);
                }
            }

            // Remove what is left over (deleted or filtered out)
            foreach (KeyValuePair<string, ListViewItem> viewableBlueprint in viewableBlueprints)
            {
                lvwBlueprints.Items.Remove(viewableBlueprint.Value);
            }
        }

        private void lvwBlueprints_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Debug.Print("lvwBlueprints.SelectedItems.Count = " + lvwBlueprints.SelectedItems.Count);
            if (lvwBlueprints.SelectedItems.Count == 1)
            {
                Debug.Print("Selected item = " + lvwBlueprints.SelectedItems[0].SubItems[0].Text);
                Debug.Print("Selected item = " + lvwBlueprints.SelectedItems[0].SubItems[0].Tag);
                selectedBlueprint = lvwBlueprints.SelectedItems[0].SubItems[0].Tag as Blueprint;
                populateForm();
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            empireContext = EmpireContext.getInstance();

            PlayerContext playerContext = EmpireContext.PlayerContext;

            Blueprint blueprint;
            if (selectedBlueprint != null)
            {
                blueprint = selectedBlueprint;
            }
            else
            {
                blueprint = new Blueprint();
                Guid myUuid = Guid.NewGuid();
                blueprint.UUID = myUuid.ToString();
            }

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
            }
            else if (cmbEvolution.Text != null)
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

            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                blueprint.Properties[row.Cells[0].Tag as string] = row.Cells[2].Value as string;
            }

            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                string resourceName = row.Cells[0].Value as string;
                string resourceAmount = row.Cells[1].Value as string;
                if (resourceName != null)
                {
                    blueprint.Resources[resourceName] = resourceAmount;
                }
            }

            if (selectedBlueprint == null)
            {
                playerContext.blueprintList.Add(blueprint);
            }
            playerContext.writeContext();
            populateListView(new List<Blueprint>(playerContext.blueprintList));
        }

        private void populateForm()
        {
            if (selectedBlueprint == null)
            {
                return;
            }
            txtFilterBlueprintType.Text = "";
            updateBlueprintTypeList();
            cmbBlueprintType.SelectedItem = empireContext.findBlueprintType(selectedBlueprint.BluePrintType);
            cmbShipClass.SelectedItem = empireContext.findShipClass(selectedBlueprint.Class);
            cmbTechLevel.SelectedItem = empireContext.findTechLevel(selectedBlueprint.TechLevel);
            cmbEvolution.SelectedItem = empireContext.findEvolution(selectedBlueprint.Evolution);

            txtFilterBaseBlueprint.Text = "";
            updateBaseBlueprintList();
            cmbBaseBlueprint.SelectedItem = playerContext.findBlueprint(selectedBlueprint.baseBlueprintUUID);

            txtName.Text = selectedBlueprint.Name;
            txtNickName.Text = selectedBlueprint.NickName;
            txtDescription.Text = selectedBlueprint.Description;

            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string property = row.Cells["Property"].Tag as string;
                string value = "";
                bool found = selectedBlueprint.Properties.TryGetValue(property, out value);
                if (!found || value == null)
                {
                    value = "";
                }
                row.Cells["CurrentValue"].Value = value;
            }

            dgvResources.Rows.Clear();
            foreach (KeyValuePair<string, string> resource in selectedBlueprint.Resources)
            {
                dgvResources.Rows.Add();
                DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
                row.Cells[0].Value = resource.Key;
                row.Cells[1].Value = resource.Value;
            }

        }

        private void clearForm()
        {
            selectedBlueprint = null;
            txtFilterBlueprintType.Text = "";
            updateBlueprintTypeList();
            cmbBlueprintType.SelectedItem = null;
            cmbShipClass.SelectedItem = null;
            cmbTechLevel.SelectedItem = null;
            cmbEvolution.SelectedItem = null;

            txtFilterBaseBlueprint.Text = "";
            updateBaseBlueprintList();
            cmbBaseBlueprint.SelectedItem = null;

            txtName.Text = "";
            txtNickName.Text = "";
            txtDescription.Text = "";

            dgvStatistics.Rows.Clear();
            dgvResources.Rows.Clear();
        }

        private void flpSearchList_SizeChanged(object sender, EventArgs e)
        {
            //lvwBlueprints.Height = flpSearchList.Height - flpBlueprintSearch.Height;
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (selectedBlueprint != null)
            {
                playerContext.blueprintList.Remove(selectedBlueprint);
                selectedBlueprint = null;
                playerContext.writeContext();
                populateListView(new List<Blueprint>(playerContext.blueprintList));
                lvwBlueprints.SelectedItems.Clear();

                clearForm();
            }
        }
    }
}
