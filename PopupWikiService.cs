using System;
using System.Collections;
using System.Collections.Generic;
using Anotar.Serilog;
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
    private string MainLanguage => Config.WikiLanguages.Split(',')[0];
    private readonly HttpClient _httpClient;
    public string RestApiBaseUrl => $"http://{MainLanguage}.wikipedia.org/api/rest_v1/";
    public string MediaWikiApiBaseUrl => $"http://{MainLanguage}.wikipedia.org/w/api.php";

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

    /// <summary>
    /// Get wikipedia or wiktionary search results for each of the user's languages for a term.
    /// </summary>
    /// <param name="term"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<string> GetSearchResults(string term, SearchType type)
    {
      if (string.IsNullOrEmpty(term))
      {
        Console.WriteLine("Attempted to call GetSearchResults with a null or empty term.");
        return null;
      }

      string[] languages = Config.WikiLanguages.Split(',');
      string html = null;

      foreach (string language in languages)
      {
        string searchres = await WikiSearch(term, language, type);

        if (!string.IsNullOrEmpty(searchres))
        {
          // TODO: Can this be made safer?
          dynamic search = JsonConvert.DeserializeObject(searchres);
          string searchTerm = search[0];
          JArray searchTitles = search[1];
          JArray searchUrls = search[3];

          // Returns a block of results for a language
          if (searchTitles.Count > 0 && searchUrls.Count > 0)
          {
            html += $"<h3>{language} search results</h3>";
            html += "<ul>";
            var LinkArray = searchTitles.Zip(searchUrls, (title, link) => $"<a href=\"{link}\">{title}</a>");
            foreach (var item in LinkArray)
            {
              html += $"<li>{item}</li>";
            }
            html += "</ul>";
          }
        }
      }

      if (!string.IsNullOrEmpty(html))
      {
        // Wiktionary search returns desktop links, so convert to mobile.
        if (type == SearchType.wiktionary)
        {
          html = HtmlFilters.WiktionaryDesktopToMobileLinks(html);
          html = $"<h1>Search Results for \"{term}\"</h1>{html}";
        }
      }

      return html;
    }

    /// <summary>
    /// Get html string for a mobile-html wiktionary page.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<string> GetWiktionaryMobileHtml(string url)
    {
      string html = null;
      if (WikiUrlUtils.IsMobileWiktionaryUrl(url))
      {
        Uri uri = new Uri(url);
        string[] splitUri = uri.DnsSafeHost.Split('.');
        if (splitUri.Length > 0)
        {
          string language = uri.DnsSafeHost.Split('.')[0];
          string res = await SendHttpGetRequest(url);
          if (!string.IsNullOrEmpty(res))
          {
            // Apply Html Filters
            html = HtmlFilters.WiktionaryDesktopToMobileLinks(res);
            html = HtmlFilters.ConvRelToAbsLinks(html, $"https://{language}.m.wiktionary.org", WikiUrlUtils.IsDesktopWiktionaryUrl);
            html = HtmlFilters.RemoveScripts(html);
            html = HtmlFilters.RemoveImageParentLink(html);
            html = HtmlFilters.UpdateBaseHref(html, $"https://{language}.m.wiktionary.org/wiki");
            html = HtmlFilters.RemoveHeader(html);
            string stylesheet = $"https://{language}.m.wiktionary.org/w/load.php?lang={language}&modules=ext.wikimediaBadges%7Cmediawiki.hlist%7Cmediawiki.ui.button%2Cicon%7Cmobile.init.styles%7Cskins.minerva.base.styles%7Cskins.minerva.content.styles%7Cskins.minerva.content.styles.images%7Cskins.minerva.icons.images%2Cwikimedia%7Cskins.minerva.mainMenu.icons%2Cstyles&only=styles&skin=minerva";
            html = HtmlFilters.UpdateWiktionaryStylesheet(html, stylesheet);
            html = HtmlFilters.UpdateMetaIE(html);
            html = HtmlFilters.RemoveEditSectionBtns(html);
          }
        }
      }
      return html;
    }

    /// <summary>
    /// Gets the mobile-html for a wiki page.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    public async Task<string> GetWikipediaMobileHtml(string title, string language)
    {
      string html = null;
      if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(language))
      {
        string articleUrlTitle = ParseTitle(title);
        string url = $"https://{language}.wikipedia.org/api/rest_v1/page/mobile-html/{articleUrlTitle}";
        string res = await SendHttpGetRequest(url);
        if (!string.IsNullOrEmpty(res))
        {
          // Apply Html Filters.
          // TODO: Add relative wiki to absolute.
          html = HtmlFilters.RemoveScripts(res);
          html = HtmlFilters.ConvRelToAbsLinks(html, $"https://{language}.wikipedia.org", WikiUrlUtils.IsDesktopWikipediaUrl);
          html = HtmlFilters.WiktionaryDesktopToMobileLinks(html);
          html = HtmlFilters.UpdateBaseHref(html, $"https://{language}.wikipedia.org/wiki");
          html = HtmlFilters.ConvertImagePlaceholders(html);
          html = HtmlFilters.RemoveImageParentLink(html);
          html = HtmlFilters.OpenCollapsedDivs(html);
          html = HtmlFilters.UpdateMetaIE(html);
          html = HtmlFilters.ShowHiddenSections(html);
        }
      }
      return html;
    }

    /// <summary>
    /// Search the wikipedia or wiktionary opensearch api.
    /// </summary>
    /// <param name="term"></param>
    /// <param name="language"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private async Task<string> WikiSearch(string term, string language, SearchType type)
    {
      if (!string.IsNullOrEmpty(term))
      {
        if (!string.IsNullOrEmpty(language))
        {
          string WikiSearchUrl = $"https://{language}.{type.ToString()}.org/w/api.php?action=opensearch";

          string url = $"{WikiSearchUrl}" +
                       $"&search={term}" +
                       $"&limit={Config.NumSearchResults}" +
                       $"&namespace=0" +
                       $"&format=json";

          Console.WriteLine($"Sent a search request to url \"{url}\" from WikiSearch.");
          
          string res = await SendHttpGetRequest(url);
          return res;
        }
        else
        {
          Console.WriteLine("Attempted to call WikiSearch with a null or empty language.");
          return null;
        }
      }

      Console.WriteLine("Attempted to call WikiSearch with a null or empty term.");
      return null;
    }

    /// <summary>
    /// Formats the title into URL / API format.
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    private string ParseTitle(string title)
    {
      return title.Trim().Replace(" ", "_");
    }

    // TODO
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
