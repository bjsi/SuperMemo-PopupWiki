using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.PopupWiki
{

  [Serializable]
  public class Picture
  {
    public string source { get; set; }
    public int width { get; set; }
    public int height { get; set; }
  }

  [Serializable]
  public class Page
  {
    public int pageid { get; set; }
    public int ns { get; set; }
    public string title { get; set; }
    public string extract { get; set; }
    public Picture thumbnail { get; set; }
    public Picture original { get; set; }
    public string pageimage { get; set; }
    public string description { get; set; }
  }

  [Serializable]
  public class Query
  {
    public int[] pageids;
    public Page[] pages { get; set; }
  }

  [Serializable]
  public class MinimalistWiki
  {
    public Query query { get; set; }
  }

  [Serializable]
  public class Section
  {
    public string text;
  }

  [Serializable]
  public class MediumWiki
  {
    public string displaytitle;
    public string description;
    public Section[] sections;

    // On error
    public string title;
  }
}
