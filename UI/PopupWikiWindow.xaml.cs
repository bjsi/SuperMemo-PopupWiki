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
    private string MainLanguage => Config.WikiLanguages.Split(',')[0];
    private string language = string.Empty;
    private WindowType windowType;

    // Current page references
    private string currentUrl = string.Empty;
    private string currentTitle = string.Empty;

    private PopupWikiService wikiService;


    public PopupWikiWindow(PopupWikiService _wikiService, string html, string _language, WindowType _type)
    {
      wikiService = _wikiService;
      language = _language;
      windowType = _type;


      InitializeComponent();
      ConfigureWindowVariables();

      // Disable any extraction / import on search results pages.
      if (windowType == WindowType.wiktionarySearch || windowType ==  WindowType.wikiSearch)
      {
        BtnImport.IsEnabled = false;
        BtnSMExtract.IsEnabled = false;
        BtnSMPriorityExtract.IsEnabled = false;
      }
      else
      {
        BtnImport.IsEnabled = true;
        BtnSMExtract.IsEnabled = true;
        BtnSMPriorityExtract.IsEnabled = true;
      }

      if (!string.IsNullOrWhiteSpace(html))
      {
        // TODO: Should html filtering be happening here???
        if (windowType == WindowType.wiktionarySearch)
        {
          html = filterDictSearchResults(html);
        }
        wf_Browser.DocumentText = html;
        GetPageReferences(html);

        Console.WriteLine($"The current PopupWindow is a {windowType.ToString()} window.");
        Console.WriteLine($"The language of the current PopupWindow is {language}.");
        Console.WriteLine($"The current page url is {currentUrl}.");
        Console.WriteLine($"The current page title is {currentTitle}.");

      }
      else
      {
        LogTo.Information($"Attempted to open an empty PopupWiki {windowType.ToString()} window.");
      }

    }

    /// <summary>
    /// Set the size and startup location of the PopupWiki window.
    /// </summary>
    private void ConfigureWindowVariables()
    {
      this.Width = Config.WindowWidth;
      this.Height = Config.WindowHeight;

      // Adds a random offset to top and left variables to prevent stacking windows.
      Random rnd = new Random();
      int topRndOffset = rnd.Next(1, 40);
      int leftRndOffset = rnd.Next(1, 50);

      this.WindowStartupLocation = WindowStartupLocation.Manual;
      this.Left = Config.WindowLeft + leftRndOffset;
      this.Top = Config.WindowTop + topRndOffset;
    }

    /// <summary>
    /// Converts Wiktionary search result links to mobile wikitionary links.
    /// TODO: Should this be moved somewhere else?
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    private string filterDictSearchResults(string html)
    {
      if (windowType == WindowType.wiktionarySearch)
      {
        if (!string.IsNullOrEmpty(html))
        {

          var doc = new HtmlAgilityPack.HtmlDocument();
          doc.LoadHtml(html);
          var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");

          if (linkNodes != null)
          {
            // Change links to mobile wiktionary links.
            foreach (var linkNode in linkNodes)
            {
              string href = linkNode.Attributes["href"].Value;
              if (!string.IsNullOrEmpty(href))
              {
                // Adjust to mobile wiki links
                string[] splitHref = href.Split('.');
                splitHref[0] += ".m";
                linkNode.Attributes["href"].Value = string.Join(".", splitHref);
              }
            }
          }
          html = doc.DocumentNode.OuterHtml;
        }
        LogTo.Error($"Attempted to run FilterDictSearchResults on a {windowType.ToString()} window.");
      }
      return html;
    }

    /// <summary>
    /// Get the reference information for SuperMemo elements created from this page.
    /// Supports wikipedia pages and wiktionary pages.
    /// </summary>
    /// <param name="html"></param>
    private void GetPageReferences(string html)
    {
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html);

      // Wikipedia has no html node with url, so create it using the title.
      if (windowType == WindowType.wikipedia)
      {
        HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
          currentTitle = titleNode.InnerText;
          string urlTitle = currentTitle.Replace(" ", "_");
          currentUrl = $"https://{language}.{windowType.ToString()}.org/wiki/{urlTitle}";
        }
      }
        
      // Wiktionary has rel="canonical" - href is the page url
      else if (windowType == WindowType.wiktionary)
      {
        var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel]");
        if (linkNodes != null)
        {
          foreach(var linkNode in linkNodes)
          {
            if (linkNode.Attributes["rel"].Value == "canonical")
            {
              Uri uri = new Uri(linkNode.Attributes["href"].Value);
              string urlTitle = uri.Segments.Last();
              currentTitle = urlTitle.Replace("_", " ");
              currentUrl = $"https://{language}.{windowType.ToString()}.org/wiki/{urlTitle}";
              break;
            }
          }
        }
      }
      else
      {
        LogTo.Warning("GetPageReferences was called with an unsupported domain type.");
      }
    }

    /// <summary>
    /// Adds an image to the registry and returns an ImageContent object ready to be added to a new element.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="title"></param>
    /// <returns></returns>
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


    /// <summary>
    /// Adds a sound to the registry and returns a SoundContent object ready to be added to a new element.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="title"></param>
    /// <returns></returns>    
    protected ContentBase CreateAudioContent()
    {
      // TODO: Requires my addition to the sound reg.
      // TODO: Test with file types other than .wav
      // TODO: Check the GetFileType method
      // TODO: Needs a SoundWrapper class?

      throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the currently selected range in the PopupWiki window.
    /// </summary>
    /// <returns></returns>
    private IHTMLTxtRange GetSelectedRange()
    {
      var htmlDoc = (wf_Browser.Document.DomDocument as IHTMLDocument2);
      IHTMLSelectionObject selection = htmlDoc.selection;
      IHTMLTxtRange range = (IHTMLTxtRange)selection.createRange();
      return range;
    }
    
    /// <summary>
    /// Parses image urls from html string.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    private List<string> ParseImageUrls(string html)
    {
      List<string> imageUrls = new List<string>();

      if (!string.IsNullOrEmpty(html))
      {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
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
      }
      else
      {
        Console.WriteLine("Attempted to call ParseImageUrls on a null / empty html string.");
      }
      return imageUrls;
    }

    /// <summary>
    /// Parses audio urls from html string.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    private List<string> ParseAudioUrls(string html)
    {

      // TODO: Test this method
      // Use for extracting audio into SM Elements
      // TODO: Check the GetFileType method i changed in the sound reg
      // TODO: Check that SM can handle .ogg files.

      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html);
      List<string> audioUrls = new List<string>();
      HtmlNodeCollection audioNodes = doc.DocumentNode.SelectNodes("//audio");
      if (audioNodes != null)
      {
        foreach (HtmlNode audioNode in audioNodes)
        {
          // The source url of an Audio element is saved as a child of audio nodes
          // In <Source src="...">

          audioNode.SelectSingleNode(".//Souce[@src]");

          if (audioNode != null)
          {
            string audioUrl = audioNode.Attributes["src"].Value;
            if (audioUrl.StartsWith("//"))
            {
              audioUrls.Add($"http:{audioUrl}");
            }
          }
        }
      }
      return audioUrls;
    }

    /// <summary>
    /// Returns the html text from an html string, converting relative links to full links.
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    private string ParseTextHtml(string html)
    {
      if (!string.IsNullOrWhiteSpace(html))
      {
        html = HtmlFilters.RemoveImages(html);
        // TODO: Fix the underlying issue
        if (windowType == WindowType.wikipedia)
        {
          html = HtmlFilters.ConvRelToAbsLinks(html, $"https://{language}.wikipedia.org", WikiUrlUtils.IsDesktopWikipediaUrl);
        }
        else if (windowType == WindowType.wiktionary)
        {
          html = HtmlFilters.ConvRelToAbsLinks(html, $"https://{language}.wiktionary.org", WikiUrlUtils.IsDesktopWiktionaryUrl);

        }
        html = HtmlFilters.WiktionaryMobileToDesktopLinks(html);
      }
      Console.WriteLine(html);
      return html;
    }

    /// <summary>
    /// Use selected text to create a new extract element in SuperMemo.
    /// </summary>
    /// <param name="priority"></param>
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

      // Get just the text.
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

    /// <summary>
    /// Add blue extract highlight to extracted text.
    /// </summary>
    /// <param name="range"></param>
    private void AddExtractHighlight(IHTMLTxtRange range)
    {
      range.execCommand("BackColor", false, "#44C2FF");
    }

    private void BtnSMExtract_Click(object sender, RoutedEventArgs e)
    {
      CreateSMExtract();
    }

    // TODO: Implement a real import method
    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
      // Open in user's browser.
      System.Diagnostics.Process.Start(currentUrl);

    }

    /// <summary>
    /// Specify priority for SuperMemo extract.
    /// </summary>
    private async void CreateSMExtractWithPriority()
    {
      var selRange = GetSelectedRange();
      
      if (selRange == null || selRange.htmlText == null)
      {
        return;
      }

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
      AddLinkClickEvents();
    }

    /// <summary>
    /// Adds click events to desktop wikipedia links and mobile wiktionary links.
    /// </summary>
    private void AddLinkClickEvents()
    {
      var Links = wf_Browser.Document.Links;
      if (Links != null)
      {
        foreach (HtmlElement link in Links)
        {
          if (WikiUrlUtils.IsDesktopWikipediaUrl(link.GetAttribute("href")))
          {
            link.Click += new HtmlElementEventHandler(WikipediaLinkClick);
          }
          else if (WikiUrlUtils.IsMobileWiktionaryUrl(link.GetAttribute("href")))
          {
            link.Click += new HtmlElementEventHandler(WiktionaryLinkClick);
          }
        }
      }
    }
    
    /// <summary>
    /// Adds a click event to a wikipedia link.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void WikipediaLinkClick(object sender, EventArgs e)
    {
      HtmlElement element = ((HtmlElement)sender);
      string href = element.GetAttribute("href");

      string html = string.Empty;

      if (!string.IsNullOrEmpty(href))
      {
        if (WikiUrlUtils.IsDesktopWikipediaUrl(href))
        {
          var uri = new Uri(href);

          // Get language
          var language = uri.DnsSafeHost.Split('.')[0];

          // Get title
          var articleTitle = uri.Segments.Last();

          // Attempt to find wikipedia article
          html = await wikiService.GetWikipediaMobileHtml(articleTitle, language);
          WindowType type = WindowType.wikipedia;

          if (!string.IsNullOrEmpty(html))
          {
            OpenNewPopupWindow(wikiService, html, language, type);
            return;
          }
          LogTo.Error($"Failed to get wikipedia article for link {href}");
          return;
        }
        LogTo.Information("Can't open link because it isn't a valid wiki Url");
        return;
      }
      LogTo.Error("Attempted to open link, but href attribute was null or empty.");
    }

    /// <summary>
    /// Open a new PopupWiki window from a PopupWiki window.
    /// </summary>
    /// <param name="wikiService"></param>
    /// <param name="html"></param>
    /// <param name="language"></param>
    /// <param name="type"></param>
    private void OpenNewPopupWindow(PopupWikiService wikiService, string html, string language, WindowType type)
    {
      if (string.IsNullOrEmpty(html))
      {
        Console.WriteLine("Attempted to open a new popup window with empty html");
        return;
      }

      System.Windows.Application.Current.Dispatcher.Invoke(
      () =>
        {
          var wdw = new PopupWikiWindow(wikiService, html, language, type);
          wdw.ShowAndActivate();
        }
      );
    }

    /// <summary>
    /// Adds a click event to a wiktionary link.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void WiktionaryLinkClick(object sender, EventArgs e)
    {
      HtmlElement element = ((HtmlElement)sender);
      string href = element.GetAttribute("href");

      string html = string.Empty;

      if (!string.IsNullOrEmpty(href))
      {
        if (WikiUrlUtils.IsMobileWiktionaryUrl(href))
        {
          var uri = new Uri(href);

          // Get language
          var language = uri.DnsSafeHost.Split('.')[0];

          // Attempt to find wiktionary article
          html = await wikiService.GetWiktionaryMobileHtml(href);
          WindowType type = WindowType.wiktionary;

          if (!string.IsNullOrEmpty(html))
          {
            OpenNewPopupWindow(wikiService, html, language, type);
            return;
          }
          Console.WriteLine($"Failed to get wikipedia article for link {href}");
          return;
        }
        LogTo.Information("Can't open link because it isn't a valid wiki Url");
        return;
      }
      LogTo.Error("Attempted to open link, but href attribute was null or empty.");
    }

    /// <summary>
    /// Search the selected text in wiktionary.
    /// </summary>
    private async void WiktionarySearchSelText()
    {
      var selRange = GetSelectedRange();
      if (string.IsNullOrEmpty(selRange.text))
      {
        return;
      }

      var filteredSelText = string.Concat(selRange.text
                                          .Where(c => 
                                                    c != '.'
                                                 && c != '?'
                                                 && c != '!'
                                                 && c != ','))
                                  .Trim('\n', '\t', ' ', '\r');

      var result = await Forge.Forms.Show.Window()
                                    .For(new Prompt<string> { Title = "Search Wiktionary:", Value = filteredSelText});

      if (!result.Model.Confirmed)
      {
        return;
      }

      filteredSelText = result.Model.Value;

      if (string.IsNullOrEmpty(filteredSelText))
      {
        return;
      }

      string html = await wikiService.GetSearchResults(filteredSelText, SearchType.wiktionary);

      if (string.IsNullOrEmpty(html))
      {
        html = $"<h1>No results found for \"{filteredSelText}\".</h1>";
      }

      OpenNewPopupWindow(wikiService, html, null, WindowType.wiktionarySearch);

    }

    /// <summary>
    /// Directly attempts to get article for the selected text in the user's main language.
    /// If there is no direct article, redirect to a wikipedia search results page.
    /// </summary>
    private async void OpenWikiSelText()
    {

      var selRange = GetSelectedRange();
      if (string.IsNullOrEmpty(selRange.text))
      {
        return;
      }

      var filteredSelText = string.Concat(selRange.text
                                          .Where(c => 
                                                    c != '.'
                                                 && c != '?'
                                                 && c != '!'
                                                 && c != ','))
                                  .Trim('\n', '\t', ' ', '\r');

      var result = await Forge.Forms.Show.Window()
                                    .For(new Prompt<string> { Title = "Find Wikipedia article:", Value = filteredSelText});

      if (!result.Model.Confirmed)
      {
        return;
      }

      filteredSelText = result.Model.Value;

      if (string.IsNullOrEmpty(filteredSelText))
      {
        return;
      }
    
      // Attempt to directly get article in user's main language.
      string html = await wikiService.GetWikipediaMobileHtml(filteredSelText, MainLanguage);
      WindowType type = WindowType.wikipedia;

      // If no direct article found, redirect to search.
      if (string.IsNullOrEmpty(html))
      {
        html = await wikiService.GetSearchResults(filteredSelText, SearchType.wikipedia);
        type = WindowType.wikiSearch;
      }

      // If no direct article and no search results, return nothing found message.
      if (string.IsNullOrEmpty(html))
      {
        html = $"<h1>No results found for \"{filteredSelText}\".</h1>";
      }

      OpenNewPopupWindow(wikiService, html, language, type);
    }

    // TODO: Change to Alt
    private void wf_Docbody(object sender, HtmlElementEventArgs e)
    {

      Console.WriteLine($"Key Pressed Code == {e.KeyPressedCode}");

      // Extracts only allowed on non-search pages
      if (windowType == WindowType.wikipedia || windowType == WindowType.wiktionary)
      {
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


      // ctrl + w
      if (e.KeyPressedCode == 23)
      {
        OpenWikiSelText();
      }
      // ctrl d
      else if (e.KeyPressedCode == 4)
      {
        WiktionarySearchSelText();
      }

      else if (e.KeyPressedCode == 27)
      {
        Close();
      }
    }

    private void wf_Browser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
      Console.WriteLine("Preview Key Down Event");
    }
  }
}
