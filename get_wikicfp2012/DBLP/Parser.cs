using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.SqlClient;

namespace get_wikicfp2012.DBLP
{
    class Parser
    {
        public Parser ParseConf(string filename)
        {
            List<string> items = new List<string>();
            Regex regex = new Regex("(?<=(\\<bht key=\\\"/db/conf/)).*?(?=(/))");
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

        public Parser UpdateLinks()
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

        public Parser ParseCite(string filename)
        {
            Console.WriteLine("File read start");
            Dictionary<int, ParserCiteItem> items = new Dictionary<int, ParserCiteItem>();
            ParserCiteItem currentItem = null;
            string _line = "";
            StreamReader file = new StreamReader(filename);
            while ((_line = file.ReadLine()) != null)
            {
                string line = _line.Trim();
                if (String.IsNullOrEmpty(line) || (currentItem == null))
                {
                    currentItem = new ParserCiteItem();
                }
                if (line.StartsWith("#*"))
                {
                    currentItem.Name = line.Substring(2);
                }
                if (line.StartsWith("#%"))
                {
                    int id = Convert.ToInt32(line.Substring(2));
                    currentItem.References.Add(id);
                }
                if (line.StartsWith("#index"))
                {
                    int id = Convert.ToInt32(line.Substring(6));
                    currentItem.ID = id;
                    items.Add(id, currentItem);
                }
            }
            file.Close();
            Console.WriteLine("File read end");
            Console.WriteLine("Loaded {0}", items.Count);
            Console.WriteLine("Refs {0}", items.Values.Select(x => x.References.Count).Sum());


            Console.WriteLine("DB read start");

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
            Dictionary<string, int> events = new Dictionary<string, int>();

            cmd = connection.CreateCommand();
            cmd.CommandText = "select * from tblEvent where Type<20";
            dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    string name = dr["Name"].ToString();
                    int id = Convert.ToInt32(dr["id"]);
                    if (!events.ContainsKey(name))
                    {
                        events.Add(name, id);
                    }
                }
            }
            dr.Close();
            Console.WriteLine("DB read end - loaded {0}", events.Count);

            int matched = 0;
            foreach (ParserCiteItem item in items.Values)
            {
                if (events.ContainsKey(item.Name))
                {
                    item.databaseID = events[item.Name];
                    matched++;
                }
                else
                {
                    item.databaseID = -1;
                }
            }
            Console.WriteLine("Matched {0} out of {1}", matched, items.Count);

            Console.WriteLine("Save Start");
            SqlCommand command;
            string filenameSave = String.Format("{0}lines\\tblReference.csv", Program.CACHE_ROOT);
            int linkId = 0;
            int paperFound = 0;
            using (StreamWriter sw = File.CreateText(filenameSave))
            {
                foreach (ParserCiteItem item in items.Values)
                {

                    if (item.databaseID >= 0)
                    {
                        paperFound++;
                        foreach (int link in item.References)
                        {
                            if (items.ContainsKey(link) && (items[link].databaseID >= 0))
                            {
                                sw.WriteLine("{0},{1},{2}",
                                           ++linkId,
                                           item.databaseID,
                                           items[link].databaseID
                                           );
                            }
                        }
                    }
                }

            }

            Console.WriteLine("Written papers:{0} refs:{1}", paperFound, linkId);
            connection.Close();
            return this;
        }
    }
}