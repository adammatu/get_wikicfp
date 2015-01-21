using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Stats
{
    public class TriCreationStat : IFileStorable
    {
        public int Count12;
        public int Count13;
        public int Count23;
        public int Months;

        public int ID
        {
            get
            {
                return Months;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}",
                Months,
                Count12,
                Count13,
                Count23);
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            Months = Convert.ToInt32(parts[0]);
            Count12 = Convert.ToInt32(parts[1]);
            Count13 = Convert.ToInt32(parts[2]);
            Count23 = Convert.ToInt32(parts[3]);
            return this;
        }
    }
}
