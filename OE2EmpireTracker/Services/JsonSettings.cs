using Newtonsoft.Json;
using NLog;

namespace OE2EmpireTracker.Services
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new SortedDictionaryContractResolver()
        };

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    }
}
