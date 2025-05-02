using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NewsImporterApp.Core;

namespace NewsImporterApp.Services
{
    public class GeminiAiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IExceptionHandler? _exceptionHandler;
        private const string BaseApiUrl = "https://generativelanguage.googleapis.com/v1";
        private string _apiKey;
        private string _modelName;

        /// <summary>
        /// Initializes a new instance of the GeminiAiService class
        /// </summary>
        /// <param name="exceptionHandler">Optional exception handler for global tracking</param>
        public GeminiAiService(IExceptionHandler? exceptionHandler = null)
        {
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            _exceptionHandler = exceptionHandler;
            _modelName = "gemini-2.0-flash-lite";
        }

        /// <summary>
        /// Sets the API key for access to Google Gemini API
        /// </summary>
        /// <param name="apiKey">API key</param>
        /// <param name="modelName">Optional model name</param>
        public void SetApiKey(string apiKey, string modelName = "gemini-2.0-flash-lite")
        {
            _apiKey = apiKey;
            _modelName = modelName;
        }

        /// <summary>
        /// Analyzes HTML content using Gemini AI and looks for AI-related news
        /// </summary>
        /// <param name="htmlContent">HTML content to analyze</param>
        /// <returns>Analysis result as JSON</returns>
        public async Task<string> GetAllAiNewsFromMarkdown(string htmlContent)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    var ex = new InvalidOperationException("API key is not set. Use the SetApiKey() method.");
                    _exceptionHandler?.AddException(ex);
                    throw ex;
                }

                var prompt = "Najdi v uvedeném HTML textu všechny novinky týkající se AI, a v json formátu mi vrať název novinky, " +
                    "URL na detail, datum vydání novinky, odkaz na image novinky pokud existuje, text novinky. " +
                    "Vrať výsledky jako JSON pole objektů s vlastnostmi 'title', 'url', 'date', 'imageUrl', 'text'.";

                return await CallGeminiApiAsync(prompt, htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAiNewsFromMarkdown: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                throw;
            }
        }

        /// <summary>
        /// Analyzes Markdown content using Gemini AI and looks for AI-related news
        /// </summary>
        /// <param name="markdownContent">Markdown content to analyze</param>
        /// <returns>Analysis result as JSON</returns>
        public async Task<string> GetAllAiNewsFromMarkdownAsync(string markdownContent)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    var ex = new InvalidOperationException("API key is not set. Use the SetApiKey() method.");
                    _exceptionHandler?.AddException(ex);
                    throw ex;
                }

                var prompt = "Najdi v uvedeném Markdown textu všechny novinky týkající se AI, a v json formátu mi vrať název novinky, " +
                    "URL na detail, datum vydání novinky, odkaz na image novinky pokud existuje, text novinky. " +
                    "Vrať výsledky jako JSON pole objektů s vlastnostmi 'title', 'url', 'date', 'imageUrl', 'text'. date je formát date pro JSON";

                return await CallGeminiApiAsync(prompt, markdownContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAiNewsFromMarkdownAsync: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                throw;
            }
        }

        public async Task<string> GetContentOfNewFromMarkdownAsync(string markdownContent, string title)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    var ex = new InvalidOperationException("API key is not set. Use the SetApiKey() method.");
                    _exceptionHandler?.AddException(ex);
                    throw ex;
                }

                var prompt = $"Níže v textu je Markdown, najdi v něm kompletní text novinky '{title}' a přelož mi ji do českého jazyka. " +
                    $"Také zkus v Markdownu k novince najít datum publikace novinky." +
                    $"Data vrat jako JSON s vlastnostma (TitleCz, ContentCz, SummaryEn, SummaryCz, PublishDate) kde TitleCz je titulek přeložený do češtiny, ContentCz bude celý český překlad, SummaryEn bude ten souhrn v angličtině a SummaryCz bude souhrn v češtině a PublishDate bude datum pubklikace novnky pokud existuje a ulož ho formát date pro JSON";


                return await CallGeminiApiAsync(prompt, markdownContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetContentOfNewFromMarkdownAsync: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                throw;
            }
        }

        /// <summary>
        /// Calls Gemini API with the given prompt and content
        /// </summary>
        /// <param name="prompt">Text query for the model</param>
        /// <param name="content">Content to analyze</param>
        /// <returns>API response as a string</returns>
        private async Task<string> CallGeminiApiAsync(string prompt, string content)
        {
            try
            {
                string fullModelName = $"models/{_modelName}";
                string requestUrl = $"{BaseApiUrl}/{fullModelName}:generateContent?key={_apiKey}";

                // Building the request body
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt },
                                new { text = content }
                            }
                        }
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody, _jsonOptions);
                var requestContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, requestContent);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Extracting text from the response
                        using JsonDocument doc = JsonDocument.Parse(responseString);
                        JsonElement root = doc.RootElement;

                        if (root.TryGetProperty("candidates", out JsonElement candidatesElement) &&
                            candidatesElement.EnumerateArray().Any())
                        {
                            JsonElement firstCandidate = candidatesElement.EnumerateArray().First();
                            if (firstCandidate.TryGetProperty("content", out JsonElement contentElement) &&
                                contentElement.TryGetProperty("parts", out JsonElement partsElement) &&
                                partsElement.EnumerateArray().Any())
                            {
                                JsonElement firstPart = partsElement.EnumerateArray().First();
                                if (firstPart.TryGetProperty("text", out JsonElement textElement))
                                {
                                    return textElement.GetString() ?? string.Empty;
                                }
                            }
                        }

                        var ex = new JsonException("Unable to extract text from API response");
                        _exceptionHandler?.AddException(ex);
                        return string.Empty;
                    }
                    catch (JsonException ex)
                    {
                        Console.Error.WriteLine($"Error processing JSON response: {ex.Message}");
                        _exceptionHandler?.AddException(ex);
                        return responseString; // Returning the original response in case of error
                    }
                }
                else
                {
                    var ex = new HttpRequestException($"API Error: HTTP Status Code {response.StatusCode}\nResponse: {responseString}");
                    Console.Error.WriteLine($"API Error: HTTP Status Code {response.StatusCode}");
                    Console.Error.WriteLine($"API Response:\n{responseString}");
                    _exceptionHandler?.AddException(ex);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error calling Gemini API: {ex.Message}");
                _exceptionHandler?.AddException(ex);
                return string.Empty;
            }
        }
    }
}
