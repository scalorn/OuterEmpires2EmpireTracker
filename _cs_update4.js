const fs = require("fs");
let c = fs.readFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", "utf8");
const ob = String.fromCharCode(123);
const cb = String.fromCharCode(125);
const afterDeleteResource = "dgvResources.Rows.RemoveAt(rowIndex);\n            UpdateSaveButtonState();\n        " + cb;
const handler1 = [
  "        /// <summary>",
  "        /// Adds a new empty row to the statistics grid for a user-defined property.",
  "        /// </summary>",
  "        private void BtnAddStatistic_Click(object sender, EventArgs e)",
  "        " + ob,
  '            int rowIndex = dgvStatistics.Rows.Add();',
  '            dgvStatistics.Rows[rowIndex].Cells["Property"].ReadOnly = false;',
  '            dgvStatistics.Rows[rowIndex].Cells["Property"].Value = string.Empty;',
  '            dgvStatistics.Rows[rowIndex].Cells["CurrentValue"].Value = string.Empty;',
  '            _cachedGridKey = null;',
  '            UpdateSaveButtonState();',
  "        " + cb,
].join("\n");
const handler2 = [
  "        /// <summary>",
  "        /// Deletes the selected statistics row. Type-defined properties cannot be deleted.",
  "        /// </summary>",
  "        private void BtnDeleteStatistic_Click(object sender, EventArgs e)",
  "        " + ob,
  '            if (dgvStatistics.CurrentRow == null) return;',
  '            int rowIndex = dgvStatistics.CurrentRow.Index;',
  '',
  '            string propertyName = dgvStatistics.Rows[rowIndex].Cells["Property"].Value as string;',
  '            if (string.IsNullOrEmpty(propertyName))',
  "            " + ob,
  '                dgvStatistics.Rows.RemoveAt(rowIndex);',
  '                _cachedGridKey = null;',
  '                UpdateSaveButtonState();',
  '                return;',
  "            " + cb,
  '',
  '            // Check if this is a type-defined property',
  '            var bt = cmbBlueprintType.SelectedItem != null',
  '                ? empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == cmbBlueprintType.SelectedItem)',
  '                : null;',
  "            string[] definedProps = bt?.Properties ?? Array.Empty<string>();",
  '            if (definedProps.Contains(propertyName))',
  "            " + ob,
  "                MessageBox.Show(",
  "                    string.Format(" + String.fromCharCode(34) + "Cannot delete type-defined property " + String.fromCharCode(39) + ob + "0" + cb + String.fromCharCode(39) + String.fromCharCode(34) + ", propertyName),",
  "                    " + String.fromCharCode(34) + "Delete Property" + String.fromCharCode(34) + ",",
  '                    MessageBoxButtons.OK,',
  '                    MessageBoxIcon.Information);',
  '                return;',
  "            " + cb,
  '',
  '            viewModel.RemoveProperty(propertyName);',
  '            dgvStatistics.Rows.RemoveAt(rowIndex);',
  '            _cachedGridKey = null;',
  '            UpdateSaveButtonState();',
  "        " + cb,
].join("\n");
c = c.replace(afterDeleteResource, afterDeleteResource + "\n\n" + handler1 + "\n\n" + handler2);

fs.writeFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", c, "utf8");
console.log("CS Phase 4 done");
