using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace get_wikicfp2012.Stats
{
    public class CareerLine
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        private Dictionary<int, CareerLinePerson> lines = new Dictionary<int, CareerLinePerson>();

        public CareerLine CrawlerFirst()
        {
            //
            Console.WriteLine("Start");
            string sql;
            SqlCommand command;
            SqlDataReader dr;
            //
            Dictionary<int, int> counts = new Dictionary<int, int>();
            sql = @"SELECT p.ID as pid, l.ID as lid, e.Type as etype
                    from [dbo].[tblPerson] p
                    left join [dbo].[tblLink] l on l.Person_ID = p.ID
                    left join [dbo].[tblEvent] e on e.ID = l.Event_ID                    
                    ";
            connection.Open();
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int id = Convert.ToInt32(dr["pid"]);
                    int etype = Convert.ToInt32(dr["etype"]);
                    if (etype == 17)
                    {
                        //www
                        continue;
                    }
                    if (counts.ContainsKey(id))
                    {
                        counts[id]++;
                    }
                    else
                    {
                        counts[id] = 1;
                    }
                }
            }
            dr.Close();
            Console.WriteLine("IDs listed");
            //
            foreach (int id in counts.Keys)
            {
                if (counts[id] > 2)
                {
                    CareerLinePerson item = new CareerLinePerson()
                    {
                        ID = id,
                        Level = 0
                    };
                    lines.Add(item.ID, item);
                }
            }
            Console.WriteLine("Lines created");
            //
            Dictionary<int, CareerLinkData> events = new Dictionary<int, CareerLinkData>();
            sql = @"select e.ID as eid,e.Type as etype,year(eg.Date) as eyear,eg.ID as egid 
                    from tblEvent e 
                    left join tblEventGroup eg on eg.ID=e.EventGroup_ID
                    where e.Type!=17";
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if (dr["eyear"] == DBNull.Value)
                    {
                        continue;
                    }
                    int eid = Convert.ToInt32(dr["eid"]);
                    int egid = Convert.ToInt32(dr["egid"]);
                    int etype = Convert.ToInt32(dr["etype"]);
                    int eyear = Convert.ToInt32(dr["eyear"]);
                    events.Add(eid, new CareerLinkData
                    {
                        ID = eid,
                        egID = egid,
                        type = etype,
                        year = eyear,
                        count = 0
                    });
                }
            }
            dr.Close();
            Console.WriteLine("Events read");
            //
            sql = @"SELECT Event_ID as eid from [dbo].[tblLink]";
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int eid = Convert.ToInt32(dr["eid"]);
                    if (events.ContainsKey(eid))
                    {
                        events[eid].count++;
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Events count read");
            //
            /*
            List<int> eventsIds=events.Keys.ToList();
            int index1 = 0;
            while (index1 < eventsIds.Count)
            {
                if (events[eventsIds[index1]].type >= 20)
                {
                    int index2 = index1 + 1;
                    while (index2 < eventsIds.Count)
                    {
                        if (events[eventsIds[index2]].type >= 20)
                        {
                            if (events[eventsIds[index1]].egID == events[eventsIds[index2]].egID)
                            {
                                events.Remove(eventsIds[index2]);
                                eventsIds.RemoveAt(index2);
                                continue;
                            }
                        }
                        index2++;
                    }
                }
                index1++;
            }
            Console.WriteLine("Events duplicates removed");
            */  
            //
            HashSet<string> pairs = new HashSet<string>();
            sql = @"SELECT Event_ID as eid, Person_ID as pid from [dbo].[tblLink]";
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int eid = Convert.ToInt32(dr["eid"]);
                    int pid = Convert.ToInt32(dr["pid"]);
                    if ((events.ContainsKey(eid)) && (lines.ContainsKey(pid)))
                    {
                        string key = String.Format("{0}|{1}", pid, eid);
                        if (pairs.Contains(key))
                        {
                            continue;
                        }
                        int year = events[eid].year - 1920;
                        year = (year < 0) ? 0 : ((year > 99) ? 99 : year);
                        if (events[eid].type < 20)
                        {
                            lines[pid].Years[year].publications++;
                            lines[pid].Years[year].publicationsConnections += events[eid].count;
                        }
                        else
                        {
                            lines[pid].Years[year].committees++;
                            lines[pid].Years[year].committeesConnections += events[eid].count;
                        }
                        pairs.Add(key);
                    }
                }
            }
            Console.WriteLine("All count read");
            //
            FileStorage<CareerLinePerson>.Save("line", 1, lines);
            return this;
        }

        private ushort Add(ushort v1, int c1, ushort v2, int c2)
        {
            return (ushort)(((int)v1 * c1 + (int)v2 * c2) / (c1 + c2));
        }

        private CareerLinePerson JoinLines(int maxId, CareerLinePerson line1, CareerLinePerson line2)
        {
            CareerLinePerson newLine = new CareerLinePerson();
            newLine.ID = maxId + 1;
            newLine.Length = (line1.Length > line2.Length) ? line1.Length : line2.Length;
            newLine.Level = line1.Level + line2.Level;
            for (int n = 0; n < 100; n++)
            {
                if (n < line1.Length)
                {
                    if (n < line2.Length)
                    {
                        newLine.Years[n].committees = Add(line1.Years[n].committees, line1.Level, line2.Years[n].committees, line2.Level);
                        newLine.Years[n].committeesConnections = Add(line1.Years[n].committeesConnections, line1.Level, line2.Years[n].committeesConnections, line2.Level);
                        newLine.Years[n].publications = Add(line1.Years[n].publications, line1.Level, line2.Years[n].publications, line2.Level);
                        newLine.Years[n].publicationsConnections = Add(line1.Years[n].publicationsConnections, line1.Level, line2.Years[n].publicationsConnections, line2.Level);
                    }
                    else
                    {
                        newLine.Years[n].committees = line1.Years[n].committees;
                        newLine.Years[n].committeesConnections = line1.Years[n].committeesConnections;
                        newLine.Years[n].publications = line1.Years[n].publications;
                        newLine.Years[n].publicationsConnections = line1.Years[n].publicationsConnections;
                    }
                }
                else
                {
                    if (n < line2.Length)
                    {
                        newLine.Years[n].committees = line2.Years[n].committees;
                        newLine.Years[n].committeesConnections = line2.Years[n].committeesConnections;
                        newLine.Years[n].publications = line2.Years[n].publications;
                        newLine.Years[n].publicationsConnections = line2.Years[n].publicationsConnections;
                    }
                    else
                    {
                        newLine.Years[n].committees = 0;
                        newLine.Years[n].committeesConnections = 0;
                        newLine.Years[n].publications = 0;
                        newLine.Years[n].publicationsConnections = 0;
                    }
                }
            }
            return newLine;
        }

        private ushort Limit(ushort value)
        {
            if (value > 500)
            {
                value = 500;
            }
            return (ushort)(value * 100);
        }

        private double LineDistance(CareerLinePerson line1, CareerLinePerson line2, int groupType)
        {
            int l = (line1.Length < line2.Length) ? line1.Length : line2.Length;
            double dist = 0;
            for (int n = 0; n < l; n++)
            {
                double d;
                if ((groupType == 0) || (groupType == 1))
                {
                    d = line1.Years[n].committees - line2.Years[n].committees;
                    dist = dist + d * d;
                }
                if ((groupType == 0) || (groupType == 2))
                {
                    d = line1.Years[n].publications - line2.Years[n].publications;
                    dist = dist + d * d;
                }
            }
            return dist;
        }

        public bool Covered()
        {
            List<CareerLinePerson> largest = new List<CareerLinePerson>();
            int smallestLargest = 0;
            int count = 0;
            foreach (CareerLinePerson line in lines.Values)
            {
                count += line.Level;
                if ((line.Level > smallestLargest) || (largest.Count < 10))
                {
                    largest.Add(line);
                    largest = largest.OrderByDescending(x => x.Level).Take(10).ToList();
                    smallestLargest = largest.Select(x => x.Level).Min();
                }
            }
            int covered = largest.Select(x => x.Level).Sum();
            return covered > 0.8 * count;
        }

        public CareerLine LimitSets(int groupType)
        {
            Console.WriteLine("Start");
            FileStorage<CareerLinePerson>.Load("line", 1, lines);
            Console.WriteLine("Loaded");
            foreach (int id in lines.Keys)
            {
                CareerLinePerson line = lines[id];
                int start = 0;
                while ((start < 100) && (line.Years[start].committees == 0) && (line.Years[start].publications == 0))
                {
                    start++;
                }
                if (start == 100)
                {
                    line.Length = 0;
                    continue;
                }
                line.Length = 100 - (2019 - 2012) - start;
                line.Level = 1;
                for (int n = 0; n < 100; n++)
                {
                    if (n < line.Length)
                    {
                        line.Years[n].committees = Limit(line.Years[start + n].committees);
                        line.Years[n].committeesConnections = Limit(line.Years[start + n].committeesConnections);
                        line.Years[n].publications = Limit(line.Years[start + n].publications);
                        line.Years[n].publicationsConnections = Limit(line.Years[start + n].publicationsConnections);
                    }
                    else
                    {
                        line.Years[n].committees = 0;
                        line.Years[n].committeesConnections = 0;
                        line.Years[n].publications = 0;
                        line.Years[n].publicationsConnections = 0;
                    }
                }
            }
            Console.WriteLine("Length counted");
            Console.WriteLine("Count left: {0}", lines.Count);
            List<int> linesIdsi = lines.Keys.ToList();
            int index = 0;
            while (index < linesIdsi.Count)
            {
                if (lines[linesIdsi[index]].Length < 3)
                {
                    lines.Remove(linesIdsi[index]);
                }
                index++;
            }
            Console.WriteLine("Empty Removed");
            Console.WriteLine("Count left: {0}", lines.Count);
            //
            double maxDist = 100000;            
            while (!Covered())
            {
                Console.WriteLine("MaxDist: {0}", maxDist);
                Dictionary<int, CareerLinePerson> buckets = new Dictionary<int, CareerLinePerson>();
                List<int> linesIdsb = lines.Keys.ToList();
                int maxIdb = linesIdsb.Max();
                DateTime start = DateTime.Now;
                while (linesIdsb.Count > 0)
                {
                    CareerLinePerson line = lines[linesIdsb[0]];
                    linesIdsb.RemoveAt(0);

                    double distb = (int)1E9;
                    int idb = -1;
                    foreach (int keyb in buckets.Keys)
                    {
                        double _dist = LineDistance(line, buckets[keyb], groupType);
                        if (_dist < distb)
                        {
                            distb = _dist;
                            idb = keyb;
                        }
                    }
                    if ((idb < 0) || (distb > maxDist))
                    {
                        buckets.Add(line.ID, line);
                    }
                    else
                    {
                        CareerLinePerson newLine = JoinLines(maxIdb, line, buckets[idb]);
                        buckets.Remove(idb);
                        buckets.Add(newLine.ID, newLine);
                        maxIdb++;
                    }
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        Console.WriteLine("Count left: {0} | Buckets:{1}", linesIdsb.Count,buckets.Count);
                        start = DateTime.Now;
                    }
                }
                lines.Clear();
                lines = buckets;
                Console.WriteLine("--------------------");
                Console.WriteLine("Count left: {0}", lines.Count);
                maxDist = maxDist * 1.2;
            }
            Console.WriteLine("Removed by bucketing");
            /*
            int removed = 1000;
            while (removed>10)
            {
                removed = 0;
                Console.WriteLine("Count left: {0}", lines.Count);
                List<int> linesIds = lines.Keys.ToList();
                int index1 = 0;
                int maxId = linesIds.Max();
                while (index1 < linesIds.Count)
                {
                    bool removedNow = false;
                    int index2 = index1 + 1;
                    while (index2 < linesIds.Count)
                    {
                        int dist = LineDistance(lines[linesIds[index1]],lines[linesIds[index2]]);                            
                        if (dist < 100)
                        {
                            CareerLinePerson newLine = JoinLines(maxId, lines[linesIds[index1]], lines[linesIds[index2]]);
                            lines.Remove(linesIds[index1]);                            
                            lines.Remove(linesIds[index2]);
                            int i1 = linesIds[index1];
                            int i2 = linesIds[index2];
                            linesIds.Remove(i1);
                            linesIds.Remove(i2);
                            lines.Add(newLine.ID, newLine);
                            maxId++;
                            removed++;
                            removedNow = true;
                            break;
                        }
                        index2++;
                    }
                    if (!removedNow)
                    {
                        index1++;
                    }
                }
            }
            Console.WriteLine("Close Removed");
            while (lines.Count > 10)
            {
                Console.WriteLine("Count left: {0}", lines.Count);
                List<int> linesIds = lines.Keys.ToList();
                int _index1 = 0, _index2 = 1;
                int _dist = (int)1E9;
                int index1 = 0;
                int maxId = 0;
                while (index1 < linesIds.Count)
                {
                    int index2 = index1 + 1;
                    while (index2 < linesIds.Count)
                    {
                        int dist = LineDistance(lines[linesIds[index1]],lines[linesIds[index2]]);
                        if (dist < _dist)
                        {
                            _index1 = index1;
                            _index2 = index2;
                            _dist = dist;
                        }
                        index2++;
                    }
                    if (linesIds[index1] > maxId)
                    {
                        maxId = linesIds[index1];
                    }
                    index1++;
                }
                CareerLinePerson newLine = JoinLines(maxId, lines[linesIds[_index1]], lines[linesIds[_index2]]);
                lines.Remove(linesIds[_index1]);
                lines.Remove(linesIds[_index2]);
                lines.Add(newLine.ID, newLine);
            }
             
            Console.WriteLine("Div 10");
            foreach (int id in lines.Keys)
            {
                CareerLinePerson line = lines[id];
                for (int n = 0; n < 100; n++)
                {
                    line.Years[n].committees = (ushort)(line.Years[n].committees / 10);
                    line.Years[n].committeesConnections = (ushort)(line.Years[n].committeesConnections / 10);
                    line.Years[n].publications = (ushort)(line.Years[n].publications / 10);
                    line.Years[n].publicationsConnections = (ushort)(line.Years[n].publicationsConnections / 10);
                }
            }
             */
            Console.WriteLine("Saving");
            FileStorage<CareerLinePerson>.Save("line", 10 + groupType, lines);
            return this;
        }

        public CareerLine CountLengths()
        {
            Console.WriteLine("Start");
            FileStorage<CareerLinePerson>.Load("line", 1, lines);
            Console.WriteLine("Loaded");
            Dictionary<int, CareerLineLength> lengths = new Dictionary<int, CareerLineLength>();
            foreach (int id in lines.Keys)
            {
                CareerLinePerson line = lines[id];
                int start, length;
                // all
                start = 0;
                while ((start < 100) && (line.Years[start].committees == 0) && (line.Years[start].publications == 0))
                {
                    start++;
                }
                if (start < 100)
                {
                    length = 100 - (2019 - 2012) - start;
                    if (!lengths.ContainsKey(length))
                    {
                        lengths.Add(length, new CareerLineLength()
                        {
                            Length = length,
                            Count = 0,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                    }
                    lengths[length].Count++;
                }
                // committees
                start = 0;
                while ((start < 100) && (line.Years[start].committees == 0))
                {
                    start++;
                }
                if (start < 100)
                {
                    length = 100 - (2019 - 2012) - start;
                    if (!lengths.ContainsKey(length))
                    {
                        lengths.Add(length, new CareerLineLength()
                        {
                            Length = length,
                            Count = 0,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                    }
                    lengths[length].CountCommittee++;
                }
                // publications
                start = 0;
                while ((start < 100) && (line.Years[start].publications == 0))
                {
                    start++;
                }
                if (start < 100)
                {
                    length = 100 - (2019 - 2012) - start;
                    if (!lengths.ContainsKey(length))
                    {
                        lengths.Add(length, new CareerLineLength()
                        {
                            Length = length,
                            Count = 0,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                    }
                    lengths[length].CountPublication++;
                }
            }
            FileStorage<CareerLineLength>.Save("line", 3, lengths);
            return this;
        }
    }
}