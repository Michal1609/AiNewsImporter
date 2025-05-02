using System.Text.Json;
using NewsImport.PlaywrightTest;
using NewsImport.PlaywrightTest.Models;
using NewsImport.PlaywrightTest.Utilities;
using NewsImporterApp.Models;
using NewsImporterApp.Services;

namespace NewsImporterApp.Core
{
    public class NewsImporterApplication : IExceptionHandler
    {
        private readonly ConfigurationService _configService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly List<Exception> _exceptions = new();

        public NewsImporterApplication()
        {
            _jsonOptions = ConfigurationService.CreateJsonOptions();
            _configService = new ConfigurationService(_jsonOptions, this);
        }

        /// <summary>
        /// Adds an exception to the global collection
        /// </summary>
        /// <param name="exception">Exception to add</param>
        public void AddException(Exception exception)
        {
            _exceptions.Add(exception);
        }

        public async Task<int> RunAsync()
        {
            // 1. Loading configuration
            var config = await _configService.LoadConfigAsync();
            if (config == null)
            {
                Console.WriteLine("Unable to load configuration. The program will exit.");
                return 1;
            }

            // 2. Initializing services
            var sourceFileService = new SourceFileService(this);
            var apiService = new ApiService(config, this);
            
            // 3. Loading news sources
            var newsSources = await LoadNewsSourcesAsync(sourceFileService, apiService);
            if (newsSources == null || newsSources.Count == 0)
            {
                Console.WriteLine("Unable to load the list of sources. The program will exit.");
                return 1;
            }

            // 4. Initializing content processing services
            var geminiService = new GeminiAiService(this);
            geminiService.SetApiKey(config.GoogleApiKey);
            var htmlCleaner = new HtmlCleaner();
            var markdownConverter = new MarkdownConverter();
            var playwrightService = new PlaywrightService(this);

            // 5. Creating news processor
            var newsProcessor = new NewsProcessor(
                geminiService,
                htmlCleaner,
                markdownConverter,
                playwrightService,
                _jsonOptions,
                config.PageLoadTimeoutMs,
                this);

            // 6. Processing news
            var allNewsItems = await newsProcessor.ProcessNewsSourcesAsync(newsSources);

            // 7. Saving results
            Console.WriteLine("Deserializing to string");
            var result = JsonSerializer.Serialize(allNewsItems, _jsonOptions);
            Console.WriteLine("Saving file");
            File.WriteAllText("result.json", result);

            // 8. Sending results to the server
            await apiService.SendNewsToWebAsync(allNewsItems);
            await apiService.SendErrorsToWebAsync(_exceptions);

            // 9. Saving exceptions to file
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
                // Getting sources from API
                var apiResponse = await apiService.GetSourcesAsync();
                if (apiResponse == null || apiResponse.Sources == null || apiResponse.Sources.Count == 0)
                {
                    Console.WriteLine("Failed to retrieve sources from API or API did not return any sources.");
                    
                    // If we can't get sources from API, we try to load sources from file
                    var existingSources = await sourceFileService.LoadSourcesAsync();
                    if (existingSources.Count == 0)
                    {
                        Console.WriteLine("Unable to load sources from file.");
                        return null;
                    }
                    
                    // Converting the dictionary of sources to a list of NewsSourceItem
                    return existingSources.Select(s => new NewsSourceItem
                    {
                        Url = s.Key,
                        Type = s.Value.Type,
                        LastFetched = s.Value.LastFetched
                    }).ToList();
                }
                
                Console.WriteLine($"Successfully loaded {apiResponse.Sources.Count} sources from API.");
                
                // Updating sources in file
                var updatedSources = await sourceFileService.UpdateSourcesFromApiAsync(apiResponse.Sources);
                
                // Converting the dictionary of sources to a list of NewsSourceItem
                var sourceItems = updatedSources.Select(s => new NewsSourceItem
                {
                    Url = s.Key,
                    Type = s.Value.Type,
                    LastFetched = s.Value.LastFetched
                }).ToList();
                
                Console.WriteLine($"Sources have been updated. Total {sourceItems.Count} sources.");
                return sourceItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sources: {ex.Message}");
                _exceptions.Add(ex);
                return null;
            }
        }
    }
} 