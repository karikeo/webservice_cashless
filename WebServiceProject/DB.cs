using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

namespace WebServiceProject
{
    class DB
    {
        public static SqlConnection connection = null;

        public void ConnectionOpen()
        {
            connection = new SqlConnection(ConfigurationManager.ConnectionStrings["BDTRansactionConnectionString"].ConnectionString);
            connection.Open();
        }
        public void ConnectionClose()
        {
            connection.Close();
        }
    }
}