// <copyright file="GameApiSurveyResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the asset survey response from the game API.
    /// </summary>
    public class GameApiSurveyResponse
    {
        /// <summary>
        /// Gets or sets the survey detail.
        /// </summary>
        [JsonProperty("survey")]
        public GameApiSurveyDetail Survey { get; set; }
    }

    /// <summary>
    /// DTO representing the survey detail within the asset survey response.
    /// </summary>
    public class GameApiSurveyDetail
    {
        /// <summary>
        /// Gets or sets the survey identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trace elements were included in the scan.
        /// </summary>
        [JsonProperty("traceElements")]
        public bool TraceElements { get; set; }

        /// <summary>
        /// Gets or sets the date and time the scan was performed.
        /// </summary>
        [JsonProperty("scanDate")]
        public DateTime ScanDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the character who performed the scan.
        /// </summary>
        [JsonProperty("scanCharacter")]
        public string ScanCharacter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the encrypted identifier for the survey.
        /// </summary>
        [JsonProperty("encryptedId")]
        public string EncryptedId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system object identifier that was surveyed.
        /// </summary>
        [JsonProperty("systemObjectId")]
        public int SystemObjectId { get; set; }

        /// <summary>
        /// Gets or sets the type of the object that was surveyed (e.g. planet, asteroid).
        /// </summary>
        [JsonProperty("objectType")]
        public string ObjectType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of surveyed resources.
        /// </summary>
        [JsonProperty("resources")]
        public List<GameApiSurveyResource> Resources { get; set; } = new List<GameApiSurveyResource>();
    }

    /// <summary>
    /// DTO representing a single surveyed resource within a survey report.
    /// </summary>
    public class GameApiSurveyResource
    {
        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [JsonProperty("resourceId")]
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the accessibility rating of the resource.
        /// </summary>
        [JsonProperty("accessibility")]
        public double Accessibility { get; set; }

        /// <summary>
        /// Gets or sets the abundance level of the resource.
        /// </summary>
        [JsonProperty("abundance")]
        public int Abundance { get; set; }

        /// <summary>
        /// Gets or sets the rarity classification of the resource.
        /// </summary>
        [JsonProperty("rarityClassification")]
        public string RarityClassification { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum reserve amount for the resource.
        /// </summary>
        [JsonProperty("maxReserve")]
        public int? MaxReserve { get; set; }
    }
}
