// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using NewsImport.PlaywrightTest;
using NewsImport.PlaywrightTest.Models;
using NewsImport.PlaywrightTest.Utilities;
using NewsImporterApp.Core;
using NewsImporterApp.Services;
using NewsImporterApp.Models;




var excpetions = new List<Exception>();
var _jsonOptions = CreateJsonOptions();

var config = await LoadConfigAsync(_jsonOptions);
if (config == null)
{
    Console.WriteLine("Nelze načíst konfiguraci. Program bude ukončen.");
    return;
}

var newsSources = await LoadNewsSourcesAsync();
if (newsSources == null || newsSources.Count == 0)
{
    Console.WriteLine("Nelze načíst seznam zdrojů. Program bude ukončen.");
    return;
}

// 3. Inicializace služeb
var geminiService = new GeminiAiService();
geminiService.SetApiKey(config.GoogleApiKey);
var htmlCleaner = new HtmlCleaner();
var markdownConverter = new MarkdownConverter();
var playwrightService = new PlaywrightService();


// 7. Seznam všech nalezených novinek
var allNewsItems = new List<NewsItem>();

// 8. Zpracování všech zdrojů
int processedCount = 0;
int totalSources = newsSources.Count;

foreach (var source in newsSources)
{
    try
    {
        Console.WriteLine();
        Console.WriteLine($"[{++processedCount}/{totalSources}] Zpracovávám: {source.Url}");

        // get news
        string htmlContent = await playwrightService.GetPageContentAsync(source.Url, config.PageLoadTimeoutMs);
        Console.WriteLine("CleanHtml");
        string cleanedHtml = htmlCleaner.CleanHtml(htmlContent);
        Console.WriteLine("ConvertTomarkdow");
        string markdownContent = markdownConverter.ConvertToMarkdown(cleanedHtml);
        Console.WriteLine("GetAllAiNewsFromMarkdownAsync");
        string markdownAnalysisResult = await geminiService.GetAllAiNewsFromMarkdownAsync(markdownContent);
        Console.WriteLine("Odstranim json text");
        
        markdownAnalysisResult = markdownAnalysisResult.Replace("```json", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("```", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        markdownAnalysisResult = markdownAnalysisResult.Trim();
        Console.WriteLine("Deserializuji novinky");
        var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(markdownAnalysisResult, _jsonOptions);

        if (newsItems == null || newsItems.Count == 0)
        {
            Console.WriteLine("Žádné novinky nebyly nalezeny.");
            continue;
        }

        newsItems = newsItems!.DistinctBy(item => item.Title).ToList();

        foreach (var item in newsItems)
        {
            try
            {
                if (item.Date != null && item.Date < source.LastScrapeDate.AddDays(-1))
                    continue;

                var baseUri = new Uri(source.Url);
                item.Url = new Uri(baseUri, item.Url).ToString();

                // get content for new
                Console.WriteLine($"Nacitam novnku: {item.Url}");
                htmlContent = await playwrightService.GetPageContentAsync(item.Url, 10000);
                Console.WriteLine("CleanHtml");
                cleanedHtml = htmlCleaner.CleanHtml(htmlContent);
                Console.WriteLine("Markdown");
                markdownContent = markdownConverter.ConvertToMarkdown(cleanedHtml);
                Console.WriteLine("geminy");
                markdownAnalysisResult = await geminiService.GetContentOfNewFromMarkdownAsync(markdownContent, item.Title);
                //markdownAnalysisResult = markdownAnalysisResult.Substring(8, markdownAnalysisResult.Length - 11);
                try
                {
                    Console.WriteLine("Test na json");
                    markdownAnalysisResult = markdownAnalysisResult.Replace("```json", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                            .Replace("```", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                    markdownAnalysisResult = markdownAnalysisResult.Trim();

                    var contet = JsonSerializer.Deserialize<NewsContent>(markdownAnalysisResult);
                    if (contet == null)
                        continue;

                    if (contet.PublishDate != null && contet.PublishDate < source.LastScrapeDate.AddDays(-1))
                        continue;

                    if (item.Date is null && contet.PublishDate is null)
                        continue;

                    Console.WriteLine("Ukladam novinku");
                    allNewsItems.Add(new NewsItem
                    {
                        Title = item.Title,
                        Url = item.Url,
                        Date = item.Date ?? contet.PublishDate,
                        ImageUrl = item.ImageUrl,
                        Text = contet.ContentCz,
                        SummaryCz = contet.SummaryCz,
                        SummaryEn = contet.SummaryEn,
                        ContentCz = contet.ContentCz,
                        ImportedDate = DateTime.Now,
                        SourceName = source.Url,
                    });
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Chyba při deserializaci obsahu novinky '{item.Title}': {ex.Message}");
                    excpetions.Add(ex);
                    continue;
                }
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Chyba při zpracování obsahu novinky '{item.Title}': {ex.Message}");
                excpetions.Add(ex);
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při zpracování novinky '{item.Title}': {ex.Message}");
                excpetions.Add(ex);
                continue;

            }
        }
            
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chyba při zpracování zdroje {source.Url}: {ex.Message}");
        excpetions.Add(ex);
        continue;
    }
}

Console.WriteLine("Mam vsechnuy novinky, delam distinct podle nazvu");
allNewsItems = allNewsItems.DistinctBy(x => x.Title).ToList();
Console.WriteLine("Desertializaco do string");
var result = JsonSerializer.Serialize(allNewsItems, _jsonOptions);
Console.WriteLine("Ukladama file");
File.WriteAllText("result.json", result);

AggregateException aggregateException = new AggregateException(excpetions);
//save all exceptions to file
File.WriteAllText("exceptions.txt", aggregateException.ToString());


static async Task<AppConfig?> LoadConfigAsync(JsonSerializerOptions jsonOptions)
{
    try
    {
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
                JsonSerializer.Serialize(defaultConfig, jsonOptions));

            Console.WriteLine($"Vytvořen výchozí konfigurační soubor: {configPath}");
            Console.WriteLine("Zadejte svůj Google API klíč do konfiguračního souboru a spusťte program znovu.");
            return null;
        }

        string configJson = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(configJson, jsonOptions);

        // Ověření, zda máme všechna potřebná nastavení
        if (config == null || string.IsNullOrEmpty(config.GoogleApiKey))
        {
            Console.WriteLine("Chybí Google API klíč v konfiguračním souboru.");
            return null;
        }

        return config;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chyba při načítání konfigurace: {ex.Message}");
        return null;
    }
}

static JsonSerializerOptions CreateJsonOptions()
{
    return new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

static async Task<List<NewsSourceItem>?> LoadNewsSourcesAsync()
{
    //return new List<NewsSourceItem> { new NewsSourceItem {  LastScrapeDate =  DateTime.Now, Url = "https://openai.com/news/" } };
    try
    {
        string sourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sources.txt");
        var sources = new List<NewsSourceItem>();

        // Pokud soubor neexistuje, vytvoříme jej
        if (!File.Exists(sourcesPath))
        {
            return new List<NewsSourceItem>();
        }

        // Načtení a parsing zdrojů
        string[] lines = await File.ReadAllLinesAsync(sourcesPath);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            string[] parts = line.Split(';');
            if (parts.Length != 2)
            {
                Console.WriteLine($"Neplatný formát řádku: {line}");
                continue;
            }

            if (!DateTime.TryParseExact(parts[1], "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out DateTime lastScrapeDate))
            {
                Console.WriteLine($"Neplatný formát data: {parts[1]}");
                continue;
            }

            sources.Add(new NewsSourceItem
            {
                Url = parts[0],
                LastScrapeDate = lastScrapeDate
            });
        }

        return sources;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chyba při načítání zdrojů: {ex.Message}");
        return null;
    }
}