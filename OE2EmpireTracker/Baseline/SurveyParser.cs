using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OE2EmpireTracker.Baseline
{
    public class SurveyParser
    {
        string data = "<div class=\"SmallSlideOut_FormSection\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;\"><br class=\"Apple-interchange-newline\"><span> </span><div class=\"SmallSlideOut_Form_Row_NameOfItem_Section\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; vertical-align: top; padding-left: 10px; width: 298.792px;\"><div class=\"SmallSlideOut_Form_Row_Text_Bold\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); font-family: BarlowSemiCondensed-Bold;\"></div><div class=\"SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); max-width: 300px; font-size: 13px;\">A detailed survey report taken on<span> </span>27JUL24-11:44p<span> </span>by<span> </span>Scalorn Scorpus</div></div></div><div class=\"LeftSlideout_SectionContent_Divider\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); margin-top: 3px; margin-bottom: 3px; width: 356.396px; height: 2px; background-image: linear-gradient(to right, rgb(0, 54, 1), rgb(0, 83, 9), rgb(0, 113, 15), rgb(0, 145, 22), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 145, 22), rgb(0, 113, 15), rgb(0, 83, 9), rgb(0, 54, 1)); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;\"></div><div class=\"SmallSlideOut_FormSection\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;\"><div class=\"SmallSlideOut_Form_Row\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-bottom: 5px; padding-right: 10px;\"><div class=\"ScanDetailOutput\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); font-size: 15px;\"><div class=\"ScanRarityTypeRow\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;\">Common<span> </span>Elements<span> </span>Detected:</div><div class=\"div_block ui_text_lightgrey ScanDetailOutputResourceName\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;\">Post-Trans Metals (Low Purity)</div><span> </span><div class=\"div_block ui_text_blue_light ScanDetailOutputResourceDetail\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 89px;\">41/hour</div><div class=\"ScanRarityTypeRow\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;\">Uncommon<span> </span>Elements<span> </span>Detected:</div><div class=\"div_block ui_text_lightgrey ScanDetailOutputResourceName\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;\">Heavy Trans-Metals (High Purity)</div><span> </span><div class=\"div_block ui_text_blue_light ScanDetailOutputResourceDetail\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 89px;\">20/hour</div><span> </span><div class=\"div_block ui_text_lightgrey ScanDetailOutputResourceName\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;\">Heavy Trans-Metals (High Purity)</div><span> </span><div class=\"div_block ui_text_blue_light ScanDetailOutputResourceDetail\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 89px;\">38/hour</div><div class=\"ScanRarityTypeRow\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;\">Rare<span> </span>Elements<span> </span>Detected:</div><div class=\"div_block ui_text_lightgrey ScanDetailOutputResourceName\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;\">Lanthanides (High Purity)</div><span> </span><div class=\"div_block ui_text_blue_light ScanDetailOutputResourceDetail\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 89px;\">5/hour</div><div class=\"ScanRarityTypeRow\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;\">Trace:</div><div class=\"div_block ui_text_lightgrey ScanDetailOutputResourceName\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;\">(Unknown Trace Elements)</div><span> </span><div class=\"div_block ui_text_blue_light\" style=\"scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block;\">?/hour</div></div></div></div>";

        public void parseIt()
        {
            processHTML(data);
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
                Debug.Print(item.Name + " " + item.LocalName + " " + item.NodeType + " C" + depth + " = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children((depth + 1), item.ChildNodes);
                }
            }
        }

    }
}
