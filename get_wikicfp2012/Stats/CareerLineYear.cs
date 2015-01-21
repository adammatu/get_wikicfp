using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace get_wikicfp2012.Stats
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CareerLineYear
    {
        public ushort publications = 0;
        public ushort committees = 0;
        public ushort publicationsConnections = 0;
        public ushort committeesConnections = 0;

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}",
                publications,
                publicationsConnections,
                committees,
                committeesConnections);
        }

        public void FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            publications = Convert.ToUInt16(parts[0]);
            publicationsConnections = Convert.ToUInt16(parts[1]);
            committees = Convert.ToUInt16(parts[2]);
            committeesConnections = Convert.ToUInt16(parts[3]);
        }
    }
}