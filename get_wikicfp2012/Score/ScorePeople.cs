using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using get_wikicfp2012.Stats;
using System.IO;

namespace get_wikicfp2012.Score
{
    public class ScorePeople
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public Dictionary<int, ScorePersonData> people = new Dictionary<int, ScorePersonData>();
        public Dictionary<int, ScorePersonEventData> events = new Dictionary<int, ScorePersonEventData>();
        private int tblPersonScoreID;

        public ScorePeople Prepare()
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
                    people.Add(ID, new ScorePersonData());
                }
            }
            dr.Close();
            Console.WriteLine("People done");
            //events
            command = connection.CreateCommand();
            command.CommandText = "select e.ID as ID, eg.Date as Date, eg.Type as Type, c.cnt as Cnt from tblEvent e left join tblEventGroup eg on eg.ID=e.EventGroup_ID left join ( select e.ID,count(*) as cnt from tblEvent e join tblReference r on r.Event_ID=e.ID group by e.ID ) c on c.ID=e.ID";
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
                    int cnt = (dr["Cnt"].GetType() == typeof(DBNull)) ? -1 : Convert.ToInt32(dr["cnt"]);
                    events.Add(ID, new ScorePersonEventData
                    {
                        Date = date,
                        citeCount = cnt
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
            foreach (int pID in people.Keys)
            {
                people[pID].startYear = people[pID].eventLinks.Select(x => events[x].Year).Min();
            }
            //
            Console.WriteLine("Read End");
            return this;
        }

        public ScorePeople Close()
        {
            connection.Close();
            return this;
        }

        public ScorePeople CalculateTranfers()
        {
            ScoreTransferStatList basedOnYear = new ScoreTransferStatList();
            ScoreTransferStatList basedOnConnections = new ScoreTransferStatList();
            Dictionary<int, ScoreTransferGroup> links = new Dictionary<int, ScoreTransferGroup>();
            int cnt = 0;
            int lastCnt = 0;
            DateTime started = DateTime.Now;
            DateTime start = DateTime.Now;
            int year = 0;
            foreach (ScorePersonEventData ev in events.Values.OrderBy(x => x.Date))
            {
                if (ev.Date.Year != year)
                {
                    Console.WriteLine("Year {0} done", year);
                    foreach (ScoreTransferGroup item in links.Values)
                    {
                        item.Count = item.Links.Count;
                    }
                    year = ev.Date.Year;
                }
                List<int> pIDs = ev.peopleLinks;
                for (int i1 = 0; i1 < pIDs.Count; i1++)
                {
                    for (int i2 = i1 + 1; i2 < pIDs.Count; i2++)
                    {
                        int pID1 = pIDs[i1];
                        int pID2 = pIDs[i2];
                        if (pID2 == pID1)
                        {
                            continue;
                        }
                        if (!links.ContainsKey(pID1))
                        {
                            links.Add(pID1, new ScoreTransferGroup());
                        }
                        if (!links.ContainsKey(pID2))
                        {
                            links.Add(pID2, new ScoreTransferGroup());
                        }
                        if (links[pID1].Links.ContainsKey(pID2))
                        {
                            continue;
                        }
                        foreach (int pID1Link in links[pID1].Links.Keys)
                        {
                            if (links[pID1Link].Links.ContainsKey(pID2))
                            {
                                int year0 = links[pID1].Links[pID1Link].Year;
                                int conn0 = links[pID1].Links[pID1Link].Connections;
                                if (links[pID2].Links[pID1Link].Connections < conn0)
                                {
                                    basedOnYear.AddCountSucess(year0);
                                    basedOnConnections.AddCountSucess(conn0);
                                }
                            }
                        }
                        int year1 = ev.Year - people[pID1].startYear;
                        int conn1 = links[pID1].Links.Count;
                        links[pID2].Links.Add(pID1, new ScoreTransferLink
                            {
                                Year = year1,
                                Connections = conn1
                            });
                        basedOnYear.AddCount(year1, conn1);
                        basedOnConnections.AddCount(conn1, conn1);
                        int year2 = ev.Year - people[pID2].startYear;
                        int conn2 = links[pID2].Links.Count;
                        links[pID1].Links.Add(pID2, new ScoreTransferLink
                        {
                            Year = year2,
                            Connections = conn2
                        });
                        basedOnYear.AddCount(year2, conn2);
                        basedOnConnections.AddCount(conn2, conn2);
                    }
                }
                cnt++;
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                {
                    int seconds = (int)(((TimeSpan)(DateTime.Now - start)).TotalSeconds * (events.Count - cnt) / (cnt - lastCnt));
                    Console.WriteLine("Count left: {0} | minutes left: {1}", events.Count - cnt, seconds / 60);
                    start = DateTime.Now;
                    lastCnt = cnt;
                }
            }
            FileStorage<ScoreTransferStat>.Save("trans", 1, basedOnYear);
            FileStorage<ScoreTransferStat>.Save("trans", 2, basedOnConnections);
            Console.WriteLine("Saved");
            return this;
        }

        public ScorePeople CalculateAll()
        {
            tblPersonScoreID = 0;
            int minYear = people.Values.Select(x => x.startYear).Min();
            //int minYear = 2010;
            for (int year = minYear; year <= Program.MAXYEAR; year++)
            {
                Console.WriteLine("Year {0} start", year);
                foreach (int pID in people.Keys)
                {
                    if ((people[pID].startYear - 1) > year)
                    {
                        continue;
                    }
                    people[pID].connectionCount.Add(year,
                        new ScorePersonCount()
                        {
                            connection = people[pID].peopleLinkScore.Count,
                            triangle = people[pID].triangleCount
                        });
                }
                Console.WriteLine("Year {0} init done", year);
                int pcnt = 0;
                int tpcnt = people.Values.Where(x => x.startYear <= year).Count();
                Console.WriteLine("Year {0} count {1}", year, tpcnt);
                DateTime started = DateTime.Now;
                DateTime start = DateTime.Now;
                foreach (int pID in people.Keys)
                {
                    if (people[pID].startYear > year)
                    {
                        continue;
                    }
                    List<ScorePersonEventData> yearEvents = people[pID].eventLinks.Select(x => events[x]).Where(x => x.Year == year).ToList();
                    double newScore = 0.0;
                    foreach (ScorePersonEventData ev in yearEvents)
                    {
                        int cnt = ev.peopleLinks.Count;
                        if (cnt < 2)
                        {
                            continue;
                        }
                        foreach (int pIDl in ev.peopleLinks)
                        {
                            if (pIDl == pID)
                            {
                                continue;
                            }
                            double linkScore = (people[pIDl].connectionCount.ContainsKey(year - 1)) ? people[pIDl].connectionCount[year - 1].score : 1;
                            int tri = (people[pIDl].connectionCount.ContainsKey(year - 1)) ? people[pIDl].connectionCount[year - 1].triangle : 0;
                            int ccnt = (people[pIDl].connectionCount.ContainsKey(year - 1)) ? people[pIDl].connectionCount[year - 1].connection : 0;
                            int tcnt = ccnt * (ccnt - 1) / 2;
                            double transfer = (tcnt >= 5) ? ((double)tri / tcnt) : 0;
                            double weightedScore = 1.0 + (linkScore - 1.0) * transfer;
                            if (people[pID].peopleLinkScore.ContainsKey(pIDl))
                            {
                                if (people[pID].peopleLinkScore[pIDl] < weightedScore)
                                {
                                    newScore += weightedScore - people[pID].peopleLinkScore[pIDl];
                                    people[pID].peopleLinkScore[pIDl] = weightedScore;
                                }
                            }
                            else
                            {
                                foreach (int fID in people[pID].peopleLinkScore.Keys)
                                {
                                    if (people[fID].peopleLinkScore.Keys.Contains(pIDl))
                                    {
                                        people[fID].triangleCount++;
                                    }
                                }
                                newScore += weightedScore;
                                people[pID].peopleLinkScore.Add(pIDl, weightedScore);
                            }
                        }
                    }
                    if (newScore > 100)
                    {
                        int k = 0;
                    }
                    double prevScore = (people[pID].connectionCount.ContainsKey(year - 1)) ? people[pID].connectionCount[year - 1].score : 1;
                    double score = prevScore + newScore;
                    people[pID].connectionCount[year].score = score;

                    pcnt++;
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        int seconds = (int)(((TimeSpan)(DateTime.Now - started)).TotalSeconds * (tpcnt - pcnt) / pcnt);
                        Console.WriteLine("Count left: {0} | minutes left: {1}", tpcnt - pcnt, seconds / 60);
                        start = DateTime.Now;
                    }
                }
                Console.WriteLine("Year {0} done", year);
                SaveYear(year);
                Console.WriteLine("Year {0} remove", year - 1);
                foreach (int pID in people.Keys)
                {
                    people[pID].connectionCount.Remove(year - 1);
                }
            }
            SaveStartYear();
            return this;
        }

        private void SaveStartYear()
        {
            //
            Console.WriteLine("Save Year Start");
            string filename = String.Format("{0}lines\\tblPersonStartYear.csv", Program.CACHE_ROOT);
            //         
            int id = 0;
            using (StreamWriter sw = File.AppendText(filename))
            {
                foreach (int pID in people.Keys)
                {
                    ScorePersonData person = people[pID];
                    sw.WriteLine("{0},{1},{2}",
                            ++id,
                            pID,
                            person.startYear);
                }
            }
        }

        private void SaveYear(int year)
        {
            //
            Console.WriteLine("Save Start");
            string filename = String.Format("{0}lines\\tblPersonScore.csv", Program.CACHE_ROOT);
            //         
            using (StreamWriter sw = File.AppendText(filename))
            {
                foreach (int pID in people.Keys)
                {
                    ScorePersonData person = people[pID];
                    if (year < person.startYear)
                    {
                        continue;
                    }
                    double score = person.connectionCount[year].score;
                    int ccnt = person.connectionCount[year].connection;
                    int tri = person.connectionCount[year].triangle;
                    sw.WriteLine("{0},{1},{2},{3},{4},{5}",
                        ++tblPersonScoreID,
                        pID,
                        year,
                        String.Format("{0:0.0000}", score).Replace(",", "."),
                        ccnt,
                        tri);

                }
            }
            Console.WriteLine("Save Done");
        }

        public ScorePeople BulkImport()
        {
            SqlCommand command;
            string filename = String.Format("{0}lines\\tblPersonScore.csv", Program.CACHE_ROOT);
            //
            command = connection.CreateCommand();
            Console.WriteLine("Bulk Import");
            command.CommandText = String.Format("bulk insert tblPersonScore from '{0}' with ( FIELDTERMINATOR = ',', ROWTERMINATOR = '\n' )", filename);
            Console.WriteLine(command.CommandText);
            //command.ExecuteNonQuery();            
            return this;
        }

        public ScorePeople CalculateHIndex()
        {
            Console.WriteLine("Count Start");
            foreach (ScorePersonData person in people.Values)
            {
                List<int> citeCnts = new List<int>();
                foreach (int id in person.eventLinks)
                {
                    if (events[id].citeCount >= 0)
                    {
                        citeCnts.Add(events[id].citeCount);
                    }
                }
                if (citeCnts.Count > 0)
                {
                    citeCnts = citeCnts.OrderByDescending(x => x).ToList();
                    int h = 0;
                    while ((h < citeCnts.Count) && (citeCnts[h] >= (h + 1)))
                    {
                        h++;
                    }
                    person.hIndex = h;
                }
                else
                {
                    person.hIndex = -1;
                }

            }
            Console.WriteLine("Count Done");
            return this;
        }

        public ScorePeople SaveHIndex()
        {
            Console.WriteLine("Save Start");
            SqlCommand command;
            //people
            command = connection.CreateCommand();
            foreach (int pID in people.Keys)
            {
                ScorePersonData person = people[pID];
                if (person.hIndex >= 0)
                {
                    command.CommandText = String.Format("update tblPerson set HIndex={0} where ID={1};", person.hIndex, pID);
                    command.ExecuteNonQuery();
                }
            }
            return this;
        }

    }
}