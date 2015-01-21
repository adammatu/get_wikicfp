using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace get_wikicfp2012.Stats
{
    public class TriSingleLink : IFileStorable
    {
        public int ID1;
        public int ID2;
        public DateTime Created;
        public int Type;        
        public int IDEvent;
        public double Score; 

        public int ID
        {
            get
            {
                return 0;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                ID1,
                ID2,
                Created.ToString("yyyy.MM.dd"),
                Type,
                IDEvent,
                String.Format("{0:0.0000}", Score).Replace(",", ".")
                );
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            ID1 = Convert.ToInt32(parts[0]);
            ID2 = Convert.ToInt32(parts[1]);
            Created = DateTime.ParseExact(parts[2], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            Type = Convert.ToInt32(parts[3]);
            if (parts.Length > 4)
            {
                IDEvent = Convert.ToInt32(parts[4]);
            }
            else
            {
                IDEvent = 0;
            }
            if (parts.Length > 5)
            {
                Score = Convert.ToDouble(parts[5].Replace(".", ","));
            }
            else
            {
                Score = 0;
            }
            return this;
        }
    }
}
