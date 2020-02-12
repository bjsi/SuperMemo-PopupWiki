using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.PopupWiki
{

  [Serializable]
  public class Thumbnail
  {
    public string source { get; set; }
    public int width { get; set; }
    public int height { get; set; }
  }

  [Serializable]
  public class OriginalImage
  {
    public string source { get; set; }
    public int width { get; set; }
    public int height { get; set; }
  }

  [Serializable]
  public class Coordinates
  {
    public double lat { get; set; }
    public double lon { get; set; }
  }

  [Serializable]
  public class Summary
  {
    public string title { get; set; }
    public string displaytitle { get; set; }
    public int pageid { get; set; }
    public string extract { get; set; }
    public string extract_html { get; set; }
    public Thumbnail thumbnail { get; set; }
    public OriginalImage originalimage { get; set; }
    public string lang { get; set; }
    public string dir { get; set; }
    public string timestamp { get; set; }
    public string description { get; set; }
    public Coordinates coordinates { get; set; }
  }

  [Serializable]
  public class Page
  {
    public int pageid { get; set; }
    public int ns { get; set; }
    public string title { get; set; }
    public string extract { get; set; }
  }

  [Serializable]
  public class Query
  {
    public Page[] pages { get; set; }
  }

  [Serializable]
  // TESTING only some of the returned properties
  public class Extract
  {
    public Query query { get; set; }
  }
}
