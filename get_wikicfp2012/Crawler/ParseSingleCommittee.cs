using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Crawler
{
    public class ParseSingleCommittee
    {
        public string Name = "";
        public DateTime Date = DateTime.MinValue;
        public Dictionary<int, string> Members = new Dictionary<int, string>();

        public bool isEmpty()
        {
            return Members.Count == 0;
        }

        public bool ShouldJoin(ParseSingleCommittee other)
        {
            if (other.isEmpty())
            {
                return false;
            }
            if (Name == other.Name)
            {
                return true;
            }
            int common = 0;
            foreach (int id in Members.Keys)
            {
                if (other.Members.ContainsKey(id))
                {
                    common++;
                }
            }
            int diff = Members.Count + other.Members.Count - 2 * common;
            int maxDiff = (Members.Count + other.Members.Count) / 20 + 1;
            return diff <= maxDiff;
        }

        public void Join(ParseSingleCommittee other)
        {
            if (Date == DateTime.MinValue)
            {
                Date = other.Date;
            }
            else if (other.Date != DateTime.MinValue)
            {
                Date = (Date < other.Date) ? Date : other.Date;
            }
            if (String.IsNullOrEmpty(Name))
            {
                Name = other.Name;
            }
            else if (!String.IsNullOrEmpty(other.Name))
            {
                Name = (Name.Length < other.Name.Length) ? Name : other.Name;
            }
            foreach (int id in other.Members.Keys)
            {
                if (!Members.ContainsKey(id))
                {
                    Members.Add(id, other.Members[id]);
                }
            }
        }
    }
}
