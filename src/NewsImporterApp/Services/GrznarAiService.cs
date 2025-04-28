using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using NewsImporterApp.Contracts.GetSources;
using NewsImporterApp.Core;

namespace NewsImporterApp.Services
{
    /// <summary>
    /// Service for communication with GrznarAi API
    /// </summary>
    public class GrznarAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;
        
        /// <summary>
        /// Initializes a new instance of the GrznarAiService
        /// </summary>
        /// <param name="config">Application configuration</param>
        public GrznarAiService(AppConfig config)
        {
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
            
            _apiKey = config.GrznarAiApiKey;
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
    }
} 