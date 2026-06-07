namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Defines string constants for game API typeC codes used to classify
    /// cargo items and asset locations. API codes are case-sensitive, but
    /// comparison code uses OrdinalIgnoreCase for resilience against API
    /// inconsistencies.
    ///
    /// NOTE: The "SH" vs "Sh" pair is ambiguous — "SH" (both uppercase)
    /// means ShipHull, while "Sh" (capital S, lowercase h) means Share.
    /// This pair requires case-sensitive discrimination in MapAssetTypeC.
    /// </summary>
    public static class AssetTypeCodes
    {
        // --- Cargo Item TypeC Codes (swagger-confirmed) ---

        /// <summary>Blueprint ("Bp") — swagger-confirmed.</summary>
        public const string Blueprint = "Bp";

        /// <summary>Crate ("Cr") — swagger-confirmed.</summary>
        public const string Crate = "Cr";

        /// <summary>Survey ("Sc") — swagger-confirmed.</summary>
        public const string Survey = "Sc";

        /// <summary>Ship Part ("S") — swagger-confirmed.</summary>
        public const string ShipPart = "S";

        /// <summary>Flatpack ("F") — swagger-confirmed.</summary>
        public const string Flatpack = "F";

        /// <summary>Workforce ("W") — swagger-confirmed.</summary>
        public const string Workforce = "W";

        /// <summary>Resource ("R") — swagger-confirmed.</summary>
        public const string Resource = "R";

        /// <summary>Ammunition ("A") — swagger-confirmed.</summary>
        public const string Ammunition = "A";

        /// <summary>Deployable ("D") — swagger-confirmed.</summary>
        public const string Deployable = "D";

        /// <summary>Share ("Sh") — swagger-confirmed. Case-sensitive: lowercase h.</summary>
        public const string Share = "Sh";

        /// <summary>Commodity ("L") — swagger-confirmed.</summary>
        public const string CommodityL = "L";

        // --- Cargo Item TypeC Codes (discovered in real API data) ---

        /// <summary>Commodity ("C") — observed in real API data, not in swagger type filter.</summary>
        public const string Commodity = "C";

        /// <summary>Ship Hull ("SH") — observed in real API data. Case-sensitive: both uppercase.</summary>
        public const string ShipHull = "SH";

        // --- Location TypeC Codes ---

        /// <summary>Colony location ("Co") — location type from asset API.</summary>
        public const string Colony = "Co";

        /// <summary>Station location ("St") — location type from asset API.</summary>
        public const string Station = "St";

        /// <summary>Ship location ("Sh") — location type from asset API.</summary>
        public const string Ship = "Sh";
    }
}
