using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace get_wikicfp2012.DBLP
{
    class ParserDBLP
    {
        string[] tags = { "article", "inproceedings", "proceedings", "book", "incollection", "phdthesis", "mastersthesis", "www" };
        string[] authorTags = { "author", "editor" };
        string[] groupTags = { "booktitle", "journal" };

        public void Parse(string filename)
        {
            DataStoreDBLP store = new DataStoreDBLP();
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
                            int eventId = store.AddEvent(title, group, groupType, Array.IndexOf(tags, currentTag), date, key, url, "");

                            foreach (string author in authors)
                            {
                                int authorId = store.AddPerson(author);
                                store.AddLink(authorId, eventId);
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
    }
}