using OE2EmpireTracker.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace OE2EmpireTracker.Services.Migration
{
    public static class DeterministicUUID
    {
        private static readonly Guid Namespace =
            new Guid("e0058083-0f64-b398-ed53-762f7d8b8eb2");

        // Separate namespace for colony UUIDs to avoid collisions with blueprint UUIDs
        private static readonly Guid ColonyNamespace =
            new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        // Separate namespace for default survey UUIDs
        private static readonly Guid DefaultSurveyNamespace =
            new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        // Separate namespace for asteroid UUIDs (SystemName:AsteroidName)
        private static readonly Guid AsteroidNamespace =
            new Guid("c3d4e5f6-a7b8-9012-cdef-234567890abc");

        /// <summary>
        /// Generates a deterministic UUID v5 from the blueprint's dedup key fields.
        /// Input string format: "Name|Evolution|BluePrintType|Class|TechLevel"
        /// </summary>
        public static string Generate(string name, int evolution,
            string blueprintType, int cls, string techLevel)
        {
            string input = $"{name ?? ""}|{evolution}|{blueprintType ?? ""}|{cls}|{techLevel ?? ""}";
            return GenerateV5(Namespace, input).ToString();
        }

        /// <summary>
        /// Generates a deterministic UUID v5 from a Blueprint's dedup key.
        /// </summary>
        public static string Generate(Blueprint bp)
        {
            return Generate(bp.Name, bp.Evolution, bp.BluePrintType,
                bp.Class, bp.TechLevel);
        }

        /// <summary>
        /// Generates a deterministic UUID v5 from a Colony's dedup key.
        /// </summary>
        public static string Generate(Colony colony)
        {
            return Generate(colony.OwnerUUID, colony.PlanetName, colony.SystemName);
        }

        /// <summary>
        /// Generates a deterministic UUID v5 from colony identity fields.
        /// Input string format: "OwnerUUID|PlanetName|SystemName"
        /// </summary>
        public static string Generate(string ownerUUID, string planetName, string systemName)
        {
            string input = $"{ownerUUID ?? ""}|{planetName ?? ""}|{systemName ?? ""}";
            return GenerateV5(ColonyNamespace, input).ToString();
        }

        /// <summary>
        /// Generates a deterministic UUID v5 for a default survey from colony identity fields.
        /// Input string format: "OwnerUUID|PlanetName|SystemName"
        /// </summary>
        public static string GenerateDefaultSurvey(string ownerUUID, string planetName, string systemName)
        {
            string input = $"{ownerUUID ?? ""}|{planetName ?? ""}|{systemName ?? ""}";
            return GenerateV5(DefaultSurveyNamespace, input).ToString();
        }

        /// <summary>
        /// Generates a deterministic UUID v5 for an asteroid from SystemName:AsteroidName.
        /// Input string format: "SystemName:AsteroidName"
        /// </summary>
        public static string GenerateAsteroid(string systemName, string asteroidName)
        {
            string input = $"{systemName ?? ""}:{asteroidName ?? ""}";
            return GenerateV5(AsteroidNamespace, input).ToString();
        }

        /// <summary>
        /// UUID v5 implementation: SHA-1 hash of namespace + name,
        /// formatted as a version-5 UUID.
        /// </summary>
        private static Guid GenerateV5(Guid namespaceId, string name)
        {
            // Convert namespace to big-endian bytes (RFC 4122)
            byte[] namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes);

            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            byte[] hash;

            using (var sha1 = SHA1.Create())
            {
                sha1.TransformBlock(namespaceBytes, 0, namespaceBytes.Length,
                    null, 0);
                sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
                hash = sha1.Hash;
            }

            // Set version 5 and variant bits
            hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
            hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant RFC 4122

            // Convert back from big-endian
            SwapByteOrder(hash);
            byte[] result = new byte[16];
            Array.Copy(hash, 0, result, 0, 16);
            return new Guid(result);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            // Swap bytes for little-endian .NET Guid layout
            (guid[0], guid[3]) = (guid[3], guid[0]);
            (guid[1], guid[2]) = (guid[2], guid[1]);
            (guid[4], guid[5]) = (guid[5], guid[4]);
            (guid[6], guid[7]) = (guid[7], guid[6]);
        }
    }
}
