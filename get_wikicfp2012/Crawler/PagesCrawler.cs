using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Globalization;


namespace get_wikicfp2012.Crawler
{
    enum PagesCrawlerOptions { None, SingleThreaded, PastEvents, PastEvents2, Load };

    class PagesCrawler
    {
        List<CFPFilePaserItem> items = new List<CFPFilePaserItem>();
        List<string> itemsVisited = new List<string>();
        object itemsLock = new object();
        WebInput input = new WebInput();
        string output;
        object outputLock = new object();
        string newConfFile;
        string newConfFile3;        
        List<Thread> threads = new List<Thread>();
        DateTime start = DateTime.Now;
        int threadsStarted = 0;

        private void LoadFile(string inputFile, bool skipNew, int list, bool visited)
        {
            lock (itemsLock)
            {
                if (!File.Exists(inputFile))
                {
                    return;
                }
                StreamReader file = new StreamReader(inputFile);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] lineItems = line.Split("\t".ToCharArray());
                    if (lineItems.Length != 3)
                    {
                        continue;
                    }
                    if (skipNew && lineItems[0].EndsWith("?"))
                    {
                        continue;
                    }
                    if (visited)
                    {
                        itemsVisited.Add(lineItems[2].Trim());
                    }
                    else
                    {
                        items.Add(new CFPFilePaserItem
                        {
                            ID = lineItems[0].Trim(),
                            Name = lineItems[1].Trim(),
                            Link = lineItems[2].Trim(),
                            List = list
                        });
                    }
                }
            }
        }

        public PagesCrawler ParseFile(string inputFile, string outputFile, bool skipNew)
        {
            newConfFile = Program.CACHE_ROOT + Path.ChangeExtension(inputFile, "2.csv");
            newConfFile3 = Program.CACHE_ROOT + Path.ChangeExtension(inputFile, "3.csv");
            output = Program.CACHE_ROOT + outputFile;
            //
            LoadFile(Program.CACHE_ROOT + inputFile, skipNew, 1, false);
            LoadFile(newConfFile, skipNew, 2, false);
            LoadFile(newConfFile3, skipNew, 3, false);
            Console.WriteLine("Loaded {0}", items.Count);
            LoadFile(ParseSingle.VISITED_FILE, skipNew, 1, true);
            Console.WriteLine("Loaded Visited {0}", itemsVisited.Count);
            return this;
        }

        private void StoreCFP(CFPFilePaserItem newItem,int list)
        {
            lock (itemsLock)
            {
                foreach (CFPFilePaserItem item in items)
                {
                    if (item.Link.Equals(newItem.Link))
                    {
                        return;
                    }
                }
                items.Add(newItem);
                if (list == 2)
                {
                    using (StreamWriter sw = File.AppendText(newConfFile))
                    {
                        sw.WriteLine("{0}\t{1}\t{2}", newItem.ID, newItem.Name, newItem.Link);
                    }
                }
                else if (list == 3)
                {
                    using (StreamWriter sw = File.AppendText(newConfFile3))
                    {
                        sw.WriteLine("{0}\t{1}\t{2}", newItem.ID, newItem.Name, newItem.Link);
                    }
                }
            }
        }

        public PagesCrawler ClearVisited()
        {
            input.ClearVisited();
            return this;
        }

        public PagesCrawler Action(PagesCrawlerOptions action)
        {
            ClearVisited();
            switch (action)
            {
                case PagesCrawlerOptions.PastEvents:
                    {
                        List<CFPFilePaserItem> itemsCopy = new List<CFPFilePaserItem>();
                        itemsCopy.AddRange(items);
                        foreach (CFPFilePaserItem item in itemsCopy)
                        {
                            if (item.List == 1)
                            {
                                RunInThreads(findPastEventsAndStoreUrlTest, item);
                            }
                        }
                        WaitForThreads();                        
                        break;
                    }
                case PagesCrawlerOptions.PastEvents2:
                    {
                        List<CFPFilePaserItem> itemsCopy = new List<CFPFilePaserItem>();
                        itemsCopy.AddRange(items);
                        foreach (CFPFilePaserItem item in itemsCopy)
                        {
                            findPastEventsAndStore(item);
                        }
                        WaitForThreads();
                        break;
                    }
                case PagesCrawlerOptions.SingleThreaded:
                    {
                        if (Program.LOAD_SQL)
                        {
                            ParseSingle.storage.Initialize();
                        }
                        CommitteeTagParser.Init();
                        List<CFPFilePaserItem> itemsCopy = new List<CFPFilePaserItem>();
                        itemsCopy = new List<CFPFilePaserItem>();
                        itemsCopy.AddRange(items);
                        Console.WriteLine("Total Count: {0}", itemsCopy.Count);
                        foreach (CFPFilePaserItem item in itemsCopy)
                        {
                            if (itemsVisited.Contains(item.Link))
                            {
                                continue;
                            }
                            RunInThreads(ParseEvent, item);
                            Thread.Sleep(500);
                        }
                        WaitForThreads();
                        break;
                    }
                case PagesCrawlerOptions.Load:
                    {
                        LoadIntoDatabase();
                        break;
                    }
            }
            return this;
        }

        private void RunInThreads(ParameterizedThreadStart target, CFPFilePaserItem item)
        {
            while (threads.Count >= Program.THREAD_COUNT)
            {
                int i = 0;
                while (i < threads.Count)
                {
                    if (threads[i].ThreadState == ThreadState.Stopped)
                    {
                        threads.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                Thread.Sleep(100);
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 5)
                {
                    start = DateTime.Now;
                    Console.WriteLine("Threads started: {0} current: {1}", threadsStarted, threads.Count);
                }
            }

            Thread thread = new Thread(target);
            thread.Start(item);
            threads.Add(thread);
            threadsStarted++;

            if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 5)
            {
                start = DateTime.Now;
                Console.WriteLine("Threads started: {0} current: {1}", threadsStarted, threads.Count);
            }
        }

        private void WaitForThreads()
        {
            while (threads.Count > 0)
            {
                int i = 0;
                while (i < threads.Count)
                {
                    if (threads[i].ThreadState == ThreadState.Stopped)
                    {
                        threads.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                Thread.Sleep(100);
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                {
                    start = DateTime.Now;
                    Console.WriteLine("Threads started: {0} current: {1}", threadsStarted, threads.Count);
                }
            }
        }

        private void ParseEvent(object _item)
        {
            CFPFilePaserItem item = (CFPFilePaserItem)_item;
            Console.WriteLine("reading: " + item.ID);
            new ParseSingle(item, itemsVisited.Contains(item.ID)).Run();
        }

        public void findPastEventsAndStoreUrlTest(object _item)
        {
            CFPFilePaserItem item = (CFPFilePaserItem)_item;
            foreach (CFPFilePaserItem newItem in findPastEvents(item, true))
            {
                StoreCFP(newItem, 2);
            }
        }

        public void findPastEventsAndStore(object _item)
        {
            CFPFilePaserItem item = (CFPFilePaserItem)_item;
            foreach (CFPFilePaserItem newItem in findPastEvents(item, false))
            {
                StoreCFP(newItem, 3);
            }
        }

        public List<CFPFilePaserItem> findPastEvents(CFPFilePaserItem item, bool urlTest)
        {
            List<CFPFilePaserItem> result = new List<CFPFilePaserItem>();
            Console.WriteLine("reading: " + item.ID);

            if (!input.IsCached(item.Link))
            {
                return result;
            }
            string url = item.Link;
            Dictionary<string, string> text = input.GetPage(ref url);
            if (text.Count == 0)
            {
                return result;
            }
            foreach (string location in text.Keys)
            {
                /*
                DateTime date = DateParser.findDate(text, item.ID);
                Console.WriteLine("{0:yyyy.MM.dd}", date);
                 */
                // good code  :)))
                if (urlTest)
                {
                    try
                    {
                        Regex yearMatch = new Regex("[0-9]{4}");
                        string yearConf = Convert.ToInt32(yearMatch.Match(item.ID).Value).ToString();
                        if (item.Link.Contains(yearConf))
                        {
                            for (int year = 2014, notFound = 0; (year > 1990) && (notFound < 4); year--)
                            {
                                string urlYear = item.Link.Replace(yearConf, year.ToString());
                                string newText = input.GetPageCombined(ref urlYear, WebInputOptions.IncludeVisited);
                                if (newText == "")
                                {
                                    notFound++;
                                }
                                else
                                {
                                    result.Add(new CFPFilePaserItem
                                    {
                                        ID = item.ID.Replace(yearConf, year.ToString()),
                                        Link = urlYear,
                                        Name = item.Name.Replace(yearConf, year.ToString())
                                    });
                                    notFound = 0;
                                    Console.WriteLine("Found {0}", year);
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                else
                {
                    getEventsData(item, item.Link);
                    Dictionary<string, string> urls = ParseSingle.parser.ListUrls(location, text[location]);
                    foreach (string _newUrl in urls.Keys)
                    {
                        string newUrl = _newUrl;
                        string name = urls[newUrl].ToLower();
                        foreach (string w in EventTagParser.eventsWords)
                        {
                            if (name.Contains(w))
                            {
                                Dictionary<string, string> pastEventsPage = input.GetPage(ref newUrl);
                                foreach (string newPage in pastEventsPage.Keys)
                                {
                                    result.AddRange(getEventsData(item, newPage));
                                    DateParser.countResults[2]++;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            Console.WriteLine("cc:{0} cr:{1} cp:{2}", DateParser.countResults[0], DateParser.countResults[1], DateParser.countResults[2]);
            return result;
        }

        public List<CFPFilePaserItem> getEventsData(CFPFilePaserItem item, string url)
        {
            //
            lock (outputLock)
            {
                using (StreamWriter sw = File.AppendText(output))
                {
                    sw.WriteLine(item.ID + " " + url);
                }
            }
            //
            List<CFPFilePaserItem> result = new List<CFPFilePaserItem>();
            Dictionary<string, string> text = ParseSingle.input.GetPage(ref url, WebInputOptions.IncludeVisited);
            foreach (string location in text.Keys)
            {
                result.AddRange(EventTagParser.ParseEvents(text[location], item.ID, location, output, outputLock));
            }
            //
            lock (outputLock)
            {
                using (StreamWriter sw = File.AppendText(output))
                {
                    sw.WriteLine("----");
                }
            }
            //
            return result;
        }

        private void LoadIntoDatabase()
        {
            Dictionary<string, int> confIDs = new Dictionary<string, int>();
            get_wikicfp2012.DBLP.DataStoreDBLP dataStore = new get_wikicfp2012.DBLP.DataStoreDBLP();

            int ccount = 0;
            string confName = "", confID = "", confLink = "";
            StreamReader file = new StreamReader(ParseSingle.OUTPUT_FILE);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                if (line[0] == '+')
                {
                    List<string> parts = new List<string>(line.Split('\t'));
                    while ((parts.Count > 1) && (Regex.Match(parts[1], "[0-9]+:[^\t]+").Length == 0))
                    {
                        parts[0] = String.Format("{0} {1}", parts[0], parts[1]).Trim();
                        parts.RemoveAt(1);
                    }
                    if (parts.Count > 1)
                    {
                        string[] parts0 = parts[0].Split("+[]()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        DateTime confDate = DateTime.ParseExact(parts0[parts0.Length - 2], "yyyy.MM.dd", CultureInfo.InvariantCulture);
                        int count = Convert.ToInt32(parts0[parts0.Length - 1]);
                        string committee = parts0[0];

                        string group = confID;
                        int pos = confID.LastIndexOf(" ");
                        if (pos >= 0)
                        {
                            group = group.Substring(0, pos);
                        }
                        string ID = String.Format("{0} {1:yyyy}", group, confDate);
                        string Name = String.Format("{0}|{1}", confName, committee);

                        int cID = dataStore.AddEvent(Name, confName, 10, 20, confDate, ID, "", confLink);

                        for (int i = 0; i < count; i++)
                        {
                            string[] partsN = parts[i + 1].Split(':');
                            if (partsN.Length==2)
                            {
                                int pID = Convert.ToInt32(partsN[0]);
                                dataStore.AddLink(pID, cID);
                            }
                        }
                    }
                }
                else
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length == 3)
                    {
                        confID = parts[0];
                        confName = parts[1];
                        confLink = parts[2];
                    }
                    Console.WriteLine("{0} {1}", ccount + 1, confName);
                    ccount++;
                }
            }
        }
    }
}