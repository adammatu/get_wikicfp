using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace get_wikicfp2012.Crawler
{
    class EventTagParser
    {
        public static string[] eventsWords = { "past", "previous", "conferences", "about", "related" };
        public static int[] countResults = { 0, 0, 0 };

        public static List<CFPFilePaserItem> ParseEvents(string text, string ID, string url, string output, object outputLock)
        {
            text = text.Replace("\n", " ").Replace("\r", " ").Replace("&nbsp;", " ");
            int level = 0;
            Stack<string> tags = new Stack<string>();
            Stack<string> tagsFull = new Stack<string>();
            int pos = 0;
            int isBody = 0;
            bool isCommittee = false;
            int closingLevel = 0;
            int searchForLevel = 0;
            int eventsSectionStartPos = -1;
            List<CFPFilePaserItem> result = new List<CFPFilePaserItem>();
            while (pos < text.Length)
            {
                int tagPos = text.IndexOf("<", pos);
                if (tagPos < 0)
                {
                    break;
                }
                //
                string content = text.Substring(pos, tagPos - pos).Trim();
                string contentLower = content.ToLower().Trim();
                if (contentLower.Length > 0)
                {
                    bool found = false;
                    int wordCount = contentLower.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;
                    if (wordCount < 10)
                    {
                        foreach (string w in eventsWords)
                        {
                            if (contentLower.Contains(w))
                            {
                                isCommittee = false;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found && (searchForLevel == 0))
                    {
                        searchForLevel = 1;
                        closingLevel = tags.Count;
                    }
                    if (found && (isBody > 0))
                    {
                        bool isLink = false;
                        if (tags.Contains("a"))
                        {
                            foreach (string tagFull in tagsFull)
                            {
                                if (tagFull.StartsWith("a "))
                                {
                                    if (tagFull.Contains("href"))
                                    {
                                        isLink = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!isLink)
                        {
                            countResults[0]++;
                            Console.WriteLine(content);
                            //
                            lock (outputLock)
                            {
                                using (StreamWriter sw = File.AppendText(output))
                                {
                                    sw.WriteLine(content);
                                }
                            }
                            //
                            isCommittee = true;
                            if (eventsSectionStartPos < 0)
                            {
                                eventsSectionStartPos = pos;
                            }
                        }
                    }
                }
                //
                int tagEnd = text.IndexOf(">", tagPos);
                if (tagEnd < 0)
                {
                    break;
                }
                string tag = text.Substring(tagPos + 1, tagEnd - tagPos - 1).Trim().ToLower();
                if (tag.EndsWith("/"))
                {
                    pos = tagEnd + 1;
                    continue;
                }
                bool closing = tag.StartsWith("/");
                if (closing)
                {
                    tag = tag.Substring(1).Trim();
                }
                int spacePos = tag.IndexOf(" ");
                string tagName = ((spacePos < 0) ? tag : tag.Substring(0, spacePos)).Trim();
                if (!tagName.StartsWith("!") && (tagName != "img") && (tagName != "br"))
                {
                    if (closing)
                    {
                        if (tags.Contains(tagName))
                        {
                            while (tags.Pop() != tagName)
                            {
                            }
                            while (tags.Count < tagsFull.Count)
                            {
                                tagsFull.Pop();
                            }
                        }
                        /*
                        else
                        {
                            return;
                        }
                        */
                        if (tagName == "body")
                        {
                            isBody--;
                        }
                        if ((searchForLevel == 2) && (tags.Count < closingLevel))
                        {
                            isCommittee = false;
                            searchForLevel = 0;
                            if (eventsSectionStartPos > 0)
                            {
                                string eventsSection = text.Substring(eventsSectionStartPos, pos - eventsSectionStartPos + 1);
                                result.AddRange(ParseEventsSection(eventsSection, ID, url));
                                eventsSectionStartPos = -1;
                            }
                        }
                    }
                    else
                    {
                        tags.Push(tagName);
                        tagsFull.Push(tag);
                        if (tagName == "body")
                        {
                            isBody++;
                        }
                        if (searchForLevel == 1)
                        {
                            closingLevel = tags.Count - 1;
                            searchForLevel = 2;
                        }
                    }
                }
                pos = tagEnd + 1;
            }
            if (result.Count == 0)
            {
                result.AddRange(ParseEventsSection(text, ID, url));
            }
            return result;
        }

        private static List<CFPFilePaserItem> ParseEventsSection(string text, string ID, string url)
        {
            List<CFPFilePaserItem> result = new List<CFPFilePaserItem>();
            result.AddRange(ParseEventsSectionLinks(text, ID, url));
            result.AddRange(ParseEventsSectionLabels(text, ID, url));
            return result;
        }

        private static List<CFPFilePaserItem> ParseEventsSectionLabels(string text, string ID, string url)
        {
            List<CFPFilePaserItem> result = new List<CFPFilePaserItem>();
            Regex regex = new Regex("<a .*?href=\"(.*?)\".*?>((.|\n)*?)</a>", RegexOptions.IgnoreCase);
            Regex yearMatch = new Regex("[0-9]{4}");
            int pos = 0;
            while (pos < text.Length)
            {
                Match year = yearMatch.Match(text, pos);
                if (!year.Success)
                {
                    break;
                }
                int yearPos = year.Index;
                int yearInt = 0;
                try
                {
                    yearInt = Convert.ToInt32(year.Value);
                }
                catch
                {
                    yearInt = 0;
                }
                if ((yearInt < 1900) || (yearInt > 2100))
                {
                    pos = yearPos + 4;
                    continue;
                }
                Match link = regex.Match(text, yearPos + 4);
                if (!link.Success)
                {
                    break;
                }
                int linkPos = link.Index;
                Match nextYear = yearMatch.Match(text, yearPos + 4);
                if ((nextYear.Success) && (nextYear.Index < linkPos))
                {
                    pos = nextYear.Index - 1;
                    continue;
                }
                string name = WebTools.RemoveTags(link.Groups[2].Value);
                string location = link.Groups[1].Value;
                if (!location.StartsWith("#"))
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
                    if (UrlParser.CheckLink(location))
                    {
                        countResults[1]++;
                        Console.WriteLine("? {0} --> {1}", name, location);
                        result.Add(new CFPFilePaserItem
                        {
                            ID = ID + "?",
                            Name = name,
                            Link = location
                        });
                    }
                }
                pos = linkPos;
            }
            return result;
        }

        private static List<CFPFilePaserItem> ParseEventsSectionLinks(string text, string ID, string url)
        {
            List<CFPFilePaserItem> result = new List<CFPFilePaserItem>();
            Regex regex = new Regex("<a .*?href=\"(.*?)\".*?>((.|\n)*?)</a>", RegexOptions.IgnoreCase);
            Regex yearMatch = new Regex("[0-9]{4}");
            foreach (Match match in regex.Matches(text))
            {
                int i = 1;
                string name = WebTools.RemoveTags(match.Groups[2].Value);
                string location = match.Groups[1].Value;
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
                bool linkVerified = false;
                if (yearMatch.IsMatch(name))
                {
                    linkVerified = true;
                }
                foreach (string w in CommitteeTagParser.linkWords)
                {
                    if (name.Contains(w))
                    {
                        linkVerified = true;
                        break;
                    }
                }
                if (linkVerified)
                {
                    countResults[1]++;
                    Console.WriteLine("{0} --> {1}", name, location);
                    result.Add(new CFPFilePaserItem
                    {
                        ID = ID + "?",
                        Name = name,
                        Link = location
                    });
                }
            }

            return result;
        }
    }
}
