using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public enum SkillName
    {
        [Description("Human Resources")]   HumanResources,
        [Description("Foreman")]            Foreman,
        [Description("Founder")]            Founder,
        [Description("Energy Efficiency")]  EnergyEfficiency,
        [Description("Builder")]            Builder,
        [Description("Refining Focus")]     RefiningFocus,
        [Description("Production Focus")]   ProductionFocus,
        [Description("Extraction Focus")]   ExtractionFocus,
        [Description("Damage Control")]     DamageControl,
        [Description("Engineering Capacity")] EngineeringCapacity,
        [Description("Sounds As A Pound")]  SoundsAsAPound,
        [Description("Self Made Millionaire")] SelfMadeMillionaire,
        [Description("AAA Healthcare")]     AAAHealthcare,
        [Description("Job Opportunities")]  JobOpportunities,
        [Description("Contract Management")] ContractManagement,
        [Description("Research Review")]    ResearchReview,
        [Description("Research Methods")]   ResearchMethods,
        [Description("Research Focus")]     ResearchFocus,
        [Description("Surveying Methods")]  SurveyingMethods,
        [Description("Scanning Methods")]   ScanningMethods,
        [Description("Quartermaster")]      Quartermaster,
        [Description("Broker")]             Broker,
    }

    public enum SkillGroupName
    {
        [Description("Colony Director")]    ColonyDirector,
        [Description("Colony Founder")]     ColonyFounder,
        [Description("Colony Operations")]  ColonyOperations,
        [Description("Commander")]          Commander,
        [Description("Engineer")]           Engineer,
        [Description("Entrepeneur")]        Entrepeneur,
        [Description("Job Management")]     JobManagement,
        [Description("Researcher")]         Researcher,
        [Description("Surveyor")]           Surveyor,
        [Description("Trader")]             Trader,
    }

    public static class SkillNameExtensions
    {
        public static string ToDisplayName(this SkillName skill)
        {
            FieldInfo fi = skill.GetType().GetField(skill.ToString());
            var attr = fi?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? skill.ToString();
        }

        public static string ToDisplayName(this SkillGroupName group)
        {
            FieldInfo fi = group.GetType().GetField(group.ToString());
            var attr = fi?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? group.ToString();
        }
    }

    public class PlayerSkill
    {
        public int Level { get; set; } = 0;
        public bool TrainingStarted { get; set; } = false;
        public CountDownTime CompletionTime { get; set; } = new CountDownTime();
    }
}
