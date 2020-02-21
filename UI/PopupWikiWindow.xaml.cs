using Forge.Forms;
using System.Linq;
using Anotar.Serilog;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Windows.Input;
using System;
using HtmlAgilityPack;
using mshtml;
using SuperMemoAssistant.Services;
using System.Collections.Generic;
using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using System.Net; 
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Sys.Drawing;
using System.Windows;
using SuperMemoAssistant.Extensions;
using System.Windows.Forms.Integration;

namespace SuperMemoAssistant.Plugins.PopupWiki.UI
{
  /// <summary>
  /// Interaction logic for PopupWikiWindow.xaml
  /// </summary>
  public partial class PopupWikiWindow
  {
    private PopupWikiCfg Config => Svc<PopupWiki>.Plugin.Config;
    private string WikiBaseUrl => $"http://{Config.WikiLanguage}.wikipedia.org/wiki";

    // Current page references
    private string currentUrl = string.Empty;
    private string currentTitle = string.Empty;

    private string currentPageHtml = string.Empty;
    private PopupWikiService wikiService;


    public PopupWikiWindow(PopupWikiService _wikiService, string html)
    {
      InitializeComponent();
      wikiService = _wikiService;

      if (!string.IsNullOrEmpty(html))
      {
        wf_Browser.DocumentText = html;
        GetPageReferences(html);
        currentPageHtml = html;
      }
    }

    private async void ImportArticle(double priority = -1)
    {
      // TODO: Not working perfectly
      // Images don't show up properly.

      if (currentUrl.Contains(WikiBaseUrl))
      {
        Console.WriteLine($"Getting {currentUrl}");
        string html = await wikiService.GetWikiPage(currentUrl);

        if (!string.IsNullOrEmpty(html))
        {
          var contents = new List<ContentBase>();
          var parentEl = Svc.SM.UI.ElementWdw.CurrentElement;

          contents.Add(new TextContent(true, html));

          if (contents.Count > 0)
          {
            if (priority < 0 || priority > 100)
            {
              priority = Config.PageExtractPriority;
            }

            Svc.SM.Registry.Element.Add(
              out _,
              ElemCreationFlags.ForceCreate,
              new ElementBuilder(ElementType.Topic,
                                 contents.ToArray())
                .WithParent(Config.ExtractType == PopupWikiCfg.ExtractMode.Child ? parentEl : null)
                .WithConcept(Config.ExtractType == PopupWikiCfg.ExtractMode.Hook ? parentEl.Concept : null)
                .WithLayout("Article") // TODO
                .WithTitle(currentTitle)
                .WithPriority(priority)
                .WithReference(
                  r => r.WithTitle(currentTitle)
                        .WithSource("Wikipedia")
                        .WithLink(currentUrl))
                .DoNotDisplay()
            );

            GetWindow(this)?.Activate();
          }
        }
      }
    }
  
    private async void ImportArticleWithPriority()
    {
      var result = await Forge.Forms.Show.Window()
                         .For(new Prompt<double> { Title = "Article Import Priority?", Value = -1 });
      if (!result.Model.Confirmed)
      {
        return;
      }

      if (result.Model.Value < 0 || result.Model.Value > 100)
      {
        return;
      }

      ImportArticle(result.Model.Value);
    }

    private void GetPageReferences(string html)
    {
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html);
      HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//title");
      if (titleNode != null)
      {
        currentTitle = titleNode.InnerText;
        string urlTitle = currentTitle.Replace(" ", "_");
        currentUrl = $"{WikiBaseUrl}/{urlTitle}";
      }
    }

    protected ContentBase CreateImageContent(Image image, string title)
    {
      if (image == null)
        return null;

      int imgRegistryId = Svc.SM.Registry.Image.AddMember(
        new ImageWrapper(image),
        title
      );

      if (imgRegistryId <= 0)
        return null;

      return new ImageContent(imgRegistryId,
                              Config.ImageStretchType);
    }

    private IHTMLTxtRange GetSelectedRange()
    {
      var htmlDoc = (wf_Browser.Document.DomDocument as IHTMLDocument2);
      IHTMLSelectionObject selection = htmlDoc.selection;
      IHTMLTxtRange range = (IHTMLTxtRange)selection.createRange();
      return range;
    }

