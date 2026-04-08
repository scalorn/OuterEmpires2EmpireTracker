using Newtonsoft.Json;

namespace OE2EmpireTracker.Services
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
    }
}
