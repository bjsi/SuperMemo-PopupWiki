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
using Stubble.Core.Builders;
using System.IO;

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

    public async Task<string> GetExtract(string title)
    {
      string action = "query";
      string format = "json";
      string errorformat = "bc";
      string prop = "description|extracts|pageimages";
      string list = "";
      string meta = "";
      int utf8 = 1;
      string formatversion = "latest";
      int indexpageids = 1;
      string piprop = "thumbnail|name|original";
      int pithumbsize = 300;
      int exchars = 1200;
      bool exintro = true;
      int exlimit = 1;

      string url = $"{MediaWikiApiBaseUrl}" +
                   $"?action={action}" +
                   $"&format={format}" +
                   $"&errorformat={errorformat}" +
                   $"&prop={prop}" +
                   $"&list={list}" +
                   $"&meta={meta}" +
                   $"&utf8={utf8}" +
                   $"&formatversion={formatversion}" +
                   $"&indexpageids={indexpageids}" +
                   $"&piprop={piprop}" +
                   $"&pithumbsize={pithumbsize}" +
                   $"&titles={title}" +
                   $"&exchars={exchars}" +
                   $"&exintro={exintro}" +
                   $"&exlimit={exlimit}";

      // TODO if query.pageids[0] == -1, there are no matches

      string res = await SendHttpGetRequest(url);
      Extract extract = JsonConvert.DeserializeObject<Extract>(res);
      return StubbleHtml(extract);
      string extract_html = extract.query.pages[0].extract;
      Page extract_page = extract.query.pages[0];

      var stubble = new StubbleBuilder().Build();

      string filled_html =
        $"<html lang=\"en\">" +
        $"<head>" +
          $"<meta charset=\"utf-8\"/>" +
          $"<meta " +
              $"name=\"viewport\" " +
              $"content=\"width=device-width, initial-scale=1, shrink-to-fit=no\"" +
          $"/>" +
          $"<link " +
              $"href=\"https://fonts.googleapis.com/css?family=Lato&display=swap\" " +
              $"rel=\"stylesheet\"/>" +
          $"<link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css\">" +
      $"<title>{extract_page.title}</title>" +
        $"</head>" +
        $"<div>";

      if (extract_page.thumbnail != null)
      {

        filled_html +=
              $"<img src=\"{extract_page.thumbnail.source}\"" +
                   $"alt=\"{extract_page.title}_img\"" +
                   $"style=\"width:{extract_page.thumbnail.width}px;" +
                           $"height: {extract_page.thumbnail.height}px;" +
                           $"float:right; margin - left:7px; margin - bottom:5px;\" />";
      }

      // TODO add content urls to go to the full article

      filled_html +=
        $"<span style=\"font-size: 20px;\">" +
          $"<b>{extract_page.title}</b></span>" +
          $"<p>{extract_page.description}</p>" +
          $"{(string.IsNullOrEmpty(extract_html) ? $"No extract found for {title}" : $"{extract_html}")}" +
      $"</div>" +
      $"</html>";
        
      return filled_html;

    }
    public string StubbleHtml(Extract extract)
    {
      var stubble = new StubbleBuilder().Build();
      // If e
      // TODO if query.pageids[0] == -1, there are no matches
        // TODO At build time the html template is placed in the app root.
        // TODOOOOOOOOO That didn't work remember to place in app root!!
      using (StreamReader streamReader = new StreamReader(@"PopupWikiTemplate.Mustache", Encoding.UTF8))
      {
        var obj = extract.query.pages[0];
        var output = stubble.Render(streamReader.ReadToEnd(), obj);
        Console.WriteLine(output);
        return output;
      }
      
      // Else return "sorry no matches found."

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
