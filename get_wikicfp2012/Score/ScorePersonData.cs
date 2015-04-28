using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Score
{
    public class ScorePersonCount
    {
        public int connection;
        public int triangle;
        public double score;
    }

    public class ScorePersonData
    {
        public List<int> eventLinks = new List<int>();        
        public Dictionary<int, double> peopleLinkScore = new Dictionary<int, double>();
        public int triangleCount = 0;
        public Dictionary<int, ScorePersonCount> connectionCount = new Dictionary<int, ScorePersonCount>();        
        public int startYear;
        public int hIndex = -1;
    }

    public class ScorePersonEventData
    {
        public List<int> peopleLinks = new List<int>();
        public int citeCount = 0;
        
        public int Year
        {
            get
            {
                return Date.Year;
            }
        }
        
        public DateTime Date;
    }
}
