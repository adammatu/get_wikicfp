using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Score
{
    public class ScoreValueBase
    {
        public const double STEP_COEF = 0.9;
        public const int BASE_YEAR = 2000;
        public const int YEAR_COUNT = 20;
        public const double MAX_DIFF = 0.1;

        public List<int> links = new List<int>();        
        private double[] currValue;
        private double[] prevValue;

        public double this[int year]
        {
            get
            {
                if (year < BASE_YEAR)
                {
                    return 0;
                }
                return currValue[year - BASE_YEAR];
            }
            set
            {
                //double newValue = currValue[year - BASE_YEAR] + (value - currValue[year - BASE_YEAR]) * STEP_COEF;
                double newValue = value;
                prevValue[year - BASE_YEAR] = currValue[year - BASE_YEAR];
                currValue[year - BASE_YEAR] = newValue;                
            }
        }

        public bool Changed(int year)
        {
            return Math.Abs(currValue[year - BASE_YEAR] - prevValue[year - BASE_YEAR]) > MAX_DIFF;
        }

        public ScoreValueBase()
        {
            currValue = Enumerable.Repeat(0.0, YEAR_COUNT).ToArray();
            prevValue = Enumerable.Repeat(0.0, YEAR_COUNT).ToArray();
        }
    }

    public class ScoreValuePerson : ScoreValueBase
    {
    }

    public class ScoreValueEvent : ScoreValueBase
    {
        public int startYear;
        public bool isConf;
    }
}
