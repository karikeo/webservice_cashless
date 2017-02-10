using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

namespace SecureWebService
{
    public class DBConn
    {
        public static SqlConnection conn = null;

        public void Connection_ToDB()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["BDTRansactionConnectionString"].ConnectionString);
            conn.Open();
        }
        public void SqlConnectionClose()
        {
            conn.Close();
        }
    }
}