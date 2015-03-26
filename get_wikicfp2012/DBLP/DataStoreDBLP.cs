using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace get_wikicfp2012.DBLP
{
    class DataStoreDBLP
    {
        Dictionary<string, int> names = new Dictionary<string, int>();
        Dictionary<string, int> groups = new Dictionary<string, int>();

        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);

        public DataStoreDBLP()
        {
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());                
            }
        }

        public void Close()
        {            
            connection.Close();
        }

        public int AddPerson(string name)
        {
            if (names.ContainsKey(name))
            {
                return names[name];
            }
            int id = 0;
            string sql;
            SqlCommand command = connection.CreateCommand();

            sql = String.Format("INSERT INTO [dbo].[tblPerson] ([Name],[Affiliation]) VALUES ('{0}',''); SELECT @@IDENTITY",
                name.Replace("'", "''"));
            command.CommandText = sql;
            id = Convert.ToInt32(command.ExecuteScalar());

            names.Add(name, id);
            return id;
        }

        public int AddEvent(string name, string group, int groupType, int type, DateTime date, string key, string url, string groupUrl)
        {
            string sql;
            SqlCommand command;
            //
            int groupId = -1;
            if (group != "")
            {
                string groupKey = String.Format("'{0}'|'{1:yyyy-MM-dd}'", group, date);
                if (groups.ContainsKey(groupKey))
                {
                    groupId = groups[groupKey];
                }
                else
                {
                    command = connection.CreateCommand();
                    if (group.Length > 200)
                    {
                        group = group.Substring(0, 200);
                    }
                    sql = String.Format("INSERT INTO [dbo].[tblEventGroup] ([Name],[Type],[Url],[Date]) VALUES ('{0}',{1},'{2}','{3:yyyy-MM-dd}'); SELECT @@IDENTITY",
                        group.Replace("'", "''"), groupType, groupUrl.Replace("'", "''"), date);
                    command.CommandText = sql;
                    groupId = Convert.ToInt32(command.ExecuteScalar());

                    groups.Add(groupKey, groupId);
                }
            }
            //
            int id = 0;
            command = connection.CreateCommand();
            sql = String.Format("INSERT INTO [dbo].[tblEvent] ([Name],[Type],[EventGroup_ID],[Key],[Url]) VALUES ('{0}',{1},{2},'{3}','{4}'); SELECT @@IDENTITY",
                name.Replace("'", "''"), 10 + type, groupId, key.Replace("'", "''"), url.Replace("'", "''")
                );
            command.CommandText = sql;
            id = Convert.ToInt32(command.ExecuteScalar());
            return id;
        }

        public void AddLink(int personId, int eventId)
        {
            string sql = String.Format("INSERT INTO [dbo].[tblLink] ([Person_ID],[Event_ID]) VALUES ({0},{1})",
                personId, eventId);
            SqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}