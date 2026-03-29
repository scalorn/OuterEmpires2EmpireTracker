using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.Forms.Blueprint;
using System.Drawing;
using System.Numerics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace OE2EmpireTracker.Tests.Blueprint
{
    [TestFixture]
    public class BlueprintScannerTests
    {
        private BlueprintScanner _scanner;

        [SetUp]
        public void SetUp()
        {
            _scanner = new BlueprintScanner();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Wraps content in minimal HTML so SgmlReader can parse it.
        /// </summary>
        private static string Html(string body) =>
            $"<html><body>{body}</body></html>";

        private static string TitleDiv(string text) =>
            $"<div class='SmallSlideOut_Form_Row_Text_Bold'>{text}</div>";

        private static string EvoDiv(string number) =>
            $"<div class='EvolutionNumber'>{number}</div>";

        private static string DescDiv(string text) =>
            $"<div class='SmallSlideOut_Form_Row_Description'>{text}</div>";

        private static string ResourceRow(string name, string qty) =>
            $"<div class='ScanDetailOutputResourceName'>{name}</div>" +
            $"<div class='ScanDetailOutputResourceDetail'>{qty}</div>";

        private static string PropRow(string label, string value) =>
            $"<div class='ShipComponentProperty'>" +
            $"<div class='CargoInfoDialogue'>{label}</div>" +
            $"<div class='div_block ui_text_blue_light'>{value}</div>" +
            $"</div>";

        private static string BP_AMX_LL_Milspec_Page1 = @"Version:0.9
StartHTML:0000000155
EndHTML:0000014177
StartFragment:0000000191
EndFragment:0000014141
SourceURL:https://game.dev.outerempires.net/game
<html>
<body>
<!--StartFragment--><div class=""SmallSlideOut_FormSection"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><br class=""Apple-interchange-newline""><span> </span><div class=""SmallSlideOut_Form_Row_NameOfItem_Section"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; vertical-align: top; padding-left: 10px; width: 298.797px;""><div class=""SmallSlideOut_Form_Row_Text_Bold"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); font-family: BarlowSemiCondensed-Bold;""><div class=""EvolutionLeft"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background: url(&quot;0f3e805217c98030f0c5.png&quot;) -558px -718px no-repeat; width: 5px; height: 14px; display: inline-block;""></div><span> </span><div class=""EvolutionNumber"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; font-family: BarlowSemiCondensed-Bold; font-size: 14px; color: rgb(238, 160, 34); position: relative; top: -1px;"">5</div><span> </span><div class=""EvolutionRight"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background: url(&quot;0f3e805217c98030f0c5.png&quot;) -571px -718px no-repeat; width: 5px; height: 14px; display: inline-block;""></div><span> </span>AMX-LL Reactor Core (MilSpec)</div><div class=""SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); max-width: 300px; font-size: 13px;"">Reactor that generates power for the ship</div></div></div><div class=""LeftSlideout_SectionContent_Divider"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); margin-top: 3px; margin-bottom: 3px; width: 356.391px; height: 2px; background-image: linear-gradient(to right, rgb(0, 54, 1), rgb(0, 83, 9), rgb(0, 113, 15), rgb(0, 145, 22), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 145, 22), rgb(0, 113, 15), rgb(0, 83, 9), rgb(0, 54, 1)); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""></div><div id=""BlueprintTabs_5l37V"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><div class=""BlueprintTab"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block;""><div class=""BlueprintTab_Selector BlueprintTab_Content_Selected"" id=""BlueprintStatsTab_5l37V"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgb(0, 237, 162); color: black; display: inline-block; padding-left: 2px; padding-right: 2px; clip-path: polygon(6% 0%, 95% 0%, 100% 100%, 0% 100%);""><div class=""BlueprintTab_Text"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-left: 5px; padding-right: 5px;"">Statistics</div></div></div><span> </span><div class=""BlueprintTab"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block;""><div class=""BlueprintTab_Selector BlueprintTab_Content"" id=""BlueprintRecipeTab_5l37V"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(0, 237, 162, 0.17); color: rgb(0, 237, 162); display: inline-block; padding-left: 2px; padding-right: 2px; clip-path: polygon(6% 0%, 95% 0%, 100% 100%, 0% 100%);""><div class=""BlueprintTab_Text"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-left: 5px; padding-right: 5px;"">Required Resources</div></div></div></div><div class=""ScrollingArea BlueprintMainContentContainer"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); height: 325px; overflow: hidden auto; color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><div class=""SmallSlideOut_FormSection BlueprintContentSection"" id=""BlueprintStats_5l37V"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35);""><div class=""SmallSlideOut_Form_Row"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-bottom: 5px; padding-right: 10px;""><div class="""" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35);""><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(77, 77, 77, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Manufacture Run Time</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">9 hours<br style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35);""></div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(37, 37, 37, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Class</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">6</div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(77, 77, 77, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Mass</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">861<span> </span><span class=""ui_text_red_light BlueprintDetailsSmallText"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(255, 75, 80); font-size: 14px;"">(▼<span> </span>-9)</span></div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(37, 37, 37, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Cargo Volume Size</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">360</div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(77, 77, 77, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Power Generated</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">2258MW<span> </span><span class=""ui_text_dark_green BlueprintDetailsSmallText"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(33, 140, 12); font-size: 14px;"">(▲<span> </span>435)</span></div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(37, 37, 37, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Health (Hitpoints)</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">4824<span> </span><span class=""ui_text_red_light BlueprintDetailsSmallText"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(255, 75, 80); font-size: 14px;"">(▼<span> </span>-396)</span></div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(77, 77, 77, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Eng. Capacity Required</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">1080</div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(37, 37, 37, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Power regeneration rate</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">31.5MW/s</div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(77, 77, 77, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Wear and Tear Rate</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">2.959%<span> </span><span class=""ui_text_red_light BlueprintDetailsSmallText"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(255, 75, 80); font-size: 14px;"">(▼<span> </span>-0.041)</span></div></div><div class=""ShipComponentProperty"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(37, 37, 37, 0.65); padding-top: 2px; padding-bottom: 2px;""><div class=""CargoInfoDialogue div_block"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; width: 200px; padding-left: 20px; vertical-align: top;"">Maximum Damage Repair %</div><span> </span><div class=""div_block ui_text_blue_light"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;"">86.57%<span> </span><span class=""ui_text_dark_green BlueprintDetailsSmallText"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(33, 140, 12); font-size: 14px;"">(▲<span> </span>6.57)</span></div></div></div></div></div></div><!--EndFragment-->
</body>
</html>";

        private static string BP_AMX_LL_Milspec_Page2 = 
@"Version:0.9
StartHTML:0000000155
EndHTML:0000010490
StartFragment:0000000191
EndFragment:0000010454
SourceURL:https://game.dev.outerempires.net/game
<html>
<body>
<!--StartFragment--><div class=""SmallSlideOut_FormSection"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><br class=""Apple-interchange-newline""><span> </span><div class=""SmallSlideOut_Form_Row_NameOfItem_Section"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; vertical-align: top; padding-left: 10px; width: 298.797px;""><div class=""SmallSlideOut_Form_Row_Text_Bold"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); font-family: BarlowSemiCondensed-Bold;""><div class=""EvolutionLeft"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background: url(&quot;0f3e805217c98030f0c5.png&quot;) -558px -718px no-repeat; width: 5px; height: 14px; display: inline-block;""></div><span> </span><div class=""EvolutionNumber"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; font-family: BarlowSemiCondensed-Bold; font-size: 14px; color: rgb(238, 160, 34); position: relative; top: -1px;"">5</div><span> </span><div class=""EvolutionRight"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background: url(&quot;0f3e805217c98030f0c5.png&quot;) -571px -718px no-repeat; width: 5px; height: 14px; display: inline-block;""></div><span> </span>AMX-LL Reactor Core(MilSpec)</div><div class=""SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); max-width: 300px; font-size: 13px;"">Reactor that generates power for the ship</div></div></div><div class=""LeftSlideout_SectionContent_Divider"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); margin-top: 3px; margin-bottom: 3px; width: 356.391px; height: 2px; background-image: linear-gradient(to right, rgb(0, 54, 1), rgb(0, 83, 9), rgb(0, 113, 15), rgb(0, 145, 22), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 145, 22), rgb(0, 113, 15), rgb(0, 83, 9), rgb(0, 54, 1)); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""></div><div id = ""BlueprintTabs_wHZfF"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><div class=""BlueprintTab"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block;""><div class=""BlueprintTab_Selector BlueprintTab_Content"" id=""BlueprintStatsTab_wHZfF"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(0, 237, 162, 0.17); color: rgb(0, 237, 162); display: inline-block; padding-left: 2px; padding-right: 2px; clip-path: polygon(6% 0%, 95% 0%, 100% 100%, 0% 100%);""><div class=""BlueprintTab_Text"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-left: 5px; padding-right: 5px;"">Statistics</div></div></div><span> </span><div class=""BlueprintTab"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block;""><div class=""BlueprintTab_Selector BlueprintTab_Content_Selected"" id=""BlueprintRecipeTab_wHZfF"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgb(0, 237, 162); color: black; display: inline-block; padding-left: 2px; padding-right: 2px; clip-path: polygon(6% 0%, 95% 0%, 100% 100%, 0% 100%);""><div class=""BlueprintTab_Text"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-left: 5px; padding-right: 5px;"">Required Resources</div></div></div></div><div class=""ScrollingArea BlueprintMainContentContainer"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); height: 325px; overflow: hidden auto; color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><div class=""SmallSlideOut_FormSection BlueprintContentSection BlueprintHiddenFirst BlueprintResourcePaddingLeft"" id=""BlueprintRecipe_wHZfF"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: block; padding-left: 20px;""><div class=""SmallSlideOut_Form_Row"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-bottom: 5px; padding-right: 10px;""><div class="""" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35);""><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Common<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Alkaline Earth Metals</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">9,366</div><span> </span><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Acidic Inorganics</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">1,927</div><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Uncommon<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Heavy Trans-Metals</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">2,121</div><span> </span><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Complex Non-Metallics</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">2,036</div><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Rare<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Heavy Alkaline Earth Metals</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">2,440</div><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Very Rare<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">S1. Translivermoric Exotics</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">699</div></div></div></div></div><!--EndFragment-->
</body>
</html>";

        // -----------------------------------------------------------------------
        // processHtml — name and tech level
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_NameWithTechLevel_ParsesBoth()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Pulse Cannon (MilSpec)")));

            Assert.AreEqual("Pulse Cannon", bp.Name);
            Assert.AreEqual("MilSpec", bp.TechLevel);
        }

        [Test]
        public void ProcessHtml_NameWithoutTechLevel_SetsNameOnly()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Basic Thruster")));

            Assert.AreEqual("Basic Thruster", bp.Name);
            Assert.IsNull(bp.TechLevel);
        }

        [Test]
        public void ProcessHtml_NameWithLeadingTrailingWhitespace_IsTrimmed()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("  Cargo Pod  ")));

            Assert.AreEqual("Cargo Pod", bp.Name);
        }

        // -----------------------------------------------------------------------
        // processHtml — evolution
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_EvolutionNumber_ParsedAsInt()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(EvoDiv("3") + TitleDiv("Pulse Cannon3")));

            Assert.AreEqual(3, bp.Evolution);
        }

        [Test]
        public void ProcessHtml_EvolutionRemovedFromTitle()
        {
            var bp = new Data.Blueprint();
            // Title contains the evo number appended — scanner should strip it
            _scanner.processHtml(bp, Html(EvoDiv("2") + TitleDiv("Jump Drive2")));

            Assert.AreEqual("Jump Drive", bp.Name);
        }

        [Test]
        public void ProcessHtml_NoEvolutionNode_EvolutionRemainsDefault()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Shield Generator")));

            Assert.AreEqual(0, bp.Evolution);
        }

        // -----------------------------------------------------------------------
        // processHtml — description
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_Description_IsPopulated()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(DescDiv("A powerful weapon system.")));

            Assert.AreEqual("A powerful weapon system.", bp.Description);
        }

        // -----------------------------------------------------------------------
        // processHtml — resources
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SingleResource_IsExtracted()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(ResourceRow("Iron", "500")));

            Assert.IsTrue(bp.Resources.ContainsKey("Iron"));
            Assert.AreEqual("500", bp.Resources["Iron"]);
        }

        [Test]
        public void ProcessHtml_MultipleResources_AllExtracted()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(
                ResourceRow("Iron", "500") +
                ResourceRow("Carbon", "250") +
                ResourceRow("Titanium", "100")));

            Assert.AreEqual("500", bp.Resources["Iron"]);
            Assert.AreEqual("250", bp.Resources["Carbon"]);
            Assert.AreEqual("100", bp.Resources["Titanium"]);
        }

        [Test]
        public void ProcessHtml_ResourceQuantityWithCommas_StripsNonDigits()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(ResourceRow("Iron", "1,500")));

            Assert.AreEqual("1500", bp.Resources["Iron"]);
        }

        [Test]
        public void ProcessHtml_NoResources_ResourcesDictionaryIsEmpty()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Empty Blueprint")));

            Assert.IsNotNull(bp.Resources);
            Assert.AreEqual(0, bp.Resources.Count);
        }

        // -----------------------------------------------------------------------
        // processHtml — properties
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SingleProperty_IsExtracted()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(PropRow("Mass", "450")));

            string val;
            bp.Properties.getString("Mass", null, out val);
            Assert.AreEqual("450", val);
        }

        [Test]
        public void ProcessHtml_PropertyWithDeltaText_DeltaIsStripped()
        {
            var bp = new Data.Blueprint();
            // Delta indicators like "(▲ 435)" should be removed
            _scanner.processHtml(bp, Html(PropRow("Power", "1200 (▲ 435)")));

            string val;
            bp.Properties.getString("Power", null, out val);
            Assert.AreEqual("1200", val);
        }

        [Test]
        public void ProcessHtml_MultipleProperties_AllExtracted()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, Html(
                PropRow("Mass", "450") +
                PropRow("Health", "2000") +
                PropRow("PowerRequired", "150")));

            string mass, health, power;
            bp.Properties.getString("Mass", null, out mass);
            bp.Properties.getString("Health", null, out health);
            bp.Properties.getString("PowerRequired", null, out power);

            Assert.AreEqual("450", mass);
            Assert.AreEqual("2000", health);
            Assert.AreEqual("150", power);
        }

        // -----------------------------------------------------------------------
        // processHtml — robustness
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_EmptyHtml_DoesNotThrow()
        {
            var bp = new Data.Blueprint();
            Assert.DoesNotThrow(() => _scanner.processHtml(bp, Html("")));
        }

        [Test]
        public void ProcessHtml_MalformedHtml_DoesNotThrow()
        {
            var bp = new Data.Blueprint();
            Assert.DoesNotThrow(() => _scanner.processHtml(bp, "<div unclosed"));
        }

        [Test]
        public void ProcessHtml_AMX_LL_Milspec()
        {
            var bp = new Data.Blueprint();
            _scanner.processHtml(bp, BP_AMX_LL_Milspec_Page1);
            _scanner.processHtml(bp, BP_AMX_LL_Milspec_Page2);

            Assert.AreEqual("Reactor", bp.BluePrintType);
            Assert.AreEqual("Reactor that generates power for the ship", bp.Description);
            string manuTime, equipClass, mass, cargoVolumeSize, power, health, engCap, powerRegenRate, wearRate, dmgRate;
            bp.Properties.getString("Class", null, out equipClass);
            Assert.AreEqual("6", equipClass);
            Assert.AreEqual(6, bp.Class);

            bp.Properties.getString("ManufactureTime", null, out manuTime);
            Assert.AreEqual("9 hours", manuTime);

            bp.Properties.getString("Mass", null, out mass);
            Assert.AreEqual("861", mass);

            bp.Properties.getString("CargoVolumeSize", null, out cargoVolumeSize);
            Assert.AreEqual("360", cargoVolumeSize);

            bp.Properties.getString("PowerGenerated", null, out power);
            Assert.AreEqual("861", mass);

            bp.Properties.getString("Health", null, out health);
            Assert.AreEqual("4824", health);

            bp.Properties.getString("EngCapacityRequired", null, out engCap);
            Assert.AreEqual("1080", engCap);

            bp.Properties.getString("PowerRegenerationRate", null, out powerRegenRate);
            Assert.AreEqual("31.5MW/s", powerRegenRate);

            bp.Properties.getString("WearAndTearRate", null, out wearRate);
            Assert.AreEqual("2.959%", wearRate);

            bp.Properties.getString("MaximumDamageRepairRate", null, out dmgRate);
            Assert.AreEqual("86.57%", dmgRate);

            Assert.AreEqual("9366", bp.Resources["Alkaline Earth Metals"]);
            Assert.AreEqual("1927", bp.Resources["Acidic Inorganics"]);
            Assert.AreEqual("2121", bp.Resources["Heavy Trans-Metals"]);
            Assert.AreEqual("2036", bp.Resources["Complex Non-Metallics"]);
            Assert.AreEqual("2440", bp.Resources["Heavy Alkaline Earth Metals"]);
            Assert.AreEqual("699",  bp.Resources["S1. Translivermoric Exotics"]);
        }

    }
}
