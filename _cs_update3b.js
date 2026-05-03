const fs = require("fs");
let c = fs.readFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", "utf8");
const oldPpc = [
  'string selectedUUID = cmbPricingPlan.SelectedValue as string;',
  '            cmbPricingPlan.DataSource = null;',
  '',
  '            var plans = playerContext.GetCurrentPlayerPricingPlans();',
  '            var items = new List<object>();',
].join("\n");
const newPpc = [
  'string selectedName = cmbPricingPlan.SelectedItem;',
  '',
  '            var plans = playerContext.GetCurrentPlayerPricingPlans();',
  '            var planNames = new List<string>();',
  '            _pricingPlanList = new List<string>();',
].join("\n");
c = c.replace(oldPpc, newPpc);
const oldAdd = [
  'items.Add(new { Name = "(none)", UUID = string.Empty });',
  '            foreach (var p in CollectionSortHelper.OrderPricingPlans(plans))',
  '                items.Add(new { Name = p.Name, UUID = p.UUID });',
].join("\n");
const newAdd = [
  'planNames.Add("(none)");',
  '            _pricingPlanList.Add(string.Empty);',
  '            foreach (var p in CollectionSortHelper.OrderPricingPlans(plans))',
  '            {',
  '                planNames.Add(p.Name);',
  '                _pricingPlanList.Add(p.UUID);',
  '            }',
].join("\n");
c = c.replace(oldAdd, newAdd);
const oldDs = [
  'cmbPricingPlan.DisplayMember = "Name";',
  '            cmbPricingPlan.ValueMember = "UUID";',
  '            cmbPricingPlan.DataSource = items;',
].join("\n");
c = c.replace(oldDs, "cmbPricingPlan.SetItems(planNames, selectedName ?? " + String.fromCharCode(34) + "(none)" + String.fromCharCode(34) + ");");
const oldRestore = [
  '',
  '            if (!string.IsNullOrEmpty(selectedUUID) && items.Any(i => ((dynamic)i).UUID == selectedUUID))',
  '                cmbPricingPlan.SelectedValue = selectedUUID;',
  '            else',
  '                cmbPricingPlan.SelectedIndex = 0;',
].join("\n");
c = c.replace(oldRestore, "");
c = c.replace("string planUUID = cmbPricingPlan.SelectedValue as string;", "int planIdx = cmbPricingPlan.SelectedFullIndex;\n            string planUUID = planIdx >= 0 && planIdx < _pricingPlanList.Count ? _pricingPlanList[planIdx] : string.Empty;");

fs.writeFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", c, "utf8");
console.log("CS Phase 3b done");
