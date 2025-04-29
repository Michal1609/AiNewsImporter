using System.Text.Json;
using NewsImport.PlaywrightTest;
using NewsImport.PlaywrightTest.Models;
using NewsImport.PlaywrightTest.Utilities;
using NewsImporterApp.Models;
using NewsImporterApp.Services;

namespace NewsImporterApp.Core
{
    public class NewsImporterApplication
    {
        private readonly ConfigurationService _configService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly List<Exception> _exceptions = new();

        public NewsImporterApplication()
        {
            _jsonOptions = ConfigurationService.CreateJsonOptions();
            _configService = new ConfigurationService(_jsonOptions);
        }

        public async Task<int> RunAsync()
        {
            // 1. Načtení konfigurace
            var config = await _configService.LoadConfigAsync();
            if (config == null)
            {
                Console.WriteLine("Nelze načíst konfiguraci. Program bude ukončen.");
                return 1;
            }

            // 2. Inicializace služeb
            var sourceFileService = new SourceFileService();
            var apiService = new ApiService(config);
            
            // 3. Načtení zdrojů novinek
            var newsSources = await LoadNewsSourcesAsync(sourceFileService, apiService);
            if (newsSources == null || newsSources.Count == 0)
            {
                Console.WriteLine("Nelze načíst seznam zdrojů. Program bude ukončen.");
                return 1;
            }

            // 4. Inicializace služeb pro zpracování obsahu
            var geminiService = new GeminiAiService();
            geminiService.SetApiKey(config.GoogleApiKey);
            var htmlCleaner = new HtmlCleaner();
            var markdownConverter = new MarkdownConverter();
            var playwrightService = new PlaywrightService();

            // 5. Vytvoření procesoru novinek
            var newsProcessor = new NewsProcessor(
                geminiService,
                htmlCleaner,
                markdownConverter,
                playwrightService,
                _jsonOptions,
                config.PageLoadTimeoutMs);

            // 6. Zpracování novinek
            var allNewsItems = await newsProcessor.ProcessNewsSourcesAsync(newsSources);

            // 7. Uložení výsledků
            Console.WriteLine("Desertializaco do string");
            var result = JsonSerializer.Serialize(allNewsItems, _jsonOptions);
            Console.WriteLine("Ukladama file");
            File.WriteAllText("result.json", result);

            // 8. Odeslání výsledků na server
            await apiService.SendNewsToWebAsync(allNewsItems);
            await apiService.SendErrorsToWebAsync(_exceptions);

            // 9. Uložení výjimek do souboru
            if (_exceptions.Count > 0)
            {
                AggregateException aggregateException = new AggregateException(_exceptions);
                File.WriteAllText("exceptions.txt", aggregateException.ToString());
            }

            return 0;
        }

        private async Task<List<NewsSourceItem>?> LoadNewsSourcesAsync(SourceFileService sourceFileService, ApiService apiService)
        {
            try
            {
                // Získání zdrojů z API
                var apiResponse = await apiService.GetSourcesAsync();
                if (apiResponse == null || apiResponse.Sources == null || apiResponse.Sources.Count == 0)
                {
                    Console.WriteLine("Nepodařilo se získat zdroje z API nebo API nevrátilo žádné zdroje.");
                    
                    // Pokud nelze získat zdroje z API, pokusíme se načíst zdroje ze souboru
                    var existingSources = await sourceFileService.LoadSourcesAsync();
                    if (existingSources.Count == 0)
                    {
                        Console.WriteLine("Nelze načíst zdroje ze souboru.");
                        return null;
                    }
                    
                    // Převedeme slovník zdrojů na seznam NewsSourceItem
                    return existingSources.Select(s => new NewsSourceItem
                    {
                        Url = s.Key,
                        Type = s.Value.Type,
                        LastFetched = s.Value.LastFetched
                    }).ToList();
                }
                
                Console.WriteLine($"Úspěšně načteno {apiResponse.Sources.Count} zdrojů z API.");
                
                // Aktualizace zdrojů v souboru
                var updatedSources = await sourceFileService.UpdateSourcesFromApiAsync(apiResponse.Sources);
                
                // Převedeme slovník zdrojů na seznam NewsSourceItem
                var sourceItems = updatedSources.Select(s => new NewsSourceItem
                {
                    Url = s.Key,
                    Type = s.Value.Type,
                    LastFetched = s.Value.LastFetched
                }).ToList();
                
                Console.WriteLine($"Zdroje byly aktualizovány. Celkem {sourceItems.Count} zdrojů.");
                return sourceItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při načítání zdrojů: {ex.Message}");
                _exceptions.Add(ex);
                return null;
            }
        }
    }
} 