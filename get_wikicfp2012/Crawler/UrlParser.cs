using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace get_wikicfp2012.Crawler
{
    public class UrlParser
    {
        private static string[] skipExt = { "jpg", "jpeg", "png", "gif", "bmp", "pdf", "doc", "xls", "zip", "z", "tar", 
                               "gz", "rar", "wmv", "avi", "ps", "ppt", "mov", "avi", "mpg",
                           "exe","mpeg","wav"};
        private WebInput input = new WebInput();

        public Dictionary<string, string> ListUrls(string baseUrl, string text)
        {
            text = text.Replace("\n", " ").Replace("\r", " ").Replace("&nbsp;", " ");
            if (baseUrl.IndexOf("://") > 5)
            {
                baseUrl = "http://" + baseUrl;
            }
            Uri baseUri = new Uri(baseUrl);
            Dictionary<string, string> result = new Dictionary<string, string>();
            Regex regex = new Regex("<a .*?href=\"(.*?)\".*?>(.*?)</a>", RegexOptions.IgnoreCase);
            Regex regexAlt = new Regex("alt=\"(.*?)\"", RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                string name = WebTools.RemoveTags(match.Groups[2].Value);
                string url = match.Groups[1].Value.Trim();
                string alt = "";
                if (regexAlt.IsMatch(match.Value))
                {
                    alt = regexAlt.Match(match.Value).Groups[1].Value;
                }
                if (url.StartsWith("#"))
                {
                    continue;
                }
                if (!url.Contains("://"))
                {
                    try
                    {
                        Uri newUri = new Uri(baseUri, url);
                        url = newUri.AbsoluteUri;
                    }
                    catch
                    {
                        continue;
                    }
                }
                else
                {
                    try
                    {
                        Uri newUri = new Uri(url);
                        if (baseUri.Host != newUri.Host)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }              
                if (url.IndexOf("#") >= 0)
                {
                    url = url.Substring(0, url.IndexOf("#"));
                }
                if (!CheckLink(url))
                {
                    continue;
                }
                if (input.IsVisited(url))
                {
                    continue;
                }
                if (DataStore.Instance.contains404(url))
                {
                    continue;
                }
                if ((String.IsNullOrEmpty(url)) || (result.ContainsKey(url)))
                {
                    continue;
                }
                if (!String.IsNullOrEmpty(alt))
                {
                    name = String.Format("{0} [{1}]", name, alt);
                }
                result.Add(url, name);
            }
            return result;
        }

        public static bool CheckLink(string url)
        {
            if (url.StartsWith("mailto:") ||
                  url.StartsWith("skype:") ||
                  url.StartsWith("ftp:"))
            {
                return false;
            }
            if (url.LastIndexOf(".") >= 0)
            {
                string ext = url.Substring(url.LastIndexOf(".") + 1).ToLower();
                if (Array.IndexOf(skipExt, ext) >= 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}