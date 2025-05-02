using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NewsImport.PlaywrightTest.Utilities;

/// <summary>
/// Class for cleaning HTML code before processing
/// </summary>
public class HtmlCleaner
{
    /// <summary>
    /// Cleans HTML from CSS, JavaScript and other unnecessary elements
    /// </summary>
    /// <param name="html">Original HTML code</param>
    /// <returns>Cleaned HTML</returns>
    public string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Removing all script tags
            var scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script");
            if (scriptNodes != null)
            {
                foreach (var script in scriptNodes.ToList())
                {
                    script.Remove();
                }
            }

            // Removing all style tags
            var styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var style in styleNodes.ToList())
                {
                    style.Remove();
                }
            }

            // Removing all link tags (mainly for external CSS)
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            if (linkNodes != null)
            {
                foreach (var link in linkNodes.ToList())
                {
                    link.Remove();
                }
            }

            // Removing inline styles from all elements
            var elementsWithStyle = htmlDoc.DocumentNode.SelectNodes("//@style");
            if (elementsWithStyle != null)
            {
                foreach (var element in elementsWithStyle.ToList())
                {
                    element.Attributes["style"].Remove();
                }
            }

            // Removing classes and IDs (often used for CSS)
            var elementsWithClass = htmlDoc.DocumentNode.SelectNodes("//@class");
            if (elementsWithClass != null)
            {
                foreach (var element in elementsWithClass.ToList())
                {
                    element.Attributes["class"].Remove();
                }
            }

            var elementsWithId = htmlDoc.DocumentNode.SelectNodes("//@id");
            if (elementsWithId != null)
            {
                foreach (var element in elementsWithId.ToList())
                {
                    // We keep IDs for some important elements, such as headings, paragraphs, etc.
                    if (!IsImportantElement(element.Name))
                    {
                        element.Attributes["id"].Remove();
                    }
                }
            }

            // Removing meta tags related to styling
            var metaNodes = htmlDoc.DocumentNode.SelectNodes("//meta[contains(@name, 'viewport') or contains(@name, 'theme-color')]");
            if (metaNodes != null)
            {
                foreach (var meta in metaNodes.ToList())
                {
                    meta.Remove();
                }
            }

            // Removing comments
            var commentNodes = htmlDoc.DocumentNode.SelectNodes("//comment()");
            if (commentNodes != null)
            {
                foreach (var comment in commentNodes.ToList())
                {
                    comment.Remove();
                }
            }

            // Removing SVGs (often used for icons that are not important for Markdown)
            var svgNodes = htmlDoc.DocumentNode.SelectNodes("//svg");
            if (svgNodes != null)
            {
                foreach (var svg in svgNodes.ToList())
                {
                    svg.Remove();
                }
            }

            // Removing script attributes (onclick, onload, onerror, etc.)
            var attributesToRemove = new[] { "onclick", "onload", "onerror", "onmouseover", "onmouseout" };
            foreach (var attrName in attributesToRemove)
            {
                var nodesWithAttr = htmlDoc.DocumentNode.SelectNodes($"//@{attrName}");
                if (nodesWithAttr != null)
                {
                    foreach (var node in nodesWithAttr.ToList())
                    {
                        node.Attributes[attrName].Remove();
                    }
                }
            }

            // We can also remove unnecessary parts of the document like header, footer, navigation, ads
            var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//footer | //header | //nav | //aside | //div[contains(@class, 'ad') or contains(@class, 'ads') or contains(@id, 'ad') or contains(@id, 'ads')]");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove.ToList())
                {
                    // Check if this element contains important content
                    if (!ContainsImportantContent(node))
                    {
                        node.Remove();
                    }
                }
            }

            // Return cleaned HTML
            return htmlDoc.DocumentNode.OuterHtml;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning HTML: {ex.Message}");
            return html; // Return original HTML in case of error
        }
    }

    /// <summary>
    /// Checks if the element is important and should keep its ID
    /// </summary>
    private bool IsImportantElement(string elementName)
    {
        var importantElements = new[] { "h1", "h2", "h3", "h4", "h5", "h6", "p", "article", "section", "main" };
        return importantElements.Contains(elementName.ToLower());
    }

    /// <summary>
    /// Checks if the element contains important content that should not be removed
    /// </summary>
    private bool ContainsImportantContent(HtmlNode node)
    {
        // Check if the element contains important elements like headings, paragraphs, images, etc.
        var importantChildren = node.SelectNodes(".//h1 | .//h2 | .//h3 | .//h4 | .//h5 | .//h6 | .//p[string-length(text()) > 50] | .//img[@alt] | .//article | .//main | .//section[string-length(text()) > 100]");
        return importantChildren != null && importantChildren.Count > 0;
    }
}