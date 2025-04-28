using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewsImporterApp.Services
{
    public class GeminiAiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string BaseApiUrl = "https://generativelanguage.googleapis.com/v1";
        private string _apiKey;
        private string _modelName;

        /// <summary>
        /// Inicializuje novou instanci třídy GeminiAiService
        /// </summary>
        public GeminiAiService()
        {
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            _modelName = "gemini-2.0-flash-lite";
        }

        /// <summary>
        /// Nastaví API klíč pro přístup ke Google Gemini API
        /// </summary>
        /// <param name="apiKey">API klíč</param>
        /// <param name="modelName">Volitelný název modelu</param>
        public void SetApiKey(string apiKey, string modelName = "gemini-2.0-flash-lite")
        {
            _apiKey = apiKey;
            _modelName = modelName;
        }

        /// <summary>
        /// Analyzuje HTML obsah pomocí Gemini AI a hledá novinky týkající se AI
        /// </summary>
        /// <param name="htmlContent">HTML obsah k analýze</param>
        /// <returns>Výsledek analýzy jako JSON</returns>
        public async Task<string> GetAllAiNewsFromMarkdown(string htmlContent)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API klíč není nastaven. Použijte metodu SetApiKey().");
            }

            var prompt = "Najdi v uvedeném HTML textu všechny novinky týkající se AI, a v json formátu mi vrať název novinky, " +
                "URL na detail, datum vydání novinky, odkaz na image novinky pokud existuje, text novinky. " +
                "Vrať výsledky jako JSON pole objektů s vlastnostmi 'title', 'url', 'date', 'imageUrl', 'text'.";

            return await CallGeminiApiAsync(prompt, htmlContent);
        }

        /// <summary>
        /// Analyzuje Markdown obsah pomocí Gemini AI a hledá novinky týkající se AI
        /// </summary>
        /// <param name="markdownContent">Markdown obsah k analýze</param>
        /// <returns>Výsledek analýzy jako JSON</returns>
        public async Task<string> GetAllAiNewsFromMarkdownAsync(string markdownContent)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API klíč není nastaven. Použijte metodu SetApiKey().");
            }

            var prompt = "Najdi v uvedeném Markdown textu všechny novinky týkající se AI, a v json formátu mi vrať název novinky, " +
                "URL na detail, datum vydání novinky, odkaz na image novinky pokud existuje, text novinky. " +
                "Vrať výsledky jako JSON pole objektů s vlastnostmi 'title', 'url', 'date', 'imageUrl', 'text'. date je formát date pro JSON";

            return await CallGeminiApiAsync(prompt, markdownContent);
        }

        public async Task<string> GetContentOfNewFromMarkdownAsync(string markdownContent, string title)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API klíč není nastaven. Použijte metodu SetApiKey().");
            }

            var prompt = $"Níže v textu je Markdown, najdi v něm kompletní text novinky '{title}' a přelož mi ji do českého jazyka. " +
                $"Také zkus v Markdownu k novince najít datum publikace novinky." +
                $"Data vrat jako JSON s vlastnostma (ContentCz, SummaryEn, SummaryCz, PublishDate) kde ContentCz bude celý český překlad, SummaryEn bude ten souhrn v angličtině a SummaryCz bude souhrn v češtině a PublishDate bude datum pubklikace novnky pokud existuje a ulož ho formát date pro JSON";


            return await CallGeminiApiAsync(prompt, markdownContent);
        }

        /// <summary>
        /// Volá Gemini API s daným promptem a obsahem
        /// </summary>
        /// <param name="prompt">Textový dotaz pro model</param>
        /// <param name="content">Obsah k analýze</param>
        /// <returns>Odpověď API jako řetězec</returns>
        private async Task<string> CallGeminiApiAsync(string prompt, string content)
        {
            string fullModelName = $"models/{_modelName}";
            string requestUrl = $"{BaseApiUrl}/{fullModelName}:generateContent?key={_apiKey}";

            // Sestavení těla požadavku
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
                    // Extrahujeme text z odpovědi
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

                    return string.Empty;
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"Chyba při zpracování JSON odpovědi: {ex.Message}");
                    return responseString; // Vracíme původní odpověď v případě chyby
                }
            }
            else
            {
                Console.Error.WriteLine($"Chyba API: HTTP Status Kód {response.StatusCode}");
                Console.Error.WriteLine($"Odpověď API:\n{responseString}");
                return string.Empty;
            }
        }
    }
}
