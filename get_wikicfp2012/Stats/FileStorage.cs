using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace get_wikicfp2012.Stats
{
    public interface IFileStorable
    {
        string ToString();
        IFileStorable FromString(string text);
        int ID { get; }
    }

    public class FileStorage<T> where T : class,IFileStorable, new()
    {
        public static void Load(string prefix, int ID, Dictionary<int, T> list)
        {
            Load(GetFileName(prefix, ID), list);
        }

        public static void Load(string filename, Dictionary<int, T> list)
        {
            list.Clear();
            StreamReader file = new StreamReader(filename);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                T item = new T().FromString(line) as T;
                list.Add(item.ID, item);
            }
        }

        public static void Save(string prefix, int ID, Dictionary<int, T> list)
        {
            Save(GetFileName(prefix, ID), list);
        }

        public static void Save(string filename, Dictionary<int, T> list)
        {
            if (list.Count == 0)
            {
                throw new NullReferenceException();
            }
            using (StreamWriter sw = File.CreateText(filename))
            {
                foreach (IFileStorable line in list.Values)
                {
                    sw.WriteLine(line.ToString());
                }
            }
        }

        public static void Load(string prefix, int ID, List<T> list)
        {
            Load(GetFileName(prefix, ID), list);
        }

        public static void Load(string filename, List<T> list)
        {
            list.Clear();
            StreamReader file = new StreamReader(filename);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                T item = new T().FromString(line) as T;
                list.Add(item);
            }
        }

        public static void Save(string prefix, int ID, List<T> list)
        {
            Save(GetFileName(prefix, ID), list);
        }

        public static void Save(string filename, List<T> list)
        {
            using (StreamWriter sw = File.CreateText(filename))
            {
                foreach (IFileStorable line in list)
                {
                    sw.WriteLine(line.ToString());
                }
            }
        }


        private static string GetFileName(string prefix, int id)
        {
            return String.Format("{0}lines\\{1}{2}.txt", Program.CACHE_ROOT, prefix, id);
        }
    }
}