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
        private readonly IExceptionHandler _exceptionHandler;

        public NewsProcessor(
            GeminiAiService geminiService,
            HtmlCleaner htmlCleaner,
            MarkdownConverter markdownConverter,
            PlaywrightService playwrightService,
            JsonSerializerOptions jsonOptions,
            int pageLoadTimeoutMs,
            IExceptionHandler exceptionHandler)
        {
            _geminiService = geminiService;
            _htmlCleaner = htmlCleaner;
            _markdownConverter = markdownConverter;
            _playwrightService = playwrightService;
            _jsonOptions = jsonOptions;
            _pageLoadTimeoutMs = pageLoadTimeoutMs;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<List<NewsItem>> ProcessNewsSourcesAsync(List<NewsSourceItem> newsSources)
        {
            var allNewsItems = new List<NewsItem>();
            
            int processedCount = 0;
            int totalSources = newsSources.Count;

            foreach (var source in newsSources)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine($"[{++processedCount}/{totalSources}] Processing: {source.Url}");

                    var newsItems = await ProcessNewsSourceAsync(source);
                    if (newsItems != null && newsItems.Any())
                    {
                        allNewsItems.AddRange(newsItems);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing source {source.Url}: {ex.Message}");
                    _exceptionHandler.AddException(ex);
                    continue;
                }
            }

            Console.WriteLine("Got all news items, making distinct by title");
            return allNewsItems.DistinctBy(x => x.Title).ToList();
        }

        private async Task<List<NewsItem>> ProcessNewsSourceAsync(NewsSourceItem source)
        {
            var sourceNewsItems = new List<NewsItem>();
            
            try 
            {
                // Get news from source
                string htmlContent = await _playwrightService.GetPageContentAsync(source.Url, _pageLoadTimeoutMs);
                Console.WriteLine("CleanHtml");
                string cleanedHtml = _htmlCleaner.CleanHtml(htmlContent);
                Console.WriteLine("ConvertToMarkdown");
                string markdownContent = _markdownConverter.ConvertToMarkdown(cleanedHtml);
                Console.WriteLine("GetAllAiNewsFromMarkdownAsync");
                string markdownAnalysisResult = await _geminiService.GetAllAiNewsFromMarkdownAsync(markdownContent);
                Console.WriteLine("Removing json text");
                
                markdownAnalysisResult = CleanJsonMarkdown(markdownAnalysisResult);
                
                Console.WriteLine("Deserializing news items");
                var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(markdownAnalysisResult, _jsonOptions);

                if (newsItems == null || newsItems.Count == 0)
                {
                    Console.WriteLine("No news items were found.");
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

                        // Getting news content
                        var newsItem = await ProcessNewsItemAsync(item, source);
                        if (newsItem != null)
                        {
                            sourceNewsItems.Add(newsItem);
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine($"Error processing news content '{item.Title}': {ex.Message}");
                        _exceptionHandler.AddException(ex);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing news item '{item.Title}': {ex.Message}");
                        _exceptionHandler.AddException(ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing source content from {source.Url}: {ex.Message}");
                _exceptionHandler.AddException(ex);
            }
            
            return sourceNewsItems;
        }

        private async Task<NewsItem?> ProcessNewsItemAsync(NewsItem item, NewsSourceItem source)
        {
            try
            {
                // get content for news
                Console.WriteLine($"Loading news item: {item.Url}");
                string htmlContent = await _playwrightService.GetPageContentAsync(item.Url, 10000);
                Console.WriteLine("CleanHtml");
                string cleanedHtml = _htmlCleaner.CleanHtml(htmlContent);
                Console.WriteLine("Markdown");
                string markdownContent = _markdownConverter.ConvertToMarkdown(cleanedHtml);
                Console.WriteLine("Gemini");
                string markdownAnalysisResult = await _geminiService.GetContentOfNewFromMarkdownAsync(markdownContent, item.Title);
                
                Console.WriteLine("Testing for json");
                markdownAnalysisResult = CleanJsonMarkdown(markdownAnalysisResult);

                var content = JsonSerializer.Deserialize<NewsContent>(markdownAnalysisResult);
                if (content == null)
                    return null;

                if (content.PublishDate != null && content.PublishDate < source.LastFetched.AddDays(-1))
                    return null;

                if (item.Date is null && content.PublishDate is null)
                    return null;

                Console.WriteLine("Saving news item");
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
                Console.WriteLine($"Error deserializing news content '{item.Title}': {ex.Message}");
                _exceptionHandler.AddException(ex);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing news item '{item.Title}': {ex.Message}");
                _exceptionHandler.AddException(ex);
                return null;
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