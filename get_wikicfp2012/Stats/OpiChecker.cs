using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace get_wikicfp2012.Stats
{
    public class OpiChecker
    {
        SqlConnection connection = new SqlConnection(Program.CONNECTION_STRING);
        Dictionary<int, TriOpiPerson> ids = new Dictionary<int, TriOpiPerson>();
        public bool IsLoaded { get; private set; }

        public OpiChecker()
        {
            IsLoaded = false;
        }

        public void Create()
        {
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return;
            }
            ids.Clear();
            string sql;
            SqlCommand command = connection.CreateCommand();

            sql = "SELECT * FROM [dbo].[tblPerson] where [OPI]=1";
            command.CommandText = sql;

            SqlDataReader dr = command.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    int ID = Convert.ToInt32(dr["ID"]);
                    if (!ids.ContainsKey(ID))
                    {
                        ids.Add(ID, new TriOpiPerson()
                            {
                                ID = ID
                            });
                    }
                }
            }
            dr.Close();
            connection.Close();
            FileStorage<TriOpiPerson>.Save("opi", 1, ids);
            IsLoaded = true;
        }

        public void Load()
        {
            FileStorage<TriOpiPerson>.Load("opi", 1, ids);
            IsLoaded = true;
        }

        public bool isOpi(int id)
        {
            return ids.ContainsKey(id);
        }
    }
}