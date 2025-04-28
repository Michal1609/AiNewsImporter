using HtmlAgilityPack;
using ReverseMarkdown;
using System.Text.RegularExpressions;

namespace NewsImport.PlaywrightTest
{
    /// <summary>
    /// Třída pro konverzi HTML do Markdown formátu
    /// </summary>
    public class MarkdownConverter
    {
        private readonly Converter _converter;
        
        /// <summary>
        /// Inicializuje novou instanci konvertoru Markdown
        /// </summary>
        public MarkdownConverter()
        {
            // Konfigurace konvertoru ReverseMarkdown
            var config = new ReverseMarkdown.Config
            {
                // Nastavení, jak zacházet s neznámými tagy
                UnknownTags = Config.UnknownTagsOption.PassThrough,
                // Zachovat původní formátování
                SmartHrefHandling = true,
                // Nastavení pro seznamy
                ListBulletChar = '*',
                // Nastavení pro text
                RemoveComments = true,
                // Zpracování obrázků
                GithubFlavored = true
            };
            
            _converter = new Converter(config);
        }
        
        /// <summary>
        /// Převede HTML obsah na Markdown
        /// </summary>
        /// <param name="htmlContent">HTML obsah</param>
        /// <returns>Markdown reprezentace HTML obsahu</returns>
        public string ConvertToMarkdown(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return string.Empty;
            }
            
            try
            {
                // Předběžné čištění HTML - odstranění skriptů, stylů a dalších nepotřebných elementů
                htmlContent = CleanupHtml(htmlContent);
                
                // Použití ReverseMarkdown knihovny pro konverzi
                try
                {
                    // Vytvoření HtmlAgilityPack dokumentu pro správné parsování
                    var document = new HtmlDocument();
                    document.LoadHtml(htmlContent);
                    
                    // Vyčištění dokumentu od javascriptu a zbytečných elementů
                    CleanupDocument(document);
                    
                    // Konverze na Markdown pomocí ReverseMarkdown
                    var markdown = _converter.Convert(document.DocumentNode.OuterHtml);
                    
                    // Dodatečné čištění výsledného markdownu
                    markdown = CleanupMarkdown(markdown);
                    
                    return markdown;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při konverzi HTML na Markdown: {ex.Message}");
                    return $"Chyba při konverzi do Markdown: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Chyba při zpracování HTML: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Vyčistí HTML kód od nepotřebných elementů
        /// </summary>
        /// <param name="html">HTML kód</param>
        /// <returns>Vyčištěný HTML kód</returns>
        private string CleanupHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }
            
            // Odstranění JavaScript kódu
            html = Regex.Replace(html, @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>", "", RegexOptions.IgnoreCase);
            
            // Odstranění CSS stylů
            html = Regex.Replace(html, @"<style\b[^<]*(?:(?!</style>)<[^<]*)*</style>", "", RegexOptions.IgnoreCase);
            
            // Odstranění komentářů
            html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);
            
            // Odstranění zbytečných mezer
            html = Regex.Replace(html, @"\s{2,}", " ");
            
            return html;
        }
        
        /// <summary>
        /// Vyčistí HTML dokument od nepotřebných elementů pomocí HtmlAgilityPack
        /// </summary>
        /// <param name="document">HTML dokument</param>
        private void CleanupDocument(HtmlDocument document)
        {
            // Identifikace a odstranění nepotřebných elementů
            var nodesToRemove = new List<HtmlNode>();
            
            // Najdeme všechny script, style, meta, link, svg, iframe elementy
            var elementsToRemove = document.DocumentNode.SelectNodes("//script|//style|//meta|//link|//iframe|//svg");
            
            if (elementsToRemove != null)
            {
                foreach (var node in elementsToRemove)
                {
                    nodesToRemove.Add(node);
                }
            }
            
            // Najdeme elementy s konkrétními atributy a třídami, které pravděpodobně obsahují reklamy, navigaci apod.
            var attributeElementsToRemove = document.DocumentNode.SelectNodes("//*[contains(@class, 'ad') or contains(@class, 'ads') or contains(@class, 'banner') or contains(@class, 'cookie') or contains(@class, 'footer') or contains(@class, 'navigation') or contains(@id, 'ad')]");
            
            if (attributeElementsToRemove != null)
            {
                foreach (var node in attributeElementsToRemove)
                {
                    nodesToRemove.Add(node);
                }
            }
            
            // Odstranění nalezených elementů
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }
        
        /// <summary>
        /// Dodatečné vyčištění výsledného Markdown textu
        /// </summary>
        /// <param name="markdown">Markdown text</param>
        /// <returns>Vyčištěný Markdown text</returns>
        private string CleanupMarkdown(string markdown)
        {
            // Odstranění prázdných řádků na začátku a konci
            markdown = markdown.Trim();
            
            // Nahrazení více než dvou prázdných řádků za sebou dvěma prázdnými řádky
            markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
            
            // Odstranění nadbytečných mezer na začátku řádků
            markdown = Regex.Replace(markdown, @"(?<=\n) +", "");
            
            // Oprava odkazů, které mají nadbytečné znaky
            markdown = Regex.Replace(markdown, @"\[([^\]]+)\]\s*\(([^)]+)\)", "[$1]($2)");
            
            // Oprava nadpisů, které nemají mezeru za # (např. "#Nadpis" -> "# Nadpis")
            for (int i = 1; i <= 6; i++)
            {
                string prefix = new string('#', i);
                markdown = Regex.Replace(markdown, $@"{prefix}([^\s#])", $"{prefix} $1");
            }
            
            return markdown;
        }
    }
} 