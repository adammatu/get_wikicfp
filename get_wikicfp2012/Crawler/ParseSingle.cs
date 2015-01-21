using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Crawler
{
    public class ParseSingle
    {
        private CFPFilePaserItem item;
        static WebInput input = new WebInput();
        static UrlParser parser = new UrlParser();
        public static CFPStorageData storage = new CFPStorageData();

        public ParseSingle(CFPFilePaserItem item)
        {
            this.item = item;
        }

        public void Run()
        {
            download(item, item.Link, 0);
            //findCommittees(item, item.Link, 0);
        }

        public void download(CFPFilePaserItem item, string url, int level)
        {
            if (level > 2)
            {
                return;
            }
            Dictionary<string, string> text = input.GetPage(ref url,WebInputOptions.ForceDownload);
            if (text.Count == 0)
            {
                return;
            }
            foreach (string location in text.Keys)
            {
                Dictionary<string, string> urls = parser.ListUrls(location, text[location]);
                foreach (string _newUrl in urls.Keys)
                {
                    string newUrl = _newUrl;
                    string name = urls[newUrl].ToLower();
                    foreach (string w in CommitteeTagParser.linkWords)
                    {
                        if (name.Contains(w))
                        {
                            download(item, newUrl, level + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void findCommittees(CFPFilePaserItem item, string url, int level)
        {
            if (level > 2)
            {
                return;
            }
            Dictionary<string, string> text = input.GetPage(ref url);
            if (text.Count == 0)
            {
                return;
            }
            foreach (string location in text.Keys)
            {
                getCommiteeData(item, location);                
                Dictionary<string, string> urls = parser.ListUrls(location, text[location]);
                foreach (string _newUrl in urls.Keys)
                {
                    string newUrl = _newUrl;
                    string name = urls[newUrl].ToLower();
                    foreach (string w in CommitteeTagParser.linkWords)
                    {
                        if (name.Contains(w))
                        {
                            findCommittees(item, newUrl, level + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void getCommiteeData(CFPFilePaserItem item, string url)
        {   
            // date
            Dictionary<string, string> text = input.GetPage(ref url, WebInputOptions.IncludeVisited);
            foreach (string location in text.Keys)
            {
                DateTime date = DateTime.MinValue;
                if (text[location] != "")
                {
                    date = DateParser.findDate(text[location], item.ID);
                    Console.WriteLine("{0:yyyy.MM.dd}", date);
                }
                TagStructure tags = CommitteeTagParser.ParseCommitee(text[location]);
                tags.ScanNames(storage);
                tags.Print();
            }
        }
    }
}
