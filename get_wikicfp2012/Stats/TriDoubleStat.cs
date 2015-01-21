using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Stats
{
    public class TriDoubleStat : IFileStorable
    {
        public int ID { get; set; }     
        public double Value;
        public int Count;

        public override string ToString()
        {
            return String.Format("{0}|{1:0.0000}|{2}",
                ID,
                Value,
                Count);
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            ID = Convert.ToInt32(parts[0]);
            Value = Convert.ToDouble(parts[1]);
            Count = Convert.ToInt32(parts[2]);
            return this;
        }
    }
}
