using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NewsImporterApp.Core;

namespace NewsImporterApp.Services
{
    public class ConfigurationService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IExceptionHandler? _exceptionHandler;

        public ConfigurationService(JsonSerializerOptions jsonOptions, IExceptionHandler? exceptionHandler = null)
        {
            _jsonOptions = jsonOptions;
            _exceptionHandler = exceptionHandler;
        }

        public static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<AppConfig?> LoadConfigAsync()
        {
            try
            {
                // Konfigurace z appsettings.json
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(configPath))
                {
                    // Vytvoříme výchozí konfigurační soubor
                    var defaultConfig = new AppConfig
                    {
                        GoogleApiKey = "",
                        PageLoadTimeoutMs = 5000
                    };

                    await File.WriteAllTextAsync(configPath,
                        JsonSerializer.Serialize(defaultConfig, _jsonOptions));

                    Console.WriteLine($"Vytvořen výchozí konfigurační soubor: {configPath}");
                    Console.WriteLine("Zadejte svůj Google API klíč do konfiguračního souboru a spusťte program znovu.");
                    return null;
                }

                string configJson = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(configJson, _jsonOptions);
                

                // Načtení user secrets
                var configurationBuilder = new ConfigurationBuilder()
                    .AddJsonFile(configPath)
                    .AddUserSecrets<AppConfig>();
                
                IConfiguration configuration = configurationBuilder.Build();
                
                // Načtení hodnot z user secrets
                string? googleApiKey = configuration["googleApiKey"];
                if (!string.IsNullOrEmpty(googleApiKey))
                {
                    config.GoogleApiKey = googleApiKey;
                }
                
                string? grznarAiBaseUrl = configuration["grznarAiBaseUrl"];
                if (!string.IsNullOrEmpty(grznarAiBaseUrl))
                {
                    config.GrznarAiBaseUrl = grznarAiBaseUrl;
                }
                
                string? grznarAiApiKey = configuration["GrznarAiApiKey"];
                if (!string.IsNullOrEmpty(grznarAiApiKey))
                {
                    config.GrznarAiApiKey = grznarAiApiKey;
                }
                
                if (string.IsNullOrEmpty(config.GrznarAiApiKey))
                {
                    Console.WriteLine("Chybí GrznarAi API klíč v user secrets.");
                    return null;
                }

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při načítání konfigurace: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                return null;
            }
        }
    }
} 