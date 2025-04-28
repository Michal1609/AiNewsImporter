using System;
using NewsImporterApp.Contracts.GetSources;

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
    /// Type of the news source
    /// </summary>
    public SourceType Type { get; set; }
    
    /// <summary>
    /// Date of the last successful fetch
    /// </summary>
    public DateTime LastFetched { get; set; }
} 