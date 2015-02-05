using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace get_wikicfp2012.Crawler
{
    public class GoogleScholar
    {
        private List<List<string>> allwords = new List<List<string>>();
        private WebInput web = new WebInput();
        Regex citationRegex = new Regex("(href=\"/scholar\\?cites=)([0-9]+)(.*Cited by )([0-9]+)");

        const int PAGE_SIZE = 10;

        public GoogleScholar LoadWords()
        {
            for (int n = 0; n < 3; n++)
            {
                List<string> words = new List<string>();
                string filename = String.Format("{0}google\\words{1}.txt", Program.CACHE_ROOT, n + 1);
                if (!File.Exists(filename))
                {
                    continue;
                }
                StreamReader file = new StreamReader(filename);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string _line = line.Trim().ToLower();
                    if (_line.Length > 0)
                    {
                        words.Add(_line);
                    }
                }
                allwords.Add(words);
            }

            return this;
        }

        public GoogleScholar GetAll(int count, int pages)
        {
            Random r = new Random();
            for (int n = 0; n < count; n++)
            {
                string query = "";
                foreach (List<string> words in allwords)
                {
                    query = String.Format("{0} {1}", query, words[r.Next(words.Count)]);
                }
                query = query.Trim();
                Console.WriteLine("reading: {0} ({1}%) {2}", n, (100 * n / count), query);
                for (int page = 0; page < pages; page++)
                {
                    if (!GetSingle(query, page))
                    {
                        Console.WriteLine("ERROR");
                        return this;
                    }
                    //wait
                    Thread.Sleep(1000 * (300 + r.Next(300)));
                }
            }
            return this;
        }

        private bool GetSingle(string query, int page, bool force = false)
        {
            if (force)
            {
                Console.WriteLine("force");
            }
            query = query.Replace(" ", "+");
            string url = String.Format("https://scholar.google.com/scholar?start={0}&q={1}&hl=en&as_sdt=0%2C5", page * PAGE_SIZE, query);
            int count = 0;
            using (StreamWriter sw = File.AppendText(Program.CACHE_ROOT + "google\\output.txt"))
            {
                foreach (string text in web.GetPage(ref url, (force) ? WebInputOptions.ForceDownload : WebInputOptions.IncludeVisited).Values)
                {
                    foreach (Match match in citationRegex.Matches(text))
                    {
                        if (match.Groups.Count != 5)
                        {
                            continue;
                        }
                        string id = match.Groups[2].Value;
                        string cite = match.Groups[4].Value;
                        count++;
                        sw.WriteLine("{0} {1} {2} {3}", query, id, page * PAGE_SIZE + count, cite);
                    }
                }
            }
            Console.WriteLine("found: {0}", count);
            if ((!force) && (count == 0))
            {
                return GetSingle(query, page, true);
            }
            return count > 0;
        }
    }
}