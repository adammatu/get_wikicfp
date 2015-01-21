using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace get_wikicfp2012.Export
{
    class ExportCSV
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public ExportCSV()
        {
        }

        public ExportCSV Open()
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

        private void Store(string path, string table, string[] columns, string where)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = String.Format("select * from {0} {1}", table, where);
            SqlDataReader dr = cmd.ExecuteReader();
            string filename = String.Format("{0}\\{1}.csv", path, table);
            using (StreamWriter sw = File.CreateText(filename))
            {
                bool first;
                first = true;
                foreach (string column in columns)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sw.Write("\t");
                    }
                    sw.Write(column);
                }
                sw.WriteLine();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        first = true;
                        foreach (string column in columns)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                sw.Write("\t");
                            }
                            string item;
                            if (dr[column].GetType()==DBNull.Value.GetType())
                            {                                
                                item="(null)";
                            }
                            else
                            {
                            item = dr[column].ToString();
                            if (dr[column] is DateTime)
                            {
                                item = String.Format("{0:yyyy-MM-dd}", dr[column]);
                            }
                            }
                            //item = item.Replace("|", " ");
                            sw.Write(item);
                        }
                        sw.WriteLine();
                    }
                }
            }
            dr.Close();
        }

        public ExportCSV Store(string path)
        {
            return Store(path, 0);
        }

        public ExportCSV Store(string path, int limit)
        {
            if (limit > 0)
            {
                path = path + limit;
            }
            Directory.CreateDirectory(path);
            Store(path, "tblConference", new string[] { "ID", "Name" }, Where("tblConference", limit));
            Store(path, "tblPerson", new string[] { "ID", "Name" }, Where("tblPerson", limit));            
            Store(path, "tblLink", new string[] { "ID", "Person_ID", "Event_ID" }, Where("tblLink", limit));
            Store(path, "tblEvent", new string[] { "ID", "EventGroup_ID", "Name", "Type", "Key", "Url", "Conference_ID" }, Where("tblEvent", limit));
            Store(path, "tblEventGroup", new string[] { "ID", "Name", "Type", "Date", "Url", "Conference_ID" }, Where("tblEventGroup", limit));
            Store(path, "tblPersonOPI", new string[] { "ID", "Name", "OPI" }, Where("tblPersonOPI", limit));
            return this;
        }

        public string Where(string table, int limit)
        {
            if (limit <= 0)
            {
                return "";
            }
            switch (table)
            {
                case "tblPerson":
                    {
                        return String.Format("where ID in (select top {0} ID from tblPerson)", limit);
                    }
                case "tblLink":
                    {
                        return String.Format("where Person_ID in (select top {0} ID from tblPerson)", limit);
                    }
                case "tblEvent":
                    {
                        return String.Format("where ID in (select Event_ID from tblLink where Person_ID in (select top {0} ID from tblPerson))", limit);
                    }
                case "tblEventGroup":
                    {
                        return String.Format("where ID in (select EventGroup_ID from tblEvent where ID in (select Event_ID from tblLink where Person_ID in (select top {0} ID from tblPerson)))", limit);
                    }
                case "tblPersonOPI":
                    {
                        return String.Format("where ID in (select top {0} ID from tblPerson)", limit);
                    }                    
            }
            return "";
        }

        public ExportCSV CountCommitteeSizes()
        {
            Dictionary<int, List<int>> sizes = new Dictionary<int, List<int>>();
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = 
                @"select year(eg.Date) as Year,eg.Name as GroupName ,e.Name,count(*) as Cnt from tblEventGroup eg 
                left join tblEvent e on e.EventGroup_ID=eg.ID
                left join tblLink l on l.Event_ID=e.ID
                where eg.Type=10
                group by year(eg.Date),eg.Name,e.Name";
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int year = Convert.ToInt32(dr["Year"]);
                    int cnt = Convert.ToInt32(dr["Cnt"]);
                    if (!sizes.ContainsKey(year))
                    {
                        sizes.Add(year, new List<int>());
                    }
                    sizes[year].Add(cnt);
                }
            }
            dr.Close();
            string filename = String.Format("{0}lines\\comsiz.txt", Program.CACHE_ROOT);
            using (StreamWriter sw = File.CreateText(filename))
            {
                sw.WriteLine("year|cnt|mean|sd|med|mad");
                foreach (int year in sizes.Keys)
                {
                    if ((year < 2000) || (year > 2012))
                    {
                        continue;
                    }
                    /*
                    int min = sizes[year].Min();
                    int max = sizes[year].Max();                    
                    double lo = min + (max - min) / 10;
                    double hi = max - (max - min) / 10;
                    List<int> items = sizes[year].Where(x => (x >= lo) && (x <= hi)).ToList();
                    */
                    List<int> items = sizes[year];
                    //items = items.Where(x => x > 1).ToList();
                    double mean1 = (double)items.Sum() / items.Count;
                    //items = items.Where(x => x < 10 * mean1).ToList();

                    int cnt = items.Count;
                    int margin = cnt / 20;
                    //items = items.OrderBy(x => x).Skip(margin).Take(cnt - 2 * margin).ToList();
                    items = items.OrderBy(x => x).Take(cnt - margin).ToList();

                    if (items.Count < 1)
                    {
                        continue;
                    }
                    double mean = (double)items.Sum() / items.Count;
                    double sd = items.Select(x => ((double)x - mean) * ((double)x - mean)).Sum();
                    int med = items.OrderBy(x => x).Skip(items.Count / 2).FirstOrDefault();
                    List<int> medl = items.Select(x => Math.Abs(x - med)).ToList();
                    int mad = medl.OrderBy(x => x).Skip(medl.Count / 2).FirstOrDefault();
                    sd = Math.Sqrt(sd / items.Count);
                    sw.WriteLine("{0}|{1}|{2}|{3}|{4}|{5}", year, items.Count, mean, sd, med, mad);
                }
            }
            return this;
        }
    }
}