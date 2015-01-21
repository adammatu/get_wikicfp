using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace get_wikicfp2012.Probability
{
    public class GropuResultFlat
    {
        public int ID1;
        public int ID2;
        public DateTime Created;
        public double Score;
    }

    public class GroupResultSubItem
    {
        public DateTime Date;
        public double Score;
    }

    public class GroupResultItem : Dictionary<int, GroupResultSubItem>
    {
    }

    public class GroupResult : Dictionary<int, GroupResultItem>
    {
        public void Add(int id, int ev, DateTime date, double score)
        {
            if (!ContainsKey(id))
            {
                Add(id, new GroupResultItem());
            }
            if (this[id].ContainsKey(ev))
            {
                if (this[id][ev].Date > date)
                {
                    this[id][ev].Date = date;
                    this[id][ev].Score = score;
                }
            }
            else
            {
                this[id].Add(ev, new GroupResultSubItem()
                    {
                        Date = date,
                        Score = score
                    });
            }
        }

        public bool IsLater(int id, int ev, DateTime date)
        {
            if (ContainsKey(id))
            {
                if (this[id].ContainsKey(ev))
                {
                    return (this[id][ev].Date < date);
                }
            }
            return true;
        }

        public void Add(GroupResult first,int id, int ev, DateTime date, double score)
        {
            if (first.IsLater(id, ev, date))
            {
                Add(id, ev, date, score);
            }
        }
    }

    public abstract class GroupBase
    {
        public string Name = "";
        public abstract void Prepare();
        public abstract GroupResult GetFirst();
        public abstract GroupResult GetSecond(GroupResult first);

        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public GroupResult GetList(string query)
        {
            GroupResult result = new GroupResult();
            //
            Console.WriteLine("Read Start");
            SqlCommand command;
            SqlDataReader dr;
            //            
            connection.Open();
            command = connection.CreateCommand();
            command.CommandText = query;
            dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int id = Convert.ToInt32(dr["id"]);
                    DateTime date = Convert.ToDateTime(dr["date"]);
                    double score = Convert.ToDouble(dr["score"]);
                    int ev = Convert.ToInt32(dr["evID"]);
                    if (!result.ContainsKey(id))
                    {
                        result.Add(id,new GroupResultItem());
                    }
                    if (result[id].ContainsKey(ev))
                    {
                        if (result[id][ev].Date > date)
                        {
                            result[id][ev].Date = date;
                            result[id][ev].Score = score;
                        }
                    }
                    else
                    {
                        result[id].Add(ev, new GroupResultSubItem()
                            {
                                Date = date,
                                Score = score
                            });
                    }
                }
            }
            dr.Close();
            Console.WriteLine("Read End");
            return result;
        }
    }
}