using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.PopupWiki
{
  // TODO: Make these checks safer.
  // TODO: Add a valid wiki language checker with an enum?
  public static class WikiUrlUtils
  {
    /// <summary>
    /// Returns true if url is a desktop wikipedia url else false.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsDesktopWikipediaUrl(string url)
    {
      if (!string.IsNullOrEmpty(url))
      {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
          Uri uri = new Uri(url);
          string[] splitUri = uri.DnsSafeHost.Split('.');
          if (splitUri != null && splitUri.Length >= 2)
          {
            if (splitUri[1] == "wikipedia")
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns true if the url is a desktop wiktionary url else false.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsDesktopWiktionaryUrl(string url)
    {
      if (!string.IsNullOrEmpty(url))
      {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
          Uri uri = new Uri(url);
          string[] splitUri = uri.DnsSafeHost.Split('.');
          if (splitUri != null && splitUri.Length >= 2)
          {
            if (splitUri[1] == "wiktionary")
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns true if the url is a mobile wikipedia url else false.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsMobileWikipediaUrl(string url)
    {
      if (!string.IsNullOrEmpty(url))
      {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
          Uri uri = new Uri(url);
          string[] splitUri = uri.DnsSafeHost.Split('.');
          if (splitUri != null && splitUri.Length >= 3)
          {
            if (splitUri[1] == "m")
            {
              if (splitUri[2] == "wikipedia")
              {
                return true;
              }
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Returns true if the url is a mobile wiktionary url else false.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsMobileWiktionaryUrl(string url)
    {
      if (!string.IsNullOrEmpty(url))
      {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
          Uri uri = new Uri(url);
          string[] splitUri = uri.DnsSafeHost.Split('.');
          if (splitUri != null && splitUri.Length >= 3)
          {
            if (splitUri[1] == "m")
            {
              if (splitUri[2] == "wiktionary")
              {
                return true;
              }
            }
          }
        }
      } 
      return false;
    }

    /// <summary>
    /// Attempts to convert an absolute desktop wiktionary link into a mobile wiktionary link.
    /// Returns null on failure.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string ConvDesktopWiktionaryToMob(string url)
    {
      if (IsDesktopWiktionaryUrl(url))
      {
        Uri uri = new Uri(url);
        string[] splitUri = uri.DnsSafeHost.Split('.');
        if (splitUri != null && splitUri.Length >= 2)
        {
          splitUri[1] += ".m";
          url = string.Join(".", splitUri);
        }
      }
      if (IsMobileWiktionaryUrl(url))
      {
        return url;
      }
      return null;
    }

    public static string ConvMobWiktionaryToDesktop(string url)
    {
      if (IsMobileWiktionaryUrl(url))
      {
        Uri uri = new Uri(url);
        string[] splitUri = uri.DnsSafeHost.Split('.');
        if (splitUri != null && splitUri.Length >= 2)
        {
          if (splitUri[1] == "m")
          {
            var desktop = url.Split('.').ToList();
            desktop.RemoveAt(1);
            url = string.Join(".", desktop);
          }
        }
      }
      if (IsDesktopWiktionaryUrl(url))
      {
        return url;
      }
      return null;
    }

    /// <summary>
    /// Attempts to convert a relative url to a desktop wikipedia url.
    /// Returns null on failure.
    /// TODO: How to make this safer?
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="relUrl"></param>
    /// <returns></returns>
    public static string ConvRelToAbsLink(string baseUrl, string relUrl)
    {
      if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(relUrl))
      {
        // UriKind.Relative will be false for rel urls containing #
        if (Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
        {
          if (baseUrl.EndsWith("/"))
          {
            baseUrl = baseUrl.TrimEnd('/');
          }

          if (relUrl.StartsWith("/") && !relUrl.StartsWith("//"))
          {
            if (relUrl.StartsWith("/wiki") || relUrl.StartsWith("/w/"))
            {
              return $"{baseUrl}{relUrl}";
            }
            return $"{baseUrl}/wiki{relUrl}";
          }
          else if (relUrl.StartsWith("./"))
          {
            if (relUrl.StartsWith("./wiki") || relUrl.StartsWith("./w/"))
            {
              return $"{baseUrl}{relUrl.Substring(1)}";
            }
            return $"{baseUrl}/wiki{relUrl.Substring(1)}";
          }
          else if (relUrl.StartsWith("#"))
          {
            return $"{baseUrl}/wiki/{relUrl}";
          }
        }
      }
      return null;
    }
  }
}
