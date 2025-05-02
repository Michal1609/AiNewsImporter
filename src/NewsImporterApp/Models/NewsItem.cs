using System;

namespace NewsImport.PlaywrightTest.Models;

/// <summary>
/// Represents a single news item
/// </summary>
public class NewsItem
{
    /// <summary>
    /// Unique identifier of the news item
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// News headline
    /// </summary>
    public string Title { get; set; }
    public string TitleCz { get; set; }

    /// <summary>
    /// News content in Markdown format
    /// </summary>
    public string ContentCz { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string SummaryCz { get; set; } = string.Empty;

    /// <summary>
    /// URL from which the news was downloaded
    /// </summary>
    public string Url { get; set; }

    public string ImageUrl { get; set; }

    /// <summary>
    /// Name of the source website
    /// </summary>
    public string SourceName { get; set; }      
    
    /// <summary>
    /// Date and time of news publication
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// News text in the news list
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Date and time of news import
    /// </summary>
    public DateTime ImportedDate { get; set; } = DateTime.Now;
} 