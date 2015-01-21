using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace get_wikicfp2012.Crawler
{
    class DataStore
    {
        private DataStore()
        {
            load404();
            load302();
        }

        private static DataStore instance;
        private static object instanceLock = new object();

        public static DataStore Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new DataStore();
                    }
                    return instance;
                }
            }
        }

        private List<string> visitedPages404 = new List<string>();
        private object lock404 = new object();
        private Dictionary<string, string> visitedPages302 = new Dictionary<string, string>();
        private object lock302 = new object();

        private void load404()
        {
            lock (lock404)
            {
                try
                {
                    using (StreamReader file = new StreamReader(Program.CACHE_ROOT + "cache\\list404.csv"))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            visitedPages404.Add(line);
                        }
                    }
                    Console.WriteLine("Loaded 404");
                }
                catch
                {
                    Console.WriteLine("Loaded 404 not found");
                }
            }
        }

        public void store404(string url)
        {
            lock (lock404)
            {
                visitedPages404.Add(url);
                using (StreamWriter sw = File.AppendText(Program.CACHE_ROOT + "cache\\list404.csv"))
                {
                    sw.WriteLine(url);
                }
            }
        }

        private void load302()
        {
            lock (lock302)
            {
                try
                {
                    using (StreamReader file = new StreamReader(Program.CACHE_ROOT + "cache\\list302.csv"))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            string[] lineItems = line.Split("\t".ToCharArray());
                            if (lineItems.Length != 3)
                            {
                                continue;
                            }
                            visitedPages302.Add(lineItems[0], lineItems[1]);
                        }
                    }
                    Console.WriteLine("Loaded 302");
                }
                catch
                {
                    Console.WriteLine("Loaded 302 not found");
                }
            }
        }

        public void store302(string url, string newUrl)
        {
            lock (lock302)
            {
                if (newUrl == "")
                {
                    return;
                }
                if (visitedPages302.ContainsKey(url))
                {
                    return;
                }
                visitedPages302.Add(url, newUrl);
                using (StreamWriter sw = File.AppendText(Program.CACHE_ROOT + "cache\\list302.csv"))
                {
                    sw.WriteLine("{0}\t{1}", url, newUrl);
                }
            }
        }

        public bool contains404(string url)
        {
            lock (lock404)
            {
                return visitedPages404.Contains(url);
            }
        }

        public bool contains302(string url)
        {
            lock (lock302)
            {
                return visitedPages302.ContainsKey(url);
            }
        }

        public string get302(string url)
        {
            lock (lock302)
            {
                return visitedPages302[url];
            }
        }
    }
}