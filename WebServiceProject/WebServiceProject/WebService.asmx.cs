using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Web.Services.Protocols;

namespace WebServiceProject
{
    /// <summary>
    /// Summary description for WebService
    /// </summary>
    //[WebService(Namespace = "http://badplanet.ddns.net/")]
    [WebService(Namespace = "http://www.vendomatica.cl/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {
        public static int count = 0;
        class DBConn
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

        [WebMethod]
        public DataSet GetSaldoActualizado(string email, string password)
        {
            DataSet ds = new DataSet();
            DBConn MyConnection = new DBConn();

            MyConnection.Connection_ToDB();
            SqlCommand comm = new SqlCommand();
            comm.Connection = DBConn.conn;
            comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
            comm.Parameters.Add("password", SqlDbType.VarChar).Value = password;
            comm.CommandText = "SELECT saldo_actualizado FROM Transaccion_ultimo WHERE userid = (SELECT userid FROM Usuario WHERE email = (@email) AND password = (@password))";
            SqlDataAdapter adp = new SqlDataAdapter();
            adp.SelectCommand = comm;
            adp.Fill(ds);
            return ds;

        }

        [WebMethod]
        public DataSet NewTransaction(string email, int tipoTransaccion, string montoTransaccion)
        {
            int numRows = 0;
            
            DataTable table = new DataTable();

            decimal mTransaccion = 0;
            mTransaccion = System.Convert.ToDecimal(montoTransaccion);

            DBConn MyConnection = new DBConn();
            MyConnection.Connection_ToDB();

            SqlCommand comm = new SqlCommand();
            comm.Connection = DBConn.conn;
            comm.CommandText = "SP_Inserta_Transaccion";
            comm.CommandType = CommandType.StoredProcedure;

            comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
            comm.Parameters.Add("tipoTransaccion", SqlDbType.Int).Value = tipoTransaccion;
            comm.Parameters.Add("montoTransaccion", SqlDbType.Money).Value = mTransaccion;

            /*
            comm.Parameters.AddWithValue("@useridParam", userid);
            comm.Parameters.AddWithValue("@tipoTransaccion_id", tipoTransaccion_id);
            comm.Parameters.AddWithValue("@montoTransaccion", montoTransaccion);
            */

            numRows = comm.ExecuteNonQuery();
            table.TableName = "Table";
            table.Columns.Add("Rows", typeof(int));
            table.Rows.Add(numRows);
            DataSet ds = new DataSet();
            ds.Tables.Add(table);
            MyConnection.SqlConnectionClose();
            return ds;

        }
    }
}
