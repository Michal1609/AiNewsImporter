using System.Text.Json;
using NewsImport.PlaywrightTest;
using NewsImport.PlaywrightTest.Models;
using NewsImport.PlaywrightTest.Utilities;
using NewsImporterApp.Core;
using NewsImporterApp.Models;

namespace NewsImporterApp.Services
{
    public class NewsProcessor
    {
        private readonly GeminiAiService _geminiService;
        private readonly HtmlCleaner _htmlCleaner;
        private readonly MarkdownConverter _markdownConverter;
        private readonly PlaywrightService _playwrightService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly int _pageLoadTimeoutMs;

        public NewsProcessor(
            GeminiAiService geminiService,
            HtmlCleaner htmlCleaner,
            MarkdownConverter markdownConverter,
            PlaywrightService playwrightService,
            JsonSerializerOptions jsonOptions,
            int pageLoadTimeoutMs)
        {
            _geminiService = geminiService;
            _htmlCleaner = htmlCleaner;
            _markdownConverter = markdownConverter;
            _playwrightService = playwrightService;
            _jsonOptions = jsonOptions;
            _pageLoadTimeoutMs = pageLoadTimeoutMs;
        }

        public async Task<List<NewsItem>> ProcessNewsSourcesAsync(List<NewsSourceItem> newsSources)
        {
            var allNewsItems = new List<NewsItem>();
            var exceptions = new List<Exception>();
            
            int processedCount = 0;
            int totalSources = newsSources.Count;

            foreach (var source in newsSources)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine($"[{++processedCount}/{totalSources}] Zpracovávám: {source.Url}");

                    var newsItems = await ProcessNewsSourceAsync(source);
                    if (newsItems != null && newsItems.Any())
                    {
                        allNewsItems.AddRange(newsItems);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při zpracování zdroje {source.Url}: {ex.Message}");
                    exceptions.Add(ex);
                    continue;
                }
            }

            Console.WriteLine("Mam vsechnuy novinky, delam distinct podle nazvu");
            return allNewsItems.DistinctBy(x => x.Title).ToList();
        }

        private async Task<List<NewsItem>> ProcessNewsSourceAsync(NewsSourceItem source)
        {
            var sourceNewsItems = new List<NewsItem>();
            
            // Get news from source
            string htmlContent = await _playwrightService.GetPageContentAsync(source.Url, _pageLoadTimeoutMs);
            Console.WriteLine("CleanHtml");
            string cleanedHtml = _htmlCleaner.CleanHtml(htmlContent);
            Console.WriteLine("ConvertTomarkdow");
            string markdownContent = _markdownConverter.ConvertToMarkdown(cleanedHtml);
            Console.WriteLine("GetAllAiNewsFromMarkdownAsync");
            string markdownAnalysisResult = await _geminiService.GetAllAiNewsFromMarkdownAsync(markdownContent);
            Console.WriteLine("Odstranim json text");
            
            markdownAnalysisResult = CleanJsonMarkdown(markdownAnalysisResult);
            
            Console.WriteLine("Deserializuji novinky");
            var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(markdownAnalysisResult, _jsonOptions);

            if (newsItems == null || newsItems.Count == 0)
            {
                Console.WriteLine("Žádné novinky nebyly nalezeny.");
                return sourceNewsItems;
            }

            newsItems = newsItems.DistinctBy(item => item.Title).ToList();

            foreach (var item in newsItems)
            {
                try
                {
                    if (item.Date != null && item.Date < source.LastFetched.AddDays(-1))
                        continue;

                    var baseUri = new Uri(source.Url);
                    item.Url = new Uri(baseUri, item.Url).ToString();

                    // Získání obsahu novinky
                    var newsItem = await ProcessNewsItemAsync(item, source);
                    if (newsItem != null)
                    {
                        sourceNewsItems.Add(newsItem);
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine($"Chyba při zpracování obsahu novinky '{item.Title}': {ex.Message}");
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při zpracování novinky '{item.Title}': {ex.Message}");
                    continue;
                }
            }
            
            return sourceNewsItems;
        }

        private async Task<NewsItem?> ProcessNewsItemAsync(NewsItem item, NewsSourceItem source)
        {
            // get content for new
            Console.WriteLine($"Nacitam novnku: {item.Url}");
            string htmlContent = await _playwrightService.GetPageContentAsync(item.Url, 10000);
            Console.WriteLine("CleanHtml");
            string cleanedHtml = _htmlCleaner.CleanHtml(htmlContent);
            Console.WriteLine("Markdown");
            string markdownContent = _markdownConverter.ConvertToMarkdown(cleanedHtml);
            Console.WriteLine("geminy");
            string markdownAnalysisResult = await _geminiService.GetContentOfNewFromMarkdownAsync(markdownContent, item.Title);
            
            try
            {
                Console.WriteLine("Test na json");
                markdownAnalysisResult = CleanJsonMarkdown(markdownAnalysisResult);

                var content = JsonSerializer.Deserialize<NewsContent>(markdownAnalysisResult);
                if (content == null)
                    return null;

                if (content.PublishDate != null && content.PublishDate < source.LastFetched.AddDays(-1))
                    return null;

                if (item.Date is null && content.PublishDate is null)
                    return null;

                Console.WriteLine("Ukladam novinku");
                return new NewsItem
                {
                    Title = item.Title,
                    TitleCz = content.TitleCz,
                    Url = item.Url,
                    Date = item.Date ?? content.PublishDate,
                    ImageUrl = item.ImageUrl,
                    Text = content.ContentCz,
                    SummaryCz = content.SummaryCz,
                    SummaryEn = content.SummaryEn,
                    ContentCz = content.ContentCz,
                    ImportedDate = DateTime.Now,
                    SourceName = source.Url,
                };
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Chyba při deserializaci obsahu novinky '{item.Title}': {ex.Message}");
                throw;
            }
        }

        private string CleanJsonMarkdown(string markdownContent)
        {
            return markdownContent
                .Replace("```json", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("```", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Trim();
        }
    }
} 