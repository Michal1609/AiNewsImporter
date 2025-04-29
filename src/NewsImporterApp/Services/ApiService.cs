using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NewsImport.PlaywrightTest.Models;
using NewsImporterApp.Contracts.AddErrors;
using NewsImporterApp.Contracts.AddNewsItems;
using NewsImporterApp.Contracts.GetSources;
using NewsImporterApp.Core;
using NewsImporterApp.Models;

namespace NewsImporterApp.Services
{
    /// <summary>
    /// Service for communication with GrznarAi API
    /// </summary>
    public class ApiService
    {
        private readonly AppConfig _config;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the ApiService
        /// </summary>
        /// <param name="config">Application configuration</param>
        public ApiService(AppConfig config)
        {
            _config = config;
            
            // Create service collection for dependency injection
            var services = new ServiceCollection();
            
            // Configure HttpClient
            services.AddHttpClient("GrznarAi", client => {
                client.BaseAddress = new Uri(config.GrznarAiBaseUrl);
                client.DefaultRequestHeaders.Add("X-Api-Key", config.GrznarAiApiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            
            // Get HttpClientFactory and create HttpClient
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            _httpClient = clientFactory.CreateClient("GrznarAi");
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Gets a list of AI news sources from the API
        /// </summary>
        /// <returns>List of news sources</returns>
        public async Task<AiNewsSourceListResponse?> GetSourcesAsync()
        {
            try
            {
                // Send request
                var response = await _httpClient.GetAsync("api/ainews/sources");
                
                // Check if request was successful
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error getting sources: {response.StatusCode}");
                    return null;
                }
                
                // Deserialize response
                var content = await response.Content.ReadAsStringAsync();
                var sources = JsonSerializer.Deserialize<AiNewsSourceListResponse>(content, _jsonOptions);
                
                return sources;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting sources: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sends news items to the API
        /// </summary>
        /// <param name="newsItems">News items to send</param>
        public async Task SendNewsToWebAsync(List<NewsItem> newsItems)
        {
            try
            {
                if (newsItems == null || newsItems.Count == 0)
                {
                    Console.WriteLine("Žádné novinky k odeslání.");
                    return;
                }
                
                // Převod NewsItem na AiNewsItemRequest
                var requestItems = newsItems.Select(item => new AiNewsItemRequest
                {
                    TitleEn = item.Title ?? string.Empty,
                    TitleCz = item.Title ?? string.Empty,
                    ContentEn = item.Text,
                    ContentCz = item.ContentCz,
                    SummaryEn = item.SummaryEn,
                    SummaryCz = item.SummaryCz,
                    Url = item.Url ?? string.Empty,
                    ImageUrl = item.ImageUrl,
                    SourceName = item.SourceName ?? string.Empty,
                    PublishedDate = item.Date
                }).ToList();
                
                Console.WriteLine($"Odesílám {requestItems.Count} novinek na server...");
                
                // Serializace dat
                var jsonContent = JsonSerializer.Serialize(requestItems, _jsonOptions);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                // Odeslání požadavku
                var response = await _httpClient.PostAsync("api/ainews/items", content);
                
                // Kontrola odpovědi
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Novinky byly úspěšně odeslány: {response.StatusCode}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Chyba při odesílání novinek: {response.StatusCode}");
                    Console.WriteLine($"Detail chyby: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Výjimka při odesílání novinek: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends errors to the API
        /// </summary>
        /// <param name="exceptions">Exceptions to send</param>
        public async Task SendErrorsToWebAsync(List<Exception> exceptions)
        {
            try
            {
                if (exceptions == null || exceptions.Count == 0)
                {
                    Console.WriteLine("Žádné chyby k odeslání.");
                    return;
                }
                
                // Převod výjimek na AiNewsErrorRequest
                var errorRequests = exceptions.Select(ex => new AiNewsErrorRequest
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Details = ex.InnerException?.Message,
                    Category = ex.GetType().Name
                }).ToList();
                
                Console.WriteLine($"Odesílám {errorRequests.Count} chyb na server...");
                
                // Serializace dat
                var jsonContent = JsonSerializer.Serialize(errorRequests, _jsonOptions);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                // Odeslání požadavku
                var response = await _httpClient.PostAsync("api/ainews/errors", content);
                
                // Kontrola odpovědi
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Chyby byly úspěšně odeslány: {response.StatusCode}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Chyba při odesílání chyb: {response.StatusCode}");
                    Console.WriteLine($"Detail chyby: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Výjimka při odesílání chyb: {ex.Message}");
            }
        }
    }
} 