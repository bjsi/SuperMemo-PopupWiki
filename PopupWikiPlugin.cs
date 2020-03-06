#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2019/04/22 17:20
// Modified On:  2019/04/22 20:52
// Modified By:  Alexis

#endregion



using SuperMemoAssistant.Services.Sentry;
using System.Windows;
using mshtml;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.Sys.IO.Devices;
using SuperMemoAssistant.Interop.SuperMemo.Content.Controls;
using System.Windows.Input;
using SuperMemoAssistant.Plugins.PopupWiki.UI;
using System;
using Forge.Forms;

namespace SuperMemoAssistant.Plugins.PopupWiki
{
  // ReSharper disable once UnusedMember.Global
  // ReSharper disable once ClassNeverInstantiated.Global
  public class PopupWiki : SentrySMAPluginBase<PopupWiki>
  {
    #region Constructors

    public PopupWiki() { }

    #endregion

    public PopupWikiCfg Config { get; set; }
    public PopupWikiService wikiService;

    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "PopupWiki";

    public override bool HasSettings => true;

    #endregion

    private void LoadConfig()
    {
      Config = Svc.Configuration.Load<PopupWikiCfg>().Result ?? new PopupWikiCfg();
    }

    protected override void PluginInit()
    {
      LoadConfig();

      wikiService = new PopupWikiService();

      Svc.HotKeyManager
         .RegisterGlobal(
           "OpenPopupWiki",
           "(Global) Get PopupWiki for selected term",
           HotKeyScope.SM,
           new HotKey(Key.W, KeyModifiers.CtrlAlt),
           GetPopupWiki,
           true
          )
         .RegisterGlobal(
          "OpenPopupWiktionary",
          "(Global) Get PopupWiktionary for selected term",
          HotKeyScope.SM,
          new HotKey(Key.W, KeyModifiers.AltShift),
          SearchWiktionary,
          true
          );
    }
  
    /// <summary>
    /// Gets the currently selected text in SuperMemo. Removes punctuation and strips whitespace.
    /// Opens a window to edit the selection before searching.
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetSelectedText()
    {
      var ctrlGroup = Svc.SM.UI.ElementWdw.ControlGroup;
      var htmlCtrl = ctrlGroup?.FocusedControl.AsHtml();
      var htmlDoc = htmlCtrl?.GetDocument();
      var sel = htmlDoc?.selection;

      string filteredSelText = string.Empty;

      if (!(sel?.createRange() is IHTMLTxtRange textSel))
        return null;

      if (!string.IsNullOrEmpty(textSel.text))
      {
         filteredSelText = string.Concat(textSel.text
                                         .Where(c => !Char.IsPunctuation(c)))
                                 .Trim('\n', '\t', ' ', '\r');
      }

      return filteredSelText;
    }

    /// <summary>
    /// Searches Wiktionary using selected SuperMemo text as the search term.
    /// </summary>
    public async void SearchWiktionary()
    {
      string selText = await GetSelectedText();
      if (string.IsNullOrWhiteSpace(selText))
        return;

      string html = await wikiService.GetSearchResults(selText, SearchType.wiktionary);

      if (string.IsNullOrEmpty(html))
      {
        html = $"<h1>No results found for \"{selText}\".</h1>";
      }

      OpenNewPopupWindow(wikiService, html, null, WindowType.wiktionarySearch);
    }

    /// <summary>
    /// Attempts to directly get wiki article for selected. Redirects to search if no direct article found.
    /// </summary>
    public async void GetPopupWiki()
    {
      string selText = await GetSelectedText();

      if (string.IsNullOrWhiteSpace(selText))
        return;

      string html = await wikiService.GetWikipediaMobileHtml(selText, Config.WikiLanguages.Split(',')[0]);
      WindowType type = WindowType.wikipedia;
      string language = Config.WikiLanguages.Split(',')[0];

      if (string.IsNullOrEmpty(html))
      {
        html = await wikiService.GetSearchResults(selText, SearchType.wikipedia);
        type = WindowType.wikiSearch;
        language = null;
      }

      if (string.IsNullOrEmpty(html))
      {
        html = $"<h1>No results found for \"{selText}\".</h1>";
        language = null;
        // TODO: Is this right?
        type = WindowType.wikiSearch;
      }

      OpenNewPopupWindow(wikiService, html, language, type);
    }

    /// <summary>
    /// Open a new PopupWiki Window.
    /// </summary>
    /// <param name="wikiService"></param>
    /// <param name="html"></param>
    /// <param name="language"></param>
    /// <param name="type"></param>
    public void OpenNewPopupWindow(PopupWikiService wikiService, string html, string language, WindowType type)
    {

      if (string.IsNullOrEmpty(html))
      {
        Console.WriteLine("Attempted to open new PopupWiki window with null or empty html.");
        return;
      }

      Application.Current.Dispatcher.Invoke(
        () =>
        {
          var wdw = new PopupWikiWindow(wikiService, html, language, type);
          wdw.ShowAndActivate();
        }
      );
    }

    #region Methods Impl

    /// <inheritdoc />
    public override void ShowSettings()
    {
      Application.Current.Dispatcher.Invoke(
        () => new ConfigurationWindow(Config).ShowAndActivate()
      );
    }
    
    #endregion
  }
}
