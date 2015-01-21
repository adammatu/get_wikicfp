using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace get_wikicfp2012.Stats
{
    public class CareerLineLength : IFileStorable
    {
        public int Length;
        public int Count;
        public int CountPublication;
        public int CountCommittee;

        public int ID
        {
            get
            {
                return Length;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}",
                Length,
                Count,
                CountPublication,
                CountCommittee);
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            Length = Convert.ToInt32(parts[0]);
            Count = Convert.ToInt32(parts[1]);
            CountPublication = Convert.ToInt32(parts[2]);
            CountCommittee = Convert.ToInt32(parts[3]);
            return this;
        }
    }
}