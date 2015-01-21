using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace get_wikicfp2012.Stats
{
    public class TriOpiPerson : IFileStorable
    {
        public int ID { get; set; }

        public override string ToString()
        {
            return String.Format("{0}",
                ID);
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            ID = Convert.ToInt32(parts[0]);
            return this;
        }
    }
}