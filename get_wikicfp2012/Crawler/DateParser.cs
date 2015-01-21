using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace get_wikicfp2012.Crawler
{
    class DateParser
    {
        static string[] months = { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
        static string[] monthsShort = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
        public static int[] countResults = { 0, 0, 0 };

        static public DateTime findDate(string text, string ID)
        {
            Regex special = new Regex("&#[0-9]*;");
            text = text.Replace("\n", " ").Replace("\r", " ").Replace("&nbsp;", " ");
            text = WebTools.RemoveTags(text);
            text = special.Replace(text, " ");
            Regex yearMatch = new Regex("[0-9]{4}");
            Regex yearMatch2 = new Regex("[0-9]{2}");
            Regex digit = new Regex("[0-9]+");

            DateTime result = DateTime.MinValue;
            //
            foreach (Match match in yearMatch.Matches(text))
             {
                string day = "";
                string month = "";
                int pos = match.Index - 2;
                for (int n = 0; n < 5; n++)
                {
                    if (pos < 0)
                    {
                        break;
                    }
                    int spacePos = text.LastIndexOf(" ", pos);
                    if (spacePos < 0)
                    {
                        break;
                    }
                    string word = text.Substring(spacePos + 1, pos - spacePos).ToLower();
                    if (month == "")
                    {
                        if ((Array.IndexOf(months, word) >= 0) ||
                            (Array.IndexOf(monthsShort, word) >= 0))
                        {
                            month = word;
                        }
                        if (month == "")
                        {
                            foreach (string name in months)
                            {
                                if (word.StartsWith(name))
                                {
                                    month = name;
                                    break;
                                }
                            }
                        }
                    }
                    if (day == "")
                    {
                        Match digitMatch = digit.Match(word);
                        if (digitMatch.Success)
                        {
                            day = digitMatch.Value;
                        }
                    }
                    pos = spacePos - 1;
                }
                if ((month == "") || (day == ""))
                {
                    continue;
                }
                DateTime newDate = DateTime.MinValue;
                try
                {
                    int iyear, imonth, iday;
                    iyear = Convert.ToInt32(match.Value);
                    imonth = Math.Max(Array.IndexOf(months, month), Array.IndexOf(monthsShort, month));
                    iday = Convert.ToInt32(day);
                    if ((iyear<1900)||(iyear>2100))
                    {
                        continue;
                    }
                    newDate = new DateTime(iyear, imonth + 1, iday);
                }
                catch
                {
                    newDate = DateTime.MinValue;
                }
                if (newDate > result)
                {
                    result = newDate;
                }
                //Console.WriteLine("{0}.{1}.{2}", match.Value, month, day);                
            }
            //
            if (result == DateTime.MinValue)
            {
                countResults[1]++;
                Match match = yearMatch.Match(ID);
                if (match.Success)
                {
                    int year;
                    if (Int32.TryParse(match.Value, out year))
                    {
                        result = new DateTime(year, 1, 1);
                    }
                }
                else
                {
                    match = yearMatch2.Match(ID);
                    if (match.Success)
                    {
                        int year;
                        if (Int32.TryParse(match.Value, out year))
                        {
                            if (year < 30)
                            {
                                result = new DateTime(2000 + year, 1, 1);
                            }
                            else
                            {
                                result = new DateTime(1900 + year, 1, 1);
                            }
                        }
                    }
                }
            }
            countResults[0]++;
            return result;
        }
    }
}
