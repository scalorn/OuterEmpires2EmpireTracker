namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing external character.
    /// </summary>
    public class ExternalCharacterUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// </summary>
        public ReadOnlyExternalCharacter Original { get; set; }

        public string Name { get; set; }

        public string FactionUUID { get; set; }
    }
}
