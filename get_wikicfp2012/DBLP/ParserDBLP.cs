using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace get_wikicfp2012.DBLP
{
    enum ParserDBLPMode { NoStore, Parse, Reparse };
    class ParserDBLP
    {
        string[] tags = { "article", "inproceedings", "proceedings", "book", "incollection", "phdthesis", "mastersthesis", "www" };
        string[] authorTags = { "author", "editor" };
        string[] groupTags = { "booktitle", "journal" };

        public void Parse(string filename)
        {
            Parse(filename, ParserDBLPMode.Parse, 0);
        }

        public void Parse(string filename,ParserDBLPMode mode, int skipUntilArticle)
        {
            DataStoreDBLP store = new DataStoreDBLP();
            if (mode == ParserDBLPMode.Reparse)
            {
                store.PreLoad();
            }
            int article = 0;
            StreamReader file = new StreamReader(filename);
            string _line;
            List<string> authors = new List<string>();
            string title = "", group = "", groupTag = "", key = "", url = "";
            int year = 1900;
            int month = 1;
            int day = 1;
            string currentTag = "";
            while ((_line = file.ReadLine()) != null)
            {
                while (_line.Length > 0)
                {
                    string line = "";
                    int close1 = _line.IndexOf(">");
                    if (close1 < 0)
                    {
                        break;
                    }
                    int close1open = _line.LastIndexOf("<", close1);
                    if (close1open < 0)
                    {
                        break;
                    }
                    if (_line.Substring(close1open, 2) == "</")
                    {
                        line = _line.Substring(0, close1 + 1);
                        _line = _line.Substring(close1 + 1).Trim();
                    }
                    else
                    {
                        int close2 = _line.IndexOf(">", close1 + 1);
                        if (close2 < 0)
                        {
                            line = _line;
                            _line = "";
                        }
                        else
                        {
                            line = _line.Substring(0, close2 + 1);
                            _line = _line.Substring(close2 + 1).Trim();
                        }
                    }
                    if (currentTag == "")
                    {
                        foreach (string tag in tags)
                        {
                            if (line.StartsWith("<" + tag))
                            {
                                currentTag = tag;
                                authors.Clear();
                                title = "";
                                group = "";
                                groupTag = "";
                                url = "";
                                year = 1900;
                                month = 1;
                                day = 1;
                                if (line.Contains("key=\""))
                                {
                                    int start = line.IndexOf("key=\"");
                                    int end = line.IndexOf("\"", start + 5);
                                    key = line.Substring(start + 5, end - (start + 5) - 1);
                                }
                                else
                                {
                                    key = "";
                                }
                                if ((article % 10000) == 0)
                                {
                                    Console.WriteLine(String.Format("{0:HH:mm} : {1}", DateTime.Now, article));
                                }
                                article++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (line == ("</" + currentTag + ">"))
                        {
                            DateTime date = new DateTime(year, month, day);
                            int groupType = Array.IndexOf(groupTags, groupTag);

                            switch (mode)
                            {
                                case ParserDBLPMode.NoStore:
                                    {
                                        break;
                                    }
                                case ParserDBLPMode.Parse:
                                    {
                                        int eventId = store.AddEvent(title, group, groupType, Array.IndexOf(tags, currentTag), date, key, url, "");

                                        foreach (string author in authors)
                                        {
                                            int authorId = store.AddPerson(author);
                                            store.AddLink(authorId, eventId);
                                        }
                                        break;
                                    }
                                case ParserDBLPMode.Reparse:
                                    {
                                        if (article >= skipUntilArticle)
                                        {
                                            int eventId = store.UpdateEvent(title, group, groupType, Array.IndexOf(tags, currentTag), date, key, url, "");
                                            int linkCount = store.CountLinks(eventId);
                                            if (linkCount != authors.Count)
                                            {
                                                store.ClearLinks(eventId);
                                                foreach (string author in authors)
                                                {
                                                    int authorId = store.UpdatePerson(author);
                                                    store.AddLink(authorId, eventId);
                                                }
                                            }
                                        }
                                        break;
                                    }
                            }
                            //Console.WriteLine();

                            currentTag = "";
                        }
                        else
                        {
                            if (line.StartsWith("<title>"))
                            {
                                title = line.Replace("<title>", "").Replace("</title>", "").Trim();
                            }
                            if (line.StartsWith("<year>"))
                            {
                                year = Convert.ToInt32(line.Replace("<year>", "").Replace("</year>", "").Trim());
                            }
                            if (line.StartsWith("<month>"))
                            {
                                month = Array.IndexOf(
                                    System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en-gb").DateTimeFormat.MonthNames,
                                    line.Replace("<month>", "").Replace("</month>", "").Trim()) + 1;
                                if (month == 0)
                                {
                                    month = 1;
                                }
                            }
                            if (line.StartsWith("<url>"))
                            {
                                url = line.Replace("<url>", "").Replace("</url>", "").Trim();
                            }
                            foreach (string tag in authorTags)
                            {
                                if (line.StartsWith("<" + tag + ">"))
                                {
                                    authors.Add(line.Replace("<" + tag + ">", "").Replace("</" + tag + ">", "").Trim());
                                }
                            }
                            foreach (string tag in groupTags)
                            {
                                if (line.StartsWith("<" + tag + ">"))
                                {
                                    groupTag = tag;
                                    group = line.Replace("<" + tag + ">", "").Replace("</" + tag + ">", "").Trim();
                                }
                            }
                        }
                    }
                }
            }
            file.Close();
            store.Close();
        }

        public ParserDBLP ParseConf(string filename)
        {
            List<string> items = new List<string>();
            Regex regex = new Regex("(?<=(conf/)).*?(?=(/))");
            string _line = "";
            StreamReader file = new StreamReader(filename);
            while ((_line = file.ReadLine()) != null)
            {
                foreach (Match match in regex.Matches(_line))
                {
                    if (!items.Contains(match.Value))
                    {
                        Console.WriteLine(match.Value);
                        items.Add(match.Value);
                    }
                }
            }
            file.Close();

            SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return this;
            }

            SqlCommand cmd;
            foreach (string item in items)
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = String.Format("select count(*) from tblEventGroup where Type=10 and name like '{0} %' ", item);
                int cnt = Convert.ToInt32(cmd.ExecuteScalar());
                if (cnt > 0)
                {
                    Console.WriteLine("{0}: {1}", item, cnt);
                    cmd = connection.CreateCommand();
                    cmd.CommandText = String.Format("insert into tblConference (Name) values ('{0}'); SELECT @@IDENTITY ", item);
                    int id = Convert.ToInt32(cmd.ExecuteScalar());
                    cmd = connection.CreateCommand();
                    cmd.CommandText = String.Format("update tblEventGroup set Conference_ID={1} where Type=10 and name like '{0} %' ", item, id);
                    cmd.ExecuteNonQuery();
                }
            }
            connection.Close();

            return this;
        }

        public ParserDBLP UpdateLinks()
        {
            SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return this;
            }

            SqlCommand cmd;
            SqlDataReader dr;
            Dictionary<int, string> conf = new Dictionary<int, string>();
            cmd = connection.CreateCommand();
            cmd.CommandText = "select * from tblConference";
            dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    string name = dr["Name"].ToString();
                    int id = Convert.ToInt32(dr["id"]);
                    conf.Add(id, name);
                }
            }
            dr.Close();

            int total = 0;
            foreach (int confId in conf.Keys)
            {
                string key = String.Format("conf/{0}/", conf[confId]);
                cmd = connection.CreateCommand();
                cmd.CommandText = String.Format("update tblEvent set Conference_ID={0} where left([Key],{1})='{2}' ", confId, key.Length, key);
                int result = cmd.ExecuteNonQuery();
                total += result;
                Console.WriteLine("{0}: {1} [{2}]", conf[confId], result, total);

                string confKey = string.Format("conf/{0}/", conf[confId]);
            }

            connection.Close();

            return this;
        }
    }
}