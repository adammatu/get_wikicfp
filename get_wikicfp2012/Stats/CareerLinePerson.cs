using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Stats
{
    public class CareerLinePerson : IFileStorable
    {
        public int ID {get;set;}
        public int Level;
        public int Length;
        public CareerLineYear[] Years = new CareerLineYear[100];

        public CareerLinePerson()
        {
            for (int n = 0; n < 100; n++)
            {
                Years[n] = new CareerLineYear();
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendFormat("{0}|{1}", ID, Level);
            foreach (CareerLineYear year in Years)
            {
                result.Append(" ");
                result.Append(year.ToString());
            }
            return result.ToString();
        }

        public IFileStorable FromString(string text)
        {
            string[] items = text.Split(" ".ToCharArray());
            string[] parts = items[0].Split("|".ToCharArray());
            ID = Convert.ToInt32(parts[0]);
            Level = Convert.ToInt32(parts[1]);
            for (int n = 0; n < 100; n++)
            {
                Years[n].FromString(items[n + 1]);
            }
            return this;
        }
    }
}
