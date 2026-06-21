// -----------------------------------------------------------------------
// <copyright file="PostgresBackend.Helpers.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// Private helper methods for reading/writing entities from PostgreSQL.
    /// </summary>
    internal partial class PostgresBackend
    {
// Private Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Colony ReadColonyParent(NpgsqlDataReader reader)
        {
            var colony = new Colony
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                LegacyUUID = reader["LegacyUUID"] as string,
                PlanetName = reader["PlanetName"] as string,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                ColonyName = reader["ColonyName"] as string,
                LastImportDateTime = reader["LastImportDateTime"] as string,
                ColonyId = Convert.ToInt32(reader["ColonyId"]),
                SystemId = Convert.ToInt32(reader["SystemId"]),
                ColonySize = Convert.ToInt32(reader["ColonySize"]),
                Distance = Convert.ToDecimal(reader["Distance"]),
                SurfaceVariation = Convert.ToInt32(reader["SurfaceVariation"]),
                AtmosVariation = Convert.ToInt32(reader["AtmosVariation"]),
                HexValue = reader["HexValue"] as string ?? string.Empty,
                SystemObjectTypeName = reader["SystemObjectTypeName"] as string ?? string.Empty,
                ImagePreFix = reader["ImagePreFix"] as string ?? string.Empty,
                ManufacturingBlocked = Convert.ToInt32(reader["ManufacturingBlocked"]),
                WorkerCurrentAttitude = Convert.ToInt32(reader["WorkerCurrentAttitude"]),
                ContentmentIndex = Convert.ToInt32(reader["ContentmentIndex"]),
                BlueCollarAllocated = Convert.ToInt32(reader["BlueCollarAllocated"]),
                BlueCollarUnallocated = Convert.ToInt32(reader["BlueCollarUnallocated"]),
                WhiteCollarAllocated = Convert.ToInt32(reader["WhiteCollarAllocated"]),
                WhiteCollarUnallocated = Convert.ToInt32(reader["WhiteCollarUnallocated"]),
                SpecialistAllocated = Convert.ToInt32(reader["SpecialistAllocated"]),
                SpecialistUnallocated = Convert.ToInt32(reader["SpecialistUnallocated"]),
                WageLevel = Convert.ToInt32(reader["WageLevel"]),
            };
            return colony;
        }

        private static List<ColonyStructure> LoadColonyStructures(NpgsqlConnection conn, string colonyUUID)
        {
            var structures = new List<ColonyStructure>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ColonyStructures WHERE ColonyUUID = @cid ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@cid", colonyUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var s = new ColonyStructure
                        {
                            UUID = reader["UUID"] as string,
                            FlatpackBlueprintUUID = reader["FlatpackBlueprintUUID"] as string,
                            DisplaySequence = Convert.ToInt32(reader["DisplaySequence"]),
                            BuildingID = Convert.ToInt32(reader["BuildingID"]),
                            BuildQueueSequence = Convert.ToInt32(reader["BuildQueueSequence"]),
                            MiningSurvey = reader["MiningSurvey"] as string,
                            MiningSurveyResource = reader["MiningSurveyResource"] as string,
                            MiningLeftOvers = Convert.ToDecimal(reader["MiningLeftOvers"]),
                            RefiningResource = reader["RefiningResource"] as string,
                            RefiningResourcePurity = reader["RefiningResourcePurity"] as string,
                            ResearchingBlueprintUUID = reader["ResearchingBlueprintUUID"] as string,
                            ManufacturingBlueprintUUID = reader["ManufacturingBlueprintUUID"] as string,
                            ManufacturingCommodityName = reader["ManufacturingCommodityName"] as string,
                            ManufacturingQuantity = Convert.ToInt32(reader["ManufacturingQuantity"]),
                            ManufacturingCompleted = Convert.ToInt32(reader["ManufacturingCompleted"]),
                            StagingResources = Convert.ToInt32(reader["StagingResources"]) != 0,
                            ColonyBuildingTypeId = Convert.ToInt32(reader["ColonyBuildingTypeId"]),
                            ResourceId = Convert.ToInt32(reader["ResourceId"]),
                            ResourceIcon = reader["ResourceIcon"] as string ?? string.Empty,
                            ManufactureAmountPerRun = Convert.ToInt32(reader["ManufactureAmountPerRun"]),
                            DurabilityCurrent = Convert.ToDecimal(reader["DurabilityCurrent"]),
                            DurabilityMax = Convert.ToDecimal(reader["DurabilityMax"]),
                            WageLevel = Convert.ToInt32(reader["WageLevel"]),
                        };

                        // BuildCompletionTime
                        var buildStart = reader["BuildCompletion_StartTime"] as string;
                        if (buildStart != null)
                        {
                            s.BuildCompletionTime = new CountDownTime
                            {
                                StartTime = DateTime.Parse(buildStart),
                                RepeatIntervalSeconds = reader["BuildCompletion_RepeatIntervalSeconds"] == DBNull.Value ? 0 : Convert.ToInt64(reader["BuildCompletion_RepeatIntervalSeconds"]),
                            };
                        }

                        // ProcessCompletionTime
                        var procStart = reader["ProcessCompletion_StartTime"] as string;
                        if (procStart != null)
                        {
                            s.ProcessCompletionTime = new CountDownTime
                            {
                                StartTime = DateTime.Parse(procStart),
                                RepeatIntervalSeconds = reader["ProcessCompletion_RepeatIntervalSeconds"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ProcessCompletion_RepeatIntervalSeconds"]),
                            };
                        }

                        // Load Properties and AssignedWorkers
                        s.Properties = LoadPropertyBag(conn, "ColonyStructureProperties", "StructureUUID", s.UUID);
                        s.AssignedWorkers = LoadPropertyBag(conn, "ColonyStructureWorkers", "StructureUUID", s.UUID);

                        structures.Add(s);
                    }
                }
            }

            return structures;
        }

        private static PropertyBag LoadPropertyBag(NpgsqlConnection conn, string tableName, string fkColumn, string fkValue)
        {
            var bag = new PropertyBag();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT Key, Value FROM {tableName} WHERE {fkColumn} = @fk";
                cmd.Parameters.AddWithValue("@fk", fkValue);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bag.Properties[reader.GetString(0)] = reader.GetString(1);
                    }
                }
            }

            return bag;
        }

        private static ItemBag LoadItems(NpgsqlConnection conn, string parentUUID, string parentType)
        {
            var bag = new ItemBag();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Items WHERE ParentUUID = @pid AND ParentType = @pt";
                cmd.Parameters.AddWithValue("@pid", parentUUID);
                cmd.Parameters.AddWithValue("@pt", parentType);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Item
                        {
                            UUID = reader["UUID"] as string,
                            ItemType = Enum.TryParse<ItemType.ItemTypeEnum>(reader["ItemType"] as string, true, out var it) ? it : ItemType.ItemTypeEnum.None,
                            BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            NickName = reader["NickName"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            ResourcePurity = reader["ResourcePurity"] as string ?? string.Empty,
                            Volume = Convert.ToDecimal(reader["Volume"]),
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                            MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
                            ShipPartType = reader["ShipPartType"] as string ?? string.Empty,
                            JobName = reader["JobName"] as string ?? string.Empty,
                            JobTrack = reader["JobTrack"] as string ?? string.Empty,
                        };

                        if (reader[BlueprintPropertyKeys.Mass] != DBNull.Value)
                        {
                            item.Mass = Convert.ToDecimal(reader[BlueprintPropertyKeys.Mass]);
                        }

                        if (reader["GameItemId"] != DBNull.Value)
                        {
                            item.GameItemId = Convert.ToInt32(reader["GameItemId"]);
                        }

                        if (reader["JobRef"] != DBNull.Value)
                        {
                            item.JobRef = Convert.ToInt32(reader["JobRef"]);
                        }

                        if (reader["JobDeliveryLoc"] != DBNull.Value)
                        {
                            item.JobDeliveryLoc = Convert.ToInt32(reader["JobDeliveryLoc"]);
                        }

                        if (reader["HealthPercentage"] != DBNull.Value)
                        {
                            item.HealthPercentage = Convert.ToDecimal(reader["HealthPercentage"]);
                        }

                        if (reader["LastRepairHealthPercentage"] != DBNull.Value)
                        {
                            item.LastRepairHealthPercentage = Convert.ToDecimal(reader["LastRepairHealthPercentage"]);
                        }

                        if (reader["Evolution"] != DBNull.Value)
                        {
                            item.Evolution = Convert.ToInt32(reader["Evolution"]);
                        }

                        bag.Items[item.UUID] = item;
                    }
                }
            }

            return bag;
        }

        private static void UpsertColonyParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, Colony entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Colonies (
                    UUID, OwnerUUID, LegacyUUID, PlanetName, SystemName, ColonyName,
                    LastImportDateTime, ColonyId, SystemId, ColonySize, Distance,
                    SurfaceVariation, AtmosVariation, HexValue, SystemObjectTypeName,
                    ImagePreFix, ManufacturingBlocked, WorkerCurrentAttitude, ContentmentIndex,
                    BlueCollarAllocated, BlueCollarUnallocated, WhiteCollarAllocated,
                    WhiteCollarUnallocated, SpecialistAllocated, SpecialistUnallocated, WageLevel
                ) VALUES (
                    @uuid, @owner, @legacy, @planet, @system, @colName,
                    @lastImport, @colId, @sysId, @colSize, @distance,
                    @surfVar, @atmosVar, @hex, @sysObjType,
                    @imgPre, @mfgBlocked, @attitude, @contentment,
                    @bcAlloc, @bcUnalloc, @wcAlloc,
                    @wcUnalloc, @specAlloc, @specUnalloc, @wage
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@legacy", (object)entity.LegacyUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@planet", (object)entity.PlanetName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@system", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@colName", (object)entity.ColonyName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@lastImport", (object)entity.LastImportDateTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@colId", entity.ColonyId);
                cmd.Parameters.AddWithValue("@sysId", entity.SystemId);
                cmd.Parameters.AddWithValue("@colSize", entity.ColonySize);
                cmd.Parameters.AddWithValue("@distance", (double)entity.Distance);
                cmd.Parameters.AddWithValue("@surfVar", entity.SurfaceVariation);
                cmd.Parameters.AddWithValue("@atmosVar", entity.AtmosVariation);
                cmd.Parameters.AddWithValue("@hex", entity.HexValue ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysObjType", entity.SystemObjectTypeName ?? string.Empty);
                cmd.Parameters.AddWithValue("@imgPre", entity.ImagePreFix ?? string.Empty);
                cmd.Parameters.AddWithValue("@mfgBlocked", entity.ManufacturingBlocked);
                cmd.Parameters.AddWithValue("@attitude", entity.WorkerCurrentAttitude);
                cmd.Parameters.AddWithValue("@contentment", entity.ContentmentIndex);
                cmd.Parameters.AddWithValue("@bcAlloc", entity.BlueCollarAllocated);
                cmd.Parameters.AddWithValue("@bcUnalloc", entity.BlueCollarUnallocated);
                cmd.Parameters.AddWithValue("@wcAlloc", entity.WhiteCollarAllocated);
                cmd.Parameters.AddWithValue("@wcUnalloc", entity.WhiteCollarUnallocated);
                cmd.Parameters.AddWithValue("@specAlloc", entity.SpecialistAllocated);
                cmd.Parameters.AddWithValue("@specUnalloc", entity.SpecialistUnallocated);
                cmd.Parameters.AddWithValue("@wage", entity.WageLevel);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteColonyChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string colonyUUID)
        {
            // Delete items (polymorphic FK, no CASCADE from Colonies)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND ParentType = 'Colony'";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                cmd.ExecuteNonQuery();
            }

            // Delete structure items
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"DELETE FROM Items WHERE ParentType = 'ColonyStructure' AND ParentUUID IN
                    (SELECT UUID FROM ColonyStructures WHERE ColonyUUID = @uuid)";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                cmd.ExecuteNonQuery();
            }

            // CASCADE handles ColonyStructureProperties and ColonyStructureWorkers
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ColonyStructures WHERE ColonyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertColonyStructures(NpgsqlConnection conn, NpgsqlTransaction tx, Colony entity)
        {
            for (int i = 0; i < entity.Structures.Count; i++)
            {
                var s = entity.Structures[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO ColonyStructures (
                        UUID, ColonyUUID, Sequence, FlatpackBlueprintUUID, DisplaySequence,
                        BuildingID, BuildQueueSequence, MiningSurvey, MiningSurveyResource,
                        MiningLeftOvers, RefiningResource, RefiningResourcePurity,
                        ResearchingBlueprintUUID, ManufacturingBlueprintUUID,
                        ManufacturingCommodityName, ManufacturingQuantity, ManufacturingCompleted,
                        StagingResources, ColonyBuildingTypeId, ResourceId, ResourceIcon,
                        ManufactureAmountPerRun, DurabilityCurrent, DurabilityMax, WageLevel,
                        BuildCompletion_StartTime, BuildCompletion_RepeatIntervalSeconds, BuildCompletion_IsRepeating,
                        ProcessCompletion_StartTime, ProcessCompletion_RepeatIntervalSeconds, ProcessCompletion_IsRepeating
                    ) VALUES (
                        @uuid, @colUUID, @seq, @flatpack, @dispSeq,
                        @buildId, @bqSeq, @minSurvey, @minRes,
                        @minLeft, @refRes, @refPurity,
                        @resBp, @mfgBp,
                        @mfgComm, @mfgQty, @mfgDone,
                        @staging, @cbTypeId, @resId, @resIcon,
                        @mfgPerRun, @durCur, @durMax, @wage,
                        @bcStart, @bcInterval, @bcRepeat,
                        @pcStart, @pcInterval, @pcRepeat
                    )";
                    cmd.Parameters.AddWithValue("@uuid", s.UUID);
                    cmd.Parameters.AddWithValue("@colUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@flatpack", (object)s.FlatpackBlueprintUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dispSeq", s.DisplaySequence);
                    cmd.Parameters.AddWithValue("@buildId", s.BuildingID);
                    cmd.Parameters.AddWithValue("@bqSeq", s.BuildQueueSequence);
                    cmd.Parameters.AddWithValue("@minSurvey", (object)s.MiningSurvey ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@minRes", (object)s.MiningSurveyResource ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@minLeft", (double)s.MiningLeftOvers);
                    cmd.Parameters.AddWithValue("@refRes", (object)s.RefiningResource ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@refPurity", (object)s.RefiningResourcePurity ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@resBp", (object)s.ResearchingBlueprintUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mfgBp", (object)s.ManufacturingBlueprintUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mfgComm", (object)s.ManufacturingCommodityName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mfgQty", s.ManufacturingQuantity);
                    cmd.Parameters.AddWithValue("@mfgDone", s.ManufacturingCompleted);
                    cmd.Parameters.AddWithValue("@staging", s.StagingResources ? 1 : 0);
                    cmd.Parameters.AddWithValue("@cbTypeId", s.ColonyBuildingTypeId);
                    cmd.Parameters.AddWithValue("@resId", s.ResourceId);
                    cmd.Parameters.AddWithValue("@resIcon", s.ResourceIcon ?? string.Empty);
                    cmd.Parameters.AddWithValue("@mfgPerRun", s.ManufactureAmountPerRun);
                    cmd.Parameters.AddWithValue("@durCur", (double)s.DurabilityCurrent);
                    cmd.Parameters.AddWithValue("@durMax", (double)s.DurabilityMax);
                    cmd.Parameters.AddWithValue("@wage", s.WageLevel);

                    // BuildCompletionTime
                    if (s.BuildCompletionTime != null)
                    {
                        cmd.Parameters.AddWithValue("@bcStart", s.BuildCompletionTime.StartTime.ToString("O"));
                        cmd.Parameters.AddWithValue("@bcInterval", s.BuildCompletionTime.RepeatIntervalSeconds);
                        cmd.Parameters.AddWithValue("@bcRepeat", s.BuildCompletionTime.IsRepeating ? 1 : 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@bcStart", DBNull.Value);
                        cmd.Parameters.AddWithValue("@bcInterval", DBNull.Value);
                        cmd.Parameters.AddWithValue("@bcRepeat", DBNull.Value);
                    }

                    // ProcessCompletionTime
                    if (s.ProcessCompletionTime != null)
                    {
                        cmd.Parameters.AddWithValue("@pcStart", s.ProcessCompletionTime.StartTime.ToString("O"));
                        cmd.Parameters.AddWithValue("@pcInterval", s.ProcessCompletionTime.RepeatIntervalSeconds);
                        cmd.Parameters.AddWithValue("@pcRepeat", s.ProcessCompletionTime.IsRepeating ? 1 : 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@pcStart", DBNull.Value);
                        cmd.Parameters.AddWithValue("@pcInterval", DBNull.Value);
                        cmd.Parameters.AddWithValue("@pcRepeat", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }

                // Insert properties
                foreach (var kvp in s.Properties.Properties)
                {
                    using (var propCmd = conn.CreateCommand())
                    {
                        propCmd.Transaction = tx;
                        propCmd.CommandText = "INSERT INTO ColonyStructureProperties (StructureUUID, Key, Value) VALUES (@sid, @key, @val)";
                        propCmd.Parameters.AddWithValue("@sid", s.UUID);
                        propCmd.Parameters.AddWithValue("@key", kvp.Key);
                        propCmd.Parameters.AddWithValue("@val", kvp.Value);
                        propCmd.ExecuteNonQuery();
                    }
                }

                // Insert assigned workers
                foreach (var kvp in s.AssignedWorkers.Properties)
                {
                    using (var wCmd = conn.CreateCommand())
                    {
                        wCmd.Transaction = tx;
                        wCmd.CommandText = "INSERT INTO ColonyStructureWorkers (StructureUUID, Key, Value) VALUES (@sid, @key, @val)";
                        wCmd.Parameters.AddWithValue("@sid", s.UUID);
                        wCmd.Parameters.AddWithValue("@key", kvp.Key);
                        wCmd.Parameters.AddWithValue("@val", kvp.Value);
                        wCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void InsertItems(NpgsqlConnection conn, NpgsqlTransaction tx, string parentUUID, string parentType, ItemBag items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var kvp in items.Items)
            {
                var item = kvp.Value;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO Items (
                        UUID, ParentUUID, ParentType, ItemType, BaseItemTypeID, Name, NickName,
                        Description, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP,
                        MaxRepairPercent, Mass, GameItemId, JobRef, JobDeliveryLoc,
                        HealthPercentage, LastRepairHealthPercentage, Evolution,
                        ShipPartType, JobName, JobTrack
                    ) VALUES (
                        @uuid, @parent, @ptype, @itype, @baseId, @name, @nick,
                        @desc, @qty, @purity, @vol, @curHp, @maxHp,
                        @maxRepair, @mass, @gameId, @jobRef, @jobDel,
                        @health, @lastRepair, @evo,
                        @shipPart, @jobName, @jobTrack
                    )";
                    cmd.Parameters.AddWithValue("@uuid", item.UUID);
                    cmd.Parameters.AddWithValue("@parent", parentUUID);
                    cmd.Parameters.AddWithValue("@ptype", parentType);
                    cmd.Parameters.AddWithValue("@itype", item.ItemType.ToString());
                    cmd.Parameters.AddWithValue("@baseId", item.BaseItemTypeID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@nick", item.NickName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", item.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@purity", item.ResourcePurity ?? string.Empty);
                    cmd.Parameters.AddWithValue("@vol", (double)item.Volume);
                    cmd.Parameters.AddWithValue("@curHp", item.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", item.MaxHP);
                    cmd.Parameters.AddWithValue("@maxRepair", (double)item.MaxRepairPercent);
                    cmd.Parameters.AddWithValue("@mass", item.Mass.HasValue ? (object)(double)item.Mass.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@gameId", item.GameItemId.HasValue ? (object)item.GameItemId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@jobRef", item.JobRef.HasValue ? (object)item.JobRef.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@jobDel", item.JobDeliveryLoc.HasValue ? (object)item.JobDeliveryLoc.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@health", item.HealthPercentage.HasValue ? (object)(double)item.HealthPercentage.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastRepair", item.LastRepairHealthPercentage.HasValue ? (object)(double)item.LastRepairHealthPercentage.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@evo", item.Evolution.HasValue ? (object)item.Evolution.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@shipPart", item.ShipPartType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@jobName", item.JobName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@jobTrack", item.JobTrack ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Blueprint Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Blueprint ReadBlueprintParent(NpgsqlDataReader reader)
        {
            var bp = new Blueprint
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                BaseBlueprintUUID = reader["BaseBlueprintUUID"] as string,
                LegacyUUID = reader["LegacyUUID"] as string,
                BluePrintType = reader["BluePrintType"] as string,
                TechLevel = reader["TechLevel"] as string,
                Class = Convert.ToInt32(reader["Class"]),
                Evolution = Convert.ToInt32(reader["Evolution"]),
                CopyCost = Convert.ToInt32(reader["CopyCost"]),
                BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                Name = reader["Name"] as string ?? string.Empty,
                NickName = reader["NickName"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Volume = Convert.ToDecimal(reader["Volume"]),
            };

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                bp.ItemType = it;
            }

            if (reader["GameApiBlueprintId"] != DBNull.Value)
            {
                bp.GameApiBlueprintId = Convert.ToInt32(reader["GameApiBlueprintId"]);
            }

            if (reader["LastDetailImportUtc"] != DBNull.Value)
            {
                var dtStr = reader["LastDetailImportUtc"] as string;
                if (dtStr != null)
                {
                    bp.LastDetailImportUtc = DateTime.Parse(dtStr);
                }
            }

            return bp;
        }

        private static Dictionary<string, string> LoadBlueprintResources(NpgsqlConnection conn, string blueprintUUID)
        {
            var resources = new Dictionary<string, string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ResourceName, Amount FROM BlueprintResources WHERE BlueprintUUID = @bpUUID";
                cmd.Parameters.AddWithValue("@bpUUID", blueprintUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var amount = Convert.ToInt32(reader[1]);
                        resources[name] = amount.ToString();
                    }
                }
            }

            return resources;
        }

        private static void UpsertBlueprintParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, Blueprint entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Blueprints (
                    UUID, OwnerUUID, BaseBlueprintUUID, LegacyUUID, BluePrintType,
                    TechLevel, Class, Evolution, CopyCost, ItemType,
                    BaseItemTypeID, Name, NickName, Description, Quantity, Volume,
                    GameApiBlueprintId, LastDetailImportUtc
                ) VALUES (
                    @uuid, @owner, @baseBp, @legacy, @bpType,
                    @tech, @class, @evo, @copyCost, @itemType,
                    @baseId, @name, @nick, @desc, @qty, @vol,
                    @gameApiId, @lastImport
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@baseBp", (object)entity.BaseBlueprintUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@legacy", (object)entity.LegacyUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bpType", (object)entity.BluePrintType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tech", (object)entity.TechLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@class", entity.Class);
                cmd.Parameters.AddWithValue("@evo", entity.Evolution);
                cmd.Parameters.AddWithValue("@copyCost", entity.CopyCost);
                cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                cmd.Parameters.AddWithValue("@baseId", entity.BaseItemTypeID ?? string.Empty);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@nick", entity.NickName ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                cmd.Parameters.AddWithValue("@vol", (double)entity.Volume);
                cmd.Parameters.AddWithValue("@gameApiId", entity.GameApiBlueprintId.HasValue ? (object)entity.GameApiBlueprintId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@lastImport", entity.LastDetailImportUtc.HasValue ? (object)entity.LastDetailImportUtc.Value.ToString("O") : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteBlueprintChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string blueprintUUID)
        {
            // CASCADE handles these, but explicit delete within transaction is cleaner for INSERT OR REPLACE
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BlueprintProperties WHERE BlueprintUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", blueprintUUID);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BlueprintResources WHERE BlueprintUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", blueprintUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertBlueprintChildren(NpgsqlConnection conn, NpgsqlTransaction tx, Blueprint entity)
        {
            if (entity.Properties != null)
            {
                foreach (var kvp in entity.Properties.Properties)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO BlueprintProperties (BlueprintUUID, Key, Value) VALUES (@bpUUID, @key, @val)";
                        cmd.Parameters.AddWithValue("@bpUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@key", kvp.Key);
                        cmd.Parameters.AddWithValue("@val", kvp.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (entity.Resources != null)
            {
                foreach (var kvp in entity.Resources)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO BlueprintResources (BlueprintUUID, ResourceName, Amount) VALUES (@bpUUID, @resName, @amount)";
                        cmd.Parameters.AddWithValue("@bpUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@resName", kvp.Key);
                        cmd.Parameters.AddWithValue("@amount", int.TryParse(kvp.Value, out var amt) ? amt : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Survey Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Survey ReadSurveyParent(NpgsqlDataReader reader)
        {
            var survey = new Survey
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                Name = reader["Name"] as string ?? string.Empty,
                NickName = reader["NickName"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Volume = Convert.ToDecimal(reader["Volume"]),
                ScannedBy = reader["ScannedBy"] as string,
                DateTime = reader["DateTime"] as string,
                PlanetName = reader["PlanetName"] as string,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SurveyID = reader["SurveyID"] as string,
                ScannerBlueprintUUID = reader["ScannerBlueprintUUID"] as string,
                AsteroidUUID = reader["AsteroidUUID"] as string ?? string.Empty,
                SystemObjectId = Convert.ToInt32(reader["SystemObjectId"]),
            };

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                survey.ItemType = it;
            }

            var surveyTypeStr = reader["SurveyType"] as string;
            if (Enum.TryParse<SurveyType>(surveyTypeStr, true, out var st))
            {
                survey.SurveyType = st;
            }

            if (reader["GameApiSurveyId"] != DBNull.Value)
            {
                survey.GameApiSurveyId = Convert.ToInt32(reader["GameApiSurveyId"]);
            }

            if (reader["LastDetailImportUtc"] != DBNull.Value)
            {
                var dtStr = reader["LastDetailImportUtc"] as string;
                if (dtStr != null)
                {
                    survey.LastDetailImportUtc = DateTime.Parse(dtStr);
                }
            }

            return survey;
        }

        private static Dictionary<string, string> LoadSurveyProperties(NpgsqlConnection conn, string surveyUUID)
        {
            var props = new Dictionary<string, string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Key, Value FROM SurveyProperties WHERE SurveyUUID = @sUUID";
                cmd.Parameters.AddWithValue("@sUUID", surveyUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        props[reader.GetString(0)] = reader.GetString(1);
                    }
                }
            }

            return props;
        }

        private static Dictionary<string, SurveyResource> LoadSurveyResources(NpgsqlConnection conn, string surveyUUID)
        {
            var resources = new Dictionary<string, SurveyResource>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ResourceKey, Resource, Purity, Amount FROM SurveyResources WHERE SurveyUUID = @sUUID";
                cmd.Parameters.AddWithValue("@sUUID", surveyUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        resources[key] = new SurveyResource
                        {
                            Resource = reader["Resource"] as string ?? string.Empty,
                            Purity = reader["Purity"] as string ?? string.Empty,
                            Amount = Convert.ToInt32(reader["Amount"]).ToString(),
                        };
                    }
                }
            }

            return resources;
        }

        private static void UpsertSurveyParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, Survey entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Surveys (
                    UUID, OwnerUUID, ItemType, BaseItemTypeID, Name, NickName,
                    Description, Quantity, Volume, ScannedBy, DateTime,
                    PlanetName, SystemName, SurveyID, ScannerBlueprintUUID,
                    SurveyType, AsteroidUUID, SystemObjectId,
                    GameApiSurveyId, LastDetailImportUtc
                ) VALUES (
                    @uuid, @owner, @itemType, @baseId, @name, @nick,
                    @desc, @qty, @vol, @scannedBy, @dateTime,
                    @planet, @system, @surveyId, @scannerBp,
                    @surveyType, @asteroidUUID, @sysObjId,
                    @gameApiId, @lastImport
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                cmd.Parameters.AddWithValue("@baseId", entity.BaseItemTypeID ?? string.Empty);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@nick", entity.NickName ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                cmd.Parameters.AddWithValue("@vol", (double)entity.Volume);
                cmd.Parameters.AddWithValue("@scannedBy", (object)entity.ScannedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dateTime", (object)entity.DateTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@planet", (object)entity.PlanetName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@system", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@surveyId", (object)entity.SurveyID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@scannerBp", (object)entity.ScannerBlueprintUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@surveyType", entity.SurveyType.ToString());
                cmd.Parameters.AddWithValue("@asteroidUUID", entity.AsteroidUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysObjId", entity.SystemObjectId);
                cmd.Parameters.AddWithValue("@gameApiId", entity.GameApiSurveyId.HasValue ? (object)entity.GameApiSurveyId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@lastImport", entity.LastDetailImportUtc.HasValue ? (object)entity.LastDetailImportUtc.Value.ToString("O") : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteSurveyChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string surveyUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SurveyProperties WHERE SurveyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", surveyUUID);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SurveyResources WHERE SurveyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", surveyUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertSurveyChildren(NpgsqlConnection conn, NpgsqlTransaction tx, Survey entity)
        {
            if (entity.Properties != null)
            {
                foreach (var kvp in entity.Properties)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO SurveyProperties (SurveyUUID, Key, Value) VALUES (@sUUID, @key, @val)";
                        cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@key", kvp.Key);
                        cmd.Parameters.AddWithValue("@val", kvp.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (entity.Resources != null)
            {
                foreach (var kvp in entity.Resources)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO SurveyResources (SurveyUUID, ResourceKey, Resource, Purity, Amount) VALUES (@sUUID, @resKey, @res, @purity, @amount)";
                        cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@resKey", kvp.Key);
                        cmd.Parameters.AddWithValue("@res", kvp.Value.Resource ?? string.Empty);
                        cmd.Parameters.AddWithValue("@purity", kvp.Value.Purity ?? string.Empty);
                        cmd.Parameters.AddWithValue("@amount", int.TryParse(kvp.Value.Amount, out var amt) ? amt : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Station Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Station ReadStationParent(NpgsqlDataReader reader)
        {
            var station = new Station
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SystemId = Convert.ToInt32(reader["SystemId"]),
            };

            if (reader["GameLocationId"] != DBNull.Value)
            {
                station.GameLocationId = Convert.ToInt32(reader["GameLocationId"]);
            }

            return station;
        }

        private static List<ShipComponentSlot> LoadStationComponents(NpgsqlConnection conn, string stationUUID)
        {
            var components = new List<ShipComponentSlot>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StationComponents WHERE StationUUID = @sUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@sUUID", stationUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        components.Add(new ShipComponentSlot
                        {
                            SlotType = reader["SlotType"] as string ?? string.Empty,
                            BlueprintUUID = reader["BlueprintUUID"] as string ?? string.Empty,
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                        });
                    }
                }
            }

            return components;
        }

        private static void LoadStationItems(NpgsqlConnection conn, Station station)
        {
            station.MunitionsHold = LoadItems(conn, station.UUID, "StationMunitions");
            station.Holds = new Dictionary<string, ItemBag>();

            // Load all station hold items grouped by ParentType pattern 'StationHold:holdName'
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT ParentType FROM Items WHERE ParentUUID = @uuid AND ParentType LIKE 'StationHold:%'";
                cmd.Parameters.AddWithValue("@uuid", station.UUID);
                using (var reader = cmd.ExecuteReader())
                {
                    var holdTypes = new List<string>();
                    while (reader.Read())
                    {
                        holdTypes.Add(reader.GetString(0));
                    }

                    foreach (var holdType in holdTypes)
                    {
                        var holdName = holdType.Substring("StationHold:".Length);
                        station.Holds[holdName] = LoadItems(conn, station.UUID, holdType);
                    }
                }
            }
        }

        private static void UpsertStationParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, Station entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Stations (UUID, Name, OwnerUUID, SystemName, SystemId, SystemObjectId, GameLocationId)
                                   VALUES (@uuid, @name, @owner, @sysName, @sysId, @sysObjId, @gameLocId)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@sysName", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysId", entity.SystemId ?? 0);
                cmd.Parameters.AddWithValue("@sysObjId", 0);
                cmd.Parameters.AddWithValue("@gameLocId", entity.GameLocationId.HasValue ? (object)entity.GameLocationId.Value : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteStationChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string stationUUID)
        {
            // Delete items (polymorphic FK, no CASCADE)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'StationMunitions' OR ParentType LIKE 'StationHold:%')";
                cmd.Parameters.AddWithValue("@uuid", stationUUID);
                cmd.ExecuteNonQuery();
            }

            // Delete components
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StationComponents WHERE StationUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", stationUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertStationComponents(NpgsqlConnection conn, NpgsqlTransaction tx, Station entity)
        {
            if (entity.Components == null)
            {
                return;
            }

            for (int i = 0; i < entity.Components.Count; i++)
            {
                var comp = entity.Components[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO StationComponents (StationUUID, Sequence, SlotType, BlueprintUUID, CurrentHP, MaxHP)
                                       VALUES (@sUUID, @seq, @slotType, @bpUUID, @curHp, @maxHp)";
                    cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@slotType", comp.SlotType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bpUUID", comp.BlueprintUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", comp.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", comp.MaxHP);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void InsertStationItems(NpgsqlConnection conn, NpgsqlTransaction tx, Station entity)
        {
            // Insert munitions hold
            InsertItems(conn, tx, entity.UUID, "StationMunitions", entity.MunitionsHold);

            // Insert named holds
            if (entity.Holds != null)
            {
                foreach (var kvp in entity.Holds)
                {
                    InsertItems(conn, tx, entity.UUID, "StationHold:" + kvp.Key, kvp.Value);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Asteroid Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Asteroid ReadAsteroidParent(NpgsqlDataReader reader)
        {
            var asteroid = new Asteroid
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SystemObjectId = Convert.ToInt32(reader["SystemObjectId"]),
            };

            return asteroid;
        }

        private static List<AsteroidReserve> LoadAsteroidReserves(NpgsqlConnection conn, string asteroidUUID)
        {
            var reserves = new List<AsteroidReserve>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM AsteroidReserves WHERE AsteroidUUID = @aUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@aUUID", asteroidUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var reserve = new AsteroidReserve
                        {
                            ResourceName = reader["ResourceName"] as string ?? string.Empty,
                            Purity = reader["Purity"] as string ?? string.Empty,
                            MaxReserve = Convert.ToInt32(reader["MaxReserve"]),
                        };

                        if (reader["CurrentReserve"] != DBNull.Value)
                        {
                            reserve.CurrentReserve = Convert.ToInt32(reader["CurrentReserve"]);
                        }

                        if (reader["ResetTimestamp"] != DBNull.Value)
                        {
                            reserve.ResetTimestamp = reader["ResetTimestamp"] as string ?? string.Empty;
                        }

                        reserves.Add(reserve);
                    }
                }
            }

            return reserves;
        }

        private static void UpsertAsteroidParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, Asteroid entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Asteroids (UUID, Name, OwnerUUID, SystemName, SystemId, SystemObjectId, GameApiAsteroidId)
                                   VALUES (@uuid, @name, @owner, @sysName, @sysId, @sysObjId, @gameApiId)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@sysName", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysId", 0);
                cmd.Parameters.AddWithValue("@sysObjId", entity.SystemObjectId);
                cmd.Parameters.AddWithValue("@gameApiId", DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteAsteroidChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string asteroidUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM AsteroidReserves WHERE AsteroidUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", asteroidUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertAsteroidReserves(NpgsqlConnection conn, NpgsqlTransaction tx, Asteroid entity)
        {
            if (entity.Reserves == null)
            {
                return;
            }

            for (int i = 0; i < entity.Reserves.Count; i++)
            {
                var reserve = entity.Reserves[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO AsteroidReserves (AsteroidUUID, Sequence, ResourceName, Purity, MaxReserve, CurrentReserve, ResetTimestamp)
                                       VALUES (@aUUID, @seq, @resName, @purity, @maxRes, @curRes, @resetTs)";
                    cmd.Parameters.AddWithValue("@aUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", reserve.ResourceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@purity", reserve.Purity ?? string.Empty);
                    cmd.Parameters.AddWithValue("@maxRes", reserve.MaxReserve);
                    cmd.Parameters.AddWithValue("@curRes", reserve.CurrentReserve != 0 ? (object)reserve.CurrentReserve : DBNull.Value);
                    cmd.Parameters.AddWithValue("@resetTs", !string.IsNullOrEmpty(reserve.ResetTimestamp) ? (object)reserve.ResetTimestamp : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // SupplyChain Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static SupplyChain ReadSupplyChainParent(NpgsqlDataReader reader)
        {
            return new SupplyChain
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<SupplyChainStage> LoadSupplyChainStages(NpgsqlConnection conn, string chainUUID)
        {
            var stages = new List<SupplyChainStage>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SupplyChainStages WHERE SupplyChainUUID = @cUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@cUUID", chainUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stages.Add(new SupplyChainStage
                        {
                            Sequence = Convert.ToInt32(reader["Sequence"]),
                            LocationUUID = reader["ColonyUUID"] as string ?? string.Empty,
                            ResourceName = reader["OutputItemType"] as string ?? string.Empty,
                            AccumulationThreshold = Convert.ToInt32(reader["OutputQuantity"]),
                        });
                    }
                }
            }

            return stages;
        }

        private static void UpsertSupplyChainParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, SupplyChain entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO SupplyChains (UUID, Name, OwnerUUID, Description)
                                   VALUES (@uuid, @name, @owner, @desc)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@desc", string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteSupplyChainChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string chainUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SupplyChainStages WHERE SupplyChainUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", chainUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertSupplyChainStages(NpgsqlConnection conn, NpgsqlTransaction tx, SupplyChain entity)
        {
            if (entity.Stages == null)
            {
                return;
            }

            for (int i = 0; i < entity.Stages.Count; i++)
            {
                var stage = entity.Stages[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO SupplyChainStages (SupplyChainUUID, Sequence, ColonyUUID, BlueprintUUID, OutputItemType, OutputQuantity)
                                       VALUES (@cUUID, @seq, @colUUID, @bpUUID, @outputType, @outputQty)";
                    cmd.Parameters.AddWithValue("@cUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@colUUID", stage.LocationUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bpUUID", string.Empty);
                    cmd.Parameters.AddWithValue("@outputType", stage.ResourceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@outputQty", stage.AccumulationThreshold);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // BuildPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static BuildPlan ReadBuildPlanParent(NpgsqlDataReader reader)
        {
            return new BuildPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<BuildItem> LoadBuildItems(NpgsqlConnection conn, string planUUID)
        {
            var items = new List<BuildItem>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BuildItems WHERE BuildPlanUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new BuildItem
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemName = reader["ResourceName"] as string ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                        });
                    }
                }
            }

            return items;
        }

        private static void UpsertBuildPlanParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, BuildPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO BuildPlans (UUID, Name, OwnerUUID, ColonyUUID, BlueprintUUID, Quantity, Priority)
                                   VALUES (@uuid, @name, @owner, @colony, @bp, @qty, @priority)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@colony", string.Empty);
                cmd.Parameters.AddWithValue("@bp", string.Empty);
                cmd.Parameters.AddWithValue("@qty", 0);
                cmd.Parameters.AddWithValue("@priority", 0);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteBuildPlanChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string planUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BuildItems WHERE BuildPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertBuildItems(NpgsqlConnection conn, NpgsqlTransaction tx, BuildPlan entity)
        {
            if (entity.Items == null)
            {
                return;
            }

            for (int i = 0; i < entity.Items.Count; i++)
            {
                var item = entity.Items[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO BuildItems (BuildPlanUUID, Sequence, ResourceName, Quantity, Fulfilled)
                                       VALUES (@pUUID, @seq, @resName, @qty, @fulfilled)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", item.ItemName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@fulfilled", 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // StockProfile Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static StockProfile ReadStockProfileParent(NpgsqlDataReader reader)
        {
            return new StockProfile
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<StockProfileEntry> LoadStockProfileEntries(NpgsqlConnection conn, string profileUUID)
        {
            var entries = new List<StockProfileEntry>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StockProfileEntries WHERE StockProfileUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", profileUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(new StockProfileEntry
                        {
                            GroupID = reader["ResourceName"] as string ?? string.Empty,
                            StockPlanUUID = string.Empty,
                        });
                    }
                }
            }

            return entries;
        }

        private static void UpsertStockProfileParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, StockProfile entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO StockProfiles (UUID, Name, OwnerUUID, Description)
                                   VALUES (@uuid, @name, @owner, @desc)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@desc", string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteStockProfileChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string profileUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StockProfileEntries WHERE StockProfileUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", profileUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertStockProfileEntries(NpgsqlConnection conn, NpgsqlTransaction tx, StockProfile entity)
        {
            if (entity.Entries == null)
            {
                return;
            }

            for (int i = 0; i < entity.Entries.Count; i++)
            {
                var entry = entity.Entries[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO StockProfileEntries (StockProfileUUID, Sequence, ResourceName, MinQuantity, MaxQuantity)
                                       VALUES (@pUUID, @seq, @resName, @minQty, @maxQty)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", entry.GroupID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@minQty", 0);
                    cmd.Parameters.AddWithValue("@maxQty", 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // StockPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static StockPlan ReadStockPlanParent(NpgsqlDataReader reader)
        {
            return new StockPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<StockTarget> LoadStockTargets(NpgsqlConnection conn, string planUUID)
        {
            var targets = new List<StockTarget>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StockTargets WHERE StockPlanUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        targets.Add(new StockTarget
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemName = reader["ResourceName"] as string ?? string.Empty,
                            TargetQuantity = Convert.ToInt32(reader["TargetQuantity"]),
                            CriticalThreshold = Convert.ToInt32(reader["Priority"]),
                        });
                    }
                }
            }

            return targets;
        }

        private static void UpsertStockPlanParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, StockPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO StockPlans (UUID, Name, OwnerUUID, ColonyUUID)
                                   VALUES (@uuid, @name, @owner, @colony)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@colony", string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteStockPlanChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string planUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StockTargets WHERE StockPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertStockTargets(NpgsqlConnection conn, NpgsqlTransaction tx, StockPlan entity)
        {
            if (entity.Targets == null)
            {
                return;
            }

            for (int i = 0; i < entity.Targets.Count; i++)
            {
                var target = entity.Targets[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO StockTargets (StockPlanUUID, Sequence, ResourceName, TargetQuantity, Priority)
                                       VALUES (@pUUID, @seq, @resName, @targetQty, @priority)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", target.ItemName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@targetQty", target.TargetQuantity);
                    cmd.Parameters.AddWithValue("@priority", target.CriticalThreshold);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PricingPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static PricingPlan ReadPricingPlanParent(NpgsqlDataReader reader)
        {
            return new PricingPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                FixedCostPerItem = Convert.ToDecimal(reader["FixedCostPerItem"]),
                HourlyCostRate = Convert.ToDecimal(reader["HourlyCostRate"]),
            };
        }

        private static Dictionary<string, decimal> LoadPricingPlanPrices(NpgsqlConnection conn, string planUUID)
        {
            var prices = new Dictionary<string, decimal>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ResourceName, Price FROM PricingPlanPrices WHERE PricingPlanUUID = @pUUID";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var price = Convert.ToDecimal(reader["Price"]);
                        prices[name] = price;
                    }
                }
            }

            return prices;
        }

        private static void UpsertPricingPlanParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, PricingPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO PricingPlans (UUID, Name, OwnerUUID, Description, FixedCostPerItem, HourlyCostRate)
                                   VALUES (@uuid, @name, @owner, @desc, @fixed, @hourly)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@fixed", (double)entity.FixedCostPerItem);
                cmd.Parameters.AddWithValue("@hourly", (double)entity.HourlyCostRate);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeletePricingPlanChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string planUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM PricingPlanPrices WHERE PricingPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertPricingPlanPrices(NpgsqlConnection conn, NpgsqlTransaction tx, PricingPlan entity)
        {
            if (entity.ResourcePrices == null)
            {
                return;
            }

            foreach (var kvp in entity.ResourcePrices)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO PricingPlanPrices (PricingPlanUUID, ResourceName, Price) VALUES (@pUUID, @resName, @price)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@resName", kvp.Key);
                    cmd.Parameters.AddWithValue("@price", (double)kvp.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PlayerProfile Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static PlayerProfile ReadPlayerProfileParent(NpgsqlDataReader reader)
        {
            var profile = new PlayerProfile
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                Name = reader["Name"] as string ?? string.Empty,
                Faction = reader["Faction"] as string ?? string.Empty,
                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                TotalCredits = Convert.ToDecimal(reader["TotalCredits"]),
                SkillPoints = Convert.ToInt32(reader["SkillPoints"]),
                CitizenId = reader["CitizenId"] as string ?? string.Empty,
                RegistrationDate = reader["RegistrationDate"] as string ?? string.Empty,
                ActiveTime = reader["ActiveTime"] as string ?? string.Empty,
                CharacterId = Convert.ToInt32(reader["CharacterId"]),
                FirstName = reader["FirstName"] as string ?? string.Empty,
                LastName = reader["LastName"] as string ?? string.Empty,
                ActiveTimeMinutes = Convert.ToInt32(reader["ActiveTimeMinutes"]),
                Public = new PlayerRank
                {
                    Rank = Convert.ToInt32(reader["PublicRank_Rank"]),
                    CurrentXp = Convert.ToInt64(reader["PublicRank_CurrentXp"]),
                    XpToNextLevel = Convert.ToInt64(reader["PublicRank_XpToNextLevel"]),
                    RankName = reader["PublicRank_RankName"] as string ?? string.Empty,
                },
                Private = new PlayerRank
                {
                    Rank = Convert.ToInt32(reader["PrivateRank_Rank"]),
                    CurrentXp = Convert.ToInt64(reader["PrivateRank_CurrentXp"]),
                    XpToNextLevel = Convert.ToInt64(reader["PrivateRank_XpToNextLevel"]),
                    RankName = reader["PrivateRank_RankName"] as string ?? string.Empty,
                },
                Military = new PlayerRank
                {
                    Rank = Convert.ToInt32(reader["MilitaryRank_Rank"]),
                    CurrentXp = Convert.ToInt64(reader["MilitaryRank_CurrentXp"]),
                    XpToNextLevel = Convert.ToInt64(reader["MilitaryRank_XpToNextLevel"]),
                    RankName = reader["MilitaryRank_RankName"] as string ?? string.Empty,
                },
            };

            return profile;
        }

        private static Dictionary<string, PlayerSkill> LoadPlayerSkills(NpgsqlConnection conn, string playerUUID)
        {
            var skills = new Dictionary<string, PlayerSkill>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM PlayerSkills WHERE PlayerUUID = @pUUID";
                cmd.Parameters.AddWithValue("@pUUID", playerUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var skillName = reader["SkillName"] as string ?? string.Empty;
                        var skill = new PlayerSkill
                        {
                            Level = Convert.ToInt32(reader["Level"]),
                            TrainingStarted = Convert.ToInt32(reader["TrainingStarted"]) != 0,
                            SkillId = Convert.ToInt32(reader["SkillId"]),
                            EffectDescription = reader["EffectDescription"] as string ?? string.Empty,
                            AmountPerLevel = Convert.ToInt32(reader["AmountPerLevel"]),
                            SkillGroupName = reader["SkillGroupName"] as string ?? string.Empty,
                            IsUnlocked = Convert.ToInt32(reader["IsUnlocked"]) != 0,
                            TargetLevel = Convert.ToInt32(reader["TargetLevel"]),
                            TrainingPercentageComplete = Convert.ToInt32(reader["TrainingPercentageComplete"]),
                            RemainingMinutes = Convert.ToInt32(reader["RemainingMinutes"]),
                        };

                        var completionStart = reader["Completion_StartTime"] as string;
                        if (completionStart != null)
                        {
                            skill.CompletionTime = new CountDownTime
                            {
                                StartTime = DateTime.Parse(completionStart),
                                RepeatIntervalSeconds = reader["Completion_RepeatIntervalSeconds"] == DBNull.Value ? 0 : Convert.ToInt64(reader["Completion_RepeatIntervalSeconds"]),
                            };
                        }

                        skills[skillName] = skill;
                    }
                }
            }

            return skills;
        }

        private static void UpsertPlayerProfileParent(NpgsqlConnection conn, NpgsqlTransaction tx, PlayerProfile entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO PlayerProfiles (
                    UUID, Name, Faction, FactionUUID, TotalCredits, SkillPoints,
                    CitizenId, RegistrationDate, ActiveTime, CharacterId,
                    FirstName, LastName, ActiveTimeMinutes,
                    PublicRank_Rank, PublicRank_CurrentXp, PublicRank_XpToNextLevel, PublicRank_RankName,
                    PrivateRank_Rank, PrivateRank_CurrentXp, PrivateRank_XpToNextLevel, PrivateRank_RankName,
                    MilitaryRank_Rank, MilitaryRank_CurrentXp, MilitaryRank_XpToNextLevel, MilitaryRank_RankName
                ) VALUES (
                    @uuid, @name, @faction, @factionUUID, @credits, @skillPts,
                    @citizenId, @regDate, @activeTime, @charId,
                    @firstName, @lastName, @activeMin,
                    @pubRank, @pubXp, @pubNext, @pubName,
                    @privRank, @privXp, @privNext, @privName,
                    @milRank, @milXp, @milNext, @milName
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@faction", entity.Faction ?? string.Empty);
                cmd.Parameters.AddWithValue("@factionUUID", entity.FactionUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@credits", (double)entity.TotalCredits);
                cmd.Parameters.AddWithValue("@skillPts", entity.SkillPoints);
                cmd.Parameters.AddWithValue("@citizenId", entity.CitizenId ?? string.Empty);
                cmd.Parameters.AddWithValue("@regDate", entity.RegistrationDate ?? string.Empty);
                cmd.Parameters.AddWithValue("@activeTime", entity.ActiveTime ?? string.Empty);
                cmd.Parameters.AddWithValue("@charId", entity.CharacterId);
                cmd.Parameters.AddWithValue("@firstName", entity.FirstName ?? string.Empty);
                cmd.Parameters.AddWithValue("@lastName", entity.LastName ?? string.Empty);
                cmd.Parameters.AddWithValue("@activeMin", entity.ActiveTimeMinutes);
                cmd.Parameters.AddWithValue("@pubRank", entity.Public?.Rank ?? 0);
                cmd.Parameters.AddWithValue("@pubXp", entity.Public?.CurrentXp ?? 0L);
                cmd.Parameters.AddWithValue("@pubNext", entity.Public?.XpToNextLevel ?? 0L);
                cmd.Parameters.AddWithValue("@pubName", entity.Public?.RankName ?? string.Empty);
                cmd.Parameters.AddWithValue("@privRank", entity.Private?.Rank ?? 0);
                cmd.Parameters.AddWithValue("@privXp", entity.Private?.CurrentXp ?? 0L);
                cmd.Parameters.AddWithValue("@privNext", entity.Private?.XpToNextLevel ?? 0L);
                cmd.Parameters.AddWithValue("@privName", entity.Private?.RankName ?? string.Empty);
                cmd.Parameters.AddWithValue("@milRank", entity.Military?.Rank ?? 0);
                cmd.Parameters.AddWithValue("@milXp", entity.Military?.CurrentXp ?? 0L);
                cmd.Parameters.AddWithValue("@milNext", entity.Military?.XpToNextLevel ?? 0L);
                cmd.Parameters.AddWithValue("@milName", entity.Military?.RankName ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeletePlayerProfileChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string playerUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM PlayerSkills WHERE PlayerUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", playerUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertPlayerSkills(NpgsqlConnection conn, NpgsqlTransaction tx, PlayerProfile entity)
        {
            if (entity.Skills == null)
            {
                return;
            }

            foreach (var kvp in entity.Skills)
            {
                var skillName = kvp.Key;
                var skill = kvp.Value;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO PlayerSkills (
                        PlayerUUID, SkillName, Level, TrainingStarted, SkillId,
                        EffectDescription, AmountPerLevel, SkillGroupName, IsUnlocked,
                        TargetLevel, TrainingPercentageComplete, RemainingMinutes,
                        Completion_StartTime, Completion_RepeatIntervalSeconds, Completion_IsRepeating
                    ) VALUES (
                        @pUUID, @skillName, @level, @training, @skillId,
                        @effect, @amount, @group, @unlocked,
                        @target, @pct, @remaining,
                        @cStart, @cInterval, @cRepeat
                    )";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@skillName", skillName);
                    cmd.Parameters.AddWithValue("@level", skill.Level);
                    cmd.Parameters.AddWithValue("@training", skill.TrainingStarted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@skillId", skill.SkillId);
                    cmd.Parameters.AddWithValue("@effect", skill.EffectDescription ?? string.Empty);
                    cmd.Parameters.AddWithValue("@amount", skill.AmountPerLevel);
                    cmd.Parameters.AddWithValue("@group", skill.SkillGroupName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@unlocked", skill.IsUnlocked ? 1 : 0);
                    cmd.Parameters.AddWithValue("@target", skill.TargetLevel);
                    cmd.Parameters.AddWithValue("@pct", skill.TrainingPercentageComplete);
                    cmd.Parameters.AddWithValue("@remaining", skill.RemainingMinutes);

                    if (skill.CompletionTime != null && skill.CompletionTime.StartTime != DateTime.MinValue)
                    {
                        cmd.Parameters.AddWithValue("@cStart", skill.CompletionTime.StartTime.ToString("O"));
                        cmd.Parameters.AddWithValue("@cInterval", skill.CompletionTime.RepeatIntervalSeconds);
                        cmd.Parameters.AddWithValue("@cRepeat", skill.CompletionTime.IsRepeating ? 1 : 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@cStart", DBNull.Value);
                        cmd.Parameters.AddWithValue("@cInterval", DBNull.Value);
                        cmd.Parameters.AddWithValue("@cRepeat", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // DeliveryRoute Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static DeliveryRoute ReadDeliveryRouteParent(NpgsqlDataReader reader)
        {
            return new DeliveryRoute
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<RouteStop> LoadDeliveryRouteStops(NpgsqlConnection conn, string routeUUID)
        {
            var stops = new List<RouteStop>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryRouteStops WHERE DeliveryRouteUUID = @rUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@rUUID", routeUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var stop = new RouteStop
                        {
                            Sequence = Convert.ToInt32(reader["Sequence"]),
                            ColonyUUID = reader["ColonyUUID"] as string ?? string.Empty,
                            DestinationUUID = reader["DestinationUUID"] as string ?? string.Empty,
                            FuelEstimate = Convert.ToDecimal(reader["FuelEstimate"]),
                        };

                        var destTypeStr = reader["DestinationType"] as string;
                        if (Enum.TryParse<DestinationType>(destTypeStr, true, out var dt))
                        {
                            stop.DestinationType = dt;
                        }

                        var purposeStr = reader["Purpose"] as string;
                        if (Enum.TryParse<RouteStopPurpose>(purposeStr, true, out var purpose))
                        {
                            stop.Purpose = purpose;
                        }

                        stops.Add(stop);
                    }
                }
            }

            return stops;
        }

        private static void UpsertDeliveryRouteParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, DeliveryRoute entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO DeliveryRoutes (UUID, Name, OwnerUUID)
                                   VALUES (@uuid, @name, @owner)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteDeliveryRouteChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string routeUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryRouteStops WHERE DeliveryRouteUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", routeUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertDeliveryRouteStops(NpgsqlConnection conn, NpgsqlTransaction tx, DeliveryRoute entity)
        {
            if (entity.Stops == null)
            {
                return;
            }

            for (int i = 0; i < entity.Stops.Count; i++)
            {
                var stop = entity.Stops[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO DeliveryRouteStops (
                        DeliveryRouteUUID, Sequence, ColonyUUID, DestinationType, DestinationUUID, Purpose, FuelEstimate
                    ) VALUES (
                        @rUUID, @seq, @colUUID, @destType, @destUUID, @purpose, @fuel
                    )";
                    cmd.Parameters.AddWithValue("@rUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", stop.Sequence);
                    cmd.Parameters.AddWithValue("@colUUID", stop.ColonyUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@destType", stop.DestinationType.ToString());
                    cmd.Parameters.AddWithValue("@destUUID", stop.DestinationUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@purpose", stop.Purpose.ToString());
                    cmd.Parameters.AddWithValue("@fuel", (double)stop.FuelEstimate);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // DeliveryPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static DeliveryPlan ReadDeliveryPlanParent(NpgsqlDataReader reader)
        {
            return new DeliveryPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                RouteUUID = reader["RouteUUID"] as string ?? string.Empty,
                ShipUUID = reader["ShipUUID"] as string ?? string.Empty,
                Completed = Convert.ToInt32(reader["Completed"]) != 0,
            };
        }

        private static List<DeliveryPlanStop> LoadDeliveryPlanStops(NpgsqlConnection conn, string planUUID)
        {
            var stops = new List<DeliveryPlanStop>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryPlanStops WHERE DeliveryPlanUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var stop = new DeliveryPlanStop
                        {
                            Sequence = Convert.ToInt32(reader["Sequence"]),
                            ColonyUUID = reader["ColonyUUID"] as string ?? string.Empty,
                            StopCompleted = Convert.ToInt32(reader["StopCompleted"]) != 0,
                            DestinationUUID = reader["DestinationUUID"] as string ?? string.Empty,
                        };

                        var destTypeStr = reader["DestinationType"] as string;
                        if (Enum.TryParse<DestinationType>(destTypeStr, true, out var dt))
                        {
                            stop.DestinationType = dt;
                        }

                        stops.Add(stop);
                    }
                }
            }

            // Load items for each stop
            foreach (var stop in stops)
            {
                LoadDeliveryPlanItems(conn, planUUID, stop);
            }

            return stops;
        }

        private static void LoadDeliveryPlanItems(NpgsqlConnection conn, string planUUID, DeliveryPlanStop stop)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryPlanItems WHERE DeliveryPlanUUID = @pUUID AND StopSequence = @seq ORDER BY Direction, Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                cmd.Parameters.AddWithValue("@seq", stop.Sequence);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new DeliveryItem
                        {
                            BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            ResourcePurity = reader["ResourcePurity"] as string ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            Delivered = Convert.ToInt32(reader["Delivered"]) != 0,
                        };

                        var itemTypeStr = reader["ItemType"] as string;
                        if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
                        {
                            item.ItemType = it;
                        }

                        var direction = reader["Direction"] as string;
                        if (string.Equals(direction, "DropOff", StringComparison.OrdinalIgnoreCase))
                        {
                            stop.DropOff.Add(item);
                        }
                        else
                        {
                            stop.PickUp.Add(item);
                        }
                    }
                }
            }
        }

        private static void UpsertDeliveryPlanParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, DeliveryPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO DeliveryPlans (UUID, Name, OwnerUUID, RouteUUID, ShipUUID, Completed)
                                   VALUES (@uuid, @name, @owner, @route, @ship, @completed)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@route", entity.RouteUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@ship", entity.ShipUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@completed", entity.Completed ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteDeliveryPlanChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string planUUID)
        {
            // Delete items first (they reference stops)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryPlanItems WHERE DeliveryPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryPlanStops WHERE DeliveryPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertDeliveryPlanChildren(NpgsqlConnection conn, NpgsqlTransaction tx, DeliveryPlan entity)
        {
            if (entity.Stops == null)
            {
                return;
            }

            foreach (var stop in entity.Stops)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO DeliveryPlanStops (
                        DeliveryPlanUUID, Sequence, ColonyUUID, StopCompleted, DestinationType, DestinationUUID
                    ) VALUES (
                        @pUUID, @seq, @colUUID, @completed, @destType, @destUUID
                    )";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", stop.Sequence);
                    cmd.Parameters.AddWithValue("@colUUID", stop.ColonyUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@completed", stop.StopCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@destType", stop.DestinationType.ToString());
                    cmd.Parameters.AddWithValue("@destUUID", stop.DestinationUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }

                // Insert drop-off items
                InsertDeliveryPlanItemList(conn, tx, entity.UUID, stop.Sequence, "DropOff", stop.DropOff);

                // Insert pick-up items
                InsertDeliveryPlanItemList(conn, tx, entity.UUID, stop.Sequence, "PickUp", stop.PickUp);
            }
        }

        private static void InsertDeliveryPlanItemList(NpgsqlConnection conn, NpgsqlTransaction tx, string planUUID, int stopSequence, string direction, List<DeliveryItem> items)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO DeliveryPlanItems (
                        DeliveryPlanUUID, StopSequence, Direction, Sequence,
                        ItemType, BaseItemTypeID, Name, ResourcePurity, Quantity, Delivered
                    ) VALUES (
                        @pUUID, @stopSeq, @dir, @seq,
                        @itemType, @baseId, @name, @purity, @qty, @delivered
                    )";
                    cmd.Parameters.AddWithValue("@pUUID", planUUID);
                    cmd.Parameters.AddWithValue("@stopSeq", stopSequence);
                    cmd.Parameters.AddWithValue("@dir", direction);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@itemType", item.ItemType.ToString());
                    cmd.Parameters.AddWithValue("@baseId", item.BaseItemTypeID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@purity", item.ResourcePurity ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@delivered", item.Delivered ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Ship Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Ship ReadShipParent(NpgsqlDataReader reader)
        {
            var ship = new Ship
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                TemplateUUID = reader["TemplateUUID"] as string ?? string.Empty,
                HullBlueprintUUID = reader["HullBlueprintUUID"] as string ?? string.Empty,
                LocationUUID = reader["LocationUUID"] as string ?? string.Empty,
                HullCurrentHP = Convert.ToInt32(reader["HullCurrentHP"]),
                HullMaxHP = Convert.ToInt32(reader["HullMaxHP"]),
                HullMaxRepairPercent = Convert.ToDecimal(reader["HullMaxRepairPercent"]),
            };

            var locTypeStr = reader["LocationType"] as string;
            if (Enum.TryParse<DestinationType>(locTypeStr, true, out var lt))
            {
                ship.LocationType = lt;
            }

            if (reader["GameLocationId"] != DBNull.Value)
            {
                ship.GameLocationId = Convert.ToInt32(reader["GameLocationId"]);
            }

            return ship;
        }

        private static List<ShipComponentSlot> LoadShipComponents(NpgsqlConnection conn, string shipUUID)
        {
            var components = new List<ShipComponentSlot>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ShipComponents WHERE ShipUUID = @sUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@sUUID", shipUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        components.Add(new ShipComponentSlot
                        {
                            SlotType = reader["SlotType"] as string ?? string.Empty,
                            BlueprintUUID = reader["BlueprintUUID"] as string ?? string.Empty,
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                        });
                    }
                }
            }

            return components;
        }

        private static void UpsertShipParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, Ship entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Ships (
                    UUID, Name, OwnerUUID, TemplateUUID, HullBlueprintUUID,
                    LocationType, LocationUUID, GameLocationId,
                    HullCurrentHP, HullMaxHP, HullMaxRepairPercent
                ) VALUES (
                    @uuid, @name, @owner, @template, @hullBp,
                    @locType, @locUUID, @gameLocId,
                    @hullCurHp, @hullMaxHp, @hullMaxRepair
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@template", entity.TemplateUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@hullBp", entity.HullBlueprintUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@locType", entity.LocationType.ToString());
                cmd.Parameters.AddWithValue("@locUUID", entity.LocationUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@gameLocId", entity.GameLocationId.HasValue ? (object)entity.GameLocationId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@hullCurHp", entity.HullCurrentHP);
                cmd.Parameters.AddWithValue("@hullMaxHp", entity.HullMaxHP);
                cmd.Parameters.AddWithValue("@hullMaxRepair", (double)entity.HullMaxRepairPercent);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteShipChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string shipUUID)
        {
            // Delete items (polymorphic FK, no CASCADE)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'ShipCargo' OR ParentType = 'ShipHopper')";
                cmd.Parameters.AddWithValue("@uuid", shipUUID);
                cmd.ExecuteNonQuery();
            }

            // Delete components (CASCADE would handle this on parent delete, but for upsert we delete explicitly)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ShipComponents WHERE ShipUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", shipUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertShipComponents(NpgsqlConnection conn, NpgsqlTransaction tx, Ship entity)
        {
            if (entity.Components == null)
            {
                return;
            }

            for (int i = 0; i < entity.Components.Count; i++)
            {
                var comp = entity.Components[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO ShipComponents (ShipUUID, Sequence, SlotType, BlueprintUUID, CurrentHP, MaxHP)
                                       VALUES (@sUUID, @seq, @slotType, @bpUUID, @curHp, @maxHp)";
                    cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@slotType", comp.SlotType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bpUUID", comp.BlueprintUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", comp.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", comp.MaxHP);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // ShipTemplate Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static ShipTemplate ReadShipTemplateParent(NpgsqlDataReader reader)
        {
            return new ShipTemplate
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                HullBlueprintUUID = reader["HullBlueprintUUID"] as string ?? string.Empty,
            };
        }

        private static List<ShipComponentSlot> LoadShipTemplateComponents(NpgsqlConnection conn, string templateUUID)
        {
            var components = new List<ShipComponentSlot>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ShipTemplateComponents WHERE ShipTemplateUUID = @tUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@tUUID", templateUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        components.Add(new ShipComponentSlot
                        {
                            SlotType = reader["SlotType"] as string ?? string.Empty,
                            SlotIndex = Convert.ToInt32(reader["SlotIndex"]),
                            BlueprintUUID = reader["BlueprintUUID"] as string ?? string.Empty,
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                            MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
                        });
                    }
                }
            }

            return components;
        }

        private static void UpsertShipTemplateParent(NpgsqlConnection conn, NpgsqlTransaction tx, string characterUUID, ShipTemplate entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO ShipTemplates (UUID, Name, OwnerUUID, HullBlueprintUUID)
                                   VALUES (@uuid, @name, @owner, @hullBp)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@hullBp", entity.HullBlueprintUUID ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteShipTemplateChildren(NpgsqlConnection conn, NpgsqlTransaction tx, string templateUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ShipTemplateComponents WHERE ShipTemplateUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", templateUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertShipTemplateComponents(NpgsqlConnection conn, NpgsqlTransaction tx, ShipTemplate entity)
        {
            if (entity.Components == null)
            {
                return;
            }

            for (int i = 0; i < entity.Components.Count; i++)
            {
                var comp = entity.Components[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO ShipTemplateComponents (ShipTemplateUUID, Sequence, SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent)
                                       VALUES (@tUUID, @seq, @slotType, @slotIdx, @bpUUID, @curHp, @maxHp, @maxRepair)";
                    cmd.Parameters.AddWithValue("@tUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@slotType", comp.SlotType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@slotIdx", comp.SlotIndex);
                    cmd.Parameters.AddWithValue("@bpUUID", comp.BlueprintUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", comp.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", comp.MaxHP);
                    cmd.Parameters.AddWithValue("@maxRepair", (double)comp.MaxRepairPercent);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MarketListing Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static MarketListing ReadMarketListing(NpgsqlDataReader reader)
        {
            var listing = new MarketListing
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                StationUUID = reader["StationUUID"] as string ?? string.Empty,
                ItemReferenceID = reader["ItemReferenceID"] as string ?? string.Empty,
                ItemName = reader["ItemName"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                PricePerUnit = Convert.ToDecimal(reader["PricePerUnit"]),
                CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                MaxHP = Convert.ToInt32(reader["MaxHP"]),
                MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
                BuyOrder = Convert.ToInt32(reader["BuyOrder"]) != 0,
                BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                ResourcePurity = reader["ResourcePurity"] as string ?? string.Empty,
                GameTypeCode = reader["GameTypeCode"] as string ?? string.Empty,
                GameSubTypeId = reader["GameSubTypeId"] as string ?? string.Empty,
                LocationName = reader["LocationName"] as string ?? string.Empty,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SellerName = reader["SellerName"] as string ?? string.Empty,
                SellerFactionTag = reader["SellerFactionTag"] as string ?? string.Empty,
                PrivateSale = Convert.ToInt32(reader["PrivateSale"]) != 0,
                BuyerName = reader["BuyerName"] as string ?? string.Empty,
                BuyerFactionTag = reader["BuyerFactionTag"] as string ?? string.Empty,
                IsOutbid = Convert.ToInt32(reader["IsOutbid"]) != 0,
                IsUndercut = Convert.ToInt32(reader["IsUndercut"]) != 0,
                PlacedDT = reader["PlacedDT"] as string ?? string.Empty,
                ExpiresDT = reader["ExpiresDT"] as string ?? string.Empty,
                SyncedByCharacterUUID = reader["SyncedByCharacterUUID"] as string ?? string.Empty,
                SyncTimestamp = reader["SyncTimestamp"] as string ?? string.Empty,
            };

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                listing.ItemType = it;
            }

            if (reader["MarketId"] != DBNull.Value)
            {
                listing.MarketId = Convert.ToInt64(reader["MarketId"]);
            }

            if (reader["GameTypeId"] != DBNull.Value)
            {
                listing.GameTypeId = Convert.ToInt64(reader["GameTypeId"]);
            }

            if (reader["SystemId"] != DBNull.Value)
            {
                listing.SystemId = Convert.ToInt32(reader["SystemId"]);
            }

            if (reader["GameLocationId"] != DBNull.Value)
            {
                listing.GameLocationId = Convert.ToInt32(reader["GameLocationId"]);
            }

            if (reader["AmountRemaining"] != DBNull.Value)
            {
                listing.AmountRemaining = Convert.ToInt32(reader["AmountRemaining"]);
            }

            if (reader["AmountOriginal"] != DBNull.Value)
            {
                listing.AmountOriginal = Convert.ToInt32(reader["AmountOriginal"]);
            }

            if (reader["AmountSold"] != DBNull.Value)
            {
                listing.AmountSold = Convert.ToInt32(reader["AmountSold"]);
            }

            if (reader["EscrowRemaining"] != DBNull.Value)
            {
                listing.EscrowRemaining = Convert.ToDecimal(reader["EscrowRemaining"]);
            }

            if (reader["SalesTaxEstimate"] != DBNull.Value)
            {
                listing.SalesTaxEstimate = Convert.ToDecimal(reader["SalesTaxEstimate"]);
            }

            if (reader["ValueRemaining"] != DBNull.Value)
            {
                listing.ValueRemaining = Convert.ToDecimal(reader["ValueRemaining"]);
            }

            if (reader["Evolution"] != DBNull.Value)
            {
                listing.Evolution = Convert.ToInt32(reader["Evolution"]);
            }

            if (reader["HealthPercentage"] != DBNull.Value)
            {
                listing.HealthPercentage = Convert.ToDouble(reader["HealthPercentage"]);
            }

            if (reader["CompetitorForMarketId"] != DBNull.Value)
            {
                listing.CompetitorForMarketId = Convert.ToInt64(reader["CompetitorForMarketId"]);
            }

            return listing;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MarketTransaction Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static MarketTransaction ReadMarketTransaction(NpgsqlDataReader reader)
        {
            var tx = new MarketTransaction
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                ItemReferenceID = reader["ItemReferenceID"] as string ?? string.Empty,
                ItemName = reader["ItemName"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                PricePerUnit = Convert.ToDecimal(reader["PricePerUnit"]),
                TotalPrice = Convert.ToDecimal(reader["TotalPrice"]),
                Counterparty = reader["Counterparty"] as string ?? string.Empty,
                CounterpartyFaction = reader["CounterpartyFaction"] as string ?? string.Empty,
                StationUUID = reader["StationUUID"] as string ?? string.Empty,
                Timestamp = reader["Timestamp"] as string ?? string.Empty,
                Notes = reader["Notes"] as string ?? string.Empty,
                ListingUUID = reader["ListingUUID"] as string ?? string.Empty,
                CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                MaxHP = Convert.ToInt32(reader["MaxHP"]),
                MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
            };

            var txTypeStr = reader["TransactionType"] as string;
            if (Enum.TryParse<TransactionType>(txTypeStr, true, out var tt))
            {
                tx.TransactionType = tt;
            }

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                tx.ItemType = it;
            }

            return tx;
        }










        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Task 6.7 Helpers â€” WarehouseOverflowRule, Mail, Banking, Intel
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static WarehouseOverflowRule ReadWarehouseOverflowRule(NpgsqlDataReader reader)
        {
            return new WarehouseOverflowRule
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                ColonyUUID = reader["ColonyUUID"] as string ?? string.Empty,
                ResourceName = reader["ResourceName"] as string ?? string.Empty,
                RuleType = (OverflowRuleType)Convert.ToInt32(reader["RuleType"]),
                TriggerThreshold = Convert.ToDecimal(reader["Threshold"]),
                DestinationUUID = reader["DestinationColonyUUID"] as string ?? string.Empty,
            };
        }

        private static MailMessage ReadMailMessage(NpgsqlDataReader reader)
        {
            return new MailMessage
            {
                UUID = (reader["OwnerUUID"] as string ?? string.Empty) + "_" + Convert.ToInt32(reader["MailId"]).ToString(),
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                MailId = Convert.ToInt32(reader["MailId"]),
                CharacterIdFrom = Convert.ToInt32(reader["CharacterIdFrom"]),
                FromName = reader["FromName"] as string ?? string.Empty,
                CharacterIdTo = Convert.ToInt32(reader["CharacterIdTo"]),
                ToName = reader["ToName"] as string ?? string.Empty,
                SentTime = reader["SentTime"] as string ?? string.Empty,
                Subject = reader["Subject"] as string ?? string.Empty,
                MailRead = Convert.ToInt32(reader["MailRead"]) != 0,
                MailType = reader["MailType"] as string,
                MailContent = reader["MailContent"] as string ?? string.Empty,
                LocalRead = Convert.ToInt32(reader["LocalRead"]) != 0,
            };
        }

        private static BankingTransaction ReadBankingTransaction(NpgsqlDataReader reader)
        {
            var tx = new BankingTransaction
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                TransactionDateTime = reader["TransactionDateTime"] as string ?? string.Empty,
                CreditChange = Convert.ToDecimal(reader["CreditChange"]),
                OldBalance = Convert.ToDecimal(reader["OldBalance"]),
                NewBalance = Convert.ToDecimal(reader["NewBalance"]),
                TransactionType = Convert.ToInt32(reader["TransactionType"]),
                Detail = reader["Detail"] as string ?? string.Empty,
                IsManualEntry = Convert.ToInt32(reader["IsManualEntry"]) != 0,
            };

            if (reader["CharacterId"] != DBNull.Value)
            {
                tx.CharacterId = Convert.ToInt32(reader["CharacterId"]);
            }

            if (reader["SystemObjectId"] != DBNull.Value)
            {
                tx.SystemObjectId = Convert.ToInt32(reader["SystemObjectId"]);
            }

            if (reader["SystemId"] != DBNull.Value)
            {
                tx.SystemId = Convert.ToInt32(reader["SystemId"]);
            }

            return tx;
        }

        private static IntelComment ReadIntelComment(NpgsqlDataReader reader)
        {
            return new IntelComment
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                TargetCharacterUUID = reader["TargetCharacterUUID"] as string ?? string.Empty,
                SubmitterCharacterUUID = reader["SubmitterCharacterUUID"] as string ?? string.Empty,
                Text = reader["Text"] as string ?? string.Empty,
                CreatedUtc = DateTime.Parse(reader["CreatedUtc"] as string ?? DateTime.MinValue.ToString("O")),
            };
        }

        private static IntelCommentFactionShare ReadIntelShare(NpgsqlDataReader reader)
        {
            var share = new IntelCommentFactionShare
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                IntelCommentUUID = reader["IntelCommentUUID"] as string ?? string.Empty,
                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                ClassificationLevelUUID = reader["ClassificationLevelUUID"] as string,
                ClassifiedByCharacterUUID = reader["ClassifiedByCharacterUUID"] as string,
                SharedUtc = DateTime.Parse(reader["SharedUtc"] as string ?? DateTime.MinValue.ToString("O")),
            };

            var classifiedUtcStr = reader["ClassifiedUtc"] as string;
            if (classifiedUtcStr != null)
            {
                share.ClassifiedUtc = DateTime.Parse(classifiedUtcStr);
            }

            return share;
        }

        private static string[] LoadBlueprintTypeProperties(NpgsqlConnection conn, string blueprintTypeName)
        {
            var props = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Key FROM BlueprintTypeProperties WHERE BlueprintTypeName = @name";
                cmd.Parameters.AddWithValue("@name", blueprintTypeName);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        props.Add(reader.GetString(0));
                    }
                }
            }

            return props.ToArray();
        }

        private static string[] LoadBlueprintTypeResearchableProperties(NpgsqlConnection conn, string blueprintTypeName)
        {
            var props = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Key FROM BlueprintTypeResearchableProperties WHERE BlueprintTypeName = @name";
                cmd.Parameters.AddWithValue("@name", blueprintTypeName);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        props.Add(reader.GetString(0));
                    }
                }
            }

            return props.ToArray();
        }


    }
}
