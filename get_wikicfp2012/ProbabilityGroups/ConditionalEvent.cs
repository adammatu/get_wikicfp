using get_wikicfp2012.Stats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.ProbabilityGroups
{
    public class ConditionalEvent : IFileStorable2
    {
        public int Link;        
        public int Person;
        public int Event;
        public DateTime Date;
        public int Type;
        public int Conference;
        public int GroupType;
        public string GroupName;
        public List<ConditionalLink> Reason = new List<ConditionalLink>();

        public int Group;

        public int ID
        {
            get
            {
                return Person;
            }
        }

        public int ID2
        {
            get
            {
                return Event;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                Link,
                Person,
                Event,
                Date.ToString("yyyy.MM.dd"),
                Type,
                Conference,
                GroupType,
                GroupName);
        }

        public IFileStorable2 FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            Link = Convert.ToInt32(parts[0]);
            Person = Convert.ToInt32(parts[1]);
            Event = Convert.ToInt32(parts[2]);
            Date = DateTime.ParseExact(parts[3], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            Type = Convert.ToInt32(parts[4]);
            Conference = Convert.ToInt32(parts[5]);
            GroupType = Convert.ToInt32(parts[6]);
            GroupName = parts[7];
            return this;
        }

        public override int GetHashCode()
        {
            return Link.GetHashCode();
        }
    }
}

