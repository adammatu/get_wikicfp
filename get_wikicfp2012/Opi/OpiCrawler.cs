using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.SqlClient;
using get_wikicfp2012.Crawler;

namespace get_wikicfp2012.Opi
{
    public class OpiCrawler
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public OpiCrawler()
        {
        }

        public OpiCrawler Connect()
        {
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());                
            }
            return this;
        }

        public void Close()
        {
            connection.Close();
        }

        private void AddPerson(string name, int id)
        {
            string sql;
            SqlCommand command = connection.CreateCommand();

            sql = String.Format("INSERT INTO [dbo].[tblPersonOPI] ([Name],[OPI]) VALUES ('{0}',{1})",
                name.Replace("'", "''"), id);
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private string LoadFile(string name)
        {
            string result = "";
            try
            {
                using (StreamReader sr = File.OpenText(name))
                {
                    result = sr.ReadToEnd();
                }
            }
            catch
            {
                result = "";
            }
            return result;
        }

        private bool IsError(string text)
        {
            return text.Contains("Wystąpił błąd aplikacji.");
        }

        private void ScanFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            string text = LoadFile(filename);
            if (String.IsNullOrEmpty(text))
            {
                return;
            }
            if (IsError(text))
            {
                return;
            }
            text = text.Replace("\n", " ").Replace("\r", " ");
            string name = GetName(text);
            int id = GetId(text);
            if (String.IsNullOrEmpty(name) || (id < 0))
            {
                return;
            }
            AddPerson(name, id);
        }

        private int GetId(string text)
        {
            int index = text.IndexOf("Id osoby:");
            if (index < 0)
            {
                return -1;
            }
            Regex regex = new Regex("(?<=(<td>)).*?(?=(</td>))");
            Match match = regex.Match(text, index);
            if (!match.Success)
            {
                return -1;
            }
            try
            {
                return Convert.ToInt32(match.Value);
            }
            catch
            {
                return -1;
            }
        }

        private string GetName(string text)
        {
            int index = text.IndexOf("table_ludzie");
            if (index < 0)
            {
                return "";
            }
            Regex regex = new Regex("(?<=(\\<b\\>)).*?(?=(\\<\\/b\\>))");
            Match match = regex.Match(text, index);
            if (!match.Success)
            {
                return "";
            }
            string result = match.Value;
            result = result.Replace("&nbsp;", " ").Replace("\t", " ");
            while (result.IndexOf("  ") > 0)
            {
                result = result.Replace("  ", " ");
            }
            return result.Trim();
        }

        public OpiCrawler ScanOPI(string filename)
        {
            filename = Program.CACHE_ROOT + filename;
            int count = 0;
            string[] dirs = Directory.GetDirectories(filename);
            foreach (string dir in dirs)
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    ScanFile(file);
                    if ((count % 1000) == 0)
                    {
                        Console.WriteLine("{0:HH:mm} {1}", DateTime.Now, count);
                    }
                    count++;
                }
            }
            return this;
        }

        public OpiCrawler MatchNames()
        {
            CFPStorageData storage = new CFPStorageData();
            storage.Initialize();

            List<string> names = new List<string>();
            string sql;
            SqlCommand command = connection.CreateCommand();

            sql = "SELECT * FROM [dbo].[tblPersonOPI]";
            command.CommandText = sql;

            SqlDataReader dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    string name = dr["Name"].ToString();
                    StringBuilder strb = new StringBuilder();
                    foreach (string word in name.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Char.IsUpper(word[0]))
                        {
                            strb.AppendFormat("{0} ", word);
                        }
                    }
                    name = strb.ToString();
                    name = String.Join(" ", CFPStorageData.Split(CFPStorageData.FixItem(name).ToLower()));
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
            }
            dr.Close();
            foreach (string name in names)
            {
                List<FindResult> result=storage.Find(name);
                if (result.Count == 0)
                {
                    continue;
                }
                else if (result.Count == 1)
                {
                    command = connection.CreateCommand();
                    sql = String.Format("UPDATE [dbo].[tblPerson] set [OPI]=1 where [ID]={0}",
                        result[0].ID);
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
                else
                {
                    int t = 0;
                }
            }
            return this;
        }
    }
}