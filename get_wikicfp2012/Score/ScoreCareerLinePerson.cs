using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Stats
{
    public class ScoreCareerLinePerson : IFileStorable
    {
        public int ID { get; set; }
        public int Level = 1;
        public int Length = 0;
        public int StartYear;
        public double[] Years = new double[100];

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendFormat("{0}|{1}|{2}|{3}", ID, Level, Length, StartYear);
            foreach (double year in Years)
            {
                result.Append(" ");
                result.Append(String.Format("{0:0.0000}", year).Replace(",", "."));
            }
            return result.ToString();
        }

        public IFileStorable FromString(string text)
        {
            string[] items = text.Split(" ".ToCharArray());
            string[] parts = items[0].Split("|".ToCharArray());
            ID = Convert.ToInt32(parts[0]);
            Level = Convert.ToInt32(parts[1]);
            Length = Convert.ToInt32(parts[2]);
            StartYear = Convert.ToInt32(parts[3]);
            for (int n = 0; n < 100; n++)
            {
                Years[n] = Convert.ToDouble(items[n + 1].Replace(".", ","));
            }
            return this;
        }
    }
}