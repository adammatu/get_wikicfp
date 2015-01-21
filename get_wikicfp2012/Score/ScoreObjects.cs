using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace get_wikicfp2012.Score
{
    public class ScoreObjects
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public Dictionary<int, ScoreValuePerson> people = new Dictionary<int, ScoreValuePerson>();
        public Dictionary<int, ScoreValueEvent> events = new Dictionary<int, ScoreValueEvent>();
        double[] tieStrengthPublication;
        double[] tieStrengthConference;

        public ScoreObjects Prepare()
        {
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //            
            connection.Open();
            //people
            command = connection.CreateCommand();
            command.CommandText = "select ID from tblPerson";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int ID = Convert.ToInt32(dr["ID"]);
                    people.Add(ID, new ScoreValuePerson());
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
                    int year = Convert.ToDateTime(dr["Date"]).Year;
                    int type = Convert.ToInt32(dr["Type"]);
                    events.Add(ID, new ScoreValueEvent
                        {
                            startYear = year,
                            isConf = (type == 10)
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
                        events[eID].links.Add(pID);
                        people[pID].links.Add(eID);
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Events done");
            //
            tieStrengthPublication = Enumerable.Repeat(0.0, ScoreValueBase.YEAR_COUNT).ToArray();
            int[] tieStrengthPublicationCount = Enumerable.Repeat(0, ScoreValueBase.YEAR_COUNT).ToArray();
            tieStrengthConference = Enumerable.Repeat(0.0, ScoreValueBase.YEAR_COUNT).ToArray();
            int[] tieStrengthConferenceCount = Enumerable.Repeat(0, ScoreValueBase.YEAR_COUNT).ToArray();
            foreach (int eID in events.Keys)
            {
                int year = events[eID].startYear - ScoreValueBase.BASE_YEAR;
                if ((year < 0) || (year >= ScoreValueBase.YEAR_COUNT))
                {
                    continue;
                }
                double cnt = events[eID].links.Count;
                double val = (cnt * (cnt + 1)) / 2.0;
                if (events[eID].isConf)
                {
                    tieStrengthConference[year] += val;
                    tieStrengthConferenceCount[year]++;
                }
                else
                {
                    tieStrengthPublication[year] += val;
                    tieStrengthPublicationCount[year]++;
                }
            }
            for (int year = 0; year < ScoreValueBase.YEAR_COUNT; year++)
            {
                if (tieStrengthPublicationCount[year] == 0)
                {
                    continue;
                }
                double val = tieStrengthPublication[year] / tieStrengthPublicationCount[year];
                tieStrengthPublication[year] = 1.0 / val;
            }
            for (int year = 0; year < ScoreValueBase.YEAR_COUNT; year++)
            {
                if (tieStrengthConferenceCount[year] == 0)
                {
                    continue;
                }
                double val = tieStrengthConference[year] / tieStrengthConferenceCount[year];
                tieStrengthConference[year] = 1.0 / val;
            }
            //
            Console.WriteLine("Read End");
            return this;
        }

        private void RecalculatePeople()
        {
            foreach (int pID in people.Keys)
            {
                for (int year = ScoreValueBase.BASE_YEAR; year < ScoreValueBase.BASE_YEAR + ScoreValueBase.YEAR_COUNT; year++)
                {
                    double newValue = people[pID][year - 1];                    
                    foreach (int eID in people[pID].links)
                    {
                        if (events[eID].startYear == year)
                        {
                            //double tieStrength = (events[eID].isConf) ? tieStrengthConference[year - ScoreValueBase.BASE_YEAR] : tieStrengthPublication[year - ScoreValueBase.BASE_YEAR];
                            double tieStrength = (events[eID].links.Count < 2) ? 0 :
                                //    1.0 / ((double)(events[eID].links.Count * (events[eID].links.Count - 1)) / 2.0);
                                1.0 / (double)events[eID].links.Count;
                            newValue += events[eID][year] * tieStrength;                            
                        }
                    }
                    people[pID][year] = newValue;                    
                }
            }
        }

        private void RecalculateEvents()
        {
            foreach (int eID in events.Keys)
            {
                for (int year = ScoreValueBase.BASE_YEAR; year < ScoreValueBase.BASE_YEAR + ScoreValueBase.YEAR_COUNT; year++)
                {
                    double newValue = 0;
                    if (events[eID].startYear == year)
                    {
                        foreach (int pID in events[eID].links)
                        {
                            newValue += 1.0;
                            newValue += people[pID][year];
                        }
                    }
                    events[eID][year] = newValue;
                }
            }
        }

        private bool Changed(int year)
        {
            foreach (int pID in people.Keys)
            {
                if (people[pID].Changed(year))
                {
                    return true;
                }
            }
            foreach (int eID in events.Keys)
            {
                if (events[eID].Changed(year))
                {
                    return true;
                }
            }
            return false;
        }

        public ScoreObjects CalculateAll()
        {
            for (int n=0;n<10;n++)
            {
                RecalculatePeople();
                RecalculateEvents();
                Console.WriteLine("Step done");
            } 
            return this;
        }
    }
}
