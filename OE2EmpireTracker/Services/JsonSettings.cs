using Newtonsoft.Json;
using NLog;

namespace OE2EmpireTracker.Services
{
    public static class JsonSettings
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new SortedDictionaryContractResolver()
        };
    }
}
