using System.Windows.Input;
using System;
using System.Windows.Navigation;


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
