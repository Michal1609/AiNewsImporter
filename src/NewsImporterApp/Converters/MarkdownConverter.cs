using HtmlAgilityPack;
using ReverseMarkdown;
using System.Text.RegularExpressions;

namespace NewsImport.PlaywrightTest
{
    /// <summary>
    /// Class for converting HTML to Markdown format
    /// </summary>
    public class MarkdownConverter
    {
        private readonly Converter _converter;
        
        /// <summary>
        /// Initializes a new instance of Markdown converter
        /// </summary>
        public MarkdownConverter()
        {
            // Configuration of ReverseMarkdown converter
            var config = new ReverseMarkdown.Config
            {
                // Setting how to handle unknown tags
                UnknownTags = Config.UnknownTagsOption.PassThrough,
                // Preserve original formatting
                SmartHrefHandling = true,
                // Settings for lists
                ListBulletChar = '*',
                // Settings for text
                RemoveComments = true,
                // Image processing
                GithubFlavored = true
            };
            
            _converter = new Converter(config);
        }
        
        /// <summary>
        /// Converts HTML content to Markdown
        /// </summary>
        /// <param name="htmlContent">HTML content</param>
        /// <returns>Markdown representation of HTML content</returns>
        public string ConvertToMarkdown(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return string.Empty;
            }
            
            try
            {
                // Preliminary HTML cleaning - removing scripts, styles and other unnecessary elements
                htmlContent = CleanupHtml(htmlContent);
                
                // Using ReverseMarkdown library for conversion
                try
                {
                    // Creating HtmlAgilityPack document for proper parsing
                    var document = new HtmlDocument();
                    document.LoadHtml(htmlContent);
                    
                    // Cleaning document from javascript and unnecessary elements
                    CleanupDocument(document);
                    
                    // Conversion to Markdown using ReverseMarkdown
                    var markdown = _converter.Convert(document.DocumentNode.OuterHtml);
                    
                    // Additional cleaning of the resulting markdown
                    markdown = CleanupMarkdown(markdown);
                    
                    return markdown;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while converting HTML to Markdown: {ex.Message}");
                    return $"Error while converting to Markdown: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Error while processing HTML: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Cleans HTML code from unnecessary elements
        /// </summary>
        /// <param name="html">HTML code</param>
        /// <returns>Cleaned HTML code</returns>
        private string CleanupHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }
            
            // Removing JavaScript code
            html = Regex.Replace(html, @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>", "", RegexOptions.IgnoreCase);
            
            // Removing CSS styles
            html = Regex.Replace(html, @"<style\b[^<]*(?:(?!</style>)<[^<]*)*</style>", "", RegexOptions.IgnoreCase);
            
            // Removing comments
            html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);
            
            // Removing unnecessary spaces
            html = Regex.Replace(html, @"\s{2,}", " ");
            
            return html;
        }
        
        /// <summary>
        /// Cleans HTML document from unnecessary elements using HtmlAgilityPack
        /// </summary>
        /// <param name="document">HTML document</param>
        private void CleanupDocument(HtmlDocument document)
        {
            // Identification and removal of unnecessary elements
            var nodesToRemove = new List<HtmlNode>();
            
            // Finding all script, style, meta, link, svg, iframe elements
            var elementsToRemove = document.DocumentNode.SelectNodes("//script|//style|//meta|//link|//iframe|//svg");
            
            if (elementsToRemove != null)
            {
                foreach (var node in elementsToRemove)
                {
                    nodesToRemove.Add(node);
                }
            }
            
            // Finding elements with specific attributes and classes that likely contain ads, navigation, etc.
            var attributeElementsToRemove = document.DocumentNode.SelectNodes("//*[contains(@class, 'ad') or contains(@class, 'ads') or contains(@class, 'banner') or contains(@class, 'cookie') or contains(@class, 'footer') or contains(@class, 'navigation') or contains(@id, 'ad')]");
            
            if (attributeElementsToRemove != null)
            {
                foreach (var node in attributeElementsToRemove)
                {
                    nodesToRemove.Add(node);
                }
            }
            
            // Removing found elements
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }
        
        /// <summary>
        /// Additional cleaning of the resulting Markdown text
        /// </summary>
        /// <param name="markdown">Markdown text</param>
        /// <returns>Cleaned Markdown text</returns>
        private string CleanupMarkdown(string markdown)
        {
            // Removing empty lines at the beginning and end
            markdown = markdown.Trim();
            
            // Replacing more than two empty lines in a row with two empty lines
            markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
            
            // Removing excessive spaces at the beginning of lines
            markdown = Regex.Replace(markdown, @"(?<=\n) +", "");
            
            // Fixing links that have excessive characters
            markdown = Regex.Replace(markdown, @"\[([^\]]+)\]\s*\(([^)]+)\)", "[$1]($2)");
            
            // Fixing headers that don't have a space after # (e.g. "#Header" -> "# Header")
            for (int i = 1; i <= 6; i++)
            {
                string prefix = new string('#', i);
                markdown = Regex.Replace(markdown, $@"{prefix}([^\s#])", $"{prefix} $1");
            }
            
            return markdown;
        }
    }
} 