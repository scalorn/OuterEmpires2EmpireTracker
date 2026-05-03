const fs = require("fs");
let c = fs.readFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", "utf8");

// Wire statistic button and context menu events
const afterDeleteResource = "btnDeleteResource.Click += BtnDeleteResource_Click;";
c = c.replace(afterDeleteResource, afterDeleteResource + "\n            btnAddStatistic.Click += BtnAddStatistic_Click;\n            btnDeleteStatistic.Click += BtnDeleteStatistic_Click;\n\n            // Wire context menu events\n            tsmiAddStatistic.Click += BtnAddStatistic_Click;\n            tsmiDeleteStatistic.Click += BtnDeleteStatistic_Click;\n            tsmiAddResource.Click += BtnAddResource_Click;\n            tsmiDeleteResource.Click += BtnDeleteResource_Click;");

// Change InitFilterCombos - cmbFilterType to use SetItems
const oldFilterType = "// Type filter\n            cmbFilterType.Items.Add(string.Empty);\n            foreach (BlueprintType bt in empireContext.BlueprintTypeList)\n                cmbFilterType.Items.Add(bt.Name);\n            cmbFilterType.SelectedIndex = 0;";
const newFilterType = "// Type filter\n            var filterTypeItems = new System.Collections.Generic.List<string> { string.Empty };\n            foreach (BlueprintType bt in empireContext.BlueprintTypeList)\n                filterTypeItems.Add(bt.Name);\n            cmbFilterType.SetItems(filterTypeItems, string.Empty);";
c = c.replace(oldFilterType, newFilterType);

// Change InitDetailCombos - cmbBlueprintType to use SetItems
const oldBpType = "cmbBlueprintType.DisplayMember = \"Name\";\n            cmbBlueprintType.ValueMember = \"Id\";\n            cmbBlueprintType.DataSource = empireContext.BindingSourceBlueprintType;\n            cmbBlueprintType.SelectedIndex = -1;";
const newBpType = "var bpTypeItems = new System.Collections.Generic.List<string>();\n            foreach (BlueprintType bt in empireContext.BlueprintTypeList)\n                bpTypeItems.Add(bt.Name);\n            cmbBlueprintType.SetItems(bpTypeItems, null);";
c = c.replace(oldBpType, newBpType);

// Change cmbBlueprintType event subscription
c = c.replace("cmbBlueprintType.SelectedIndexChanged += CmbBlueprintType_SelectedIndexChanged;", "cmbBlueprintType.SelectedItemChanged += CmbBlueprintType_SelectedItemChanged;");

// Change cmbFilterType event subscription
c = c.replace("cmbFilterType.SelectedIndexChanged += (s, e) =>", "cmbFilterType.SelectedItemChanged += (s, e) =>");

// Change cmbPricingPlan initialization
c = c.replace("// Configure pricing plan combo\n            cmbPricingPlan.DisplayMember = \"Name\";\n            cmbPricingPlan.ValueMember = \"UUID\";\n            PopulatePricingPlanCombo();\n            cmbPricingPlan.SelectedIndexChanged += CmbPricingPlan_SelectedIndexChanged;", "// Configure pricing plan combo\n            PopulatePricingPlanCombo();\n            cmbPricingPlan.SelectedItemChanged += CmbPricingPlan_SelectedItemChanged;");

fs.writeFileSync("OE2EmpireTracker/Forms/BlueprintV2/FormBlueprintV2.cs", c, "utf8");
console.log("CS Phase 1 done");
