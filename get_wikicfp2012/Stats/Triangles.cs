using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Globalization;

namespace get_wikicfp2012.Stats
{
    public class Triangles
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);
        OpiChecker opc = new OpiChecker();
        List<TriSingleLink> personLinks = new List<TriSingleLink>();

        public Triangles ScanLinks()
        {
            Console.WriteLine("OPI");
            OpiChecker opc = new OpiChecker();
            opc.Create();
            //
            Console.WriteLine("Start");
            string sql;
            SqlCommand command;
            SqlDataReader dr;
            //

            List<TriSingleLink> eventLinks = new List<TriSingleLink>();

            sql = @"select l.Person_ID as pid,l.Event_ID as eid,e.Type as etype,eg.Date as edate, e.Score as escore from tbllink l
                left join tblEvent e on e.ID=l.Event_ID
                left join tblEventGroup eg on eg.ID=e.EventGroup_ID";

            connection.Open();
            command = connection.CreateCommand();
            command.CommandText = sql;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int etype = Convert.ToInt32(dr["etype"]);
                    if (etype == 17)
                    {
                        //www
                        continue;
                    }
                    int pid = Convert.ToInt32(dr["pid"]);
                    int eid = Convert.ToInt32(dr["eid"]);
                    if (dr["edate"] == DBNull.Value)
                    {
                        continue;
                    }
                    DateTime edate = Convert.ToDateTime(dr["edate"]);
                    double escore = 0.0;
                    if (dr["escore"] != DBNull.Value)
                    {
                        escore = Convert.ToDouble(dr["escore"]);
                    }
                    eventLinks.Add(new TriSingleLink()
                    {
                        IDEvent = eid,
                        ID1 = pid,
                        Type = etype,
                        Created = edate,
                        Score = escore
                    });
                }
            }
            dr.Close();
            Console.WriteLine("Event Links listed - count {0}", eventLinks.Count());
            FileStorage<TriSingleLink>.Save("tri", 1, eventLinks);
            return this;
        }

        public Triangles EventToPerson()
        {
            List<TriSingleLink> eventLinks = new List<TriSingleLink>();
            List<TriSingleLink> personLinks = new List<TriSingleLink>();

            FileStorage<TriSingleLink>.Load("tri", 1, eventLinks);
            Console.WriteLine("Event Links loaded - count {0}", eventLinks.Count());
            Dictionary<int, List<TriSingleLink>> eventLinksGrouped = new Dictionary<int, List<TriSingleLink>>();
            foreach (TriSingleLink item in eventLinks)
            {
                if (!eventLinksGrouped.ContainsKey(item.IDEvent))
                {
                    eventLinksGrouped.Add(item.IDEvent, new List<TriSingleLink>());
                }
                eventLinksGrouped[item.IDEvent].Add(item);
            }
            eventLinks.Clear();
            Console.WriteLine("Event Links grouped - count {0}", eventLinksGrouped.Count());
            List<int> groupIDs = eventLinksGrouped.Keys.ToList();
            foreach (int id in groupIDs)
            {
                if (eventLinksGrouped[id].Count < 2)
                {
                    eventLinksGrouped.Remove(id);
                }
            }
            Console.WriteLine("Event Links empty removed - count {0}", eventLinksGrouped.Count());

            Dictionary<int, Dictionary<int, TriSingleLink>> personLinksGrouped = new Dictionary<int, Dictionary<int, TriSingleLink>>();
            int cnt = 0;
            DateTime started = DateTime.Now;
            DateTime start = DateTime.Now;
            foreach (int id in eventLinksGrouped.Keys)
            {
                List<TriSingleLink> items = eventLinksGrouped[id];
                if (items.Count < 2)
                {
                    continue;
                }
                int id1 = 0;
                while (id1 < items.Count)
                {
                    int id2 = id1 + 1;
                    while (id2 < items.Count)
                    {
                        int pid1 = items[id1].ID1;
                        int pid2 = items[id2].ID1;
                        if (pid1 == pid2)
                        {
                            id2++;
                            continue;
                        }
                        if (pid1 > pid2)
                        {
                            int t = pid1;
                            pid1 = pid2;
                            pid2 = t;
                        }
                        if (!personLinksGrouped.ContainsKey(pid1))
                        {
                            personLinksGrouped.Add(pid1, new Dictionary<int, TriSingleLink>());
                        }
                        if (personLinksGrouped[pid1].ContainsKey(pid2))
                        {
                            TriSingleLink link = personLinksGrouped[pid1][pid2];
                            if (link.Created > items[0].Created)
                            {
                                link.Created = items[0].Created;
                                link.Type = items[0].Type;
                                link.IDEvent = items[0].IDEvent;
                            }
                        }
                        else
                        {
                            personLinksGrouped[pid1].Add(pid2, new TriSingleLink()
                            {
                                ID1 = pid1,
                                ID2 = pid2,
                                Type = items[0].Type,
                                Created = items[0].Created,
                                IDEvent = items[0].IDEvent
                            });
                        }
                        id2++;
                    }
                    id1++;
                }
                cnt++;
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                {
                    int seconds = (int)(((TimeSpan)(DateTime.Now - started)).TotalSeconds * (eventLinksGrouped.Count - cnt) / cnt);
                    Console.WriteLine("Count left: {0} | minutes left: {1}", eventLinksGrouped.Count - cnt, seconds / 60);
                    start = DateTime.Now;
                }
            }

            groupIDs = personLinksGrouped.Keys.ToList();
            foreach (int id in groupIDs)
            {
                personLinks.AddRange(personLinksGrouped[id].Values);
                personLinksGrouped.Remove(id);
            }
            Console.WriteLine("Person Links created - count {0}", personLinks.Count());
            FileStorage<TriSingleLink>.Save("tri", 2, personLinks);
            return this;
        }

        public Triangles FakeEventToPerson()
        {
            Random rand = new Random();
            List<TriSingleLink> eventLinks = new List<TriSingleLink>();
            List<TriSingleLink> personLinks = new List<TriSingleLink>();

            Dictionary<int, List<TriSingleLink>> eventLinksGrouped = new Dictionary<int, List<TriSingleLink>>();
            List<int> groupIDs = eventLinksGrouped.Keys.ToList();

            Dictionary<int, Dictionary<int, TriSingleLink>> personLinksGrouped = new Dictionary<int, Dictionary<int, TriSingleLink>>();

            int groupTree = 2;
            int groupLevels = 6;

            int groupCount = Enumerable.Range(0, groupLevels).Select(x => (int)Math.Pow(groupTree, x)).Sum();
            //int groupCount = 20;
            int groupSize = 50;
            int groupLinks = 10;

            for (int g = 0; g < groupCount; g++)
            {
                for (int n = 0; n < groupSize; n++)
                {
                    personLinksGrouped.Add(g * groupSize + n, new Dictionary<int, TriSingleLink>());
                }
                for (int n = 0; n < groupSize; n++)
                {
                    for (int m = n + 1; m < groupSize; m++)
                    {
                        if (rand.Next(10) == 0)
                        {
                            continue;
                        }
                        personLinksGrouped[g * groupSize + n].Add(g * groupSize + m, new TriSingleLink
                        {
                            ID1 = g * groupSize + n,
                            ID2 = g * groupSize + m,
                            Type = 10,
                            Created = DateTime.Now,
                            IDEvent = 0
                        });
                    }
                }                
            }

            int nextGroup = 1;
            for (int g = 0; g < groupCount; g++)
            {
                int g1 = g;
                //int cgt = rand.Next(groupTree) + 1;
                int cgt = groupTree;
                for (int gc = 0; gc < cgt; gc++)
                {
                    int g2 = nextGroup;
                    for (int gl = 0; gl < groupLinks; gl++)
                    {
                        personLinksGrouped[g1 * groupSize + gl].Add(g2 * groupSize + gl, new TriSingleLink
                        {
                            ID1 = g1 * groupSize + gl,
                            ID2 = g2 * groupSize + gl,
                            Type = 10,
                            Created = DateTime.Now,
                            IDEvent = 0
                        });
                    }
                    nextGroup++;
                }
                if (nextGroup >= groupCount)
                {
                    break;
                }
            }
            /*
            for (int g = 0; g < groupCount; g++)
            {
                int g1 = g;
                int g2 = (g + 1) % groupCount;
                if (g2 < g1)
                {
                    int t = g2;
                    g2 = g1;
                    g1 = t;
                }
                //for (int g2 = g1 + 1; g2 < groupCount; g2++)
                {
                    personLinksGrouped[g1 * groupSize].Add(g2 * groupSize, new TriSingleLink
                    {
                        ID1 = g1 * groupSize,
                        ID2 = g2 * groupSize,
                        Type = 10,
                        Created = DateTime.Now,
                        IDEvent = 0
                    });
                }
            }
             */

            /*
            int groupCount = 2;
            int groupLinkCount = (groupCount * (groupCount - 1) / 2) * 10;

            int groupNodeCount = 1000;
            int groupEdgeCount = (groupNodeCount * (groupNodeCount - 1) / 2) / 2;
            //double neighbourDensity=0.2;
            Console.WriteLine("Edges planned: {0}", groupEdgeCount);
            for (int n = 0; n < groupNodeCount; n++)
            {
                personLinksGrouped.Add(n, new Dictionary<int, TriSingleLink>());
            }
            
            Console.WriteLine("Nodes created: {0}", personLinksGrouped.Count());
            Console.WriteLine("Edges count: {0}", groupEdgeCount);

            while (groupEdgeCount > 0)
            {
                int i1 = rand.Next(groupNodeCount);
                int i2o = rand.Next(groupNodeCount);                
                if (i1 == i2o)
                {
                    continue;
                }
                //int count = (int)(personLinksGrouped[i2o].Count * neighbourDensity);
                int count = (personLinksGrouped[i2o].Count == 0) ? 0 : rand.Next(personLinksGrouped[i2o].Count);
                for (int i = 0; i <= count; i++)
                {
                    int i2;
                    if (i == 0)
                    {
                        i2 = i2o;
                    }
                    else
                    {
                        int ii = rand.Next(personLinksGrouped[i2o].Count);
                        i2 = personLinksGrouped.Keys.ToList()[ii];
                    }
                    if (i1 > i2)
                    {
                        int t = i1;
                        i1 = i2;
                        i2 = t;
                    }
                    if (!personLinksGrouped[i1].ContainsKey(i2))
                    {
                        personLinksGrouped[i1].Add(i2, new TriSingleLink
                            {
                                ID1 = i1,
                                ID2 = i2,
                                Type = 10,
                                Created = DateTime.Now,
                                IDEvent = 0
                            });
                        groupEdgeCount--;
                    }
                }
            }
            Console.WriteLine("Edges created");

            for (int g = 1; g < groupCount; g++)
            {
                for (int n = 0; n < groupNodeCount; n++)
                {
                    int ng = g * groupNodeCount + n;
                    personLinksGrouped.Add(ng, new Dictionary<int, TriSingleLink>());
                    List<int> keys = personLinksGrouped[n].Keys.ToList();
                    for (int m = 0; m < personLinksGrouped[n].Count; m++)
                    {
                        int i2 = g * groupNodeCount + personLinksGrouped[n][keys[m]].ID2;
                        personLinksGrouped[ng].Add(i2, new TriSingleLink
                            {
                                ID1 = ng,
                                ID2 = i2,
                                Type = 10,
                                Created = DateTime.Now,
                                IDEvent = 0
                            });
                    }
                }
            }
            Console.WriteLine("Groups created");

            Console.WriteLine("Group edges count: {0}", groupLinkCount);

            while (groupLinkCount > 0)
            {
                int g1 = rand.Next(groupCount);
                int g2 = rand.Next(groupCount);
                if (g1 == g2)
                {
                    continue;
                }
                int i1 = g1 * groupNodeCount + rand.Next(groupNodeCount);
                int i2 = g2 * groupNodeCount + rand.Next(groupNodeCount);
                if (i1 == i2)
                {
                    continue;
                }

                if (i1 > i2)
                {
                    int t = i1;
                    i1 = i2;
                    i2 = t;
                }
                if (!personLinksGrouped[i1].ContainsKey(i2))
                {
                    personLinksGrouped[i1].Add(i2, new TriSingleLink
                    {
                        ID1 = i1,
                        ID2 = i2,
                        Type = 10,
                        Created = DateTime.Now,
                        IDEvent = 0
                    });
                    groupLinkCount--;
                }
            }
            Console.WriteLine("Group edges created");
            */
            groupIDs = personLinksGrouped.Keys.ToList();
            foreach (int id in groupIDs)
            {
                personLinks.AddRange(personLinksGrouped[id].Values);
                personLinksGrouped.Remove(id);
            }
            Console.WriteLine("Person Links created - count {0}", personLinks.Count());
            FileStorage<TriSingleLink>.Save("trifake", 2, personLinks);
            return this;
        }

        private string FileName(int opi)
        {
            return FileName("tri", opi);
        }

        private string FileName(string triname, int opi)
        {
            switch (opi)
            {
                case -1: return triname + "all";
                case 0: return triname + "trinon";
                case 1: return triname + "triopi";
            }
            return "";
        }

        private string FileName(string triname, int opi, int groupType)
        {
            return String.Format("{0}{1}_", FileName(triname, opi), groupType);
        }

        private bool CheckOpi(OpiChecker opc, int id, int opi)
        {
            switch (opi)
            {
                case -1: return true;
                case 0: return !opc.isOpi(id);
                case 1: return opc.isOpi(id);
            }
            return false;
        }

        public Triangles CountTypes(int opi)
        {
            Console.WriteLine("OPI");
            OpiChecker opc = new OpiChecker();
            opc.Load();
            //
            List<TriSingleLink> personLinks = new List<TriSingleLink>();

            FileStorage<TriSingleLink>.Load("tri", 2, personLinks);
            Console.WriteLine("Loaded");

            Dictionary<int, TriStat> stats = new Dictionary<int, TriStat>();
            foreach (TriSingleLink link in personLinks)
            {
                if ((!CheckOpi(opc, link.ID1, opi)) && (!CheckOpi(opc, link.ID2, opi)))
                {
                    continue;
                }
                if (!stats.ContainsKey(link.Created.Year))
                {
                    stats.Add(link.Created.Year, new TriStat()
                        {
                            Year = link.Created.Year,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                }
                if (link.Type >= 20)
                {
                    stats[link.Created.Year].CountCommittee++;
                }
                else
                {
                    stats[link.Created.Year].CountPublication++;
                }
            }
            FileStorage<TriStat>.Save(FileName(opi), 3, stats);
            return this;
        }

        public Triangles GetStats(int opi)
        {
            Console.WriteLine("OPI");
            OpiChecker opc = new OpiChecker();
            opc.Load();
            //
            List<TriSingleLink> personLinks = new List<TriSingleLink>();
            FileStorage<TriSingleLink>.Load("tri", 2, personLinks);
            Console.WriteLine("Loaded");
            Dictionary<int, Dictionary<int, TriSingleLink>> personLinksGrouped = new Dictionary<int, Dictionary<int, TriSingleLink>>();
            foreach (TriSingleLink link in personLinks)
            {
                if ((!CheckOpi(opc, link.ID1, opi)) && (!CheckOpi(opc, link.ID2, opi)))
                {
                    continue;
                }

                if (!personLinksGrouped.ContainsKey(link.ID1))
                {
                    personLinksGrouped.Add(link.ID1, new Dictionary<int, TriSingleLink>());
                }
                personLinksGrouped[link.ID1].Add(link.ID2, link);
            }
            Console.WriteLine("Groupped");
            List<TriTriangleLink> triangles = new List<TriTriangleLink>();
            DateTime started = DateTime.Now;
            DateTime start = DateTime.Now;
            int cnt = 0;
            List<int> groupIDs = personLinksGrouped.Keys.ToList();

            List<TriLinkStat> linkStats = new List<TriLinkStat>();
            for (int i = 0; i < 3; i++)
            {
                linkStats.Add(new TriLinkStat()
                {
                    Link = i,
                    CountCommittee = 0,
                    CountPublication = 0
                });
            }
            Dictionary<int, TriCreationStat> creationStats = new Dictionary<int, TriCreationStat>();

            foreach (int id in groupIDs)
            {
                List<int> keys = personLinksGrouped[id].Keys.ToList();
                int id1 = 0;
                while (id1 < keys.Count)
                {
                    int id2 = id1 + 1;
                    while (id2 < keys.Count)
                    {
                        TriSingleLink link1 = personLinksGrouped[id][keys[id1]];
                        TriSingleLink link2 = personLinksGrouped[id][keys[id2]];
                        int loId = (link1.ID2 < link2.ID2) ? link1.ID2 : link2.ID2;
                        int hiId = (link1.ID2 > link2.ID2) ? link1.ID2 : link2.ID2;
                        if (personLinksGrouped.ContainsKey(loId))
                        {
                            if (personLinksGrouped[loId].ContainsKey(hiId))
                            {
                                TriSingleLink link3 = personLinksGrouped[loId][hiId];
                                if ((link1.IDEvent != link2.IDEvent) || (link1.IDEvent != link3.IDEvent) || (link2.IDEvent != link3.IDEvent))
                                {
                                    List<TriSingleLink> links = new List<TriSingleLink>();
                                    links.Add(link1);
                                    links.Add(link2);
                                    links.Add(link3);
                                    links = links.OrderBy(x => x.Created).ToList();
                                    for (int j = 0; j < 3; j++)
                                    {
                                        if (links[j].Type >= 20)
                                        {
                                            linkStats[j].CountCommittee++;
                                        }
                                        else
                                        {
                                            linkStats[j].CountPublication++;
                                        }
                                    }
                                    for (int j = 0; j < 3; j++)
                                    {
                                        int k1 = (new int[] { 1, 2, 2 })[j];
                                        int k2 = (new int[] { 0, 0, 1 })[j];
                                        int diff = (int)((double)(((TimeSpan)(links[k1].Created - links[k2].Created)).Days) / 30.436875);
                                        if (!creationStats.ContainsKey(diff))
                                        {
                                            creationStats.Add(diff, new TriCreationStat()
                                            {
                                                Months = diff,
                                                Count12 = 0,
                                                Count13 = 0,
                                                Count23 = 0
                                            });
                                        }
                                        switch (j)
                                        {
                                            case 0: creationStats[diff].Count12++; break;
                                            case 1: creationStats[diff].Count13++; break;
                                            case 2: creationStats[diff].Count23++; break;
                                        }
                                    }
                                    /*
                                    triangles.Add(new TriTriangleLink()
                                    {
                                        ID1 = link1.ID1,
                                        ID2 = link1.ID2,
                                        ID3 = link2.ID2,
                                        Created1 = link1.Created,
                                        Created2 = link2.Created,
                                        Created3 = link3.Created
                                    });
                                     */
                                }
                            }
                        }
                        id2++;
                    }
                    id1++;
                }
                cnt++;
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                {
                    int seconds = (int)(((TimeSpan)(DateTime.Now - started)).TotalSeconds * (groupIDs.Count - cnt) / cnt);
                    Console.WriteLine("Count left: {0} | Tri:{1} | minutes left: {2}", groupIDs.Count - cnt, triangles.Count, seconds / 60);
                    start = DateTime.Now;
                }
            }
            Console.WriteLine("Triangles created");
            FileStorage<TriTriangleLink>.Save(FileName(opi), 4, triangles);
            FileStorage<TriLinkStat>.Save(FileName(opi), 5, linkStats);
            FileStorage<TriCreationStat>.Save(FileName(opi), 6, creationStats);

            return this;
        }

        public Triangles Betweenness(int opi, int groupType)
        {
            string triname = (groupType == 10) ? "trifake" : "tri";
            if (groupType == 10)
            {
                groupType = 0;
            }
            Console.WriteLine("OPI");
            if (!opc.IsLoaded)
            {
                opc.Load();
            }
            //
            if (personLinks.Count == 0)
            {
                FileStorage<TriSingleLink>.Load(triname, 2, personLinks);
            }
            Console.WriteLine("Loaded");
            Dictionary<int, List<int>> personLinksGrouped = new Dictionary<int, List<int>>();

            foreach (TriSingleLink link in personLinks)
            {
                if (((groupType == 2) && (link.Type >= 20)) ||
                    ((groupType == 1) && (link.Type < 20)))
                {
                    continue;
                }
                if ((!CheckOpi(opc, link.ID1, opi)) || (!CheckOpi(opc, link.ID2, opi)))
                {
                    continue;
                }
                if (!personLinksGrouped.ContainsKey(link.ID1))
                {
                    personLinksGrouped.Add(link.ID1, new List<int>());
                }
                personLinksGrouped[link.ID1].Add(link.ID2);
                if (!personLinksGrouped.ContainsKey(link.ID2))
                {
                    personLinksGrouped.Add(link.ID2, new List<int>());
                }
                personLinksGrouped[link.ID2].Add(link.ID1);
            }
            Console.WriteLine("Groupped");
            Dictionary<int, TriDoubleStat> centr = new Dictionary<int, TriDoubleStat>();
            List<int> vertices = personLinksGrouped.Keys.ToList();
            foreach (int v in vertices)
            {
                centr.Add(v, new TriDoubleStat() { ID = v, Value = 0, Count = 0 });
            }
            Console.WriteLine("Starting");
            DateTime started = DateTime.Now;
            DateTime start = DateTime.Now;
            int cnt = 0;
            foreach (int s in vertices)
            {
                Stack<int> S = new Stack<int>();
                Dictionary<int, List<int>> P = new Dictionary<int, List<int>>();
                Dictionary<int, double> sigma = CreateList(vertices, 0.0);
                sigma[s] = 1;
                Dictionary<int, int> d = CreateList(vertices, -1);
                d[s] = 0;
                Queue<int> Q = new Queue<int>();
                Q.Enqueue(s);
                while (Q.Count > 0)
                {
                    int v = Q.Dequeue();
                    S.Push(v);
                    foreach (int w in personLinksGrouped[v])
                    {
                        if (d[w] < 0)
                        {
                            Q.Enqueue(w);
                            d[w] = d[v] + 1;
                        }
                        if (d[w] == (d[v] + 1))
                        {
                            sigma[w] = sigma[w] + sigma[v];
                            if (!P.ContainsKey(w))
                            {
                                P.Add(w, new List<int>());
                            }
                            P[w].Add(v);
                        }
                    }
                }
                Dictionary<int, double> delta = CreateList(vertices, 0.0);
                while (S.Count > 0)
                {
                    int w = S.Pop();
                    if (P.ContainsKey(w))
                    {
                        foreach (int v in P[w])
                        {
                            delta[v] = delta[v] + sigma[v] / sigma[w] * (1.0 + delta[w]);
                        }
                    }
                    if (w != s)
                    {
                        centr[w].Value = centr[w].Value + delta[w];
                    }
                }
                cnt++;
                if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                {
                    int seconds = (int)(((TimeSpan)(DateTime.Now - started)).TotalSeconds * (vertices.Count - cnt) / cnt);
                    Console.WriteLine("Count left: {0} | minutes left: {1}", vertices.Count - cnt, seconds / 60);
                    start = DateTime.Now;
                }
            }
            FileStorage<TriDoubleStat>.Save(FileName(triname, opi, groupType), 10, centr);
            return this;
        }

        private Dictionary<int, T> CreateList<T>(List<int> vertices, T value)
        {
            Dictionary<int, T> result = new Dictionary<int, T>();
            foreach (int v in vertices)
            {
                result.Add(v, value);
            }
            return result;
        }

        public Triangles BetweennessStat(int opi, int groupType)
        {
            string triname = (groupType == 10) ? "trifake" : "tri";
            if (groupType == 10)
            {
                groupType = 0;
            }

            Dictionary<int, TriDoubleStat> centr = new Dictionary<int, TriDoubleStat>();
            FileStorage<TriDoubleStat>.Load(FileName(triname, opi, groupType), 10, centr);            
            Dictionary<int, TriDoubleStat> centrGroupped = new Dictionary<int, TriDoubleStat>();
            int step = 10;
            foreach (TriDoubleStat stat in centr.Values)
            {
                int value = (int)(stat.Value / step + 0.5);
                if (!centrGroupped.ContainsKey(value))
                {
                    centrGroupped.Add(value, new TriDoubleStat() { ID = value, Count = 0, Value = value * step }); ;
                }
                centrGroupped[value].Count++;
            }
            FileStorage<TriDoubleStat>.Save(FileName(triname, opi, groupType), 11, centrGroupped);

            return this;
        }
    }
}