using System;
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

    public async Task<Summary> GetSummary(string title)
    {
      string url = $"{RestApiBaseUrl}page/summary/{ParseTitle(title)}";
      string res = await SendHttpGetRequest(url);
      return (Summary)JsonConvert.DeserializeObject(res);
    }

    public async Task<string> GetExtract(string title)
    {
      Summary summary = await GetSummary(title);
      //Summary parsedSummary = SummaryParser(summary);

      /// TODO parse summary

      string action = "query";
      string format = "json";
      int formatversion = 2;
      string prop = "extracts";
      int exchars = 1200;
      bool exintro = true;
      int exlimit = 1;

      string url = $"{MediaWikiApiBaseUrl}" +
                   $"?action={action}" +
                   $"&prop={prop}" +
                   $"&titles={title}" +
                   $"&format={format}" +
                   $"&formatversion={formatversion}" +
                   $"&exchars={exchars}" +
                   $"&exintro={exintro}" +
                   $"&exlimit={exlimit}";

      string res = await SendHttpGetRequest(url);
      Extract extract = (Extract)JsonConvert.DeserializeObject(res);
      string extract_html = extract.query.pages[0].extract;
      string filled_html =
        $"<html lang=\"en\">" +
        $"<head>" +
          $"<meta charset=\"utf-8\"/>" +
          $"<meta" +
              $"name=\"viewport\"" +
              $"content = \"width=device-width, initial-scale=1, shrink-to-fit=no\"" +
          $"/>" +
          $"<link" +
              $"href=\"https://fonts.googleapis.com/css?family=Lato&display=swap\"" +
              $"rel=\"stylesheet\"/>" +
          $"<link rel=\"stylesheet\" href=\"css/bootstrap.min.css\"/>" +
          $"<title>{summary.displaytitle}</ title >" +
        $"</head>" +
        $"<div>";

      if (summary.thumbnail != null)
      {

        filled_html +=
              $"<img src=\"{summary.thumbnail.source}\"" +
                   $"alt=\"{summary.displaytitle}_img\"" +
                   $"style=\"width:{summary.thumbnail.width}px;" +
                           $"height: {summary.thumbnail.height}px;" +
                           $"float:right; margin - left:7px; margin - bottom:5px;" +
              $">";
      }

      // TODO add content urls to go to the full article

      filled_html +=
        $"<span style=\"font-size: 20px;\">" +
          $"<b>{summary.displaytitle}</b></span>" +
          $"{(string.IsNullOrEmpty(extract_html) ? $"No extract found for {title}" : $"{extract_html}")}" +
      $"</div>";
        
      return $"<div class=\"wiki-result\">{filled_html}</div>";

    }

    public object SummaryParser(Summary summary)
    {
      // TODO
      return summary;
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
