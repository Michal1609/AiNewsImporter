using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NewsImport.PlaywrightTest.Utilities;

/// <summary>
/// Class for cleaning HTML code before processing
/// </summary>
public class HtmlCleaner
{
    /// <summary>
    /// O�ist� HTML od CSS, JavaScriptu a dal��ch nepot�ebn�ch element�
    /// </summary>
    /// <param name="html">P�vodn� HTML k�d</param>
    /// <returns>O�i�t�n� HTML</returns>
    public string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Odstran�n� v�ech script tag�
            var scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script");
            if (scriptNodes != null)
            {
                foreach (var script in scriptNodes.ToList())
                {
                    script.Remove();
                }
            }

            // Odstran�n� v�ech style tag�
            var styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var style in styleNodes.ToList())
                {
                    style.Remove();
                }
            }

            // Odstran�n� v�ech link tag� (hlavn� pro extern� CSS)
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            if (linkNodes != null)
            {
                foreach (var link in linkNodes.ToList())
                {
                    link.Remove();
                }
            }

            // Odstran�n� inline styl� ze v�ech element�
            var elementsWithStyle = htmlDoc.DocumentNode.SelectNodes("//@style");
            if (elementsWithStyle != null)
            {
                foreach (var element in elementsWithStyle.ToList())
                {
                    element.Attributes["style"].Remove();
                }
            }

            // Odstran�n� t��d a ID (�asto pou��van� pro CSS)
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
                    // Zachov�me ID pro n�kter� d�le�it� elementy, jako jsou nadpisy, odstavce atd.
                    if (!IsImportantElement(element.Name))
                    {
                        element.Attributes["id"].Remove();
                    }
                }
            }

            // Odstran�n� meta tag� related to styling
            var metaNodes = htmlDoc.DocumentNode.SelectNodes("//meta[contains(@name, 'viewport') or contains(@name, 'theme-color')]");
            if (metaNodes != null)
            {
                foreach (var meta in metaNodes.ToList())
                {
                    meta.Remove();
                }
            }

            // Odstran�n� koment���
            var commentNodes = htmlDoc.DocumentNode.SelectNodes("//comment()");
            if (commentNodes != null)
            {
                foreach (var comment in commentNodes.ToList())
                {
                    comment.Remove();
                }
            }

            // Odstran�n� SVG (�asto pou�it� pro ikony, kter� nejsou d�le�it� pro Markdown)
            var svgNodes = htmlDoc.DocumentNode.SelectNodes("//svg");
            if (svgNodes != null)
            {
                foreach (var svg in svgNodes.ToList())
                {
                    svg.Remove();
                }
            }

            // Odstran�n� skript atribut� (onclick, onload, onerror atd.)
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

            // M��eme tak� odstranit nepot�ebn� ��sti dokumentu jako header, footer, navigation, ads
            var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//footer | //header | //nav | //aside | //div[contains(@class, 'ad') or contains(@class, 'ads') or contains(@id, 'ad') or contains(@id, 'ads')]");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove.ToList())
                {
                    // Zkontrolujeme, zda tento element nem� d�le�it� obsah
                    if (!ContainsImportantContent(node))
                    {
                        node.Remove();
                    }
                }
            }

            // Vr�t�me o�i�t�n� HTML
            return htmlDoc.DocumentNode.OuterHtml;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba p�i �i�t�n� HTML: {ex.Message}");
            return html; // Vr�t�me p�vodn� HTML v p��pad� chyby
        }
    }

    /// <summary>
    /// Zkontroluje, zda je element d�le�it� a m�l by si zachovat sv� ID
    /// </summary>
    private bool IsImportantElement(string elementName)
    {
        var importantElements = new[] { "h1", "h2", "h3", "h4", "h5", "h6", "p", "article", "section", "main" };
        return importantElements.Contains(elementName.ToLower());
    }

    /// <summary>
    /// Zkontroluje, zda element obsahuje d�le�it� obsah, kter� by nem�l b�t odstran�n
    /// </summary>
    private bool ContainsImportantContent(HtmlNode node)
    {
        // Zkontrolujeme, zda element obsahuje d�le�it� elementy jako nadpisy, odstavce, obr�zky atd.
        var importantChildren = node.SelectNodes(".//h1 | .//h2 | .//h3 | .//h4 | .//h5 | .//h6 | .//p[string-length(text()) > 50] | .//img[@alt] | .//article | .//main | .//section[string-length(text()) > 100]");
        return importantChildren != null && importantChildren.Count > 0;
    }
}