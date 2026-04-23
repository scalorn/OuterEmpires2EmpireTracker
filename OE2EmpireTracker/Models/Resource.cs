using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OE2EmpireTracker.Services;
using static OE2EmpireTracker.Models.ResourcePurity;

namespace OE2EmpireTracker.Models
{
    public class Resource
    {
        public enum ResourceEnum
        {
            None = 0,
            AcidicInorganics,
            AcidicOrganics,
            AlkaliInorganics,
            AlkaliMetals,
            AlkaliOrganics,
            AlkalineEarthMetals,
            ComplexMetallics,
            ComplexNonMetallics,
            Halogens,
            HeavyAlkaliMetals,
            HeavyAlkalineEarthMetals,
            HeavyNobleGases,
            HeavyPostTransMetals,
            HeavyTransMetals,
            LanthanideVolatiles,
            Lanthanides,
            LightHalogens,
            Metallics,
            Metaloids,
            NobleGases,
            NonMetallics,
            PostTransMetals,
            S1TranslanthanicExotics,
            S1TranslivermoricExotics,
            S1TransuranicExotics,
            S2Element126,
            S2Element127,
            S2Superactinides,
            StrongAcidicInorganics,
            StrongAlkaliInorganics,
            StrongAlkaliOrganics,
            SuperheavyExotics,
            TransMetals,
            TransuranicVolatiles
        }

        [Required]
        public Models.ResourceGroup.ResourceGroupEnum ResourceGroup { get; set; }
        public Models.ResourceClass.ResourceClassEnum ResourceClass { get; set; }

        [Required]
        public ResourceEnum ID { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name ?? string.Empty;

        private static List<Resource> _resources = GetResources();
        private static Dictionary<ResourceEnum, Resource> _resourceMapByEnum;
        private static Dictionary<string, Resource> _resourceMapByString;

        public static IReadOnlyList<Resource> Resources => _resources.AsReadOnly();
        public static IReadOnlyDictionary<ResourceEnum, Resource> ResourceMapByEnum => _resourceMapByEnum;
        public static IReadOnlyDictionary<string, Resource> ResourceMapByString => _resourceMapByString;


        private static List<Resource> GetResources()
        {
            List<Resource> instance = new List<Resource>();
            instance.Add(new Resource()
            {
                ID = ResourceEnum.AcidicInorganics,
                Name = "Acidic Inorganics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.NonCarbon,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.AcidicOrganics,
                Name = "Acidic Organics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Organic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.AlkaliInorganics,
                Name = "Alkali Inorganics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.NonCarbon,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.AlkaliMetals,
                Name = "Alkali Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.AlkaliOrganics,
                Name = "Alkali Organics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Organic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.AlkalineEarthMetals,
                Name = "Alkaline Earth Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.ComplexMetallics,
                Name = "Complex Metallics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Crystalline,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.ComplexNonMetallics,
                Name = "Complex Non-Metallics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Crystalline,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.Halogens,
                Name = "Halogens",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Reactive,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.HeavyAlkaliMetals,
                Name = "Heavy Alkali Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.RareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.HeavyAlkalineEarthMetals,
                Name = "Heavy Alkaline Earth Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.RareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.HeavyNobleGases,
                Name = "Heavy Noble Gases",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Inert,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.HeavyPostTransMetals,
                Name = "Heavy Post-Trans Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.HeavyTransMetals,
                Name = "Heavy Trans-Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.LanthanideVolatiles,
                Name = "Lanthanide Volatiles",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Volatile,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.Lanthanides,
                Name = "Lanthanides",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Volatile,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.RareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.LightHalogens,
                Name = "Light Halogens",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Reactive,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.Metallics,
                Name = "Metallics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Crystalline,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.Metaloids,
                Name = "Metaloids",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.NobleGases,
                Name = "Noble Gases",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Inert,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.NonMetallics,
                Name = "Non-Metallics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Crystalline,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.PostTransMetals,
                Name = "Post-Trans Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.S1TranslanthanicExotics,
                Name = "S1. Translanthanic Exotics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Synthetic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.S1TranslivermoricExotics,
                Name = "S1. Translivermoric Exotics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Synthetic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.S1TransuranicExotics,
                Name = "S1. Transuranic Exotics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Synthetic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.S2Element126,
                Name = "S2. Element 126",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Synthetic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.S2Element127,
                Name = "S2. Element 127",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Synthetic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.S2Superactinides,
                Name = "S2. Superactinides",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Synthetic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.StrongAcidicInorganics,
                Name = "Strong Acidic Inorganics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.NonCarbon,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.StrongAlkaliInorganics,
                Name = "Strong Alkali Inorganics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.NonCarbon,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.UncommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.StrongAlkaliOrganics,
                Name = "Strong Alkali Organics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Organic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.RareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.SuperheavyExotics,
                Name = "Superheavy Exotics",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Exotic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.VeryRareElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.TransMetals,
                Name = "Trans-Metals",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Metallic,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.CommonElements
            });
            instance.Add(new Resource()
            {
                ID = ResourceEnum.TransuranicVolatiles,
                Name = "Transuranic Volatiles",
                ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.Volatile,
                ResourceClass = Models.ResourceClass.ResourceClassEnum.RareElements
            });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new Resource() { ID = ResourceEnum.None, Name = string.Empty, ResourceGroup = Models.ResourceGroup.ResourceGroupEnum.None });

            _resourceMapByEnum = new Dictionary<ResourceEnum, Resource>();
            _resourceMapByString = new Dictionary<string, Resource>();

            foreach (Resource resource in instance)
            {
                _resourceMapByEnum[resource.ID] = resource;
                _resourceMapByString[resource.Name] = resource;
            }

            return instance;
        }
    }
}
