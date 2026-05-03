const fs = require("fs");
let c = fs.readFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", "utf8");
const oldPopBt = [
  'var foundBt = empireContext.FindBlueprintType(dataType);',
  '            cmbBlueprintType.SelectedItem = foundBt;',
  '            var bt = cmbBlueprintType.SelectedItem as BlueprintType;',
].join("\n");
const newPopBt = [
  'var foundBt = empireContext.FindBlueprintType(dataType);',
  '            if (foundBt != null)',
  '                cmbBlueprintType.SetItems(cmbBlueprintType.Items, foundBt.Name);',
  '',
  '            var bt = foundBt;',
].join("\n");
c = c.replace(oldPopBt, newPopBt);
c = c.replace("cmbBlueprintType.SelectedIndex,", "cmbBlueprintType.SelectedFullIndex,");
c = c.replace("cmbBlueprintType.SelectedIndex = -1;", "cmbBlueprintType.SetItems(cmbBlueprintType.Items, null);");
const oldField = "private List<ReadOnlyBlueprint> _baseBlueprintList = new List<ReadOnlyBlueprint>();";
c = c.replace(oldField, oldField + "\n\n        // Parallel list of pricing plan UUIDs for SelectedFullIndex lookup\n        private List<string> _pricingPlanList = new List<string>();");

fs.writeFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", c, "utf8");
console.log("CS Phase 3a done");
