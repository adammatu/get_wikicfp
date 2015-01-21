using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace get_wikicfp2012.Stats
{
    public class TriTriangleLink : IFileStorable
    {
        public int ID1;
        public int ID2;
        public int ID3;
        public DateTime Created1;
        public DateTime Created2;
        public DateTime Created3;        

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
                ID3,
                Created1.ToString("yyyy.MM.dd"),
                Created2.ToString("yyyy.MM.dd"),
                Created3.ToString("yyyy.MM.dd"));
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            ID1 = Convert.ToInt32(parts[0]);
            ID2 = Convert.ToInt32(parts[1]);
            ID3 = Convert.ToInt32(parts[2]);
            Created1 = DateTime.ParseExact(parts[3], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            Created2 = DateTime.ParseExact(parts[4], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            Created3 = DateTime.ParseExact(parts[5], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            return this;
        }
    }
}
