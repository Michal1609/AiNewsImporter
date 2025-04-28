using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NewsImport.PlaywrightTest.Utilities;

/// <summary>
/// Class for cleaning HTML code before processing
/// </summary>
public class HtmlCleaner
{
    /// <summary>
    /// Oèistí HTML od CSS, JavaScriptu a dalších nepotøebných elementù
    /// </summary>
    /// <param name="html">Pùvodní HTML kód</param>
    /// <returns>Oèištìné HTML</returns>
    public string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Odstranìní všech script tagù
            var scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script");
            if (scriptNodes != null)
            {
                foreach (var script in scriptNodes.ToList())
                {
                    script.Remove();
                }
            }

            // Odstranìní všech style tagù
            var styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var style in styleNodes.ToList())
                {
                    style.Remove();
                }
            }

            // Odstranìní všech link tagù (hlavnì pro externí CSS)
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            if (linkNodes != null)
            {
                foreach (var link in linkNodes.ToList())
                {
                    link.Remove();
                }
            }

            // Odstranìní inline stylù ze všech elementù
            var elementsWithStyle = htmlDoc.DocumentNode.SelectNodes("//@style");
            if (elementsWithStyle != null)
            {
                foreach (var element in elementsWithStyle.ToList())
                {
                    element.Attributes["style"].Remove();
                }
            }

            // Odstranìní tøíd a ID (èasto používané pro CSS)
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
                    // Zachováme ID pro nìkteré dùležité elementy, jako jsou nadpisy, odstavce atd.
                    if (!IsImportantElement(element.Name))
                    {
                        element.Attributes["id"].Remove();
                    }
                }
            }

            // Odstranìní meta tagù related to styling
            var metaNodes = htmlDoc.DocumentNode.SelectNodes("//meta[contains(@name, 'viewport') or contains(@name, 'theme-color')]");
            if (metaNodes != null)
            {
                foreach (var meta in metaNodes.ToList())
                {
                    meta.Remove();
                }
            }

            // Odstranìní komentáøù
            var commentNodes = htmlDoc.DocumentNode.SelectNodes("//comment()");
            if (commentNodes != null)
            {
                foreach (var comment in commentNodes.ToList())
                {
                    comment.Remove();
                }
            }

            // Odstranìní SVG (èasto použité pro ikony, které nejsou dùležité pro Markdown)
            var svgNodes = htmlDoc.DocumentNode.SelectNodes("//svg");
            if (svgNodes != null)
            {
                foreach (var svg in svgNodes.ToList())
                {
                    svg.Remove();
                }
            }

            // Odstranìní skript atributù (onclick, onload, onerror atd.)
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

            // Mùžeme také odstranit nepotøebné èásti dokumentu jako header, footer, navigation, ads
            var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//footer | //header | //nav | //aside | //div[contains(@class, 'ad') or contains(@class, 'ads') or contains(@id, 'ad') or contains(@id, 'ads')]");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove.ToList())
                {
                    // Zkontrolujeme, zda tento element nemá dùležitý obsah
                    if (!ContainsImportantContent(node))
                    {
                        node.Remove();
                    }
                }
            }

            // Vrátíme oèištìné HTML
            return htmlDoc.DocumentNode.OuterHtml;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba pøi èištìní HTML: {ex.Message}");
            return html; // Vrátíme pùvodní HTML v pøípadì chyby
        }
    }

    /// <summary>
    /// Zkontroluje, zda je element dùležitý a mìl by si zachovat své ID
    /// </summary>
    private bool IsImportantElement(string elementName)
    {
        var importantElements = new[] { "h1", "h2", "h3", "h4", "h5", "h6", "p", "article", "section", "main" };
        return importantElements.Contains(elementName.ToLower());
    }

    /// <summary>
    /// Zkontroluje, zda element obsahuje dùležitý obsah, který by nemìl být odstranìn
    /// </summary>
    private bool ContainsImportantContent(HtmlNode node)
    {
        // Zkontrolujeme, zda element obsahuje dùležité elementy jako nadpisy, odstavce, obrázky atd.
        var importantChildren = node.SelectNodes(".//h1 | .//h2 | .//h3 | .//h4 | .//h5 | .//h6 | .//p[string-length(text()) > 50] | .//img[@alt] | .//article | .//main | .//section[string-length(text()) > 100]");
        return importantChildren != null && importantChildren.Count > 0;
    }
}