using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Represents a single credit or debit entry in the player's game bank account.
    /// </summary>
    public class BankingTransaction
    {
        [JsonProperty("uuid")]
        public string UUID { get; set; } = string.Empty;

        [JsonProperty("ownerUUID")]
        public string OwnerUUID { get; set; } = string.Empty;

        [JsonProperty("transactionDateTime")]
        public string TransactionDateTime { get; set; } = string.Empty;

        [JsonProperty("creditChange")]
        public decimal CreditChange { get; set; } = 0m;

        [JsonProperty("oldBalance")]
        public decimal OldBalance { get; set; } = 0m;

        [JsonProperty("newBalance")]
        public decimal NewBalance { get; set; } = 0m;

        [JsonProperty("transactionType")]
        public int TransactionType { get; set; } = 0;

        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonProperty("characterId")]
        public int? CharacterId { get; set; }

        [JsonProperty("systemObjectId")]
        public int? SystemObjectId { get; set; }

        [JsonProperty("systemId")]
        public int? SystemId { get; set; }

        [JsonProperty("isManualEntry")]
        public bool IsManualEntry { get; set; } = false;
    }
}