    private List<string> ParseImageUrls(string html)
    {
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html);
      List<string> imageUrls = new List<string>();
      HtmlNodeCollection imageNodes = doc.DocumentNode.SelectNodes("//img");
      if (imageNodes != null)
      {
        foreach (HtmlNode imageNode in imageNodes)
        {
          // TODO: Make this safer
          string url = imageNode.Attributes["src"].Value;
          if (url.StartsWith("//"))
          {
            imageUrls.Add($"http:{url}");
          }
        }
      }
      return imageUrls;
    }

    private string ParseTextHtml(string html)
    {
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html);

      // Remove img elements
      doc.DocumentNode.Descendants()
                      .Where(n => n.Name == "img")
                      .ToList()
                      .ForEach(n => n.Remove());

      // Convert relative links to full links
      HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//a");
      if (linkNodes != null)
      {
        foreach (HtmlNode linkNode in linkNodes)
        {
          linkNode.Attributes["href"].Value = WikiBaseUrl + linkNode.Attributes["href"].Value.Substring(1);
        }
      }
      return doc.DocumentNode.OuterHtml;
    }

    private void CreateSMExtract(double priority = -1)
    {
      bool ret = false;
      bool hasText = false;
      bool hasImage = false;
      var contents = new List<ContentBase>();
      var parentEl = Svc.SM.UI.ElementWdw.CurrentElement;
      string ExtractTitle = null;

      // Get selected text and images
      IHTMLTxtRange range = GetSelectedRange();

      // Get selected text
      string selTextHtml = ParseTextHtml(range.htmlText);
      if (!string.IsNullOrEmpty(range.text))
      {
        contents.Add(new TextContent(true, selTextHtml));
        hasText = true;
      }

      // Get selected images
      List<string> selImageUrls = ParseImageUrls(range.htmlText);
      if (selImageUrls.Count > 0)
      {
        WebClient wc = new WebClient();

        foreach (string imageUrl in selImageUrls)
        {
          if (!string.IsNullOrEmpty(imageUrl))
          {
            // TODO: from Alexis
            // You might actually be able to skip downloading the image altogether
            // by finding the image node in your embedded browser and extracting its data.
            // There are several advantages to this:
            // -It will avoid an array of exception that could be thrown e.g. if internet is unavailable,
            // if downloading fails, if the url is not correctly formatted(e.g.it might be a relative url),
            // if the image content is actually in-line and not an url, etc.
            // -It will be faster(no download), It won't require an internet connection at all

            try
            {
              byte[] bytes = wc.DownloadData(imageUrl);
              if (bytes != null)
              {
                MemoryStream ms = new MemoryStream(bytes);
                if (ms != null)
                {
                  Image image = Image.FromStream(ms);
                  var uri = new Uri(imageUrl);
                  var filename = uri.Segments.Last();
                  var ImageContent = CreateImageContent(image, filename);
                  contents.Add(ImageContent);
                  hasImage = true;
                }
              }
            }
            catch (WebException e)
            {
              LogTo.Warning($"PopupWiki: Exception caught while downloading image: {e}");
            }
          }
        }
      }

      if (Config.ImageExtractAddHtml && !hasText && hasImage)
      {
        ExtractTitle = $"{currentTitle}: {contents.Count} image{(contents.Count == 1 ? "" : "s")}";
        contents.Add(new TextContent(true, string.Empty));
      }

      if (contents.Count > 0)
      {
        if (priority < 0 || priority > 100)
        {
          priority = Config.SMExtractPriority;
        }

        ret = Svc.SM.Registry.Element.Add(
          out _,
          ElemCreationFlags.ForceCreate,
          new ElementBuilder(ElementType.Topic,
                             contents.ToArray())
            .WithParent(Config.ExtractType == PopupWikiCfg.ExtractMode.Child ? parentEl : null)
            .WithConcept(Config.ExtractType == PopupWikiCfg.ExtractMode.Hook ? parentEl.Concept : null)
            .WithLayout("Article") // TODO
            .WithTitle(ExtractTitle)
            .WithPriority(priority)
            .WithReference(
              r => r.WithTitle(currentTitle)
                    .WithSource("Wikipedia")
                    .WithLink(currentUrl))
            .DoNotDisplay()
        );
        
        GetWindow(this)?.Activate();
    
        if (ret)
        {
          AddExtractHighlight(range);
        }
      }
    }

    private void AddExtractHighlight(IHTMLTxtRange range)
    {
      range.execCommand("BackColor", false, "#44C2FF");
    }

    private void BtnSMExtract_Click(object sender, RoutedEventArgs e)
    {
      CreateSMExtract();
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
      // TODO:
      // ImportArticle();

      // Open link in IE
      System.Diagnostics.Process.Start(currentUrl);

    }

    private async void CreateSMExtractWithPriority()
    {
      var result = await Forge.Forms.Show.Window()
                         .For(new Prompt<double> { Title = "Extract Priority?", Value = -1 });
      if (!result.Model.Confirmed)
      {
        return;
      }

      if (result.Model.Value < 0 || result.Model.Value > 100)
      {
        return;
      }

      CreateSMExtract(result.Model.Value);
    }

    private void BtnSMPriorityExtract_Click(object sender, RoutedEventArgs e)
    {
      CreateSMExtractWithPriority();
    }

    private async void BtnOpenLink_Click(object sender, RoutedEventArgs e)
    {
      // Get selected html
      var htmlDoc = (wf_Browser.Document.DomDocument as IHTMLDocument2);
      IHTMLSelectionObject selection = htmlDoc.selection;
      IHTMLTxtRange range = (IHTMLTxtRange)selection.createRange();
      string selectedHtml = range.htmlText;
      
      // Parse links from selected html
      HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(selectedHtml);

      // Find html link elements
      HtmlNodeCollection LinkCollection = doc.DocumentNode.SelectNodes("//a[@href]");
      if (LinkCollection != null)
      {
        // Create an array of search terms from urls
        // Each term is the end of the link (the article title)
        List<string> SearchTermArray = new List<string>();
        foreach (var linkNode in LinkCollection)
        {
          string href = linkNode.Attributes["href"].Value;

          if (href.StartsWith("./"))
          {
            SearchTermArray.Add(href.Replace("./", ""));
          }
          else if (href.StartsWith(WikiBaseUrl))
          {
            // TODO use URI .Segments.Last()
            SearchTermArray.Add(href.Replace($"{WikiBaseUrl}/", ""));
          }
        }

        // Open a popup wiki window for each term.
        if (SearchTermArray.Count > 0)
        {
          foreach (var term in SearchTermArray)
          {
            string html = await wikiService.GetMediumHtml(term);
            System.Windows.Application.Current.Dispatcher.Invoke(
              () =>
              {
                var wdw = new PopupWikiWindow(wikiService, html);
                wdw.ShowAndActivate();
              }
            );
          }
        }
      }
    }

    private void wf_Browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
    {
      wf_Browser.Document.Body.KeyPress += new HtmlElementEventHandler(wf_Docbody);

      // Add Click events to links
      var Links = wf_Browser.Document.Links;
      if (Links != null)
      {
        foreach (HtmlElement link in Links)
        {
          link.Click += new HtmlElementEventHandler(LinkClick);
        }
      }
    }

    private async void LinkClick(object sender, System.EventArgs e)
    {
      // TODO: What if the link is not a wiki link

      HtmlElement element = ((HtmlElement)sender);
      string href = element.GetAttribute("href");
      Console.WriteLine(href);
      var uri = new Uri(href);
      var articleTitle = uri.Segments.Last();

      // Open a new popupwiki window
      string html = await wikiService.GetMediumHtml(articleTitle);
      System.Windows.Application.Current.Dispatcher.Invoke(
      () =>
      {
        var wdw = new PopupWikiWindow(wikiService, html);
        wdw.ShowAndActivate();
      }
    );
    }

    private void wf_Docbody(object sender, HtmlElementEventArgs e)
    {
      Console.WriteLine($"Key Code: {e.KeyPressedCode}");
      // x
      if (e.KeyPressedCode == 120)
      {
        CreateSMExtract();
      }
      // ctrl + x
      else if (e.KeyPressedCode == 24)
      {
        CreateSMExtractWithPriority();
      }
    }
  }
}
