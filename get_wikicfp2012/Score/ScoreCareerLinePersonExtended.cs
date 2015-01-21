using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Stats
{
    public class ScoreCareerLinePersonExtendedYearInfo
    {
        public double compareValue;
        public double[] allValues = null;
        public int allValuesLength = 0;

        public void Join(double value)
        {
            if (allValues == null)
            {
                allValues = new double[1];
                allValues[0] = value;
                allValuesLength = 1;
                return;
            }
            if (allValuesLength == allValues.Length)
            {
                double[] newValues = new double[allValues.Length * 2];
                Array.Copy(allValues, 0, newValues, 0, allValues.Length);
                allValues = newValues;
            }
            allValues[allValuesLength] = value;
            allValuesLength++;            
        }

        public void Join(double[] values, int count)
        {
            if (allValues == null)
            {
                allValues = new double[values.Length];
                Array.Copy(values, allValues, values.Length);
                allValuesLength = count;
            }
            else
            {
                for (int n = 0; n < count; n++)
                {
                    Join(values[n]);
                }
            }
        }

        public double Quantile(int n)
        {
            if ((allValues==null)||(allValues.Length < 1))
            {
                return 0;
            }
            allValues = new List<double>(allValues).Select(x => x).OrderBy(x => x).ToArray();

            double np = (double)(allValues.Length - 1) * (double)n / 10.0;
            int n1 = (int)np;
            int n2 = n1 + 1;
            if (n1 < 0)
            {
                n1 = 0;
            }
            if (n2 >= allValues.Length)
            {
                n2 = allValues.Length - 1;
            }
            double v1 = allValues[n1];
            double v2 = allValues[n2];
            np = np - n1;
            return v1 * (1.0 - np) + v2 * np;
        }
    }

    public class ScoreCareerLinePersonExtended : IFileStorable
    {
        public int ID { get; set; }
        public int Level = 1;
        public int Length = 0;
        public int StartYear;
        public List<ScoreCareerLinePersonExtendedYearInfo> Years = new List<ScoreCareerLinePersonExtendedYearInfo>();

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            int[] vals = { -1, 1, 5, 9 };
            foreach (int val in vals)
            {
                result.AppendFormat("{0}|{1}|{2}|{3}", ID, Level, Length, StartYear);
                foreach (ScoreCareerLinePersonExtendedYearInfo year in Years)
                {
                    result.Append(" ");
                    result.Append(String.Format("{0:0.0000}", (val < 0) ? year.compareValue : year.Quantile(val)).Replace(",", "."));
                }
                result.AppendLine();
            }
            return result.ToString();
        }

        public IFileStorable FromString(string text)
        {
            //loads ScoreCareerLinePerson
            ScoreCareerLinePerson line = new ScoreCareerLinePerson();
            line.FromString(text);
            ID = line.ID;
            Level = line.Level;
            Length = line.Length;
            StartYear = line.StartYear;
            for (int n = 0; n < Length; n++)
            {
                double val = line.Years[n];
                ScoreCareerLinePersonExtendedYearInfo item = new ScoreCareerLinePersonExtendedYearInfo();
                item.Join(val);
                item.compareValue = val;
                Years.Add(item);
            }
            return this;
        }
    }
}