using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace get_wikicfp2012.ProbabilityGroups
{
    public interface IFileStorable2
    {
        string ToString();
        IFileStorable2 FromString(string text);
        int ID { get; }
        int ID2 { get; }
    }

    public class FileStorage2<T> where T : class,IFileStorable2, new()
    {
        public static void Load(string prefix, int ID, Dictionary<int, List<T>> list)
        {
            Load(GetFileName(prefix, ID), list);
        }

        public static void Load(string filename, Dictionary<int, List<T>> list)
        {
            list.Clear();
            StreamReader file = new StreamReader(filename);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                T item = new T().FromString(line) as T;
                if (!list.ContainsKey(item.ID))
                {
                    list.Add(item.ID, new List<T>());
                }
                list[item.ID].Add(item);
            }
        }

        public static void Save(string prefix, int ID, Dictionary<int, List<T>> list)
        {
            Save(GetFileName(prefix, ID), list);
        }

        public static void Save(string filename, Dictionary<int, List<T>> list)
        {
            if (list.Count == 0)
            {
                throw new NullReferenceException();
            }
            using (StreamWriter sw = File.CreateText(filename))
            {
                foreach (List<T> lines in list.Values)
                {
                    foreach (T line in lines)
                    {
                        sw.WriteLine(line.ToString());
                    }
                }
            }
        }

        private static string GetFileName(string prefix, int id)
        {
            return String.Format("{0}lines\\{1}{2}.txt", Program.CACHE_ROOT, prefix, id);
        }
    }
}
