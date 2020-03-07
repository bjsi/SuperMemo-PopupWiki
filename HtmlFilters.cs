using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.PopupWiki
{
  public static class HtmlFilters
  {

    /// <summary>
    /// Converts Wiktionary search result links to mobile wiktionary links.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string WiktionaryDesktopToMobileLinks(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        // Get all links
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (linkNodes != null)
        {
          foreach (var linkNode in linkNodes)
          {
            string href = linkNode.Attributes["href"].Value;
            if (!string.IsNullOrEmpty(href))
            {
              if (WikiUrlUtils.IsDesktopWiktionaryUrl(href))
              {
                linkNode.Attributes["href"].Value = WikiUrlUtils.ConvDesktopWiktionaryToMob(href);
              }
            }
          }
          html = doc.DocumentNode.OuterHtml;
        }
      }
      return html;
    }

    public static string WiktionaryMobileToDesktopLinks(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        // Get all links
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var linkNodes = doc.DocumentNode.SelectNodes("//a");
        if (linkNodes != null)
        {
          foreach (var linkNode in linkNodes)
          {
            string href = linkNode.Attributes["href"].Value;
            if (!string.IsNullOrEmpty(href))
            {
              if (WikiUrlUtils.IsMobileWiktionaryUrl(href))
              {
                linkNode.Attributes["href"].Value = WikiUrlUtils.ConvMobWiktionaryToDesktop(href);
                Console.WriteLine($"Converted {href} to {linkNode.Attributes["href"].Value}");
              }
            }
          }
          html = doc.DocumentNode.OuterHtml;
        }
      }
      return html;
    }

    public static string UpdateMetaIE(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // WebBrowser uses an older version of IE
        string meta = "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\">";
        HtmlNode _meta = HtmlNode.CreateNode(meta);
        HtmlNode head = doc.DocumentNode.SelectSingleNode("//head");
        head.ChildNodes.Add(_meta);

        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }

    public static string UpdateBaseHref(string html, string href)
    {
      if (!string.IsNullOrEmpty(html) && !string.IsNullOrEmpty(href))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNode _base = doc.DocumentNode.SelectSingleNode("//base");
        // Testing

        if (_base != null)
        {
          _base.SetAttributeValue("href", href);
        }
        else
        {
          // add base node
          var headNode = doc.DocumentNode.SelectSingleNode("//head");
          HtmlNode baseNode = HtmlNode.CreateNode($"<base href=\"{href}\"/>");
          headNode.ChildNodes.Add(baseNode);
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }


    /// <summary>
    /// Remove the edit section buttons on wiktionary pages.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string RemoveEditSectionBtns(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var editSectionBtns = doc.DocumentNode.SelectNodes("//span[@class='mw-editsection']");
        if (editSectionBtns != null)
        {
          foreach (var editSectionBtn in editSectionBtns)
          {
            editSectionBtn.ParentNode.RemoveChild(editSectionBtn);
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }


    public static string ConvRelToAbsLinks(string html, string baseUrl, Func<string, bool> predicate)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (linkNodes != null)
        {
          foreach(var linkNode in linkNodes)
          {
            string href = linkNode.Attributes["href"].Value;
            // Can't check if relative, fails if url contains #
            if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
            {
              string absHref = WikiUrlUtils.ConvRelToAbsLink(baseUrl, href);
              if (predicate(absHref))
              {
                Console.WriteLine($"Conversion of relative link {href} to absolute link {absHref} succeeded");
                linkNode.Attributes["href"].Value = absHref;
              }
              else
              {
                Console.WriteLine($"Conversion of relative link {href} to absolute link failed");
              }
            }
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }

    public static string RemoveHeader(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove the header Nav.
        var headerNode = doc.DocumentNode.SelectSingleNode("//header");
        if (headerNode != null)
        {
          headerNode.ParentNode.RemoveChild(headerNode);
        }

        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }

    public static string RelToAbsLinks(string html)
    {
      return html;
    }

    /// <summary>
    /// Removes images from an html string.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string RemoveImages(string html)
    {

      if (!string.IsNullOrEmpty(html))
      {

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove img elements
        doc.DocumentNode.Descendants()
                        .Where(n => n.Name == "img")
                        .ToList()
                        .ForEach(n => n.Remove());

        html = doc.DocumentNode.OuterHtml;
      }

      return html;
    }

    public static string UpdateWiktionaryStylesheet(string html, string href)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var stylesheetNode = doc.DocumentNode.SelectSingleNode("//link[@rel='stylesheet']");
        if (stylesheetNode != null)
        {
          stylesheetNode.Attributes["href"].Value = href;
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }

    /// <summary>
    /// Remove the <a> element parent of wikipedia images.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string RemoveImageParentLink(string html)
    {

      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove the <a> parent element of images
        var imageNodes = doc.DocumentNode.SelectNodes("//img");
        if (imageNodes != null)
        {
          foreach (HtmlNode imageNode in imageNodes)
          {
            if (imageNode.ParentNode.Name == "a")
            {
              var grandParentNode = imageNode.ParentNode.ParentNode;
              grandParentNode.RemoveChild(imageNode.ParentNode, true);
            }
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }

    /// <summary>
    /// Converts Wikipedia Image placeholders to actual images.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string ConvertImagePlaceholders(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection figureNodes = doc.DocumentNode.SelectNodes("//figure | //figure-inline");
        if (figureNodes != null)
        {
          foreach (HtmlNode figureNode in figureNodes)
          {
            bool hasImg = false;
            foreach (HtmlNode child in figureNode.ChildNodes)
            {
              if (figureNode.Name == "img")
              {
                hasImg = true;
                break;
              }
            }
            if (!hasImg)
            {
              // Image Placeholders all contain the word 'lazy'
              var imgPlaceholder = figureNode.SelectSingleNode("//span[contains(@class, 'lazy')]");
              if (imgPlaceholder != null)
              {
                // Replace the placeholder with an img element
                string dataHeight = imgPlaceholder.GetAttributeValue("data-height", "");
                string dataWidth = imgPlaceholder.GetAttributeValue("data-width", "");
                string dataSrc = imgPlaceholder.GetAttributeValue("data-src", "");

                if (!string.IsNullOrEmpty(dataHeight) && !string.IsNullOrEmpty(dataWidth) && !string.IsNullOrEmpty(dataSrc))
                {
                  HtmlNode imgNode = HtmlNode.CreateNode($"<img src=\"{dataSrc}\" height=\"{dataHeight}\" width=\"{dataWidth}\" />");
                  imgPlaceholder.ParentNode.ChildNodes.Add(imgNode);
                  imgPlaceholder.Remove();
                }
              }
            }
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }

      return html;
    }

    public static string RemoveScripts(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove scripts to prevent script errors
        doc.DocumentNode.Descendants()
                        .Where(n => n.Name == "script")
                        .ToList()
                        .ForEach(n => n.Remove());

        // Remove noscript tags
        var noscriptNodes = doc.DocumentNode.SelectNodes("//noscript");
        if (noscriptNodes != null)
        {
          foreach (var noscriptNode in noscriptNodes)
          {
            var parentNode = noscriptNode.ParentNode;
            parentNode.RemoveChild(noscriptNode, true);
          }
        }

        // Filter comments (can contain extra scripts)
        var commentNodes = doc.DocumentNode.SelectNodes("//comment()");
        if (commentNodes != null)
        {
          foreach (var commentNode in commentNodes)
          {
            commentNode.ParentNode.RemoveChild(commentNode);
          }
        }

        // Filter Html inline onclick events
        var onclickNodes = doc.DocumentNode.SelectNodes("//*[@onclick]");
        if (onclickNodes != null)
        {
          foreach (var onclickNode in onclickNodes)
          {
            onclickNode.Attributes.Remove("onclick");
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }


    /// <summary>
    /// Opens the collapsed divs on mobile wikipedia pages.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static string OpenCollapsedDivs(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection collapseNodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'pcs-collapse')]");
        if (collapseNodes != null)
        {
          foreach (HtmlNode collapseNode in collapseNodes)
          {
            string style = collapseNode.GetAttributeValue("style", null);
            if (style != null && style.Contains("display: none;"))
            {
              style = style.Replace("display: none;", "display: block;");
              collapseNode.SetAttributeValue("style", style);
            }
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }

    public static string ShowHiddenSections(string html)
    {
      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection sectionNodes = doc.DocumentNode.SelectNodes("//section[@style]");
        if (sectionNodes != null)
        {
          foreach (HtmlNode sectionNode in sectionNodes)
          {
            sectionNode.Attributes["style"].Value = "display: block;";
          }
        }
        html = doc.DocumentNode.OuterHtml;
      }
      return html;
    }
  }
}
