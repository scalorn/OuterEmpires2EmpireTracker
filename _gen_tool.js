const fs = require("fs");
const tool = require("./_mo_template.json");
fs.writeFileSync(".kiro/tools/member-order.js", tool.code, "utf8");
console.log("Written " + tool.code.length + " chars");
