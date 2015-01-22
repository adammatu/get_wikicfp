using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace get_wikicfp2012.Crawler
{
    class CFPCrawler
    {
        public void CrawlList(string domain,string url)
        {
            Dictionary<string, string> categories =
                WebTools.GetUrls(
                    WebTools.GetPage(domain+url),
                    "/cfp/call?conference");
            Directory.CreateDirectory(Program.CACHE_ROOT + "rfp");
            foreach (string name in categories.Keys)
            {
                Console.WriteLine("{0}: {1}", name, categories[name]);
                CrawlCategory(name, domain, categories[name]);
            }
        }

        public void CrawlCategory(string name, string domain, string url)
        {
            CrawlCategory(name, domain, url, 1);
        }
         
        public void CrawlCategory(string name, string domain, string url,int pageIndex)
        {
            
            Console.WriteLine("page: " + pageIndex);
            Directory.CreateDirectory(Program.CACHE_ROOT + "rfp\\" + name);            
            string pageUrl = (pageIndex == 1) ? url : (url + "&page=" + pageIndex);
            string text = WebTools.GetPage(domain + pageUrl);
            Dictionary<string, string> pages =
                WebTools.GetUrls(
                    text,
                    "/cfp/servlet/event.showcfp");            
            foreach (string page in pages.Keys)
            {
                Console.Write(page+", ");
                CrawlPage(Program.CACHE_ROOT + "rfp\\" + name + "\\" + page + ".html", domain, pages[page]);
            }
            Console.WriteLine();
            if (pageIndex == 20)
            {
                return;
            }
            if (WebTools.GetUrlByName(text, "Next") != "")
            {                
                CrawlCategory(name, domain, url, pageIndex + 1);
            }
        }

        public void CrawlPage(string name, string domain, string url)
        {            
            int slash = name.IndexOf("\\");
            slash = name.IndexOf("\\", slash + 1);
            string namePrefix = name.Substring(0, slash + 1);
            name = name.Substring(slash + 1);
            foreach (char ch in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(ch, '_');
            }
            name = namePrefix + name;
            if (File.Exists(name))
            {
                return;
            }
            string page = WebTools.GetPage(domain + url);
            using (StreamWriter sw = File.CreateText(name))
            {
                sw.Write(page);
            }
            Thread.Sleep(3000);
        }
    }
}
