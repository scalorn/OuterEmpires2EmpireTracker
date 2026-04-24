using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public static class SerializationSorter
    {
        internal static T[] SortByString<T>(T[] source, Func<T, string> keySelector)
        {
            if (source == null) return Array.Empty<T>();
            return source
                .OrderBy(x => keySelector(x) ?? string.Empty, StringComparer.Ordinal)
                .ToArray();
        }

        internal static T[] SortByInt<T>(T[] source, Func<T, int> keySelector)
        {
            if (source == null) return Array.Empty<T>();
            return source
                .OrderBy(x => keySelector(x))
                .ToArray();
        }

        internal static T[] SortByStringThenInt<T>(T[] source,
            Func<T, string> key1, Func<T, int> key2)
        {
            if (source == null) return Array.Empty<T>();
            return source
                .OrderBy(x => key1(x) ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => key2(x))
                .ToArray();
        }

        internal static T[] SortByStringThenString<T>(T[] source,
            Func<T, string> key1, Func<T, string> key2)
        {
            if (source == null) return Array.Empty<T>();
            return source
                .OrderBy(x => key1(x) ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => key2(x) ?? string.Empty, StringComparer.Ordinal)
                .ToArray();
        }

        public static PlayerRoot SortPlayerRoot(PlayerRoot source)
        {
            if (source == null) return null;

            var sorted = new PlayerRoot
            {
                DataVersion = source.DataVersion,
                CurrentPlayerUUID = source.CurrentPlayerUUID,
                PlayerProfile = SortByString(source.PlayerProfile, x => x.UUID),
                Blueprint = SortByString(source.Blueprint, x => x.UUID),
                Survey = SortByString(source.Survey, x => x.UUID),
                Colony = SortByString(source.Colony, x => x.UUID),
                DeliveryRoute = SortByString(source.DeliveryRoute, x => x.UUID),
                DeliveryPlan = SortByString(source.DeliveryPlan, x => x.UUID),
                PricingPlan = SortByString(source.PricingPlan, x => x.UUID),
                BuildPlan = SortByString(source.BuildPlan, x => x.UUID),
                ShipTemplate = SortByString(source.ShipTemplate, x => x.UUID),
                Ship = SortByString(source.Ship, x => x.UUID),
                Station = SortByString(source.Station, x => x.UUID),
                MarketListing = SortByString(source.MarketListing, x => x.UUID),
                MarketTransaction = SortByString(source.MarketTransaction, x => x.UUID),
                StockPlan = SortByString(source.StockPlan, x => x.UUID),
                StockProfile = SortByString(source.StockProfile, x => x.UUID),
                SupplyChain = SortByString(source.SupplyChain, x => x.UUID),
                WarehouseOverflowRule = SortByString(source.WarehouseOverflowRule, x => x.UUID),
                Faction = SortByString(source.Faction, x => x.UUID),
                ExternalCharacter = SortByString(source.ExternalCharacter, x => x.UUID),
                Asteroid = SortByString(source.Asteroid, x => x.UUID)
            };

            // Sort nested arrays on the shared entity references
            foreach (var colony in sorted.Colony)
            {
                if (colony.Structures != null)
                    colony.Structures = SortByString(colony.Structures.ToArray(), x => x.UUID).ToList();
                if (colony.Commodities != null)
                    colony.Commodities = SortByString(colony.Commodities.ToArray(), x => x.Name).ToList();
            }

            foreach (var route in sorted.DeliveryRoute)
            {
                if (route.Stops != null)
                    route.Stops = SortByInt(route.Stops.ToArray(), x => x.Sequence).ToList();
            }

            foreach (var plan in sorted.DeliveryPlan)
            {
                if (plan.Stops != null)
                {
                    plan.Stops = SortByInt(plan.Stops.ToArray(), x => x.Sequence).ToList();
                    foreach (var stop in plan.Stops)
                    {
                        if (stop.DropOff != null)
                            stop.DropOff = SortByString(stop.DropOff.ToArray(), x => x.Name).ToList();
                        if (stop.PickUp != null)
                            stop.PickUp = SortByString(stop.PickUp.ToArray(), x => x.Name).ToList();
                    }
                }
            }

            foreach (var bp in sorted.BuildPlan)
            {
                if (bp.Items != null)
                    bp.Items = SortByString(bp.Items.ToArray(), x => x.UUID).ToList();
            }

            foreach (var template in sorted.ShipTemplate)
            {
                if (template.Components != null)
                    template.Components = SortByStringThenInt(template.Components.ToArray(), x => x.SlotType, x => x.SlotIndex).ToList();
            }

            foreach (var ship in sorted.Ship)
            {
                if (ship.Components != null)
                    ship.Components = SortByStringThenInt(ship.Components.ToArray(), x => x.SlotType, x => x.SlotIndex).ToList();
            }

            foreach (var station in sorted.Station)
            {
                if (station.Components != null)
                    station.Components = SortByStringThenInt(station.Components.ToArray(), x => x.SlotType, x => x.SlotIndex).ToList();
            }

            foreach (var sp in sorted.StockPlan)
            {
                if (sp.Targets != null)
                    sp.Targets = SortByString(sp.Targets.ToArray(), x => x.UUID).ToList();
            }

            foreach (var profile in sorted.StockProfile)
            {
                if (profile.Entries != null)
                    profile.Entries = SortByString(profile.Entries.ToArray(), x => x.GroupID).ToList();
            }

            foreach (var chain in sorted.SupplyChain)
            {
                if (chain.Stages != null)
                    chain.Stages = SortByInt(chain.Stages.ToArray(), x => x.Sequence).ToList();
            }

            foreach (var asteroid in sorted.Asteroid)
            {
                if (asteroid.Reserves != null)
                    asteroid.Reserves = SortByStringThenString(asteroid.Reserves.ToArray(), x => x.ResourceName, x => x.Purity).ToList();
            }

            return sorted;
        }

        public static BaselineRoot SortBaselineRoot(BaselineRoot source)
        {
            if (source == null) return null;

            return new BaselineRoot
            {
                DataVersion = source.DataVersion,
                GameConstants = source.GameConstants,
                BlueprintType = SortByString(source.BlueprintType, x => x.Id),
                Blueprint = SortByString(source.Blueprint, x => x.UUID),
                ShipClass = SortByInt(source.ShipClass, x => x.Id),
                TechLevel = SortByString(source.TechLevel, x => x.Name),
                Commodity = SortByString(source.Commodity, x => x.ID),
                RefiningRecipe = SortByString(source.RefiningRecipe, x => x.OutputResource),
                ResearchTime = SortByInt(source.ResearchTime, x => x.Evolution)
            };
        }
    }
}