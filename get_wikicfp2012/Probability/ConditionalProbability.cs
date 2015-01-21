using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using get_wikicfp2012.Stats;
using System.IO;

namespace get_wikicfp2012.Probability
{
    public class ConditionalProbability
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);
        OpiChecker opc = new OpiChecker();
        double[] scoreLevels = new double[50];

        public ConditionalProbability Prepare()
        {   
            List<ConferenceEvent> resultPublication = new List<ConferenceEvent>();
            List<ConferenceEvent> resultConference = new List<ConferenceEvent>();
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //            
            connection.Open();
            command = connection.CreateCommand();
            command.CommandText = "select l.Person_ID as id, eg.Date as [date], eg.Type as egtype, e.Type as etype, e.Score as escore, e.Conference_ID as evID, eg.Conference_ID as evID2  from tbllink l " +                
                "left join tblEvent e on e.ID=l.Event_ID " +
                "left join tblEventGroup eg on eg.ID=e.EventGroup_ID ";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if (dr["date"] == DBNull.Value)
                    {
                        continue;
                    }
                    if (dr["evID"] != DBNull.Value)
                    {
                        int id = Convert.ToInt32(dr["id"]);
                        DateTime date = Convert.ToDateTime(dr["date"]);
                        int etype = Convert.ToInt32(dr["etype"]);
                        double escore = Convert.ToDouble(dr["escore"]);
                        int egtype = Convert.ToInt32(dr["egtype"]);
                        int evID = Convert.ToInt32(dr["evID"]);
                        if (egtype < 2 && etype < 20)
                        {
                            resultPublication.Add(new ConferenceEvent
                            {
                                ID = id,
                                IDevent = evID,
                                Created = date,
                                Score = escore
                            });
                        }
                    }
                    if (dr["evID2"] != DBNull.Value)
                    {
                        int id = Convert.ToInt32(dr["id"]);
                        DateTime date = Convert.ToDateTime(dr["date"]);
                        int etype = Convert.ToInt32(dr["etype"]);
                        double escore = Convert.ToDouble(dr["escore"]);
                        int egtype = Convert.ToInt32(dr["egtype"]);
                        int evID = Convert.ToInt32(dr["evID2"]);
                        if (egtype == 10 && etype == 30)
                        {
                            resultConference.Add(new ConferenceEvent
                            {
                                ID = id,
                                IDevent = evID,
                                Created = date,
                                Score = escore
                            });
                        }
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Read End");
            FileStorage<ConferenceEvent>.Save("event", 1, resultPublication);
            FileStorage<ConferenceEvent>.Save("event", 2, resultConference);
            Console.WriteLine("Saved");
            return this;
        }

        public ConditionalProbability Prepare2()
        {            
            List<ConferenceEvent> resultConference = new List<ConferenceEvent>();
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            command = connection.CreateCommand();
            command.CommandText = "select l.Person_ID as id, eg.[Date] as [date], eg.[Group] as [group], e.Score as escore from tbllink l " +                
                "left join tblEvent e on e.ID=l.Event_ID " +
                "left join tblEventGroup eg on eg.ID=e.EventGroup_ID "+
                "where eg.Type=10 ";
            dr = command.ExecuteReader();
            List<string> groups = new List<string>();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if (dr["date"] == DBNull.Value)
                    {
                        continue;
                    }
                    int id = Convert.ToInt32(dr["id"]);
                    DateTime date = Convert.ToDateTime(dr["date"]);
                    double escore = Convert.ToDouble(dr["escore"]);
                    string group = dr["group"].ToString();
                    if (!groups.Contains(group))
                    {
                        groups.Add(group);
                    }
                    resultConference.Add(new ConferenceEvent
                    {
                        ID = id,
                        IDevent = groups.IndexOf(group),
                        Created = date,
                        Score = escore
                    });
                }
            }
            dr.Close();
            Console.WriteLine("Read End");
            FileStorage<ConferenceEvent>.Save("event", 3, resultConference);            
            Console.WriteLine("Saved");
            return this;
        }

        private void Add(int[] table, int year)
        {
            if ((year < 2000) || (year > 2015))
            {
                return;
            }
            table[year - 2000]++;
        }

        private void AddNoShift(int[] table, int year)
        {
            if ((year < 0) || (year > 19))
            {
                return;
            }
            table[year]++;
        }

        private void AddScore(int[] table, double score)
        {
            if ((score < 0.0) || (score > scoreLevels[49]))
            {
                return;
            }
            int _score = 0;
            while (score > scoreLevels[_score])
            {
                _score++;
            }
            table[_score]++;
        }

        private void Print(Array table, StreamWriter sw)
        {
            StringBuilder s=new StringBuilder();
            for (int n = 0; n < table.Length; n++)
            {
                s.AppendFormat("{0} ",table.GetValue(n));
            }
            sw.WriteLine(s.ToString());
        }

        public void CalculateSingle(GroupBase group, StreamWriter sw)
        {
            Console.WriteLine(group.Name);
            sw.WriteLine(group.Name);
            group.Prepare();
            Console.WriteLine("prepare done");
            GroupResult first = group.GetFirst();
            Console.WriteLine("first done");
            GroupResult second = group.GetSecond(first);
            Console.WriteLine("second done");
            int countTotal = 0;
            int[] countTotalYearly = new int[20];
            int countMatch = 0;
            int[] countMatchYearly = new int[20];
            int countTotalOpi = 0;
            int[] countTotalOpiYearly = new int[20];
            int countMatchOpi = 0;
            int[] countMatchOpiYearly = new int[20];

            int[] distYears = new int[20];
            int[] distOpiYears = new int[20];
            int[] countScore = new int[50];
            int[] countOpiScore = new int[50];
            int[] totalScore = new int[50];
            int[] totalOpiScore = new int[50];
            foreach (int ID in first.Keys)
            {
                bool isOpi = opc.isOpi(ID);
                countTotal += first[ID].Count;
                if (isOpi)
                {
                    countTotalOpi += first[ID].Count;
                }
                if (second.ContainsKey(ID))
                {
                    foreach (int evID in first[ID].Keys)
                    {
                        Add(countTotalYearly, first[ID][evID].Date.Year);
                        AddScore(totalScore, first[ID][evID].Score);
                        if (isOpi)
                        {
                            Add(countTotalOpiYearly, first[ID][evID].Date.Year);
                            AddScore(totalOpiScore, first[ID][evID].Score);
                        }
                        if (second[ID].ContainsKey(evID))
                        {
                            if (first[ID][evID].Date < second[ID][evID].Date)
                            {
                                AddNoShift(distYears, second[ID][evID].Date.Year - first[ID][evID].Date.Year);
                                countMatch++;
                                Add(countMatchYearly, first[ID][evID].Date.Year);
                                AddScore(countScore, first[ID][evID].Score);
                                if (isOpi)
                                {
                                    AddNoShift(distOpiYears, second[ID][evID].Date.Year - first[ID][evID].Date.Year);
                                    countMatchOpi++;
                                    Add(countMatchOpiYearly, first[ID][evID].Date.Year);
                                    AddScore(countOpiScore, first[ID][evID].Score);
                                }
                            }
                        }
                    }
                }
            }
            sw.WriteLine("TOT {0} {1}", countTotal, countMatch);
            sw.WriteLine("OPI {0} {1}", countTotalOpi, countMatchOpi);
            Print(countTotalYearly, sw);
            Print(countMatchYearly, sw);
            Print(countTotalOpiYearly, sw);
            Print(countMatchOpiYearly, sw);
            sw.WriteLine("-shift");
            Print(distYears, sw);
            Print(distOpiYears, sw);
            sw.WriteLine("-score-total");
            Print(totalScore, sw);
            Print(totalOpiScore, sw);
            sw.WriteLine("-score");
            Print(countScore, sw);
            Print(countOpiScore, sw);
            sw.WriteLine("---");
        }

        public ConditionalProbability CalculateAll()
        {            
            //ScoreLevels            
            for (int i = 0; i < 50; i++)
            {
                scoreLevels[i] = (double)i / 25.0;
            }
            //OPI
            Console.WriteLine("OPI");
            opc.Load();
            Console.WriteLine("OPI done");
            //
            string filename = String.Format("{0}lines\\condprob.txt", Program.CACHE_ROOT);
            using (StreamWriter sw = File.CreateText(filename))
            {
                sw.AutoFlush = true;
                Print(scoreLevels, sw);
                CalculateSingle(new GroupPublishCommittee(), sw);
                CalculateSingle(new GroupMemberMember(), sw);
                CalculateSingle(new GroupMemberAMemberB(), sw);
                CalculateSingle(new GroupPublishAPublishB(), sw);
                CalculateSingle(new GroupPublishAMemberB(), sw);
                CalculateSingle(new GroupMemberAPublishB(), sw);
            }
            //
            return this;
        }
    }
}