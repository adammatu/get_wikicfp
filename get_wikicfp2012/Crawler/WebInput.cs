﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;

namespace get_wikicfp2012.Crawler
{
    [Flags]
    public enum WebInputOptions { None=0,IncludeVisited=1,ForceDownload=2 }

    public class WebInput
    {
        List<string> visitedPages = new List<string>();
        static object visitedLock = new object();
        static object filesLock = new object();

        public void ClearVisited()
        {
            visitedPages.Clear();
        }

        public bool IsVisited(string url)
        {
            lock (visitedLock)
            {
                return visitedPages.Contains(url);
            }
        }

        public bool IsCached(string url)
        {
            if (String.IsNullOrEmpty(url))
            {
                return false;
            }
            while (DataStore.Instance.contains302(url))
            {
                url = DataStore.Instance.get302(url);
            }
            string result = "", folder;
            string cacheName = GetCacheName(url, out folder);
            return (File.Exists(cacheName));
        }

        public string GetPageCombined(ref string url)
        {
            return GetPageCombined(ref url, WebInputOptions.None);
        }

        public string GetPageCombined(ref string url, WebInputOptions options)
        {
            Dictionary<string, string> text = GetPage(ref url, options);
            string result = "";
            foreach (string _location in text.Keys)
            {
                string location = _location;
                result += " " + text[location];
            }
            return result;
        }

        public Dictionary<string, string> GetPage(ref string url)
        {
            return GetPage(ref url, WebInputOptions.None);
        }

        public Dictionary<string, string> GetPage(ref string url, WebInputOptions options)
        {
            return GetPage(ref url, new List<string>(), options);
        }

        private Dictionary<string, string> GetPage(ref string url, List<string> visited, WebInputOptions options)
        {
            Dictionary<string, string> text = GetSinglePage(ref url, visited, options);
            Dictionary<string, string> textCopy = new Dictionary<string, string>();
            foreach (string location in text.Keys)
            {
                textCopy.Add(location, text[location]);
            }
            Regex redirectMatch = new Regex("<frame.*?src=\"(.*?)\"", RegexOptions.IgnoreCase);
            foreach (string location in textCopy.Keys)
            {
                foreach (Match match in redirectMatch.Matches(textCopy[location]))
                {
                    string newLocation = match.Groups[1].Value;
                    if (!newLocation.Contains("://"))
                    {
                        try
                        {
                            Uri newUri = new Uri(new Uri(location), newLocation);
                            newLocation = newUri.AbsoluteUri;
                        }
                        catch
                        {
                        }
                    }
                    Dictionary<string, string> subText = GetSinglePage(ref newLocation, visited, options);
                    foreach (string addLocation in subText.Keys)
                    {
                        if (!text.ContainsKey(addLocation) && (subText[addLocation] != ""))
                        {
                            text.Add(addLocation, subText[addLocation]);
                        }
                    }
                }
            }
            return text;
        }

