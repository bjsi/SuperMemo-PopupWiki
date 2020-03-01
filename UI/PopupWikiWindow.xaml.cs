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
    private string language;

    // Current page references
    private string currentUrl = string.Empty;
    private string currentTitle = string.Empty;

    private string currentPageHtml = string.Empty;
    private PopupWikiService wikiService;


    public PopupWikiWindow(PopupWikiService _wikiService, string html, string _language)
    {
      InitializeComponent();
      wikiService = _wikiService;
      language = _language;

      if (!string.IsNullOrEmpty(html))
      {
        wf_Browser.DocumentText = html;
        GetPageReferences(html);
        currentPageHtml = html;
      }
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
        currentUrl = $"https://{language}.wikipedia.org/wiki/{urlTitle}";
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
      if (!string.IsNullOrWhiteSpace(html))
      {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        // Remove img elements
        doc.DocumentNode.Descendants()
                        .Where(n => n.Name == "img")
                        .ToList()
                        .ForEach(n => n.Remove());

        //// Convert relative links to full links
        HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//a");
        if (linkNodes != null)
        {
          foreach (HtmlNode linkNode in linkNodes)
          {
            if (!string.IsNullOrEmpty(linkNode.Attributes["href"].Value))
            {
              string href = linkNode.Attributes["href"].Value;
              if (Uri.IsWellFormedUriString(href, UriKind.Relative))
              {
                if (href.StartsWith("./"))
                {
                  linkNode.Attributes["href"].Value = $"https://{language}.wikipedia.org/wiki/{href.Substring(2)}";
                }
              }
            }
          }
        }
        return doc.DocumentNode.OuterHtml;
      }
      return html;
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
      // Open in user's browser.
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

    private void wf_Browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
    {
      wf_Browser.Document.Body.KeyPress += new HtmlElementEventHandler(wf_Docbody);
      //Testing

      // Add Click events to links to open in popup wiki
      var Links = wf_Browser.Document.Links;
      if (Links != null)  
      {
        foreach (HtmlElement link in Links)
        {
          Console.WriteLine(link.GetAttribute("href"));

          if (IsValidWikiUrl(link.GetAttribute("href")))
          {
            Console.WriteLine(link.GetAttribute("href"));
            link.Click += new HtmlElementEventHandler(LinkClick);
          }
        }
      }
    }

    private bool IsValidWikiUrl(string link)
    {
      if (Uri.IsWellFormedUriString(link, UriKind.Absolute) 
          && (new Uri(link).DnsSafeHost.Split('.')[1] == "wikipedia"))
      {
        return true;
      }
      else if (Uri.IsWellFormedUriString(link, UriKind.Relative))
      {
        return true;
      }
      return false;
    }

    private async void LinkClick(object sender, EventArgs e)
    {
      HtmlElement element = ((HtmlElement)sender);
      string href = element.GetAttribute("href");
      if (!string.IsNullOrEmpty(href) && IsValidWikiUrl(href))
      {
        Console.WriteLine(href);
        
        var uri = new Uri(href);

        // Get language
        var language = uri.DnsSafeHost.Split('.')[0];

        // Get title
        var articleTitle = uri.Segments.Last();

        // Open a new popupwiki window
        string html = await wikiService.GetMediumHtml(articleTitle, language);
        System.Windows.Application.Current.Dispatcher.Invoke(
        () =>
          {
            var wdw = new PopupWikiWindow(wikiService, html, language);
            wdw.ShowAndActivate();
          }
        );
      }
    }

    private void wf_Docbody(object sender, HtmlElementEventArgs e)
    {
      // TODO: Change to Alt
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
