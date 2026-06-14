// <copyright file="SurveyLinkageService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Parses planet names from survey cargo items, finds or creates Survey entities,
    /// and links warehouse items to them via BaseItemTypeID.
    /// </summary>
    public class SurveyLinkageService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Regex SurveyNameRegex = new Regex(
            @"^Survey Report:\s*(.+?)\s*\(([A-Fa-f0-9]+)\)$",
            RegexOptions.Compiled);

        private readonly PlayerContext _playerContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SurveyLinkageService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for survey lookups and persistence.</param>
        public SurveyLinkageService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Processes a survey cargo item, linking it to an existing or newly created stub Survey entity.
        /// </summary>
        /// <param name="apiItem">The API cargo item with typeC="Sc".</param>
        /// <param name="localItem">The local item to link via BaseItemTypeID.</param>
        /// <param name="ownerUUID">The UUID of the owning player.</param>
        /// <returns>True if linkage was established; false if the name was malformed.</returns>
        public bool ProcessItem(AssetCargoItem apiItem, Item localItem, string ownerUUID)
        {
            var match = SurveyNameRegex.Match(apiItem.ResourceName ?? string.Empty);
            if (!match.Success)
            {
                Log.Warn(
                    "SurveyLinkage: resourceName '{0}' does not match expected format, skipping",
                    apiItem.ResourceName);
                return false;
            }

            string planetName = match.Groups[1].Value.Trim();
            string hexCode = match.Groups[2].Value;

            // Find existing survey by planet name (case-insensitive)
            var surveys = _playerContext.GetCurrentPlayerSurveys();
            var existing = surveys.FirstOrDefault(s =>
                string.Equals(s.PlanetName, planetName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                localItem.BaseItemTypeID = existing.UUID;
                Log.Info(
                    "SurveyLinkage: linked to existing survey '{0}' UUID={1}",
                    planetName,
                    existing.UUID);
                return true;
            }

            // Create stub survey
            var stub = new Models.Survey
            {
                UUID = Guid.NewGuid().ToString(),
                PlanetName = planetName,
                SurveyID = hexCode,
                OwnerUUID = ownerUUID,
                Name = apiItem.ResourceName,
            };

            _playerContext.AddSurvey(stub);
            localItem.BaseItemTypeID = stub.UUID;
            Log.Info(
                "SurveyLinkage: created stub survey '{0}' hex={1} UUID={2}",
                planetName,
                hexCode,
                stub.UUID);
            return true;
        }
    }
}
