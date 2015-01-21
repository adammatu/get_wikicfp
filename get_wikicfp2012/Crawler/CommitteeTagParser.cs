using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace get_wikicfp2012.Crawler
{
    class CommitteeTagParser
    {
        public static string[] linkWords = { "call", "paper", "new", "date", "organiz", "chair", "organis"};
        public static string[] committeeWords = { "committee", "committees", "comittee", "commitee", "committe", "commitees", "committes", "organisation" };
        public static string[] roleWords = { "chair", "board", "co-chair", "secretary", "program", "sponsorship" };        

        public static void Init()
        {
            string[] temp = new string[linkWords.Length + committeeWords.Length];
            Array.Copy(linkWords, temp, linkWords.Length);
            Array.Copy(committeeWords, 0, temp, linkWords.Length, committeeWords.Length);
            linkWords = temp;
        }

        public static TagStructure ParseCommitee(string text)
        {
            TagStructure result = new TagStructure();           
            text = text.Replace("\n", " ").Replace("\r", " ").Replace("&nbsp;", " ");
            int level = 0;
            Stack<string> tags = new Stack<string>();
            Stack<string> tagsFull = new Stack<string>();
            int pos = 0;
            int isBody = 0;
            bool isCommittee = false;
            int closingLevel = 0;
            int searchForLevel = 0;
            int prevLevel = -1;
            TagStructure commiteeTag = null;
            Stack<TagStructure> structureTag = new Stack<TagStructure>();
            while (pos < text.Length)
            {
                int tagPos = text.IndexOf("<", pos);
                if (tagPos < 0)
                {
                    break;
                }
                //
                string content = text.Substring(pos, tagPos - pos).Trim();
                string contentLower = content.ToLower().Trim();
                if (contentLower.Length > 0)
                {                    
                    bool found = false;
                    int wordCount = contentLower.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;
                    if (!isCommittee)
                    {
                        if (wordCount < 10)
                        {
                            foreach (string w in committeeWords)
                            {
                                if (contentLower.Contains(w))
                                {
                                    isCommittee = false;
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (found && (searchForLevel == 0))
                    {
                        searchForLevel = 1;
                        closingLevel = tags.Count;
                    }
                    if (found && (isBody > 0))
                    {
                        bool isLink = false;
                        if (tags.Contains("a"))
                        {
                            foreach (string tagFull in tagsFull)
                            {
                                if (tagFull.StartsWith("a "))
                                {
                                    if (tagFull.Contains("href"))
                                    {
                                        isLink = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!isLink)
                        {
                            //Console.WriteLine(content);
                            isCommittee = true;
                            commiteeTag = new TagStructure(tagsFull.Peek(), content);
                            commiteeTag.isCommitee = true;
                            structureTag.Push(commiteeTag);
                        }
                    }
                    if (isCommittee && (!found))
                    {
                        bool role = false;
                        foreach (string w in roleWords)
                        {
                            if (contentLower.Contains(w))
                            {
                                role = true;
                                break;
                            }
                        }
                        if (role)
                        {
                            structureTag.Peek().isRole = true;
                            structureTag.Peek().AddContent(content);
                            //Console.WriteLine("* " + content);
                        }
                        else
                        {
                            if (prevLevel > tags.Count)
                            {
                                structureTag.Peek().AddContent(content);
                                //Console.WriteLine("**+ " + content);                               
                            }
                            else
                            {
                                structureTag.Peek().AddContent(content);
                                //Console.WriteLine("** " + content);
                            }
                            prevLevel = tags.Count;
                        }
                    }
                }
                else
                {
                    prevLevel = -1;
                }
                //
                int tagEnd = text.IndexOf(">", tagPos);
                if (tagEnd < 0)
                {
                    break;
                }
                string tag = text.Substring(tagPos + 1, tagEnd - tagPos - 1).Trim().ToLower();
                if (tag.EndsWith("/"))
                {
                    pos = tagEnd + 1;
                    continue;
                }
                bool closing = tag.StartsWith("/");
                if (closing)
                {
                    tag = tag.Substring(1).Trim();
                }
                int spacePos = tag.IndexOf(" ");
                string tagName = ((spacePos < 0) ? tag : tag.Substring(0, spacePos)).Trim();
                if (!tagName.StartsWith("!") && (tagName != "img") && (tagName != "br"))
                {
                    if (closing)
                    {
                        if (tags.Contains(tagName))
                        {
                            while (tags.Pop() != tagName)
                            {
                            }
                            while (tags.Count < tagsFull.Count)
                            {
                                tagsFull.Pop();                                
                                if (isCommittee)
                                {
                                    if (tagsFull.Count == 0)
                                    {
                                        result.children.Add(commiteeTag);
                                        commiteeTag = null;
                                        structureTag.Clear();
                                        isCommittee = false;
                                    }
                                    else
                                    {
                                        if (structureTag.Count == 1)
                                        {
                                            TagStructure newTag = new TagStructure(tagsFull.Peek());
                                            newTag.children.Add(structureTag.Pop());
                                            structureTag.Push(newTag);
                                            commiteeTag = newTag;
                                        }
                                        else
                                        {
                                            structureTag.Pop();
                                        }
                                    }
                                }
                            }
                        }
                        /*
                        else
                        {
                            return;
                        }
                        */
                        if (tagName == "body")
                        {
                            isBody--;
                        }
                        if ((searchForLevel == 2) && (tags.Count < closingLevel))
                        {
                            if (isCommittee)
                            {
                                result.children.Add(commiteeTag);
                            }
                            commiteeTag = null;
                            structureTag.Clear();
                            isCommittee = false;
                            searchForLevel = 0;
                        }
                    }
                    else
                    {
                        tags.Push(tagName);
                        tagsFull.Push(tag);
                        if (isCommittee)
                        {
                            TagStructure newTag = new TagStructure(tag);
                            structureTag.Peek().children.Add(newTag);
                            structureTag.Push(newTag);
                        }
                        if (tagName == "body")
                        {
                            isBody++;
                        }
                        if (searchForLevel == 1)
                        {
                            closingLevel = tags.Count - 1;
                            searchForLevel = 2;
                        }
                    }
                }
                pos = tagEnd + 1;
            }
            return result;
        }        
    }
}
