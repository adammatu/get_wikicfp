using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.ProbabilityGroups
{
    public enum ConditionalSingleResultField {
        LinksTotal, LinksTotalPublication, LinksTotalCommittee, 

        LinksReasonTotal,
        LinksPublicationReasonPublicationTotal,
        LinksPublicationReasonCommitteeTotal,
        LinksCommitteeReasonPublicationTotal,
        LinksCommitteeReasonCommitteeTotal,

        TimespanLinksReasonTotal,
        TimespanLinksPublicationReasonPublicationTotal,
        TimespanLinksPublicationReasonCommitteeTotal,
        TimespanLinksCommitteeReasonPublicationTotal,
        TimespanLinksCommitteeReasonCommitteeTotal,
    };

    public class ConditionalSingleResult
    {
        public Dictionary<ConditionalSingleResultField, double> Values = new Dictionary<ConditionalSingleResultField, double>();

        public ConditionalSingleResult()
        {
            foreach (ConditionalSingleResultField cr in Enum.GetValues(typeof(ConditionalSingleResultField)))
            {
                Values.Add(cr, 0);
            }
        }

        public int GetValue(ConditionalSingleResultField index)
        {
            return (int)(Values[index] + 0.5);
        }
    }

    public class ConditionalResult
    {
        public const int YEAR_COUNT = 20;
        public Dictionary<ConditionalReason, Dictionary<int, ConditionalSingleResult>> Values = new Dictionary<ConditionalReason, Dictionary<int, ConditionalSingleResult>>();

        public ConditionalResult()
        {
            foreach (ConditionalReason cr in Enum.GetValues(typeof(ConditionalReason)))
            {
                Values[cr] = new Dictionary<int, ConditionalSingleResult>();
                for (int ix = 0; ix < YEAR_COUNT; ix++)
                {
                    Values[cr].Add(ix, new ConditionalSingleResult());
                }
            }
        }

        public void Increase(ConditionalReason cr, int ix, ConditionalSingleResultField i, double val)
        {
            if ((ix < 0) || (ix >= YEAR_COUNT))
            {
                return;
            }
            Values[cr][ix].Values[i] += val;
        }

        public void Save(string name)
        {
            string filename = String.Format("{0}lines\\cr_{1}.csv", Program.CACHE_ROOT, name);
            using (StreamWriter sw = File.CreateText(filename))
            {
                foreach (ConditionalSingleResultField i in Enum.GetValues(typeof(ConditionalSingleResultField)))
                {
                    StringBuilder line = new StringBuilder();
                    line.AppendFormat("{0},", i);
                    for (int ix = 0; ix < YEAR_COUNT; ix++)
                    {
                        line.AppendFormat("{0},", ix);
                    }
                    sw.WriteLine(line.ToString());
                    line.Clear();
                    foreach (ConditionalReason cr in Enum.GetValues(typeof(ConditionalReason)))
                    {
                        line.AppendFormat("{0},", cr.ToString());
                        for (int ix = 0; ix < YEAR_COUNT; ix++)
                        {
                            line.AppendFormat("{0},", Values[cr][ix].GetValue(i));
                        }
                        sw.WriteLine(line.ToString());
                        line.Clear();
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}