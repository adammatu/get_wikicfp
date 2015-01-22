using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using get_wikicfp2012.Stats;
using System.IO;

namespace get_wikicfp2012.Score
{
    public class ScoreEvents
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public Dictionary<int, ScoreEventPersonData> people = new Dictionary<int, ScoreEventPersonData>();
        public Dictionary<int, ScoreEventData> events = new Dictionary<int, ScoreEventData>();

        public ScoreEvents Prepare()
        {
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //            
            connection.Open();
            //people
            command = connection.CreateCommand();
            command.CommandText = "select ID,StartYear from tblPerson";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int ID = Convert.ToInt32(dr["ID"]);
                    if (dr["StartYear"].GetType() == typeof(DBNull))
                    {
                        continue;
                    }
                    int startYear = Convert.ToInt32(dr["StartYear"]);
                    people.Add(ID, new ScoreEventPersonData
                        {
                            startYear = startYear
                        });
                }
            }
            dr.Close();
            Console.WriteLine("People done");
            //events
            command = connection.CreateCommand();
            command.CommandText = "select e.ID as ID, eg.Date as Date, eg.Type as Type from tblEvent e left join tblEventGroup eg on eg.ID=e.EventGroup_ID";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if (dr["Date"].GetType() == typeof(DBNull))
                    {
                        continue;
                    }
                    int ID = Convert.ToInt32(dr["ID"]);
                    DateTime date = Convert.ToDateTime(dr["Date"]);
                    //int type = Convert.ToInt32(dr["Type"]);
                    events.Add(ID, new ScoreEventData
                    {
                        Date = date
                    });
                }
            }
            dr.Close();
            Console.WriteLine("Events done");
            //links
            command = connection.CreateCommand();
            command.CommandText = "select * from tblLink";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int pID = Convert.ToInt32(dr["Person_ID"]);
                    int eID = Convert.ToInt32(dr["Event_ID"]);
                    if (events.ContainsKey(eID) && people.ContainsKey(pID))
                    {
                        events[eID].peopleLinks.Add(pID);
                        people[pID].eventLinks.Add(eID);
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Links done");
            //score
            command = connection.CreateCommand();
            command.CommandText = "select * from tblPersonScore";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int pID = Convert.ToInt32(dr["Person_ID"]);
                    int year = Convert.ToInt32(dr["Year"]);
                    double score = Convert.ToDouble(dr["Score"]);
                    int ccnt = Convert.ToInt32(dr["ConnectionCount"]);
                    people[pID].score.Add(year, score);
                    people[pID].connectionCount.Add(year, ccnt);
                }
            }
            dr.Close();
            Console.WriteLine("Score done");
            //            
            List<int> pKeys = people.Keys.ToList();
            foreach (int pID in pKeys)
            {
                if (people[pID].eventLinks.Count == 0)
                {
                    people.Remove(pID);
                }
            }
            //
            Console.WriteLine("Read End");
            return this;
        }

        public ScoreEvents Close()
        {
            connection.Close();
            return this;
        }

        public ScoreEvents CalculateAll()
        {
            foreach (int eID in events.Keys)
            {
                ScoreEventData ev = events[eID];
                int year = ev.Year;
                int cnt = ev.peopleLinks.Count;
                if (cnt < 2)
                {
                    continue;
                }
                double connections = (cnt * (cnt - 1)) / 2.0;
                //double strength = 1.0 / connections;
                double strength = 1.0 / cnt;
                double score = 0.0;
                foreach (int pIDl in ev.peopleLinks)
                {
                    double linkScore = (people[pIDl].score.ContainsKey(year - 1)) ? people[pIDl].score[year - 1] : 1;
                    int ccnt = year - people[pIDl].startYear;
                    //double transfer = 0.016706084 - 0.00015877 * ccnt;
                    double transfer = 0.01379;
                    double weightedScore = (1.0 + (linkScore - 1.0) * transfer) * strength;
                    score += weightedScore;
                }              
                ev.Score = score;
            }
            Console.WriteLine("Events done");

            return this;
        }

        public ScoreEvents SaveAll()
        {
            //
            Console.WriteLine("Save Start");
            SqlCommand command;
            //            
            //events
            command = connection.CreateCommand();
            foreach (int eID in events.Keys)
            {
                command.CommandText = String.Format("update tblevent set Score={0} where ID={1};",
                    String.Format("{0:0.0000}", events[eID].Score).Replace(",", "."),
                    eID);
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Save Done");
            return this;
        }


        public ScoreEvents CalculateAllNow(int year)
        {
            foreach (int eID in events.Keys)
            {
                ScoreEventData ev = events[eID];
                int cnt = ev.peopleLinks.Count;
                if (cnt < 2)
                {
                    continue;
                }
                double connections = (cnt * (cnt - 1)) / 2.0;
                //double strength = 1.0 / connections;
                double strength = 1.0 / cnt;
                double score = 0.0;
                foreach (int pIDl in ev.peopleLinks)
                {
                    double linkScore = (people[pIDl].score.ContainsKey(year - 1)) ? people[pIDl].score[year - 1] : 1;
                    int ccnt = year - people[pIDl].startYear;
                    //double transfer = 0.016706084 - 0.00015877 * ccnt;
                    double transfer = 0.01379;
                    double weightedScore = (1.0 + (linkScore - 1.0) * transfer) * strength;
                    score += weightedScore;
                }
                ev.ScoreNow = score;
            }
            Console.WriteLine("Events done");

            return this;
        }

        public ScoreEvents SaveAllNow()
        {
            //
            Console.WriteLine("Save Start");
            SqlCommand command;
            //            
            //events
            command = connection.CreateCommand();
            foreach (int eID in events.Keys)
            {
                command.CommandText = String.Format("update tblevent set ScoreNow={0} where ID={1};",
                    String.Format("{0:0.0000}", events[eID].ScoreNow).Replace(",", "."),
                    eID);
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Save Done");
            return this;
        }
    }
}