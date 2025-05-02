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
        private readonly IExceptionHandler? _exceptionHandler;

        /// <summary>
        /// Initializes a new instance of the ApiService
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="exceptionHandler">Optional exception handler for global tracking</param>
        public ApiService(AppConfig config, IExceptionHandler? exceptionHandler = null)
        {
            _config = config;
            _exceptionHandler = exceptionHandler;
            
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
                _exceptionHandler?.AddException(ex);
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
                    Console.WriteLine("No news to send.");
                    return;
                }
                
                // Converting NewsItem to AiNewsItemRequest
                var requestItems = newsItems.Select(item => new AiNewsItemRequest
                {
                    TitleEn = item.Title ?? string.Empty,
                    TitleCz = item.TitleCz ?? string.Empty,
                    ContentEn = item.Text,
                    ContentCz = item.ContentCz,
                    SummaryEn = item.SummaryEn,
                    SummaryCz = item.SummaryCz,
                    Url = item.Url ?? string.Empty,
                    ImageUrl = item.ImageUrl,
                    SourceName = item.SourceName ?? string.Empty,
                    PublishedDate = item.Date
                }).ToList();
                
                Console.WriteLine($"Sending {requestItems.Count} news items to the server...");
                
                // Serializing data
                var jsonContent = JsonSerializer.Serialize(requestItems, _jsonOptions);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                // Sending request
                var response = await _httpClient.PostAsync("api/ainews/items", content);
                
                // Checking response
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"News were successfully sent: {response.StatusCode}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error sending news: {response.StatusCode}");
                    Console.WriteLine($"Error details: {errorContent}");
                    
                    var errorException = new Exception($"Error sending news: {response.StatusCode}. Details: {errorContent}");
                    _exceptionHandler?.AddException(errorException);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while sending news: {ex.Message}");
                _exceptionHandler?.AddException(ex);
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
                    Console.WriteLine("No errors to send.");
                    return;
                }
                
                // Converting exceptions to AiNewsErrorRequest
                var errorRequests = exceptions.Select(ex => new AiNewsErrorRequest
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Details = ex.InnerException?.Message,
                    Category = ex.GetType().Name
                }).ToList();
                
                Console.WriteLine($"Sending {errorRequests.Count} errors to the server...");
                
                // Serializing data
                var jsonContent = JsonSerializer.Serialize(errorRequests, _jsonOptions);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                // Sending request
                var response = await _httpClient.PostAsync("api/ainews/errors", content);
                
                // Checking response
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Errors were successfully sent: {response.StatusCode}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error sending errors: {response.StatusCode}");
                    Console.WriteLine($"Error details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while sending errors: {ex.Message}");
            }
        }
    }
} 