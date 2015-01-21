using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Web;
using System.Globalization;

namespace get_wikicfp2012.Crawler
{
    public class FindResult
    {
        public int Found;
        public int ID;
        public string Name;
        public int UnivID;

        public FindResult()
        {
            Found = 0;
        }

        public FindResult(int ID, bool univ)
        {
            Found = 1;
            this.ID = ID;
            if (univ)
            {
                Name = CFPStorageData.univs[ID];
            }
            else
            {
                Name = CFPStorageData.items[ID];
            }
            UnivID = -1;
        }

        public override string ToString()
        {
            return String.Format("{0}: {1}", ID, Name);
        }
    }

    public class CFPStorageData
    {
        public static string[] removeWords = { "prof", "dr", "univ" };
        public static string[] affiliationWords = { "academy", "univer", "institute", "polite", "centr" };

        public static Dictionary<int, string> itemsOrg = new Dictionary<int, string>();
        public static Dictionary<int, string> items = new Dictionary<int, string>();
        public static Dictionary<int, string[]> itemsSplit = new Dictionary<int, string[]>();

        public static Dictionary<int, string> univs = new Dictionary<int, string>();
        public static Dictionary<int, string[]> univsSplit = new Dictionary<int, string[]>();
        public static HashSet<string> namesWords = new HashSet<string>();

        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public static string FixItem(string item)
        {
            item = HttpUtility.HtmlDecode(item).Trim();
            String normalizedString = item.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < normalizedString.Length; i++)
            {
                Char c = normalizedString[i];
                if ((CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    // && (Char.IsLetter(c) || c == ' ')
                    )
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        public void Initialize()
        {
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());                
                return;
            }
            Console.WriteLine("Connected");
            //
            string sql;
            SqlCommand command;
            SqlDataReader dr;
            //
            sql = "SELECT * from [dbo].[tblPerson]";
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                itemsOrg.Clear();
                items.Clear();
                itemsSplit.Clear();
                while (dr.Read())
                {
                    int id = Convert.ToInt32(dr["id"]);
                    string name = dr["name"].ToString();
                    itemsOrg.Add(id, name);
                    name = FixItem(name).ToLower();                    
                    items.Add(id, name);
                    itemsSplit.Add(id, Split(name));
                }
            }
            dr.Close();
            Console.WriteLine("merging");
            Dictionary<string, int> namesWordsCounts = new Dictionary<string, int>();
            foreach (string[] wordList in itemsSplit.Values)
            {
                foreach (string word in wordList)
                {
                    if (!namesWordsCounts.ContainsKey(word))
                    {
                        namesWordsCounts.Add(word, 1);
                    }
                    else
                    {
                        namesWordsCounts[word]++;
                    }
                }
            }
            foreach (string word in namesWordsCounts.Keys)
            {
                if (namesWordsCounts[word] > 1)
                {
                    namesWords.Add(word);
                }
            }
            /*
            Console.WriteLine("Found {0} items", items.Count);
            sql = "SELECT * from [dbo].[tblEvent] where [Type]=50";
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                univs.Clear();
                univsSplit.Clear();
                while (dr.Read())
                {
                    string name = dr["name"].ToString();
                    name = FixItem(name);
                    int id = Convert.ToInt32(dr["id"]);
                    univs.Add(id, name);
                    univsSplit.Add(id, Split(name));
                }
            }
            dr.Close();
            Console.WriteLine("Found {0} univs", univs.Count);
             */
            connection.Close();
        }

        private List<string> FixFoundItem(string item)
        {
            List<string> result = new List<string>();
            if (item.Contains(" and "))
            {
                int andpos = item.IndexOf(" and ");
                result.AddRange(FixFoundItem(item.Substring(andpos + 5)));
                item = item.Substring(0, andpos);
            }
            Regex affiliationMatch = new Regex("\\(.*(\\))?");
            Regex nbspMatch = new Regex("\\&nbsp;?");
            item = nbspMatch.Replace(item, " ");
            item = affiliationMatch.Replace(item, "");
            int pos = item.IndexOf("@");
            if (pos >= 0)
            {
                item = item.Substring(0, pos).Trim();
            }
            int pos1 = item.IndexOf(",");
            int pos2 = item.IndexOf(" - ");
            if (pos1 < 0)
            {
                pos = pos2;
            }
            else
            {
                if (pos2 < 0)
                {
                    pos = pos1;
                }
                else
                {
                    pos = Math.Min(pos1, pos2);
                }
            }
            if (pos >= 0)
            {
                string newItem = item.Substring(0, pos).Trim();
                if (newItem.Contains(" ") || newItem.Contains("."))
                {
                    result.AddRange(FixFoundItem(item.Substring(pos + 1)));
                    item = newItem;
                }
                else
                {
                    item = item.Substring(pos + 1) + " " + newItem;
                }
            }
            item = item.Replace(":", "").Replace(";", "").Replace("\t", " ");
            result.Insert(0, item);
            return result;
        }

