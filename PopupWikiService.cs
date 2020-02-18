﻿using System;
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
      // string res = await SendHttpGetRequest(RestApiBaseUrl + $"page/mobile-sections-lead/{ParseTitle(title)}");
      string res = await SendHttpGetRequest(RestApiBaseUrl + $"page/mobile-html/{ParseTitle(title)}");
      //<base href="https://en.wikipedia.org/api/rest_v1/page/mobile-html" />
      // TODO Styling not working

      HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(res);
      doc.DocumentNode.Descendants()
                      .Where(n => n.Name == "script")
                      .ToList()
                      .ForEach(n => n.Remove());

      foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//figure"))
      {
        bool hasImg = false;
        foreach (HtmlNode child in node.ChildNodes)
        {
          if (node.Name == "img")
          {
            hasImg = true;
            break;
          }
        }

        if (!hasImg)
        {
          var imgPlaceholder = node.SelectSingleNode("//span[contains(@class, 'pcs-lazy-load-placeholder')]");
          if (imgPlaceholder != null)
          {
            // Replace the placeholder spans with the img data
            string height = imgPlaceholder.GetAttributeValue("data-height", "");
            string width = imgPlaceholder.GetAttributeValue("data-width", "");
            string dataSrc = imgPlaceholder.GetAttributeValue("data-src", "");
            HtmlNode imgNode = HtmlNode.CreateNode($"<img src=\"{dataSrc}\" height=\"{height}\" width=\"{width}\" />");
            imgPlaceholder.ParentNode.ChildNodes.Add(imgNode);
            imgPlaceholder.Remove();
          }
        }
      }

      string style1 = "<link rel=\"stylesheet\" href=\"https://meta.wikimedia.org/api/rest_v1/data/css/mobile/pcs\" />";
      string style2 = "<link rel=\"stylesheet\" href=\"https://meta.wikimedia.org/api/rest_v1/data/css/mobile/base\" />";
      string style3 = "<link rel=\"stylesheet\" href=\"https://en.wikipedia.org/api/rest_v1/data/css/mobile/site\" />";
      string style4 = "<link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css\">";
      string meta = "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=8\">";

      HtmlNode _base = doc.DocumentNode.SelectSingleNode("//base");
      _base.SetAttributeValue("href", "https://en.wikipedia.org/wiki/");

      HtmlNode _style1 = HtmlNode.CreateNode(style1);
      HtmlNode _style2 = HtmlNode.CreateNode(style2);
      HtmlNode _style3 = HtmlNode.CreateNode(style3);
      HtmlNode _style4 = HtmlNode.CreateNode(style4);

      HtmlNode _meta = HtmlNode.CreateNode(meta);

      HtmlNode head = doc.DocumentNode.SelectSingleNode("//head");
      //head.ChildNodes.Add(_style1);
      //head.ChildNodes.Add(_style2);
      //head.ChildNodes.Add(_style3);
      //head.ChildNodes.Add(_style4);
      head.ChildNodes.Add(_meta);

      //MediumWiki data = JsonConvert.DeserializeObject<MediumWiki>(res);
      //var stubble = new StubbleBuilder().Build();

      // TODO: Clean this up

      // TODO: Must remember to add a proper base href property.
      //Dictionary<string, object> obj = new Dictionary<string, object>();
      //obj.Add("title", data.displaytitle);
      //obj.Add("description", data.description);
      //obj.Add("text", data.sections[0].text);

      // If e
      // TODO if query.pageids[0] == -1, there are no matches
      // TODO At build time the html template is placed in the app root.
      // TODOOOOOOOOO That didn't work remember to place in app root!!
      //using (StreamReader streamReader = new StreamReader(@"MobileWikiTemplate.Mustache", Encoding.UTF8))
      //{
      //var htmlstring = stubble.Render(streamReader.ReadToEnd(), output);
      //Console.WriteLine(output);
      //return htmlstring;
      //}
      Console.WriteLine(doc.DocumentNode.OuterHtml);
      return doc.DocumentNode.OuterHtml;
    }

    // Formats title to be sent to API
    private string ParseTitle(string title)
    {
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
          responseMsg.EnsureSuccessStatusCode();
          // Will never return because EnsureSuccessStatusCode throws exception.
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
