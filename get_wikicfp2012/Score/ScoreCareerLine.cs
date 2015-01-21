using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace get_wikicfp2012.Stats
{
    public class ScoreCareerLine
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);
        
        public ScoreCareerLine CrawlerFirst()        
        {
            Dictionary<int, ScoreCareerLinePerson> lines = new Dictionary<int, ScoreCareerLinePerson>();
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //            
            connection.Open();
            //people
            command = connection.CreateCommand();
            command.CommandText = "select ID,StartYear from tblPerson";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int ID = Convert.ToInt32(dr["ID"]);
                    if (dr["StartYear"].GetType() == typeof(DBNull))
                    {
                        continue;
                    }
                    int StartYear = Convert.ToInt32(dr["StartYear"]);
                    lines.Add(ID, new ScoreCareerLinePerson
                    {
                        ID = ID,
                        StartYear = StartYear
                    });
                }
            }
            dr.Close();
            Console.WriteLine("People done");
            //
            command.CommandText = "select * from tblPersonScore";
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int pID = Convert.ToInt32(dr["Person_ID"]);
                    int year = Convert.ToInt32(dr["Year"]);
                    double score = Convert.ToDouble(dr["Score"]);
                    year -= lines[pID].StartYear;
                    lines[pID].Years[year] = score;
                    lines[pID].Length = Math.Max(lines[pID].Length, year + 1);
                }
            }
            dr.Close();
            Console.WriteLine("Score done");
            //
            FileStorage<ScoreCareerLinePerson>.Save("sline", 1, lines);
            return this;
        }

        private double Add(double v1, int c1, double v2, int c2)
        {
            return (v1 * c1 + v2 * c2) / (c1 + c2);
        }

        private ScoreCareerLinePersonExtended JoinLines(int maxId, ScoreCareerLinePersonExtended line1, ScoreCareerLinePersonExtended line2)
        {
            ScoreCareerLinePersonExtended newLine = new ScoreCareerLinePersonExtended();
            newLine.ID = maxId + 1;
            newLine.Length = (line1.Length > line2.Length) ? line1.Length : line2.Length;
            newLine.Level = line1.Level + line2.Level;
            for (int n = 0; n < newLine.Length; n++)
            {
                ScoreCareerLinePersonExtendedYearInfo item = new ScoreCareerLinePersonExtendedYearInfo();

                int cnt = 0;
                item.compareValue = 0;
                if (n < line1.Length)
                {
                    item.Join(line1.Years[n].allValues, line1.Years[n].allValuesLength);
                    item.compareValue += line1.Years[n].compareValue * line1.Years[n].allValuesLength;
                    cnt += line1.Years[n].allValuesLength;
                }
                if (n < line2.Length)
                {
                    item.Join(line2.Years[n].allValues, line2.Years[n].allValuesLength);
                    item.compareValue += line2.Years[n].compareValue * line2.Years[n].allValuesLength;
                    cnt += line2.Years[n].allValuesLength;
                }
                if (cnt > 0)
                {
                    item.compareValue /= cnt;
                }
                newLine.Years.Add(item);
            }
            return newLine;
        }

        private ushort Limit(ushort value)
        {
            if (value > 500)
            {
                value = 500;
            }
            return (ushort)(value * 100);
        }

        private double LineDistance(ScoreCareerLinePersonExtended line1, ScoreCareerLinePersonExtended line2, double maxDist)
        {
            int l = (line1.Length < line2.Length) ? line1.Length : line2.Length;
            double dist = 0;
            for (int n = 0; n < l; n++)
            {
                double d = line1.Years[n].compareValue - line2.Years[n].compareValue;
                dist += d * d;
                if (dist >= maxDist)
                {
                    return 1E9;
                }
            }
            return dist;
        }

        public bool Covered(Dictionary<int, ScoreCareerLinePersonExtended> linesExtended,int topClasses, double coverage)
        {
            List<ScoreCareerLinePersonExtended> largest = new List<ScoreCareerLinePersonExtended>();
            int smallestLargest = 0;
            int count = 0;
            foreach (ScoreCareerLinePersonExtended line in linesExtended.Values)
            {
                count += line.Level;
                if ((line.Level > smallestLargest) || (largest.Count < topClasses))
                {
                    largest.Add(line);
                    largest = largest.OrderByDescending(x => x.Level).Take(topClasses).ToList();
                    smallestLargest = largest.Select(x => x.Level).Min();
                }
            }
            int covered = largest.Select(x => x.Level).Sum();
            return covered > coverage * count;
        }

        public ScoreCareerLine LimitSets()
        {
            return LimitSets(5, 100, 10, 0.8);
        }

        public ScoreCareerLine LimitSets(int minLength,int maxCount, int topClasses,double coverage)
        {
            Dictionary<int, ScoreCareerLinePersonExtended> linesExtended = new Dictionary<int, ScoreCareerLinePersonExtended>();

            Console.WriteLine("Start");
            FileStorage<ScoreCareerLinePersonExtended>.Load("sline", 1, linesExtended);
            Console.WriteLine("Loaded");
            List<int> linesIdsi = linesExtended.Keys.ToList();
            int index = 0;
            while (index < linesIdsi.Count)
            {
                if (linesExtended[linesIdsi[index]].Length < minLength)
                {
                    linesExtended.Remove(linesIdsi[index]);
                }
                else
                {
                    if (linesExtended[linesIdsi[index]].Length > maxCount)
                    {
                        linesExtended[linesIdsi[index]].Length = maxCount;
                    }
                }
                index++;
            }
            Console.WriteLine("Empty Removed");
            Console.WriteLine("Count left: {0}", linesExtended.Count);
            //
            double maxDist = maxCount;
            while (!Covered(linesExtended, topClasses, coverage))
            {
                Console.WriteLine("MaxDist: {0}", maxDist);
                Dictionary<int, ScoreCareerLinePersonExtended> buckets = new Dictionary<int, ScoreCareerLinePersonExtended>();
                List<int> linesIdsb = linesExtended.Keys.ToList();
                int maxIdb = linesIdsb.Max();
                DateTime start = DateTime.Now;
                while (linesIdsb.Count > 0)
                {
                    ScoreCareerLinePersonExtended line = linesExtended[linesIdsb[0]];
                    linesIdsb.RemoveAt(0);

                    double distb = maxDist;
                    int idb = -1;
                    foreach (int keyb in buckets.Keys)
                    {
                        double _dist = LineDistance(line, buckets[keyb], distb);
                        if (_dist < distb)
                        {
                            distb = _dist;
                            idb = keyb;
                        }
                    }
                    if ((idb < 0) || (distb > maxDist))
                    {
                        buckets.Add(line.ID, line);
                    }
                    else
                    {
                        ScoreCareerLinePersonExtended newLine = JoinLines(maxIdb, line, buckets[idb]);
                        buckets.Remove(idb);
                        buckets.Add(newLine.ID, newLine);
                        maxIdb++;
                    }
                    if (((TimeSpan)(DateTime.Now - start)).TotalSeconds > 30)
                    {
                        Console.WriteLine("Count left: {0} | Buckets:{1}", linesIdsb.Count,buckets.Count);
                        start = DateTime.Now;
                    }
                }
                linesExtended.Clear();
                linesExtended = buckets;
                Console.WriteLine("--------------------");
                Console.WriteLine("Count left: {0}", linesExtended.Count);
                maxDist = maxDist * 1.2;
            }
            Console.WriteLine("Removed by bucketing");
            Console.WriteLine("Saving");
            FileStorage<ScoreCareerLinePersonExtended>.Save("sline", 2, linesExtended);
            return this;
        }

        public ScoreCareerLine CountLengths()
        {
            /*
            Console.WriteLine("Start");
            FileStorage<ScoreCareerLinePerson>.Load("sline", 1, lines);
            Console.WriteLine("Loaded");
            Dictionary<int, ScoreCareerLinePerson> lengths = new Dictionary<int, ScoreCareerLinePerson>();
            foreach (int id in lines.Keys)
            {
                ScoreCareerLinePerson line = lines[id];
                int start, length;
                // all
                start = 0;
                while ((start < 100) && (line.Years[start].committees == 0) && (line.Years[start].publications == 0))
                {
                    start++;
                }
                if (start < 100)
                {
                    length = 100 - (2019 - 2012) - start;
                    if (!lengths.ContainsKey(length))
                    {
                        lengths.Add(length, new ScoreCareerLinePerson()
                        {
                            Length = length,
                            Count = 0,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                    }
                    lengths[length].Count++;
                }
                // committees
                start = 0;
                while ((start < 100) && (line.Years[start].committees == 0))
                {
                    start++;
                }
                if (start < 100)
                {
                    length = 100 - (2019 - 2012) - start;
                    if (!lengths.ContainsKey(length))
                    {
                        lengths.Add(length, new ScoreCareerLinePerson()
                        {
                            Length = length,
                            Count = 0,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                    }
                    lengths[length].CountCommittee++;
                }
                // publications
                start = 0;
                while ((start < 100) && (line.Years[start].publications == 0))
                {
                    start++;
                }
                if (start < 100)
                {
                    length = 100 - (2019 - 2012) - start;
                    if (!lengths.ContainsKey(length))
                    {
                        lengths.Add(length, new ScoreCareerLinePerson()
                        {
                            Length = length,
                            Count = 0,
                            CountCommittee = 0,
                            CountPublication = 0
                        });
                    }
                    lengths[length].CountPublication++;
                }
            }
            FileStorage<CareerLineLength>.Save("sline", 3, lengths);
             */
            return this;
        }
    }
}