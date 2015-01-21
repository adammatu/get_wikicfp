using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.DBLP
{
    class ParserCiteItem
    {
        public string Name = "";
        public int ID = -1;
        public int databaseID = -1;
        public List<int> References = new List<int>();
    }
}
