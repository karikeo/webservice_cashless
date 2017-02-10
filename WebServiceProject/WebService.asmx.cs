using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Web.Services.Protocols;
using System.Web.SessionState;

namespace WebServiceProject
{
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

    public class Authentication : SoapHeader
    {
        private string _email;
        private string _password;
        private string _token;

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }
    }

    public class MySession
    {
        // private constructor
        private MySession() { }

        // Gets the current session.
        public static MySession Current
        {
            get
            {
                MySession session = (MySession)HttpContext.Current.Session["__MySession__"];
                if (session == null)
                {
                    session = new MySession();
                    HttpContext.Current.Session["__MySession__"] = session;
                    HttpContext.Current.Session.Timeout = 1;
                }
                return session;
            }
        }

        public string Token { get; set; }
        public DateTime MyDate { get; set; }
    }

    

    /// <summary>!!!
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://badplanet.ddns.net/")]
    //[WebService(Namespace = "http://www.vendomatica.cl/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {
        public Authentication ServiceCredentials;

        public bool AuthenticateUser(Authentication ServiceCredentials)
        {
            if (ServiceCredentials == null)
                return false;
            if (string.IsNullOrEmpty(ServiceCredentials.Token))
               { return false; } 
            else
            {

            }
            if (string.IsNullOrEmpty(ServiceCredentials.Email) || string.IsNullOrEmpty(ServiceCredentials.Password))
                return false;
            return true;
        }

        private bool IsUserValid(string email, string password)
        {
            // Ask the SQL Memebership to verify the credentials for us
               return System.Web.Security.Membership.ValidateUser(email, password);
            //return true;
        }

        private bool IsUserValid(Authentication ServiceCredentials)
        {
            if (ServiceCredentials == null)
                return false;

            // Does the token exists in our Cache?
            if (!string.IsNullOrEmpty(ServiceCredentials.Token))
                return (ServiceCredentials.Token != null);

            return false;
        }


        [WebMethod(EnableSession = true)]
        [SoapHeader("ServiceCredentials")]
        public DataSet GetSaldoActualizado(string email, string password)
        {
            if (AuthenticateUser(ServiceCredentials) != true)
            {
                DataTable table = new DataTable();
                table.TableName = "Table";
                table.Columns.Add("Rows", typeof(string));
                table.Rows.Add("Wrong Service Credentials");
                DataSet dsWrong = new DataSet();
                dsWrong.Tables.Add(table);
                return dsWrong;
            }

            DataSet ds = new DataSet();
            DBConn MyConnection = new DBConn();

            MyConnection.Connection_ToDB();
            SqlCommand comm = new SqlCommand();
            comm.Connection = DBConn.conn;
            comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
            comm.Parameters.Add("password", SqlDbType.VarChar).Value = password;
            comm.CommandText = "SELECT Transaccion_ultimo.saldo_actualizado,Usuario.nombre FROM dbo.Transaccion_ultimo, dbo.Usuario WHERE Transaccion_ultimo.userid = (SELECT Usuario.userid FROM dbo.Usuario WHERE Usuario.email = (@email) AND Usuario.password = (@password)) AND Usuario.email = (@email)";
            SqlDataAdapter adp = new SqlDataAdapter();
            adp.SelectCommand = comm;
            adp.Fill(ds);
            return ds;

        }

        [WebMethod(EnableSession = true)]
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

        [WebMethod(EnableSession = true)]
        public DataSet GetVendicontMAC(string serie)
        {
            DataSet ds = new DataSet();
            DBConn MyConnection = new DBConn();

            MyConnection.Connection_ToDB();

            SqlCommand comm = new SqlCommand();
            comm.Connection = DBConn.conn;
            comm.Parameters.Add("serie", SqlDbType.VarChar).Value = serie;
            comm.CommandText = "SELECT vendicontMAC FROM Maquinas WHERE serie = (@serie)";

            SqlDataAdapter adp = new SqlDataAdapter();
            adp.SelectCommand = comm;
            adp.Fill(ds);
            return ds;

        }

        [WebMethod(EnableSession = true)]
        public DataSet GetUserTransaction(string email, int count)
        {

            DataSet ds = new DataSet();
            DBConn MyConnection = new DBConn();

            MyConnection.Connection_ToDB();
            SqlCommand comm = new SqlCommand();
            comm.Connection = DBConn.conn;
            comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
            comm.Parameters.Add("count", SqlDbType.Int).Value = count;
            comm.CommandText = "SELECT TOP (@count) tipoTransaccion_id,montoTransaccion,FechaHora,SaldoDespuesTransaccion FROM Transaccion WHERE userid = (SELECT userid FROM Usuario WHERE email = (@email))";
            SqlDataAdapter adp = new SqlDataAdapter();
            adp.SelectCommand = comm;
            adp.Fill(ds);
            return ds;

        }
    }
}