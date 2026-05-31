namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Result of a banking transaction import operation.
    /// </summary>
    public class BankingImportResult
    {
        /// <summary>
        /// Gets or sets the number of transactions successfully imported.
        /// </summary>
        public int TransactionsImported { get; set; }

        /// <summary>
        /// Gets or sets the number of duplicate transactions skipped.
        /// </summary>
        public int DuplicatesSkipped { get; set; }

        /// <summary>
        /// Gets or sets the number of API pages completed.
        /// </summary>
        public int PagesCompleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the import succeeded.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the error message if the import failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the page number where the import failed, or -1 if no failure.
        /// </summary>
        public int FailedAtPage { get; set; } = -1;
    }
}
