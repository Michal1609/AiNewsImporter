using System;

namespace NewsImport.PlaywrightTest.Models;

/// <summary>
/// Reprezentuje jednu zprávu
/// </summary>
public class NewsItem
{
    /// <summary>
    /// Unikátní identifikátor zprávy
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Titulek zprávy
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Obsah zprávy ve formátu Markdown
    /// </summary>
    public string ContentCz { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string SummaryCz { get; set; } = string.Empty;

    /// <summary>
    /// URL, odkud byla zpráva stažena
    /// </summary>
    public string Url { get; set; }

    public string ImageUrl { get; set; }

    /// <summary>
    /// Název zdrojového webu
    /// </summary>
    public string SourceName { get; set; }      
    
    /// <summary>
    /// Datum a čas publikování zprávy
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Text zprávy na seznamu zpráv
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Datum a čas importu zprávy
    /// </summary>
    public DateTime ImportedDate { get; set; } = DateTime.Now;
} 