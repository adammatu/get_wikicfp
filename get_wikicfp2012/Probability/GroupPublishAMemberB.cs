using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using get_wikicfp2012.Stats;


namespace get_wikicfp2012.Probability
{
    public class GroupPublishAMemberB : GroupBase
    {
        public GroupPublishAMemberB()
        {
            Name = "A+B published then A+B members";
        }

        private List<GropuResultFlat> personLinksFirst = new List<GropuResultFlat>();
        private List<GropuResultFlat> personLinksConsecutive = new List<GropuResultFlat>();

        public override void Prepare()
        {
            List<TriSingleLink> eventLinks = new List<TriSingleLink>();

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

            Dictionary<int, Dictionary<int, GropuResultFlat>> personLinksGroupedFirst = new Dictionary<int, Dictionary<int, GropuResultFlat>>();
            Dictionary<int, Dictionary<int, GropuResultFlat>> personLinksGroupedConsecutive = new Dictionary<int, Dictionary<int, GropuResultFlat>>();
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
                        Dictionary<int, Dictionary<int, GropuResultFlat>> links;
                        if (items[0].Type == 30)
                        {
                            links = personLinksGroupedConsecutive;
                        }
                        else
                        {
                            links = personLinksGroupedFirst;
                        }
                        if (!links.ContainsKey(pid1))
                        {
                            links.Add(pid1, new Dictionary<int, GropuResultFlat>());
                        }
                        if (links[pid1].ContainsKey(pid2))
                        {
                            GropuResultFlat link = links[pid1][pid2];
                            if (link.Created > items[0].Created)
                            {
                                link.Created = items[0].Created;
                                link.Score = items[0].Score;
                            }
                        }
                        else
                        {
                            links[pid1].Add(pid2, new GropuResultFlat()
                            {
                                ID1 = pid1,
                                ID2 = pid2,
                                Created = items[0].Created,
                                Score = items[0].Score
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

            groupIDs = personLinksGroupedFirst.Keys.ToList();
            foreach (int id in groupIDs)
            {
                personLinksFirst.AddRange(personLinksGroupedFirst[id].Values);
                personLinksGroupedFirst.Remove(id);
            }
            Console.WriteLine("Person Links created first - count {0}", personLinksFirst.Count());

            groupIDs = personLinksGroupedConsecutive.Keys.ToList();
            foreach (int id in groupIDs)
            {
                personLinksConsecutive.AddRange(personLinksGroupedConsecutive[id].Values);
                personLinksGroupedConsecutive.Remove(id);
            }
            Console.WriteLine("Person Links created - count {0}", personLinksConsecutive.Count());
        }

        public override GroupResult GetFirst()
        {
            GroupResult result = new GroupResult();
            foreach (GropuResultFlat item in personLinksFirst)
            {
                result.Add(item.ID1, item.ID2, item.Created, item.Score);
            }
            return result;
        }

        public override GroupResult GetSecond(GroupResult first)
        {
            GroupResult result = new GroupResult();
            foreach (GropuResultFlat item in personLinksConsecutive)
            {
                result.Add(first, item.ID1, item.ID2, item.Created, item.Score);
            }
            return result;
        }
    }
}