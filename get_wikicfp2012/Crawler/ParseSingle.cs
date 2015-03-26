using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Crawler
{
    public class ParseSingle
    {
        private CFPFilePaserItem item;
        private bool markVisted;
        public static WebInput input = new WebInput();
        public static UrlParser parser = new UrlParser();
        public static CFPStorageData storage = new CFPStorageData();
        public static object fileLock = new object();

        public const string OUTPUT_FILE = Program.CACHE_ROOT + "cfp2\\committee.csv";
        public const string VISITED_FILE = Program.CACHE_ROOT + "cfp2\\list.visited.csv";

        public ParseSingle(CFPFilePaserItem item, bool markVisted)
        {
            this.item = item;
            this.markVisted = markVisted;
        }

        public void Run()
        {
            List<ParseSingleCommittee> result = findCommittees(item, item.Link, 0);
            if (!markVisted)
            {
                result = filterCommittees(result);
                printCommittees(item, result);
            }
        }

        public List<ParseSingleCommittee> findCommittees(CFPFilePaserItem item, string url, int level)
        {
            List<ParseSingleCommittee> result = new List<ParseSingleCommittee>();
            if (level > 2)
            {
                return result;
            }
            Dictionary<string, string> text = input.GetPage(ref url);
            if (text.Count == 0)
            {
                return result;
            }
            foreach (string location in text.Keys)
            {
                if (!markVisted)
                {
                    result.AddRange(getCommitteeData(item, location));
                }
                Dictionary<string, string> urls = parser.ListUrls(location, text[location]);
                foreach (string _newUrl in urls.Keys)
                {
                    string newUrl = _newUrl;
                    string name = urls[newUrl].ToLower();
                    foreach (string w in CommitteeTagParser.linkWords)
                    {
                        if (name.Contains(w))
                        {
                            result.AddRange(findCommittees(item, newUrl, level + 1));
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private List<ParseSingleCommittee> getCommitteeData(CFPFilePaserItem item, string url)
        {
            List<ParseSingleCommittee> result = new List<ParseSingleCommittee>();
            // date
            Dictionary<string, string> text = input.GetPage(ref url, WebInputOptions.IncludeVisited);
            foreach (string location in text.Keys)
            {
                DateTime date = DateTime.MinValue;
                if (text[location] != "")
                {
                    date = DateParser.findDate(text[location], item.ID);
                    //Console.WriteLine("{0:yyyy.MM.dd}", date);
                }
                TagStructure tags = CommitteeTagParser.ParseCommitee(text[location]);
                try
                {
                    tags.ScanNames(storage);
                }
                catch
                {
                    continue;
                }
                //tags.Print(true);
                result.Add(
                    new ParseSingleCommittee()
                    {
                        Date = date
                    }
                    );
                result.AddRange(getCommittees(tags, date));
            }
            return result;
        }

        private List<ParseSingleCommittee> getCommittees(TagStructure tags, DateTime date)
        {
            List<ParseSingleCommittee> result = new List<ParseSingleCommittee>();
            ParseSingleCommittee currentCommittee = null;
            Stack<TagStructure> tagStack = new Stack<TagStructure>();
            tagStack.Push(tags);
            while (tagStack.Count > 0)
            {
                TagStructure current = tagStack.Pop();
                current.children.Select(x => x).Reverse().ToList().ForEach(x => tagStack.Push(x));
                if ((current.isCommitee) || ((currentCommittee == null) && (current.authorsIdentified.Count > 0)))
                {
                    currentCommittee = new ParseSingleCommittee()
                    {
                        Name = (current.isCommitee) ? current.content : "",
                        Date = date
                    };
                    result.Add(currentCommittee);
                }
                foreach (FindResult author in current.authorsIdentified)
                {
                    if (!currentCommittee.Members.ContainsKey(author.ID))
                    {
                        currentCommittee.Members.Add(author.ID, author.Name);
                    }
                }
            }
            return result;
        }

        private List<ParseSingleCommittee> filterCommittees(List<ParseSingleCommittee> committees)
        {
            List<ParseSingleCommittee> result = new List<ParseSingleCommittee>();
            if (committees.Count == 0)
            {
                return result;
            }
            result.AddRange(committees);
            DateTime date = result.Select(x => x.Date).Where(x=>x>DateTime.MinValue).DefaultIfEmpty(DateTime.MinValue).Min();
            foreach(ParseSingleCommittee committee in result)
            {
                if (committee.Date==DateTime.MinValue)
                {
                    committee.Date = date;
                }
            }
            int i1 = 0;
            while (i1 < result.Count)
            {
                ParseSingleCommittee committee1 = result[i1];
                if (committee1.isEmpty())
                {
                    result.RemoveAt(i1);
                }
                else
                {
                    int i2 = i1 + 1;
                    while (i2 < result.Count)
                    {
                        ParseSingleCommittee committee2 = result[i2];
                        if (committee1.ShouldJoin(committee2))
                        {
                            committee1.Join(committee2);
                            result.RemoveAt(i2);
                        }
                        else
                        {
                            i2++;
                        }
                    }
                    i1++;
                }
            }
            return result;
        }

        private void printCommittees(CFPFilePaserItem item, List<ParseSingleCommittee> committees)
        {           
            lock (fileLock)
            {
                using (StreamWriter sw = new StreamWriter(VISITED_FILE, true))
                {
                    sw.WriteLine("{0}\t{1}\t{2}", item.ID, item.Name, item.Link);
                }
                if (committees.Count == 0)
                {
                    return;
                }
                using (StreamWriter sw = new StreamWriter(OUTPUT_FILE, true))
                {
                    sw.WriteLine("{0}\t{1}\t{2}", item.ID, item.Name, item.Link);
                    foreach (ParseSingleCommittee committee in committees)
                    {
                        StringBuilder line = new StringBuilder();
                        line.AppendFormat("+{0}({1:yyyy.MM.dd})[{2}]", committee.Name, committee.Date, committee.Members.Count);
                        foreach (int key in committee.Members.Keys)
                        {
                            line.AppendFormat("\t{0}:{1}", key, committee.Members[key]);
                        }
                        sw.WriteLine(line.ToString());
                    }
                }
            }
        }
    }
}