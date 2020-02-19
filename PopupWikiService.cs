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
    public string RestApiBaseUrl => $"http://{Config.WikiLanguage}.wikipedia.org/api/rest_v1/";
    public string MediaWikiApiBaseUrl => $"http://{Config.WikiLanguage}.wikipedia.org/w/api.php";
    public string WikiBaseUrl => $"http://{Config.WikiLanguage}.wikipedia.org/";

    // TODO: Test other languages
    public string WikiSearchUrl => $"https://{Config.WikiLanguage}.wikipedia.org/w/api.php?action=opensearch";

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

    public async Task<string> GetMediumHtml(string title)
    {
      string res = await SendHttpGetRequest(RestApiBaseUrl + $"page/mobile-html/{ParseTitle(title)}");
      if (res != null)
      {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(res);

        // Remove scripts (script errors)
        doc.DocumentNode.Descendants()
                        .Where(n => n.Name == "script")
                        .ToList()
                        .ForEach(n => n.Remove());

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
              var imgPlaceholder = figureNode.SelectSingleNode("//span[contains(@class, 'pcs-lazy-load-placeholder')]");
              if (imgPlaceholder != null)
              {
                // Replace the placeholder with an img element
                string height = imgPlaceholder.GetAttributeValue("data-height", "");
                string width = imgPlaceholder.GetAttributeValue("data-width", "");
                string dataSrc = imgPlaceholder.GetAttributeValue("data-src", "");
                HtmlNode imgNode = HtmlNode.CreateNode($"<img src=\"{dataSrc}\" height=\"{height}\" width=\"{width}\" />");
                imgPlaceholder.ParentNode.ChildNodes.Add(imgNode);
                imgPlaceholder.Remove();
              }
            }
          }
        }
        // Set the base url to desktop wiki
        HtmlNode _base = doc.DocumentNode.SelectSingleNode("//base");
        _base.SetAttributeValue("href", $"{WikiBaseUrl}/wiki");

        // WebBrowser uses an older version of IE
        string meta = "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=8\">";
        HtmlNode _meta = HtmlNode.CreateNode(meta);
        HtmlNode head = doc.DocumentNode.SelectSingleNode("//head");
        head.ChildNodes.Add(_meta);
        return doc.DocumentNode.OuterHtml;
      }

      // TODO: Clean this up
      string searchres = await WikiSearch(title);

      if (searchres != null)
      {
        // TODO: Fix this
        dynamic search = JsonConvert.DeserializeObject(searchres);
        string searchTerm = search[0];
        JArray searchTitles = search[1];
        JArray searchUrls = search[3];

        string HtmlSearchResults = string.Empty;
        
        // Returns a page of results
        if (searchTitles.Count > 0 && searchUrls.Count > 0)
        {
          HtmlSearchResults += $"<h1>Search Results for \"{searchTerm}\"</h1>";
          HtmlSearchResults += "<ul>";
          var LinkArray = searchTitles.Zip(searchUrls, (tit, link) => $"<a href=\"{link}\">{tit}</a>");
          foreach (var item in LinkArray)
          {
            HtmlSearchResults += $"<li>{item}</li>";
          }
          HtmlSearchResults += "</ul>";
          return HtmlSearchResults;
        }
      }
      
      // If no page / search results
      return $"<h1>No results found for \"{title}\".</h1>";
    }

    private async Task<string> WikiSearch(string term)
    {
      string url = $"{WikiSearchUrl}" +
                   $"&search={term}" +
                   // TODO allow customisation in the config
                   $"&limit=30" +
                   // Only Wiki articles, no talk pages etc.
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
