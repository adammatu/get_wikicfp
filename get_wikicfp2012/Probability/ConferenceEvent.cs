using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using get_wikicfp2012.Stats;
using System.Globalization;

namespace get_wikicfp2012.Probability
{
    public class ConferenceEvent : IFileStorable
    {
        public int ID { get; set; }
        public int IDevent;        
        public DateTime Created;
        public double Score;        
        
        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}",
                ID,
                IDevent,                
                Created.ToString("yyyy.MM.dd"),
                String.Format("{0:0.0000}", Score).Replace(",", ".")
                );
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            ID = Convert.ToInt32(parts[0]);
            IDevent = Convert.ToInt32(parts[1]);            
            Created = DateTime.ParseExact(parts[2], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            Score = Convert.ToDouble(parts[3].Replace(".", ","));
            return this;
        }
    }
}
