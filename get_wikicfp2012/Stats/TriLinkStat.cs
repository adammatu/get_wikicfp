using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Stats
{
    public class TriLinkStat : IFileStorable
    {
        public int CountCommittee;
        public int CountPublication;
        public int Link;

        public int ID
        {
            get
            {
                return Link;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}",
                Link,
                CountCommittee,
                CountPublication);
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            Link = Convert.ToInt32(parts[0]);
            CountCommittee = Convert.ToInt32(parts[1]);
            CountPublication = Convert.ToInt32(parts[2]);            
            return this;
        }
    }
}
