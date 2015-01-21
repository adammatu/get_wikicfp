using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.ProbabilityGroups
{
    public enum ConditionalReason
    {
        None = 0,
        Link2Friend3 = 1,
        Link2Friend2Friend3 = 2,
        Friend1 = 3,
        Link1 = 4,
        Link1Friend1 = 5
    };

    public class ConditionalLink
    {
        public ConditionalEvent Event;
        public ConditionalReason Reason;
    }
}