using System;

namespace NewsImport.PlaywrightTest.Models;

/// <summary>
/// Represents a source of news articles with tracking information
/// </summary>
public class NewsSourceItem
{
    /// <summary>
    /// URL of the news source to scrape
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Date of the last successful scrape
    /// </summary>
    public DateTime LastScrapeDate { get; set; }
} 