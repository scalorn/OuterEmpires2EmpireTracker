namespace OE2EmpireTracker.Common.Interfaces
{
    /// <summary>
    /// Enumerates the available storage backend implementations.
    /// </summary>
    public enum StorageBackendType
    {
        /// <summary>
        /// Single monolithic JSON file (current WinForms format).
        /// </summary>
        JsonSingleFile,

        /// <summary>
        /// Multiple JSON files per entity type and character (current Server format).
        /// </summary>
        JsonMultiFile,

        /// <summary>
        /// SQLite database with fully normalized relational schema.
        /// </summary>
        Sqlite,

        /// <summary>
        /// AWS DynamoDB tables.
        /// </summary>
        DynamoDb,

        /// <summary>
        /// PostgreSQL database with fully normalized relational schema.
        /// </summary>
        Postgres
    }
}
