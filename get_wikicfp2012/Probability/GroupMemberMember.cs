using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using get_wikicfp2012.Stats;


namespace get_wikicfp2012.Probability
{
    public class GroupMemberMember : GroupBase
    {
        public GroupMemberMember()
        {
            Name = "committee member then committee member again";
        }

        public override void Prepare()
        {
            
        }

        public override GroupResult GetFirst()
        {
            GroupResult result = new GroupResult();
            List<ConferenceEvent> resultConference = new List<ConferenceEvent>();
            FileStorage<ConferenceEvent>.Load("event", 3, resultConference);
            foreach (ConferenceEvent item in resultConference)
            {
                result.Add(item.ID, item.IDevent, item.Created, item.Score);
            }
            return result;
        }

        public override GroupResult GetSecond(GroupResult first)
        {
            GroupResult result = new GroupResult();
            List<ConferenceEvent> resultConference = new List<ConferenceEvent>();
            FileStorage<ConferenceEvent>.Load("event", 3, resultConference);
            foreach (ConferenceEvent item in resultConference)
            {
                result.Add(first, item.ID, item.IDevent, item.Created, item.Score);
            }
            return result;
        }
    }
}
