using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace get_wikicfp2012.Crawler
{
    class WebTools
    {
        public static string GetPage(string url)
        {
            /*
            string text;
            using (WebClient client = new WebClient())
            {
                text = client.DownloadString(url);
            }
            return text;
             */
            StringBuilder result = new StringBuilder();
            WebRequest req = WebRequest.Create(url);
            req.Timeout = 60 * 1000;
            using (Stream input = req.GetResponse().GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(input))
                {
                    string line = "";
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            result.AppendLine(line);
                        }
                    }
                }
            }
            return result.ToString();
        }

        public static Dictionary<string, string> GetUrls(string text, string prefix)
        {
            return GetUrls(text, prefix, false);
        }

        public static Dictionary<string, string> GetUrls(string text, string prefix, bool allowTags)
        {
            prefix = prefix.Replace("?", "\\?");
            Dictionary<string, string> result = new Dictionary<string, string>();
            Regex regex = new Regex((allowTags) ?
                "<a .*?href=\"(" + prefix + ".*?)\".*?>((.|\n)*?)</a>" :
                "<a href=\"(" + prefix + ".*?)\">(.*?)</a>",
                RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(text))
            {
                int i = 1;
                string name = RemoveTags(match.Groups[2].Value);
                while (result.ContainsKey(name))
                {
                    i++;
                    name = match.Groups[2].Value + "_" + i;
                }
                result.Add(name, match.Groups[1].Value);
            }
            return result;
        }

        public static string GetUrlByName(string text, string name)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            Regex regex = new Regex("<a .*?href=\"(.*?)\".*?>(" + name + ")</a>", RegexOptions.IgnoreCase);
            Match match = regex.Match(text);
            return (match.Success) ? match.Groups[2].Value : "";
        }

        public static string RemoveTags(string text)
        {
            Regex regex = new Regex("<(.|\n)*?>", RegexOptions.IgnoreCase);
            Regex regexScript1 = new Regex("<script([^>])*?/>", RegexOptions.IgnoreCase);
            Regex regexScript2 = new Regex("<script(.|\n)*?</script>", RegexOptions.IgnoreCase);
            string result = regex.Replace(regexScript2.Replace(regexScript1.Replace(text, ""), ""), "").Replace("\n", " ").Replace("\t", " ").Trim();
            int i = 0;
            while ((result.IndexOf("  ") >= 0) && (i < 10))
            {
                result = result.Replace("  ", " ");
                i++;
            }
            return result;
        }
    }
}