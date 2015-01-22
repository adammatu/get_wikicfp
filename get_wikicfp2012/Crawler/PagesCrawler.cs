using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;


namespace get_wikicfp2012.Crawler
{
    class PagesCrawler
    {
        List<CFPFilePaserItem> items = new List<CFPFilePaserItem>();
        object itemsLock = new object();
        WebInput input = new WebInput();
        string output;
        object outputLock = new object();
        string newConfFile;
        string newConfFile3;        
        List<Thread> threads = new List<Thread>();
        DateTime start = DateTime.Now;
        int threadsStarted = 0;

        private void LoadFile(string inputFile, bool skipNew, int list)
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

        public PagesCrawler ParseFile(string inputFile, string outputFile, bool skipNew)
        {
            newConfFile = Path.ChangeExtension(inputFile, "2.csv");
            newConfFile3 = Path.ChangeExtension(inputFile, "3.csv");
            output = Program.CACHE_ROOT + outputFile;
            //
            LoadFile(Program.CACHE_ROOT + inputFile, skipNew, 1);
            LoadFile(Program.CACHE_ROOT + newConfFile, skipNew, 2);
            LoadFile(Program.CACHE_ROOT + newConfFile3, skipNew, 3);
            Console.WriteLine("Loaded");
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

        public PagesCrawler Action()
        {
            if (Program.LOAD_SQL)
            {
                ParseSingle.storage.Initialize();
            }
            CommitteeTagParser.Init();
            List<CFPFilePaserItem> itemsCopy = new List<CFPFilePaserItem>();
            itemsCopy = new List<CFPFilePaserItem>();
            itemsCopy.AddRange(items);
            foreach (CFPFilePaserItem item in itemsCopy)
            {
                RunInThreads(ParseEvent, item);
                Thread.Sleep(500);
            }
            WaitForThreads();
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
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 5)
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
            new ParseSingle(item).Run();
        }
    }
}