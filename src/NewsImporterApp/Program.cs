
using System.Text.Json;
using NewsImport.PlaywrightTest;
using NewsImport.PlaywrightTest.Models;
using NewsImport.PlaywrightTest.Utilities;
using NewsImporterApp.Core;
using NewsImporterApp.Services;
using NewsImporterApp.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;

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
                if (item.Date != null && item.Date < source.LastFetched.AddDays(-1))
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

                    if (contet.PublishDate != null && contet.PublishDate < source.LastFetched.AddDays(-1))
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

// Po odeslání novinek odešleme i chyby
await SendNewsToWebAsync(allNewsItems);
await SendErrorsToWebAsync(excpetions);

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
    try
    {
        // Inicializace služby pro práci se zdroji
        var sourceFileService = new SourceFileService();
        
        // Inicializace a konfigurace GrznarAi služby
        var config = await LoadConfigAsync(CreateJsonOptions());
        if (config == null)
        {
            Console.WriteLine("Nelze načíst konfiguraci pro API.");
            return null;
        }
        
        // Vytvoření instance GrznarAi služby
        var grznarAiService = new GrznarAiService(config);
        
        // Získání zdrojů z API
        var apiResponse = await grznarAiService.GetSourcesAsync();
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
        return null;
    }
}

static async Task SendNewsToWebAsync(List<NewsItem> newsItems)
{
    try
    {
        // Načtení konfigurace
        var config = await LoadConfigAsync(CreateJsonOptions());
        if (config == null)
        {
            Console.WriteLine("Nelze načíst konfiguraci pro API.");
            return;
        }
        
        // Vytvoření HttpClient
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(config.GrznarAiBaseUrl);
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.GrznarAiApiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        
        // Převod NewsItem na AiNewsItemRequest
        var requestItems = newsItems.Select(item => new NewsImporterApp.Contracts.AddNewsItems.AiNewsItemRequest
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
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var jsonContent = JsonSerializer.Serialize(requestItems, jsonOptions);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        
        // Odeslání požadavku
        var response = await httpClient.PostAsync("api/ainews/items", content);
        
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

static async Task SendErrorsToWebAsync(List<Exception> exceptions)
{
    try
    {
        if (exceptions == null || exceptions.Count == 0)
        {
            Console.WriteLine("Žádné chyby k odeslání.");
            return;
        }
        
        // Načtení konfigurace
        var config = await LoadConfigAsync(CreateJsonOptions());
        if (config == null)
        {
            Console.WriteLine("Nelze načíst konfiguraci pro API.");
            return;
        }
        
        // Vytvoření HttpClient
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(config.GrznarAiBaseUrl);
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.GrznarAiApiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        // Převod výjimek na AiNewsErrorRequest
        var errorRequests = exceptions.Select(ex => new NewsImporterApp.Contracts.AddErrors.AiNewsErrorRequest
        {
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            Details = ex.InnerException?.Message,
            Category = ex.GetType().Name
        }).ToList();
        
        Console.WriteLine($"Odesílám {errorRequests.Count} chyb na server...");
        
        // Serializace dat
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var jsonContent = JsonSerializer.Serialize(errorRequests, jsonOptions);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        
        // Odeslání požadavku
        var response = await httpClient.PostAsync("api/ainews/errors", content);
        
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