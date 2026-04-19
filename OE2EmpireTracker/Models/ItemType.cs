using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class ItemType
    {
        public enum ItemTypeEnum
        {
            None = 0,
            Commodity,
            ShipHull,
            ShipPart,
            Munition,
            WorkDetail,
            Resource,
            Blueprint,
            Flatpack,
            Survey,
            SpaceBuildPackage,
            Share,
            Crate
        }

        public ItemTypeEnum ID { get; set; }
        public string Name { get; set; }

        private static List<ItemType> _itemTypes = getItemTypes();
        private static Dictionary<ItemTypeEnum, ItemType> _itemTypeMapByEnum;
        private static Dictionary<string, ItemType> _itemTypeMapByString;

        public static IReadOnlyList<ItemType> ItemTypes => _itemTypes.AsReadOnly();
        public static IReadOnlyDictionary<ItemTypeEnum, ItemType> ItemTypeMapByEnum => _itemTypeMapByEnum;
        public static IReadOnlyDictionary<string, ItemType> ItemTypeMapByString => _itemTypeMapByString;


        private static List<ItemType> getItemTypes()
        {
            List<ItemType> instance = new List<ItemType>();
            instance.Add(new ItemType() { ID = ItemTypeEnum.Commodity, Name = "Commodity" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.ShipHull, Name = "Hull" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.ShipPart, Name = "Part" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Munition, Name = "Munition" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.WorkDetail, Name = "Work Detail" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Resource, Name = "Resource" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Blueprint, Name = "Blueprint" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Flatpack, Name = "Flatpack" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Survey, Name = "Survey" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.SpaceBuildPackage, Name = "SpaceBuildPackage" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Share, Name = "Share" });
            instance.Add(new ItemType() { ID = ItemTypeEnum.Crate, Name = "Crate" });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new ItemType() { ID = ItemTypeEnum.None, Name = "" });

            _itemTypeMapByEnum = new Dictionary<ItemTypeEnum, ItemType>();
            _itemTypeMapByString = new Dictionary<string, ItemType>();

            foreach(ItemType itemType in instance)
            {
                _itemTypeMapByEnum[itemType.ID] = itemType;
                _itemTypeMapByString[itemType.Name] = itemType;
            }

            return instance;
        }
    }
}
