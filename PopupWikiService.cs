using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperMemoAssistant.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperMemoAssistant.Sys;
using SuperMemoAssistant.Sys.Remoting;
using Stubble.Core.Builders;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace SuperMemoAssistant.Plugins.PopupWiki
{
  public class PopupWikiService : IDisposable
  {
    private PopupWikiCfg Config => Svc<PopupWiki>.Plugin.Config;
    private readonly HttpClient _httpClient;
    public string RestApiBaseUrl => $"http://{Config.WikiLanguages.Split(',')[0]}.wikipedia.org/api/rest_v1/";
    public string MediaWikiApiBaseUrl => $"http://{Config.WikiLanguages.Split(',')[0]}.wikipedia.org/w/api.php";

    public PopupWikiService()
    {
      _httpClient = new HttpClient();
      _httpClient.DefaultRequestHeaders.Accept.Clear();
      _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void Dispose()
    {
      _httpClient?.Dispose();
    }

    public async Task<string> GetMinimalistHtml(string title)
    {
      string url = $"{MediaWikiApiBaseUrl}" +
                   $"?action=query" +
                   $"&format=json" +
                   $"&errorformat=bc" +
                   $"&prop=description|extracts|pageimages" +
                   $"&utf8=1" +
                   $"&formatversion=latest" +
                   $"&indexpageids=1" +
                   $"&piprop=thumbnail|name|original" +
                   $"&pithumbsize=300" +
                   $"&titles={title}" +
                   $"&exchars=1200" +
                   $"&exintro={true}" +
                   $"&exlimit=1";
      // TODO if query.pageids[0] == -1, there are no matches
      string res = await SendHttpGetRequest(url);
      MinimalistWiki data = JsonConvert.DeserializeObject<MinimalistWiki>(res);
      
      var stubble = new StubbleBuilder().Build();
      // If e
      // TODO if query.pageids[0] == -1, there are no matches
        // TODO At build time the html template is placed in the app root.
        // TODOOOOOOOOO That didn't work remember to place in app root!!
      using (StreamReader streamReader = new StreamReader(@"MinimalistWikiTemplate.Mustache", Encoding.UTF8))
      {
        var obj = data.query.pages[0];
        var output = stubble.Render(streamReader.ReadToEnd(), obj);
        Console.WriteLine(output);
        return output;
      }
    }

    private HtmlDocument filterHtmlScripts(HtmlDocument doc)
    {
      // Remove scripts to prevent script errors
      doc.DocumentNode.Descendants()
                      .Where(n => n.Name == "script" || n.Name == "noscript")
                      .ToList()
                      .ForEach(n => n.Remove());

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

      return doc;
    }

    private HtmlDocument FilterMobileHtml(HtmlDocument doc)
    {

      doc = filterHtmlScripts(doc);

      // Open collapsed divs by changing style to visible
      // This will open the "quick facts" sections
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

      // Convert image placeholders into actual images
      // Image placeholders exist as children of "figure" / "figure-inline" tags
      // But some figure tags already have images, so skip those.
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
            //var imgPlaceholder = figureNode.SelectSingleNode("//span[contains(@class, 'pcs-lazy-load-placeholder')]");
            // pagelib_lazy_load_placeholder
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

      // Remove the <a> parent element of images
      HtmlNodeCollection imageNodes = doc.DocumentNode.SelectNodes("//img");
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
      
      // Change style to visible on section tags
      HtmlNodeCollection sectionNodes = doc.DocumentNode.SelectNodes("//section[@style]");
      if (sectionNodes != null)
      {
        foreach (HtmlNode sectionNode in sectionNodes)
        {
          sectionNode.Attributes["style"].Value = "display: block;";
        }
      }

      // WebBrowser uses an older version of IE
      string meta = "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\">";
      HtmlNode _meta = HtmlNode.CreateNode(meta);
      HtmlNode head = doc.DocumentNode.SelectSingleNode("//head");
      head.ChildNodes.Add(_meta);

      return doc;

    }

    public async Task<string> GetSearchResults(string term, string domain = "wikipedia")
    {
      string[] languages = Config.WikiLanguages.Split(',');
      string HtmlSearchResults = string.Empty;

      foreach (string language in languages)
      {
        string searchres = await WikiSearch(term, language, domain);

        if (searchres != null)
        {
          // TODO: Can this be made safer?
          dynamic search = JsonConvert.DeserializeObject(searchres);
          string searchTerm = search[0];
          JArray searchTitles = search[1];
          JArray searchUrls = search[3];

          // Returns a page of results
          if (searchTitles.Count > 0 && searchUrls.Count > 0)
          {
            HtmlSearchResults += $"<h3>{language} search results</h3>";
            HtmlSearchResults += "<ul>";
            var LinkArray = searchTitles.Zip(searchUrls, (title, link) => $"<a href=\"{link}\">{title}</a>");
            foreach (var item in LinkArray)
            {
              HtmlSearchResults += $"<li>{item}</li>";
            }
            HtmlSearchResults += "</ul>";
          }
        }
      }

      if (!string.IsNullOrEmpty(HtmlSearchResults))
      {
        return $"<h1>Search Results for \"{term}\"</h1>{HtmlSearchResults}";
      }

      return null;
    }

    public async Task<string> GetDictMobHtml(string url)
    {
      // TODO: Add url check
      Uri uri = new Uri(url);
      string language = uri.DnsSafeHost.Split('.')[0];
      string res = await SendHttpGetRequest(url);

      if (!string.IsNullOrEmpty(res))
      {
        var doc = new HtmlDocument();
        doc.LoadHtml(res);

        doc = FilterMobileHtml(doc);

        HtmlNode _base = doc.DocumentNode.SelectSingleNode("//base");
        // Testing
        string baseHref = $"https://{language}.m.wiktionary.org/wiki";
        if (_base != null)
        {
          _base.SetAttributeValue("href", baseHref);
        }
        else
        {
          // add base node
          var headNode = doc.DocumentNode.SelectSingleNode("//head");
          HtmlNode baseNode = HtmlNode.CreateNode($"<base href=\"{baseHref}\"/>");
          headNode.ChildNodes.Add(baseNode);
        }

        // Fix the stylesheet
        var stylesheetNodes = doc.DocumentNode.SelectNodes("//link[@rel]");
        if (stylesheetNodes != null)
        {
          foreach (var stylesheetNode in stylesheetNodes)
          {
            if (stylesheetNode.Attributes["rel"].Value == "stylesheet")
            {
              stylesheetNode.Attributes["href"].Value = $"https://{language}.m.wiktionary.org/w/load.php?lang={language}&modules=ext.wikimediaBadges%7Cmediawiki.hlist%7Cmediawiki.ui.button%2Cicon%7Cmobile.init.styles%7Cskins.minerva.base.styles%7Cskins.minerva.content.styles%7Cskins.minerva.content.styles.images%7Cskins.minerva.icons.images%2Cwikimedia%7Cskins.minerva.mainMenu.icons%2Cstyles&only=styles&skin=minerva";
            }
          }
        }

        // Remove the header Nav.
        var headerNode = doc.DocumentNode.SelectSingleNode("//header");
        if (headerNode != null)
        {
          headerNode.ParentNode.RemoveChild(headerNode);
        }

        if (doc != null)
        {
          return doc.DocumentNode.OuterHtml;
        }
      }
      return null;
    }

    

    public async Task<string> GetWikiMobHtml(string title, string language)
    {
      
      string articleUrlTitle = ParseTitle(title);
      string url = $"http://{language}.wikipedia.org/api/rest_v1/page/mobile-html/{articleUrlTitle}";
      string res = await SendHttpGetRequest(url);

      // Found an article with same title as search.
      if (res != null)
      {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(res);

        // Filter the HtmlDocument
        doc = FilterMobileHtml(doc);

        // Set the base url to desktop wiki
        HtmlNode _base = doc.DocumentNode.SelectSingleNode("//base");
        if (_base != null)
        {
          _base.SetAttributeValue("href", $"https://{language}.wikipedia.org/wiki");
        }

        if (doc != null)
        {
          // Return html string
          return doc.DocumentNode.OuterHtml;
        }
      }

      // No article with same title as selected text, so redirect to search.
      string searchRes = await GetSearchResults(title);
      if (!string.IsNullOrEmpty(searchRes))
      {
        return searchRes;
      }

      // No direct article found and no search results - return a "nothing found" page
      return $"<h1>No results found for \"{title}\".</h1>";
    }

    public async Task<string> GetWikiPage(string url)
    {
      string html = await SendHttpGetRequest(url);
      return html;
    }

    // Supported domains are wikipedia and wiktionary
    private async Task<string> WikiSearch(string term, string language, string domain = "wikipedia")
    {
      string WikiSearchUrl = $"https://{language}.{domain}.org/w/api.php?action=opensearch";

      string url = $"{WikiSearchUrl}" +
                   $"&search={term}" +
                   $"&limit={Config.NumSearchResults}" +
                   $"&namespace=0" +
                   $"&format=json";

      string res = await SendHttpGetRequest(url);
      return res;
    }

    private string ParseTitle(string title)
    {
      // Formats title to be sent to Wiki API
      return title.Trim().Replace(" ", "_");
    }

    private async Task<string> SendHttpGetRequest(string path)
    {
      HttpResponseMessage responseMsg = null;

      try
      {
        responseMsg = await _httpClient.GetAsync(path);

        if (responseMsg.IsSuccessStatusCode)
        {
          return await responseMsg.Content.ReadAsStringAsync();
        }
        else
        {
          return null;
        }
      }
      catch (HttpRequestException)
      {
        if (responseMsg != null && responseMsg.StatusCode == System.Net.HttpStatusCode.NotFound)
          return null;
        else
          throw;
      }
      catch (OperationCanceledException)
      {
        return null;
      }
      finally
      {
        responseMsg?.Dispose();
      }
    }
  }
}
