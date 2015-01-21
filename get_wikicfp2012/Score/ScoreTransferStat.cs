using get_wikicfp2012.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.Score
{
    public class ScoreTransferLink
    {
        public int Year;
        public int Connections;
    }

    public class ScoreTransferGroup
    {
        public Dictionary<int, ScoreTransferLink> Links = new Dictionary<int, ScoreTransferLink>();
        public int Count;
    }

    public class ScoreTransferStatList : Dictionary<int, ScoreTransferStat>
    {
        public void AddCount(int ID, int amount)
        {
            if (!ContainsKey(ID))
            {
                Add(ID, new ScoreTransferStat
                    {
                        ID = ID
                    });
            }
            this[ID].Count += amount;
        }

        public void AddCountSucess(int ID)
        {
            if (!ContainsKey(ID))
            {
                Add(ID, new ScoreTransferStat
                {
                    ID = ID
                });
            }
            this[ID].CountSuccess++;
        }
    }

    public class ScoreTransferStat : IFileStorable
    {
        public int ID { get; set; }  
        public int Count = 0;
        public int CountSuccess = 0;

        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}",
                ID,
                Count,
                CountSuccess);
        }

        public IFileStorable FromString(string text)
        {
            string[] parts = text.Split("|".ToCharArray());
            ID = Convert.ToInt32(parts[0]);
            Count = Convert.ToInt32(parts[1]);
            CountSuccess = Convert.ToInt32(parts[2]);
            return this;
        }
    }
}