        private Dictionary<string, string> GetSinglePage(ref string url, List<string> visited, WebInputOptions options)
        {
            if (String.IsNullOrEmpty(url))
            {
                return new Dictionary<string, string>();
            }
            while ((url.IndexOf("://") > 0) && (url.IndexOf("://", url.IndexOf("://") + 3) > 0))
            {
                url = url.Substring(url.IndexOf("://") + 3).Trim();
            }
            visited.Add(url);
            while (DataStore.Instance.contains302(url))
            {
                url = DataStore.Instance.get302(url);
                if (visited.Contains(url))
                {
                    Console.WriteLine("LOOP: " + url);
                    return new Dictionary<string, string>();
                }
                visited.Add(url);
            }
            if (DataStore.Instance.contains404(url))
            {
                Console.WriteLine("404: " + url);
                return new Dictionary<string, string>();
            }
            if (((options & WebInputOptions.IncludeVisited) == 0) && IsVisited(url))
            {
                Console.WriteLine("V: " + url);
                return new Dictionary<string, string>();
            }
            string result = "";
            string folder;
            string cacheName = GetCacheName(url, out folder);
            bool exists;
            lock (filesLock)
            {
                exists = File.Exists(cacheName);
            }
            if (exists && ((options & WebInputOptions.ForceDownload) == 0))
            {
                lock (filesLock)
                {
                    Console.WriteLine("C: " + url);
                    result = File.ReadAllText(cacheName);
                }
            }
            else
            {
                Console.WriteLine("D: " + url);
                string encoding="";
                bool success = false;
                for (int retry = 0; retry<3; retry++)
                {
                    try
                    {
                        Uri uri = new Uri(url);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                        request.Method = "HEAD";
                        request.AllowAutoRedirect = false;
                        request.Timeout = 1000 * 60;
                        request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.94 Safari/537.36";
                        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

                        string location, statusCode, contentType;
                        using (var response = request.GetResponse() as HttpWebResponse)
                        {
                            location = response.GetResponseHeader("Location");
                            if (!location.Contains("://"))
                            {
                                try
                                {
                                    Uri newUri = new Uri(new Uri(url), location);
                                    location = newUri.AbsoluteUri;
                                }
                                catch
                                {
                                }
                            }
                            statusCode = response.GetResponseHeader("StatusCode");
                            contentType = response.ContentType;
                            encoding = response.CharacterSet;
                        }
                        if (!contentType.Contains("html"))
                        {
                            return new Dictionary<string, string>();
                        }
                        if ((statusCode != "OK") && (location != uri.OriginalString) && (location != ""))
                        {
                            DataStore.Instance.store302(url, location);
                            url = location;
                            if (visited.Contains(url))
                            {
                                Console.WriteLine("LOOP: " + url);
                                return new Dictionary<string, string>();
                            }
                            visited.Add(url);
                            return GetPage(ref url, visited, options);
                        }
                    }
                    catch (WebException wex1)
                    {
                        if (wex1.Status == WebExceptionStatus.ProtocolError)
                        {
                            DataStore.Instance.store404(url);
                            Console.WriteLine("E: " + url);
                            return new Dictionary<string, string>();
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex1)
                    {

                    }

                    try
                    {
                        result = WebTools.GetPage(url);
                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            DataStore.Instance.store404(url);
                            Console.WriteLine("E: " + url);
                            return new Dictionary<string, string>();
                        }

                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        return new Dictionary<string, string>();
                    }
                    success = true;
                    break;
                }
                if (!success)
                {
                    DataStore.Instance.store404(url);
                    return new Dictionary<string, string>();
                }
                /*
                if (!String.IsNullOrEmpty(encoding))
                {
                    Encoding iso = Encoding.GetEncoding(encoding);
                    Encoding utf = Encoding.UTF8;
                    string c1 = utf.GetString(iso.GetBytes(result));
                    string c2 = iso.GetString(utf.GetBytes(result));

                    result = utf.GetString(iso.GetBytes(result));
                }
                */
                try
                {
                    Directory.CreateDirectory(folder);
                    lock (filesLock)
                    {
                        using (StreamWriter sw = File.CreateText(cacheName))
                        {
                            sw.Write(result);
                        }
                    }
                }
                catch
                {
                }
            }
            lock (visitedLock)
            {
                visitedPages.Add(url);
            }
            Regex redirectMatch = new Regex("<meta[^\\>]*?REFRESH[^\\>]*?url=\"?(.*?)\"", RegexOptions.IgnoreCase);
            Match match = redirectMatch.Match(result);
            if (match.Success)
            {
                string location = match.Groups[1].Value;
                bool validTag = true;
                if (result.ToLower().Contains("<body"))
                {
                    validTag = match.Index < result.ToLower().IndexOf("<body");
                }
                if (validTag && (!String.IsNullOrEmpty(location)))
                {
                    if (!location.Contains("://"))
                    {
                        try
                        {
                            Uri newUri = new Uri(new Uri(url), location);
                            location = newUri.AbsoluteUri;
                        }
                        catch
                        {
                        }
                    }
                    if (url == location)
                    {
                        return new Dictionary<string, string>();
                    }
                    DataStore.Instance.store302(url, location);
                    url = location;
                    return GetPage(ref url, options);
                }
            }
            Dictionary<string, string> text = new Dictionary<string, string>();
            text.Add(url, result);
            return text;
        }


        private string GetCacheName(string url, out string folder)
        {
            folder = "";
            if (String.IsNullOrEmpty(url))
            {
                return "";
            }
            int pos = url.IndexOf("//");
            url = url.Substring(pos + 2);
            pos = url.IndexOf("/");
            string domain, name;
            if (pos < 0)
            {
                domain = url;
                name = "index";
            }
            else
            {
                domain = url.Substring(0, pos);
                name = url.Substring(pos + 1);
                if (name == "")
                {
                    name = "index";
                }
            }
            foreach (char ch in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(ch, '_');
                domain = domain.Replace(ch, '_');
            }
            folder = String.Format(Program.CACHE_ROOT + "cache\\{0}\\", domain);
            string result = String.Format(Program.CACHE_ROOT + "cache\\{0}\\{1}.html", domain, name);
            if (result.Length > 200)
            {
                result = result.Substring(0, 200);
            }
            return result;
        }
    }
}