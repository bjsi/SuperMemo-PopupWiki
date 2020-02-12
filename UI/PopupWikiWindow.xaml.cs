using System.Windows.Input;
using System;


namespace SuperMemoAssistant.Plugins.PopupWiki.UI
{
  /// <summary>
  /// Interaction logic for PopupWikiWindow.xaml
  /// </summary>
  public partial class PopupWikiWindow
  {
    public PopupWikiWindow(PopupWikiService wikiService, string html)
    {
      InitializeComponent();
      Console.WriteLine("Browser navigating to:");
      Console.WriteLine(html);
      Browser.NavigateToString(html);
    }

    private void Window_KeyDown(object       sender,
                                KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.Enter:
        case Key.Escape:
          Close();
          break;
      }
    }
  }
}
