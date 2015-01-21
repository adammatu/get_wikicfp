using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace get_wikicfp2012.Crawler
{
    public class TagStructure
    {
        public string name = "";
        public string content = "";
        public bool isRole = false;
        public bool isCommitee = false;
        public bool isName = false;
        public bool isAffiliation = false;

        public List<FindResult> authorsIdentified = new List<FindResult>();
        public List<TagStructure> children = new List<TagStructure>();

        public TagStructure()
        {
            name = "***";
        }

        public string FilterName(string name)
        {
            int space=name.IndexOf(" ");
            if (space < 0)
            {
                return name;
            }
            string result = name.Substring(0, space);
            int cl = name.IndexOf("class");
            if (cl > 0)
            {
                name = name.Substring(cl + 5);
                if (name.StartsWith("="))
                {
                    name = name.Substring(1);
                    if (name.StartsWith("\""))
                    {
                        name = name.Substring(1);
                        int pos = name.IndexOf('"');
                        if (pos > 0)
                        {
                            result += "." + name.Substring(0, pos);
                        }
                    }
                    else if (name.StartsWith("'"))
                    {
                        name = name.Substring(1);
                        int pos = name.IndexOf('\'');
                        if (pos > 0)
                        {
                            result += "." + name.Substring(0, pos);
                        }
                    }
                    else
                    {
                        int l = 0;
                        while ((l < name.Length) && (Char.IsLetterOrDigit(name[l]) || (name[l] == '-') || (name[l] == '_')))
                        {
                            l++;
                        }
                        if (l > 0)
                        {
                            result += "." + name.Substring(0, l);
                        }
                    }
                }
            }
            return result;
        }

        public TagStructure(string name)
        {
            this.name = FilterName(name);
        }

        public TagStructure(string name, string content)
        {
            this.name = FilterName(name);
            this.content = content;
        }

        public void AddContent(string newContent)
        {
            /*
            if (String.IsNullOrEmpty(content))
            {
                content = newContent;
            }
            else
            {
                content = String.Format("{0}, {1}", content, newContent);
            }
             */
            children.Add(new TagStructure("[text]", newContent));
        }

        public int ScanNames(CFPStorageData storage)
        {
            //
            foreach (string word in CFPStorageData.Split(CFPStorageData.FixItem(content).ToLower()))
            {
                if (CFPStorageData.namesWords.Contains(word))
                {
                    isName = true;
                    break;
                }
            }
            // affiliation
            foreach (string word in CFPStorageData.affiliationWords)
            {
                if (content.ToLower().Contains(word))
                {
                    isAffiliation = true;
                    break;
                }
            }
            //
            authorsIdentified = storage.Find(content);
            int result = authorsIdentified.Count;
            foreach (TagStructure child in children)
            {
                result += child.ScanNames(storage);
            }
            if ((result == 0) && (children.Count > 1))
            {
                authorsIdentified = storage.Find(CumulatedContent());
                result = authorsIdentified.Count;
            }
            return result;
        }

        private string CumulatedContent()
        {
            StringBuilder result = new StringBuilder();
            result.Append(content);
            foreach (TagStructure child in children)
            {
                result.Append(" ");
                result.Append(child.CumulatedContent());
            }
            if (result.ToString().ToUpper().Contains("MORZY"))
            {
                int t = 0;
            }
            return result.ToString();
        }

        public static object fileLock = new object();

        public void Print()
        {
            lock (fileLock)
            {
                Print(0);
            }
        }

        public void Print(int level)
        {
            using (StreamWriter sw = new StreamWriter(Program.CACHE_ROOT + "cfp2\\res.txt", true))
            {
                sw.WriteLine("{0}{1} {2} {3} {4} {5}", 
                    new String(' ', level), name, 
                    (isRole) ? "role" : "", (isCommitee) ? "commitee" : "",
                    (isName) ? "name" : "", (isAffiliation) ? "affiliation" : "");
                if (!String.IsNullOrEmpty(content))
                {
                    sw.WriteLine("{0}{1}", new String(' ', level + 1), content);
                    sw.WriteLine("{0}{1}", new String(' ', level + 1), CFPStorageData.FixItem(content).ToLower());
                    if (authorsIdentified.Count > 0)
                    {
                        sw.WriteLine("{0}{1}", new String(' ', level + 1), String.Join(",", authorsIdentified));
                    }
                }
            }
            foreach (TagStructure child in children)
            {
                child.Print(level + 1);
            }
        }
    }
}
