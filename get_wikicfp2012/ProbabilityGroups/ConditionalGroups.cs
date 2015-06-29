using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using get_wikicfp2012.Stats;
using System.Runtime.CompilerServices; 


namespace get_wikicfp2012.ProbabilityGroups
{
    public class ConditionalGroups
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);
        private Dictionary<int, List<ConditionalEvent>> peopleevents = new Dictionary<int, List<ConditionalEvent>>();
        private Dictionary<int, IntList> eventspeople = new Dictionary<int, IntList>();
        private List<string> eventsgroups = new List<string>();
        private Dictionary<string, List<ConditionalEvent>> groupevents = new Dictionary<string, List<ConditionalEvent>>();

        public ConditionalGroups Open()
        {
            connection.Open();
            return this;
        }

        public ConditionalGroups Close()
        {
            connection.Close();
            return this;
        }

        public ConditionalGroups ReadEvents()
        {
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //                    
            command = connection.CreateCommand();
            command.CommandText = "select l.ID as linkID, l.Person_ID as id, e.ID as eID, eg.Date as [date], eg.Type as egtype, e.Type as etype, e.Score as escore, e.Conference_ID as evID, eg.Conference_ID as evID2, eg.[Group] as gname from tbllink l " +
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

                    int id = Convert.ToInt32(dr["id"]);
                    int lid = Convert.ToInt32(dr["linkID"]);
                    int eid = Convert.ToInt32(dr["eID"]);
                    DateTime date = Convert.ToDateTime(dr["date"]);
                    int etype = Convert.ToInt32(dr["etype"]);
                    //double escore = Convert.ToDouble(dr["escore"]);
                    int egtype = Convert.ToInt32(dr["egtype"]);
                    int evID = (dr["evID"] == DBNull.Value) ? (-1) : Convert.ToInt32(dr["evID"]);
                    int evID2 = (dr["evID2"] == DBNull.Value) ? (-1) : Convert.ToInt32(dr["evID2"]);
                    string groupName = dr["gname"].ToString();
                    if (evID < 0)
                    {
                        evID = evID2;
                    }
                    if (!peopleevents.ContainsKey(id))
                    {
                        peopleevents.Add(id, new List<ConditionalEvent>());
                    }
                    peopleevents[id].Add(new ConditionalEvent
                    {
                        Link = lid,
                        Person = id,
                        Event = eid,
                        Date = date,
                        Type = etype,
                        Conference = evID,
                        GroupType = egtype,
                        GroupName = groupName
                    });
                }
            }
            dr.Close();
            Console.WriteLine("Read End");
            FileStorage2<ConditionalEvent>.Save("cg", 1, peopleevents);
            Console.WriteLine("Saved");
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsGroup(ConditionalEvent E2, ConditionalEvent E1)
        {
            return E1.Group == E2.Group;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNotGroup(ConditionalEvent E2, ConditionalEvent E1)
        {
            return E1.Group != E2.Group;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAnyGroup(ConditionalEvent E)
        {
            return E.Group >= 0;
        }

        private ConditionalReason Check(int pid, ConditionalEvent E2, ConditionalEvent E1)
        {
            IntList eventspeopleE1 = eventspeople[E1.Event];
            IntList eventspeopleE2 = eventspeople[E2.Event];
#if true // Code removed temporarily because works too slow
            if (IsNotGroup(E2, E1) && IsAnyGroup(E2))
            {
                bool l2f3 = false;             
                for (int i = 0; i < eventspeopleE1.Count; i++)
                {
                    int friendID = eventspeopleE1[i];
                    if (friendID == pid)
                    {
                        continue;
                    }
                    foreach (ConditionalEvent E3 in peopleevents[friendID])
                    {
                        if ((IsGroup(E2, E3)) && (E3.Date < E2.Date))
                        {
                            if (eventspeople[E3.Event].Contains(pid))
                            {
                                continue;
                            }
                            bool f2 = (eventspeopleE2.Contains(friendID));
                            if (f2)
                            {
                                return ConditionalReason.Link2Friend2Friend3;
                            }
                            l2f3 = true;
                            break;
                        }
                    }
                }
                //
                if (l2f3)
                {
                    return ConditionalReason.Link2Friend3;
                }
            }
#endif
            bool link = false;
            if (IsGroup(E2, E1))
            {
                link = true;
            }
            bool friend = false;
            for (int i = 0; i < eventspeopleE1.Count; i++)
            {
                int p1 = eventspeopleE1[i];
                if ((eventspeopleE2.Contains(p1)) && (p1 != pid))
                {
                    friend = true;
                    break;
                }
            }
            if (link)
            {
                if (friend)
                {
                    return ConditionalReason.Link1Friend1;
                }
                return ConditionalReason.Link1;
            }
            if (friend)
            {
                return ConditionalReason.Friend1;
            }
            return ConditionalReason.None;
        }

        public ConditionalGroups IdentifyReasons()
        {
            // SqlCommand command = connection.CreateCommand();

            Console.WriteLine("Load");
            FileStorage2<ConditionalEvent>.Load("cg", 1, peopleevents);
            Console.WriteLine("Sort and Group");
            List<int> ids = peopleevents.Keys.ToList();
            int notGroupIndex = 1;
            foreach (int id in ids)
            {
                List<ConditionalEvent> lce = peopleevents[id];
                for (int i1 = 0; i1 < lce.Count; i1++)
                {
                    ConditionalEvent lce1 = lce[i1];
                    string group = lce1.GroupName.ToLower();
                    if (group == "")
                    {
                        lce1.Group = -notGroupIndex;
                        notGroupIndex++;
                    }
                    else
                    {
                        if (!eventsgroups.Contains(group))
                        {
                            eventsgroups.Add(group);
                            groupevents.Add(group, new List<ConditionalEvent>());
                        }
                        lce1.Group = eventsgroups.IndexOf(group);
                        groupevents[group].Add(lce1);
                    }
                }
                peopleevents[id] = lce.OrderByDescending(x => x.Date).ToList();
            } 
            Console.WriteLine("Event Prepare");
            eventspeople.Clear();
            foreach (int id in ids)
            {
                foreach (ConditionalEvent e in peopleevents[id])
                {
                    if (!eventspeople.ContainsKey(e.Event))
                    {
                        eventspeople.Add(e.Event, new IntList());
                    }
                    eventspeople[e.Event].Add(id);
                }
            }
            Console.WriteLine("Prepare Done");
            string filename = String.Format("{0}lines\\tblLinkReason.csv", Program.CACHE_ROOT);
            using (StreamWriter sw = File.CreateText(filename))
            {
                int linkCount = 0;
                int cnt = 0;
                DateTime started = DateTime.Now;
                DateTime start = DateTime.Now;
                foreach (int id in ids)
                {
                    List<ConditionalEvent> lce = peopleevents[id];
                    for (int i1 = 0; i1 < lce.Count; i1++)
                    {
                        for (int i2 = i1 + 1; i2 < lce.Count; i2++) 
                        {
                            ConditionalReason reason = Check(id, lce[i1], lce[i2]);
                            if (reason != ConditionalReason.None)
                            {
                                /*
                                lce[i1].Reason.Add(new ConditionalLink
                                {
                                    Event = lce[i2],
                                    Reason = reason
                                });
                                 */
                                /*
                                string sql = String.Format(
                                    "insert into tblEventReason (Event_ID,Reason,ReasonEvent_ID) values ({0},{1},{2})",
                                    lce[i1].Event,
                                    Convert.ToInt32(reason),
                                    lce[i2].Event);

                                command.CommandText = sql;
                                command.ExecuteNonQuery();
                                 */
                                linkCount++;
                                string line = String.Format(
                                    "{0},{1},{2},{3}",
                                    linkCount,
                                    lce[i1].Link,
                                    Convert.ToInt32(reason),
                                    lce[i2].Link);
                                sw.WriteLine(line);
                            }
                        }
                    }
                    cnt++;
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        int seconds = (int)(((TimeSpan)(DateTime.Now - started)).TotalSeconds * (ids.Count - cnt) / cnt);
                        Console.WriteLine("Count left: {0} | minutes left: {1}", ids.Count - cnt, seconds / 60);
                        start = DateTime.Now;
                    }
                }
            }
            return this;
        }

        public ConditionalGroups CollectSimple()
        {
            ConditionalResultSimple result = new ConditionalResultSimple();
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //
            command = connection.CreateCommand();
            command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = "select " +
                "lr.ID as reasonID, "+
                "lr.Link_ID as linkID, "+
                "lr.Reason as reason, " +
                "e.Type as type,eg.Date as date, " +
                "e2.Type as type2,eg2.Date as date2, " +
                "e.ID as id " +
                "from tblLinkReason lr " +
                "left join tblLink l on lr.ReasonLink_ID=l.ID " +
                "left join tblEvent e on l.Event_ID=e.ID " +
                "left join tblEventGroup eg on e.EventGroup_ID=eg.ID " +
                "left join tblLink l2 on lr.Link_ID=l2.ID " +
                "left join tblEvent e2 on l2.Event_ID=e2.ID " +
                "left join tblEventGroup eg2 on e2.EventGroup_ID=eg2.ID ";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if (dr["date"] == DBNull.Value)
                    {
                        continue;
                    }

                    result.Add(new ConditionalResultSimpleItem()
                        {
                            conf = Convert.ToInt32(dr["type2"]) > 20,
                            linkID = Convert.ToInt32(dr["linkID"]),
                            reason = Convert.ToInt32(dr["reason"]),
                            reasonDate = Convert.ToDateTime(dr["date"]),
                            reasonID = Convert.ToInt32(dr["reasonID"]),
                            year = Convert.ToDateTime(dr["date2"]).Year
                        }
                        );
                }
            }
            Console.WriteLine("Prepare");
            result.Prepare();
            Console.WriteLine("Save");
            result.Save("allsim");
            return this;
        }

        public ConditionalGroups Collect()
        {
            Dictionary<int, int> eventReasonCount = new Dictionary<int, int>();
            ConditionalResult result = new ConditionalResult();
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //
            int cnt = 0;
            DateTime start = DateTime.Now;
            //
            command = connection.CreateCommand();
            command.CommandText = "select e.Type,eg.Date from tblLink l " +
                "left join tblEvent e on l.Event_ID=e.ID " +
                "left join tblEventGroup eg on e.EventGroup_ID=eg.ID";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if (dr["date"] == DBNull.Value)
                    {
                        continue;
                    }

                    int type = Convert.ToInt32(dr["type"]);
                    DateTime date = Convert.ToDateTime(dr["date"]);

                    int year = date.Year - 2000;
                    bool committee = type > 20;
                    result.Increase(ConditionalReason.None, year, ConditionalSingleResultField.LinksTotal, 1);
                    if (committee)
                    {
                        result.Increase(ConditionalReason.None, year, ConditionalSingleResultField.LinksTotalCommittee, 1);
                    }
                    else
                    {
                        result.Increase(ConditionalReason.None, year, ConditionalSingleResultField.LinksTotalPublication, 1);
                    }

                    cnt++;
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        Console.WriteLine("Events done: {0}", cnt);
                        start = DateTime.Now;
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Read Event OK");
            //
            cnt = 0;
            start = DateTime.Now;
            //            
            command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = "select " +
                "lr.Reason as reason, " +
                "e.Type as type,eg.Date as date, " +
                "e2.Type as type2,eg2.Date as date2, " +
                "e.ID as id " +
                "from tblLinkReason lr " +
                "left join tblLink l on lr.ReasonLink_ID=l.ID " +
                "left join tblEvent e on l.Event_ID=e.ID " +
                "left join tblEventGroup eg on e.EventGroup_ID=eg.ID " +
                "left join tblLink l2 on lr.Link_ID=l2.ID " +
                "left join tblEvent e2 on l2.Event_ID=e2.ID " +
                "left join tblEventGroup eg2 on e2.EventGroup_ID=eg2.ID ";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if ((dr["date"] == DBNull.Value) || (dr["date2"] == DBNull.Value))
                    {
                        continue;
                    }

                    int id = Convert.ToInt32(dr["id"]);

                    if (!eventReasonCount.ContainsKey(id))
                    {
                        eventReasonCount.Add(id, 0);
                    }
                    eventReasonCount[id]++;

                    cnt++;
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        Console.WriteLine("EventReason done: {0}", cnt);
                        start = DateTime.Now;
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Read Event Reason Counts OK");
            //            
            cnt = 0;
            start = DateTime.Now;
            //            
            command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = "select " +
                "lr.Reason as reason, " +
                "e.Type as type,eg.Date as date, " +
                "e2.Type as type2,eg2.Date as date2, " +
                "e.ID as id " +
                "from tblLinkReason lr " +
                "left join tblLink l on lr.ReasonLink_ID=l.ID " +
                "left join tblEvent e on l.Event_ID=e.ID " +
                "left join tblEventGroup eg on e.EventGroup_ID=eg.ID " +
                "left join tblLink l2 on lr.Link_ID=l2.ID " +
                "left join tblEvent e2 on l2.Event_ID=e2.ID " +
                "left join tblEventGroup eg2 on e2.EventGroup_ID=eg2.ID ";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if ((dr["date"] == DBNull.Value) || (dr["date2"] == DBNull.Value))
                    {
                        continue;
                    }

                    int id = Convert.ToInt32(dr["id"]);
                    int type = Convert.ToInt32(dr["type"]);
                    DateTime date = Convert.ToDateTime(dr["date"]);
                    int type2 = Convert.ToInt32(dr["type2"]);
                    DateTime date2 = Convert.ToDateTime(dr["date2"]);
                    ConditionalReason reason = (ConditionalReason)Convert.ToInt32(dr["reason"]);

                    int year = date.Year - 2000;
                    bool committee = type > 20;
                    int year2 = date2.Year - 2000;
                    bool committee2 = type2 > 20;

                    int timespan = date2.Year - date.Year;
                    double val = 1.0 / (double)eventReasonCount[id];

                    result.Increase(reason, year, ConditionalSingleResultField.LinksReasonTotal, val);
                    result.Increase(reason, timespan, ConditionalSingleResultField.TimespanLinksReasonTotal, val);
                    if (committee)
                    {
                        if (committee2)
                        {
                            result.Increase(reason, year, ConditionalSingleResultField.LinksCommitteeReasonCommitteeTotal, val);
                            result.Increase(reason, timespan, ConditionalSingleResultField.TimespanLinksCommitteeReasonCommitteeTotal, val);
                        }
                        else
                        {
                            result.Increase(reason, year, ConditionalSingleResultField.LinksCommitteeReasonPublicationTotal, val);
                            result.Increase(reason, timespan, ConditionalSingleResultField.TimespanLinksCommitteeReasonPublicationTotal, val);
                        }
                    }
                    else
                    {
                        if (committee2)
                        {
                            result.Increase(reason, year, ConditionalSingleResultField.LinksPublicationReasonCommitteeTotal, val);
                            result.Increase(reason, timespan, ConditionalSingleResultField.TimespanLinksPublicationReasonCommitteeTotal, val);
                        }
                        else
                        {
                            result.Increase(reason, year, ConditionalSingleResultField.LinksPublicationReasonPublicationTotal, val);
                            result.Increase(reason, timespan, ConditionalSingleResultField.TimespanLinksPublicationReasonPublicationTotal, val);
                        }
                    }

                    cnt++;
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        Console.WriteLine("EventReason done: {0}", cnt);
                        start = DateTime.Now;
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Read EventReason OK");
            result.Save("all");
            return this;
        }

    }
}