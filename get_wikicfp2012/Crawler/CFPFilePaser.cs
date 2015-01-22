using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace get_wikicfp2012.Crawler
{
    class CFPFilePaser
    {
        List<CFPFilePaserItem> items = new List<CFPFilePaserItem>();

        public void ScanNames(string filename)
        {
            Regex name = new Regex("(?<=(<span property=\"v:description\">)).*?(?=(</span>))");
            Regex link = new Regex("(?<=(Link: <a href=\")).*?(?=(\"))");
            foreach (string dir in Directory.GetDirectories(filename))
            {
                string[] files = Directory.GetFiles(dir, "*.html");
                foreach (string file in files)
                {
                    Console.WriteLine(file);
                    string content = File.ReadAllText(file);

                    items.Add(new CFPFilePaserItem
                    {
                        ID = Path.GetFileNameWithoutExtension(file),
                        Name = name.Match(content).Value,
                        Link = link.Match(content).Value
                    });
                }
            }
            Directory.CreateDirectory(Program.CACHE_ROOT + "cfp2");
            using (StreamWriter sw = File.CreateText(Program.CACHE_ROOT + "cfp2\\list.csv"))
            {
                foreach (CFPFilePaserItem item in items)
                {
                    sw.WriteLine(String.Format("{0}\t{1}\t{2}",item.ID,item.Name,item.Link));
                }
            }
        }

    }
}