        public static string[] Split(string name)
        {
            string[] words = name.Replace(".", ". ").Split(" ,-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();
            foreach (string word in words)
            {
                if ((word.EndsWith(".")) || ((word.Length > 1) && (Array.IndexOf(removeWords, word) < 0)))
                {
                    result.Add(word);
                }
            }
            return result.ToArray();
        }

        private int DistanceCompute(string word1, string word2)
        {
            if (String.IsNullOrEmpty(word1) || String.IsNullOrEmpty(word2))
            {
                return 100;
            }
            if (word1 == word2)
            {
                return 0;
            }
            if ((word1[0] != word2[0]) || (word1[word1.Length - 1] != word2[word2.Length - 1]))
            {
                return 100;
            }
            int common = 0;
            int index = 0;
            while ((index < word1.Length) && (index < word2.Length) && (word1[index] == word2[index]))
            {
                common++;
                index++;
            }
            index = 0;
            while ((index < word1.Length) && (index < word2.Length) && (word1[word1.Length - 1 - index] == word2[word2.Length - 1 - index]))
            {
                common++;
                index++;
            }
            return Math.Max(word1.Length, word2.Length) - common;
        }

        public List<FindResult> Find(string name)
        {
            List<FindResult> result = new List<FindResult>();
            List<string> names = FixFoundItem(FixItem(name));
            FindResult prev = null;
            string affiliation = "";
            foreach (string _name in names)
            {
                FindResult res = FindSingle(_name);
                if (res.Found > 0)
                {
                    result.Add(res);
                    prev = res;
                    affiliation = "";
                }
                /*
            else if (prev != null)
            {
                affiliation = affiliation + " " + _name;
                res = FindUniv(affiliation);
                if (res.Found > 0)
                {
                    prev.UnivID = res.ID;                        
                }
            }
                 */
            }
            return result;
        }

        public FindResult FindUniv(string name)
        {
            string[] words = Split(name);
            foreach (string word in words)
            {
                Console.Write(word + ",");
            }
            Console.WriteLine();
            if ((words.Length < 1) || (words.Length > 4))
            {
                return new FindResult();
            }

            KeyValuePair<int, string> result = new KeyValuePair<int, string>(-1, "");
            int points = 0;
            foreach (KeyValuePair<int, string> fitem in univs)
            {
                int _points = 0;
                string[] fwords = univsSplit[fitem.Key];
                bool firstPartMatch = true;
                foreach (string word in words)
                {
                    if (fwords.Contains(word))
                    {
                        _points += 5;
                    }
                    else
                    {
                        foreach (string fword in fwords)
                        {
                            if (word.EndsWith("."))
                            {
                                if (fword.StartsWith(word.Replace(".", "")))
                                {
                                    _points += 3;
                                }
                            }
                            else
                            {
                                int dist = DistanceCompute(word, fword);
                                int maxDist = 2 + ((word.Length > 4) ? 1 : 0);
                                if (dist == 1)
                                {
                                    _points++;
                                }
                                if (dist < maxDist)
                                {
                                    _points++;
                                    if (firstPartMatch)
                                    {
                                        firstPartMatch = false;
                                        _points++;
                                    }
                                }
                            }
                        }
                    }
                }
                if (points < _points)
                {
                    points = _points;
                    result = fitem;
                }
            }
            if (points > 5)
            {
                Console.WriteLine("+F: {0}", result.Value);
                return new FindResult(result.Key, true);
            }

            return new FindResult();

        }

        public FindResult FindSingle(string name)
        {
            name = name.Trim();
            if (name.Length == 0)
            {
                return new FindResult();
            }
            if (items.ContainsValue(name))
            {
                return new FindResult(items.Where(x => x.Value == name).Select(x => x.Key).FirstOrDefault(), false);
            }
            Dictionary<int, string> filtered = new Dictionary<int, string>();
            Console.Write("# ");
            string[] _words = Split(name);
            List<string> wordsList = new List<string>();
            foreach (string word in _words)
            {
                if ((!word.EndsWith(".")) && (word.ToUpper() == word))
                {
                    continue;
                }
                wordsList.Add(word.ToLower());
            }
            foreach (string word in wordsList)
            {
                Console.Write(word + ",");
            }
            Console.WriteLine();
            if ((wordsList.Count < 2) || (wordsList.Count > 4))
            {
                return new FindResult();
            }
            foreach (string word in wordsList)
            {
                foreach (string affWord in affiliationWords)
                {
                    if (word.StartsWith(affWord))
                    {
                        return new FindResult();
                    }
                }
            }
            string surname = wordsList[wordsList.Count - 1];
            foreach (KeyValuePair<int, string> item in items)
            {
                string[] fwords = itemsSplit[item.Key];
                string fword = fwords[fwords.Length - 1];
                if (filtered.ContainsKey(item.Key))
                {
                    continue;
                }
                if (fword == surname)
                {
                    filtered.Add(item.Key, item.Value);
                }
                else
                {
                    if (DistanceCompute(surname, fword) < 2)
                    {
                        filtered.Add(item.Key, item.Value);
                    }
                }
            }
            if (filtered.Count > 0)
            {
                KeyValuePair<int, string> result = new KeyValuePair<int, string>(-1, "");
                int points = 0;
                int missedCount = 1000;
                int matchedCount = 0;
                foreach (KeyValuePair<int, string> fitem in filtered)
                {
                    int _points = 0;
                    List<string> fwords = new List<string>(itemsSplit[fitem.Key]);
                    int fwordsRemoved = 0;
                    bool firstPartMatch = true;
                    int _matchedCount = 0;
                    foreach (string word in wordsList)
                    {
                        string matchedWord = "";
                        if (fwords.Contains(word))
                        {
                            matchedWord = word;
                            _points += 5;
                            _matchedCount++;
                        }
                        else
                        {
                            foreach (string fword in fwords)
                            {
                                if (matchedWord != "")
                                {
                                    break;
                                }
                                if (word.EndsWith("."))
                                {
                                    if (fword.StartsWith(word.Replace(".", "")))
                                    {
                                        matchedWord = fword;
                                        _points++;
                                        _matchedCount++;
                                    }
                                }
                                else if (fword.EndsWith("."))
                                {
                                    if (word.StartsWith(fword.Replace(".", "")))
                                    {
                                        matchedWord = fword;
                                        _points++;
                                        _matchedCount++;
                                    }
                                }
                                else if ((word.StartsWith(fword)) || (fword.StartsWith(word)))
                                {
                                    matchedWord = fword;
                                    _points += 2;
                                    _matchedCount++;
                                }
                                else
                                {
                                    if (word.EndsWith("."))
                                    {
                                        if (fword.StartsWith(word.Replace(".", "")))
                                        {
                                            matchedWord = fword;
                                            _points += 3;
                                            _matchedCount++;
                                        }
                                    }
                                    else
                                    {
                                        int dist = DistanceCompute(word, fword);
                                        if (dist == 1)
                                        {
                                            matchedWord = fword;
                                            _points++;
                                            _matchedCount++;
                                        }
                                        if (dist < 2)
                                        {
                                            _points++;
                                            if (firstPartMatch)
                                            {
                                                firstPartMatch = false;
                                                _points++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (matchedWord != "")
                        {
                            fwords.Remove(matchedWord);
                            fwordsRemoved++;
                        }
                    }

                    int _missedCount = Math.Max(wordsList.Count, fwords.Count + fwordsRemoved) - _matchedCount;
                    if ((missedCount > _missedCount) || ((points < _points) && (missedCount == _missedCount)))
                    {
                        points = _points;
                        result = fitem;
                        missedCount = _missedCount;
                        matchedCount = _matchedCount;
                    }
                }
                if (matchedCount > 1)
                {
                    Console.WriteLine("+F: {0}", result.Value);
                    return new FindResult(result.Key, false);
                }
            }
            return new FindResult();
        }

        public int FindByName(string name)
        {
            return itemsOrg.Where(x => x.Value == name).Select(x => x.Key).FirstOrDefault();
        }

    }
}