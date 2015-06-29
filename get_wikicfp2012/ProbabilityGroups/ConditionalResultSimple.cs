using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace get_wikicfp2012.ProbabilityGroups
{
    public class ConditionalResultSimpleItem
    {
        public int linkID;
        public bool conf;
        public int year;
        public int reason;
        public DateTime reasonDate;
        public int reasonID;
    }

    public class ConditionalResultSimpleCollector : Dictionary<int, Dictionary<int, Dictionary<bool, int>>>
    {
        public void Increment(int year, int reason, bool conf)
        {
            if ((year < 2000) || (year > 2015))
            {
                return;
            }
            if (!ContainsKey(year))
            {
                Add(year, new Dictionary<int, Dictionary<bool, int>>());
            }
            if (!this[year].ContainsKey(reason))
            {
                this[year].Add(reason, new Dictionary<bool, int>());
            }
            if (!this[year][reason].ContainsKey(conf))
            {
                this[year][reason].Add(conf, 0);
            }
            this[year][reason][conf]++;
        }

        public void Save(string name, string subname)
        {
            string filename = String.Format("{0}lines\\cr_{1}_{2}.csv", Program.CACHE_ROOT, name, subname);
            using (StreamWriter sw = File.CreateText(filename))
            {
                foreach (int year in this.Keys)
                    foreach (int reason in this[year].Keys)
                        foreach (bool conf in this[year][reason].Keys)
                        {
                            sw.WriteLine("{0} {1} {2} {3}", year, reason, (conf) ? "c" : "p", this[year][reason][conf]);
                        }
            }
        }
    }

    public class ConditionalResultSimple
    {
        private Dictionary<int, ConditionalResultSimpleItem> data = new Dictionary<int, ConditionalResultSimpleItem>();
        ConditionalResultSimpleCollector allData = new ConditionalResultSimpleCollector();
        ConditionalResultSimpleCollector firstData = new ConditionalResultSimpleCollector();

        public ConditionalResultSimple()
        {
        }

        public void Add(ConditionalResultSimpleItem item)
        {
            allData.Increment(item.year, item.reason, item.conf);
            if (!data.ContainsKey(item.linkID))
            {
                data.Add(item.linkID, item);
                return;
            }
            if ((data[item.linkID].reasonDate > item.reasonDate) ||
                ((data[item.linkID].reasonDate == item.reasonDate) && (data[item.linkID].reasonID > item.reasonID)))
            {
                data[item.linkID] = item;
            }            
        }

        public void Prepare()
        {
            foreach (ConditionalResultSimpleItem item in data.Values)
            {
                firstData.Increment(item.year, item.reason, item.conf);
            }
        }

        public void Save(string name)
        {
            allData.Save(name, "all");
            firstData.Save(name,"first");
        }
    }
}
