const fs = require("fs");
let c = fs.readFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", "utf8");

c = c.replace("private void CmbBlueprintType_SelectedIndexChanged(object sender, EventArgs e)", "private void CmbBlueprintType_SelectedItemChanged(object sender, EventArgs e)");
// Update CmbBlueprintType handler to use FilteredTextComboSet API
const oldBpHandler = "var bt = cmbBlueprintType.SelectedItem as BlueprintType;\n            string oldType = viewModel?.BluePrintType;\n            string newType = bt?.Id;\n            if (bt != null) viewModel.BluePrintType = bt.Id;";
const newBpHandler = "string selectedName = cmbBlueprintType.SelectedItem;\n            var bt = selectedName != null ? empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == selectedName) : null;\n            string oldType = viewModel?.BluePrintType;\n            string newType = bt?.Id;\n            if (bt != null) viewModel.BluePrintType = bt.Id;";
c = c.replace(oldBpHandler, newBpHandler);
c = c.replace("cmbBlueprintType.SelectedIndex,", "cmbBlueprintType.SelectedFullIndex,");
c = c.replace("private void CmbPricingPlan_SelectedIndexChanged(object sender, EventArgs e)", "private void CmbPricingPlan_SelectedItemChanged(object sender, EventArgs e)");
// Update RefreshBlueprintList for cmbFilterType
const oldFilterCheck = "if (cmbFilterType.SelectedIndex > 0)\n            {\n                string typeName = (string)cmbFilterType.SelectedItem;\n                var bt = empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == typeName);\n                if (bt != null) criteria.BlueprintTypeId = bt.Id;\n            }";
const newFilterCheck = "string filterTypeName = cmbFilterType.SelectedItem;\n            if (!string.IsNullOrEmpty(filterTypeName))\n            {\n                var bt = empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == filterTypeName);\n                if (bt != null) criteria.BlueprintTypeId = bt.Id;\n            }";
c = c.replace(oldFilterCheck, newFilterCheck);

fs.writeFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", c, "utf8");
console.log("CS Phase 2 done");
