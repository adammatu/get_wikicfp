using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.ProbabilityGroups
{
    /*
    public class IntListSet
    {
        IntList[] data = new IntList[16];

        public IntListSet()
        {
            for (int n = 0; n < 16; n++)
            {
                data[n] = new IntList();
            }
        }

        private int group(int item)
        {
            return item % 16;
        }


        public void Add(int item)
        {
            data[group(item)].Add(item);
        }

        public bool Contains(int item)
        {
            return data[group(item)].Contains(item);
        }

        public List<int> AllData
        {
            get
            {
                List<int> result = new List<int>();
                for (int i = 0; i < 16; i++)
                {
                    result.AddRange(data[i].items);
                }
                return result;
            }
        }
    }
     */

    public class IntList
    {
        public int[] items = new int[0];

        public void Add(int item)
        {
            int index = Array.BinarySearch(items, item);
            if (index >= 0)
            {
                return;
            }
            index = ~index;
            int[] newItems = new int[items.Length + 1];
            Array.Copy(items, newItems, index);
            Array.Copy(items, index, newItems, index + 1, items.Length - index);
            newItems[index] = item;
            items = newItems;
        }

        public bool Contains(int item)
        {
            return Array.BinarySearch(items, item) >= 0;
        }

        public int Count
        {
            get
            {
                return items.Length;
            }
        }

        public int this[int index]
        {
            get
            {
                return items[index];
            }
        }
    }
}