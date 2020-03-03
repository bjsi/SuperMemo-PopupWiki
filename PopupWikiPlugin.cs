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
          GetPopupWiktionary,
          true
          );
    }

    public string GetSelectedText()
    {
      var ctrlGroup = Svc.SM.UI.ElementWdw.ControlGroup;
      var htmlCtrl = ctrlGroup?.FocusedControl.AsHtml();
      var htmlDoc = htmlCtrl?.GetDocument();
      var sel = htmlDoc?.selection;

      if (!(sel?.createRange() is IHTMLTxtRange textSel))
        return null;

      if (string.IsNullOrEmpty(textSel.text))
      {
        return null;
      }
      
      var filteredSelText = string.Concat(textSel.text
                                          .Where(c => !Char.IsPunctuation(c)))
                                  .Trim('\n', '\t', ' ', '\r');
      return filteredSelText;
    }

    public async void GetPopupWiktionary()
    {
      string selText = GetSelectedText();
      if (string.IsNullOrWhiteSpace(selText))
        return;

      string html = await wikiService.GetSearchResults(selText);
      Application.Current.Dispatcher.Invoke(
        () =>
        {
          var wdw = new PopupWikiWindow(wikiService, html, null, "wiktionary");
          wdw.ShowAndActivate();
        }
      );
    }

    public async void GetPopupWiki()
    {
      // GetWikiMobHtml redirects to search if non article found.

      string selText = GetSelectedText();

      if (string.IsNullOrWhiteSpace(selText))
        return;

      string html = await wikiService.GetWikiMobHtml(selText, Config.WikiLanguages.Split(',')[0]);

      Application.Current.Dispatcher.Invoke(
        () =>
        {
          var wdw = new PopupWikiWindow(wikiService, html, Config.WikiLanguages.Split(',')[0], "wikipedia");
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
