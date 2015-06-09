using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace get_wikicfp2012.Crawler
{
    public class GoogleScholarWordItem
    {
        public string Query { get; set; }
        public List<string> Words = new List<string>();
        public int Index { get; set; }
        public int Cite { get; set; }
        public string ID { get; set; }
    }

    public class GoogleScholar
    {
        private List<List<string>> allwords = new List<List<string>>();
        private WebInput web = new WebInput();
        Regex entryRegex = new Regex("(<div class=\"gs_r\">)(.*?)(</div></div></div>)", RegexOptions.Singleline);
        Regex citationRegex = new Regex("(href=\"/scholar\\?cites=)([0-9]+)(.*Cited by )([0-9]+)");
        Regex nameRegex = new Regex("(<h3 class=\"gs_rt\">)(.*?)(<a.*?>)(.*?)(</a>)(.*?)(</h3>)");

        Regex startRegex = new Regex("(start=)([0-9]+)");
        Regex queryRegex = new Regex("(q=)([a-z,+]+)");
        string removeChars = "\\/+()[]{}.,*:?0123456789";

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
                Console.WriteLine("reading: {0} ({1}%) {2}", n + 1, (100 * n / count), query);
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
            string url = String.Format("https://scholar.google.com/scholar?{0}q={1}{2}&hl=en&as_sdt=0%2C5",
                (page == 0) ? "" : String.Format("start={0}&", page * PAGE_SIZE),
                query,
                (page == 0) ? "&btnG=" : "");
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

        public List<GoogleScholarWordItem> GetNames(string path)
        {
            List<GoogleScholarWordItem> result = new List<GoogleScholarWordItem>();
            string query = "";
            try
            {
                query = (queryRegex.Matches(path))[0].Groups[2].Value;
            }
            catch
            {
            }
            int start = 0;
            try
            {
                start = Convert.ToInt32((startRegex.Matches(path))[0].Groups[2].Value);
            }
            catch
            {
            }
            int count = 0;
            string text = File.ReadAllText(path);
            foreach (Match entryMatch in entryRegex.Matches(text))
            {
                Match citeMatch = citationRegex.Match(entryMatch.Value);

                if (citeMatch.Groups.Count != 5)
                {
                    continue;
                }
                string id = citeMatch.Groups[2].Value;
                string cite = citeMatch.Groups[4].Value;

                Match match = nameRegex.Match(entryMatch.Value);
                if (match.Groups.Count != 8)
                {
                    continue;
                }
                count++;
                string name = match.Groups[4].Value;
                //remove tags
                while (name.Length > 0)
                {
                    int pos = name.IndexOf("<");
                    int pos2 = name.IndexOf(">", pos + 1);
                    if ((pos >= 0) && (pos2 >= 0))
                    {
                        name = name.Remove(pos, pos2 - pos + 1);
                    }
                    else
                    {
                        break;
                    }
                }
                //remove special chars
                while (name.Length > 0)
                {
                    int pos = name.IndexOf("&");
                    int pos2 = name.IndexOf(";", pos + 1);
                    if ((pos >= 0) && (pos2 >= 0))
                    {
                        name = name.Remove(pos, pos2 - pos + 1);
                    }
                    else
                    {
                        break;
                    }
                }
                //remove otcher chars
                foreach (char ch in removeChars)
                {
                    name = name.Replace(ch, ' ');
                }
                string[] words = name.ToLower().Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                result.Add(new GoogleScholarWordItem()
                {
                    Index = start + count,
                    Query = query,
                    Words = new List<string>(words),
                    Cite = Convert.ToInt32(cite),
                    ID = id
                });
            }
            return result;
        }

        public GoogleScholar GetNames()
        {
            string[] folders = { "scholar.google.com", "scholar.google.pl" };
            List<GoogleScholarWordItem> result = new List<GoogleScholarWordItem>();
            int fileCount = 0;
            foreach (string folder in folders)
            {
                foreach (string filename in Directory.EnumerateFiles(String.Format(Program.CACHE_ROOT + "cache\\{0}\\", folder)))
                {
                    fileCount++;
                    Console.WriteLine("{0}: {1}", fileCount, filename);
                    result.AddRange(GetNames(filename));
                }
            }
            using (StreamWriter sw = File.AppendText(Program.CACHE_ROOT + "google\\outputWords.txt"))
            {
                foreach (GoogleScholarWordItem item in result)
                {
                    foreach (string word in item.Words)
                    {
                        sw.WriteLine("{0} {1} {2} {3} {4}", item.Query, item.Index, word, item.ID, item.Cite);
                    }
                }
            }
            return this;
        }
    }
}