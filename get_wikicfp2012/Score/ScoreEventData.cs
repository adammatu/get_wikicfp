using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Score
{
    public class ScoreEventPersonData
    {
        public List<int> eventLinks = new List<int>();        
        public Dictionary<int, double> score = new Dictionary<int, double>();
        public Dictionary<int, int> connectionCount = new Dictionary<int, int>();
        public int startYear;
    }

    public class ScoreEventData
    {
        public List<int> peopleLinks = new List<int>();

        public int Year
        {
            get
            {
                return Date.Year;
            }
        }

        public DateTime Date;
        public Double Score;
        public Double ScoreNow;
    }
}
