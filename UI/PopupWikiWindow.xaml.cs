using Forge.Forms;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Forms;
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

namespace SuperMemoAssistant.Plugins.PopupWiki.UI
{
  /// <summary>
  /// Interaction logic for PopupWikiWindow.xaml
  /// </summary>
  public partial class PopupWikiWindow
  {
    private PopupWikiCfg Config => Svc<PopupWiki>.Plugin.Config;
    private string currentUrl = string.Empty;
    private string currentTitle = string.Empty;
    private string currentPageHtml = string.Empty;


    public PopupWikiWindow(PopupWikiService wikiService, string html)
    {
      InitializeComponent();
      Browser.NavigateToString(html);
      currentPageHtml = html;

    }

    protected async void CreateSMExtractWithPriority()
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

    private void CreatePageExtract()
    {
    }
    protected ContentBase CreateImageContent(Image image,
                                             string title)
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

    private void CreateSMExtract(double priority = -1)
    {
      bool ret = false;
      bool hasText = false;
      bool hasImage = false;

      var contents = new List<ContentBase>();

      (string selText, List<string> images) = GetSelected();

      if (string.IsNullOrEmpty(selText))
      {
        return;
      }

      contents.Add(new TextContent(true, selText));
      hasText = true;

      if (images.Count > 0)
      {
        foreach (string img in images)
        {
          var doc = new HtmlDocument();
          doc.LoadHtml(img);
          HtmlNode imgNode = doc.DocumentNode.SelectSingleNode("//img");
          string imgUrl = string.Empty;
          if (imgNode != null)
          {
            imgUrl = "http:" + imgNode.Attributes["src"].Value;

            if (!string.IsNullOrEmpty(imgUrl))
            {
              WebClient wc = new WebClient();
              byte[] bytes = wc.DownloadData(imgUrl);
              MemoryStream ms = new MemoryStream(bytes);
              Image IMAGE = Image.FromStream(ms);
              var ImageContent = CreateImageContent(IMAGE, imgUrl);
              contents.Add(ImageContent);

              hasImage = true;
            }
          }
        }

      }

      if (contents.Count > 0)
      {

        if (priority < 0 || priority > 100)
        {
          priority = Config.SMExtractPriority;
        }

        var parentEl = Svc.SM.UI.ElementWdw.CurrentElement;

        ret = Svc.SM.Registry.Element.Add(
          out _,
          ElemCreationFlags.ForceCreate,
          new ElementBuilder(ElementType.Topic,
                             contents.ToArray())
            .WithParent(parentEl)
            .WithConcept(parentEl.Concept)
            .WithLayout("Article")
            .WithPriority(priority)
            .WithReference(
              r => r.WithTitle(currentTitle ?? "Unknown")
                    .WithSource("Wikipedia")
                    .WithLink(currentUrl ?? "Unknown"))
            .DoNotDisplay()
        );
      }

      Focus();
      Topmost = true;
    }

    private (string text, List<string> images) GetSelected()
    {
      string text = string.Empty;
      List<string> images = new List<string>();

      var browserDoc = new HtmlDocument();
      browserDoc.LoadHtml(currentPageHtml);
      var htmlDoc = (Browser.Document as IHTMLDocument2);
      IHTMLSelectionObject selection = htmlDoc.selection;
      IHTMLTxtRange range = (IHTMLTxtRange)selection.createRange();

      string selectedHtml = range.htmlText;

      HtmlDocument doc = new HtmlDocument();
      doc.LoadHtml(selectedHtml);

      // Parse Images
      HtmlNodeCollection selectedImages = doc.DocumentNode.SelectNodes("//img");
      if (selectedImages != null)
      {
        foreach (HtmlNode htmlImage in selectedImages)
        {
          images.Add(htmlImage.OuterHtml);
        }
      }

      // Parse Text Html
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
          // TODO 
          linkNode.Attributes["href"].Value = "http://en.wikipedia.org/wiki" + linkNode.Attributes["href"].Value.Substring(1);
          Console.WriteLine($"Link node: {linkNode.Attributes["href"].Value}");
        }
      }

      text = doc.DocumentNode.OuterHtml;

      // Set current page references
      HtmlNode titleNode = browserDoc.DocumentNode.SelectSingleNode("//head/title");
      if (titleNode != null)
      {
        currentTitle = titleNode.InnerText;
      }

      HtmlNode urlNode = browserDoc.DocumentNode.SelectSingleNode("//head/meta[@href]");
      if (urlNode != null)
      {
        currentUrl = urlNode.Attributes["href"].Value;
      }

      foreach (string image in images)
      {
        Console.WriteLine(image);
      }
      Console.WriteLine(text);

      return (text, images);
    }

    private void BtnSMExtract_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      Console.WriteLine("SM Extract");
      CreateSMExtract();
    }

    private void BtnImport_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      Console.WriteLine("Page Extract");
      CreatePageExtract();
    }

    private void BtnSMPriorityExtract_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      Console.WriteLine("Priority Extract");
      CreateSMExtractWithPriority();
    }
  }
}
