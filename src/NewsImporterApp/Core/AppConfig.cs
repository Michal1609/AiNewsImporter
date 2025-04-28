using System.Text.Json.Serialization;

namespace NewsImporterApp.Core
{
    public class AppConfig
    {
        [JsonPropertyName("googleApiKey")]
        public string GoogleApiKey { get; set; } = string.Empty;

        [JsonPropertyName("pageLoadTimeoutMs")]
        public int PageLoadTimeoutMs { get; set; } = 5000;

        // GrznarAi API Configuration
        [JsonPropertyName("grznarAiApiKey")]
        public string GrznarAiApiKey { get; set; } = string.Empty;

        [JsonPropertyName("grznarAiBaseUrl")]
        public string GrznarAiBaseUrl { get; set; } = string.Empty;
    }
}
