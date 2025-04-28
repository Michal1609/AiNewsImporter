using System.Text.Json.Serialization;

namespace NewsImporterApp.Core
{
    public class AppConfig
    {
        [JsonPropertyName("googleApiKey")]
        public string GoogleApiKey { get; set; } = string.Empty;

        [JsonPropertyName("pageLoadTimeoutMs")]
        public int PageLoadTimeoutMs { get; set; } = 5000;
    }
}
