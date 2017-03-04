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
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace WebServiceProject
{
    public class Token : SoapHeader
    {
        private string _token;

        public string SecurityToken
        {
            get { return _token; }
            set { _token = value; }
        }
    }

    public class Authentication : SoapHeader
    {
        private string _email;
        private string _password;


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


    }




    /// <summary>
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://www.vendomatica.cl/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {
        public Authentication ServiceCredentials;
        public Token SecurityToken;

        private const string _alg = "HmacSHA256";

        #region Error messages
        const string ErrorMsg1 = "Wrong Service Credentials";
        const string ErrorMsg2 = "Wrong Token";
        const string ErrorMsg3 = "Wrong Service Credentials Header";
        const string ErrorMsg4 = "Invalid Email or Password";
        #endregion

        public DataSet showError(string err)
        {
            DataTable tableMsg = new DataTable();
            tableMsg.TableName = "Table";
            tableMsg.Columns.Add("Rows", typeof(string));
            tableMsg.Rows.Add(err);
            DataSet dsWrong = new DataSet();
            dsWrong.Tables.Add(tableMsg);
            return dsWrong;
        }

        public bool CheckSecurityHeader(Token securityToken)
        {
            if ((SecurityToken == null) || (string.IsNullOrEmpty(SecurityToken.SecurityToken)))
            {
                return false;
            }
            if (IsTokenValid(SecurityToken.SecurityToken))
            {
                return true;
            }
            else return false;
        }

        public string GetEmailFromToken(string token)
        {
            string key = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            string[] parts = key.Split(new char[] { ':' });
            string email = parts[1];
            return email;
        }


        public static string GenerateToken(string email)
        {
            DB SQLConnection = new DB();
            SQLConnection.ConnectionOpen();


            SqlCommand comm = new SqlCommand();
            comm.Connection = DB.connection;
            comm.CommandText = "GetPassAndSalt";
            comm.CommandType = CommandType.StoredProcedure;
            comm.Parameters.Add("@pEmail", SqlDbType.VarChar).Value = email;
            /*
            SqlParameter output = new SqlParameter("@pSalt", SqlDbType.UniqueIdentifier);
            output.Direction = ParameterDirection.Output;
            comm.Parameters.Add(output);
            */
            SqlDataReader reader;
            reader = comm.ExecuteReader();

            string password = "";
            string salt = "";

            while (reader.Read())
            {
                salt = reader.GetValue(0).ToString();
                password = reader.GetValue(1).ToString();

            }
            reader.Close();

            SQLConnection.ConnectionClose();

            //  string salt = output.Value.ToString();

            DateTime currentDate = DateTime.UtcNow;
            long ticks = currentDate.Ticks;

            string hash = string.Join(":", new string[] { email, ticks.ToString() });
            string hashLeft = "";
            string hashRight = "";
            using (HMAC hmac = HMACSHA256.Create(_alg))
            {
                hmac.Key = Encoding.UTF8.GetBytes(GetHashedPassword(password, salt));
                hmac.ComputeHash(Encoding.UTF8.GetBytes(hash));
                hashLeft = Convert.ToBase64String(hmac.Hash);
                hashRight = string.Join(":", new string[] { email, ticks.ToString() });
            }

            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(":", hashLeft, hashRight)));

            SQLConnection.ConnectionOpen();

            SqlCommand comm2 = new SqlCommand();
            comm2.Connection = DB.connection;
            comm2.CommandText = "AddToken";
            comm2.CommandType = CommandType.StoredProcedure;
            comm2.Parameters.Add("@pToken", SqlDbType.NVarChar, 200).Value = token;

            comm2.ExecuteNonQuery();
            SQLConnection.ConnectionClose();

            return token;
        }

        public static string GetHashedPassword(string password, string salt)
        {
            string key = string.Join(":", new string[] { password, salt });
            using (HMAC hmac = HMACSHA256.Create(_alg))
            {
                // Hash the key.
                hmac.Key = Encoding.UTF8.GetBytes(salt);
                hmac.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hmac.Hash);
            }
        }

        private bool IsUserValid(string email, string password)
        {

            DB SQLConnection = new DB();
            SQLConnection.ConnectionOpen();

            SqlCommand comm = new SqlCommand();
            comm.Connection = DB.connection;
            comm.CommandText = "AuthenticateUser";
            comm.CommandType = CommandType.StoredProcedure;

            comm.Parameters.Add("@pEmail", SqlDbType.VarChar).Value = email;
            comm.Parameters.Add("@pPassword", SqlDbType.VarChar).Value = password;


            SqlParameter output = new SqlParameter("@responseMessage", SqlDbType.Int);
            output.Direction = ParameterDirection.Output;
            comm.Parameters.Add(output);

            comm.ExecuteNonQuery();
            SQLConnection.ConnectionClose();
            int res = int.Parse(output.Value.ToString());
            if (res == 1)
            {

                return true;
            }
            else { return false; }


        }

        public static bool IsTokenValid(string token)
        {
            bool result = false;

            DB SQLConnection = new DB();
            SQLConnection.ConnectionOpen();
            SqlCommand comm = new SqlCommand();
            comm.Connection = DB.connection;

            comm.Parameters.Add("token", SqlDbType.VarChar).Value = token;
            comm.CommandText = "SELECT COUNT(*) FROM Sessions WHERE token = @token";

            result = ((int)comm.ExecuteScalar()) > 0 ? true : false;

            if (result == true)
                UpdateTokenTime(token);

            SQLConnection.ConnectionClose();
            return result;
        }

        public static void UpdateTokenTime(string token)
        {
            SqlCommand comm2 = new SqlCommand();
            comm2.Connection = DB.connection;
            comm2.CommandType = CommandType.StoredProcedure;
            comm2.Parameters.Add("@pToken", SqlDbType.NVarChar, 200).Value = token;
            comm2.CommandText = "UpdateTokenTime";
            comm2.ExecuteNonQuery();
        }

        [WebMethod]
        [SoapHeader("ServiceCredentials")]
        public string AuthenticateUser()
        {
            if (ServiceCredentials == null)
                return ErrorMsg3;
            if (string.IsNullOrEmpty(ServiceCredentials.Email) || string.IsNullOrEmpty(ServiceCredentials.Password))
                return ErrorMsg1;
            // Are the credentials valid?
            if (!IsUserValid(ServiceCredentials.Email, ServiceCredentials.Password))
                return ErrorMsg4;

            string token = null;

            token = GenerateToken(ServiceCredentials.Email);
            return token;
        }

        [WebMethod]
        public string NewUser(string iNombre, string iRut, string iPassword, string iMontoasignado, int iCodcliente, int iCodinterno, int iNroTarjeta, int iEstado, string iEmail)
        {

            //Сделать проверки (email и т.д., кол-во символов, фильтрацию входящих данных)
            if (String.IsNullOrEmpty(iNombre) || (String.IsNullOrEmpty(iPassword)) || (String.IsNullOrEmpty(iEmail)))
            { return "Wrong Input Data!"; }
            DB SQLConnection = new DB();
            SQLConnection.ConnectionOpen();

            SqlCommand comm = new SqlCommand();
            comm.Connection = DB.connection;
            comm.CommandText = "AddUser";
            comm.CommandType = CommandType.StoredProcedure;

            comm.Parameters.Add("pNombre", SqlDbType.NVarChar).Value = iNombre;
            if (String.IsNullOrEmpty(iRut))
            { comm.Parameters.Add("pRut", SqlDbType.VarChar).Value = 0; }
            else
            { comm.Parameters.Add("pRut", SqlDbType.VarChar).Value = iRut; }
            comm.Parameters.Add("pPassword", SqlDbType.VarChar).Value = iPassword;
            if (String.IsNullOrEmpty(iRut))
            { comm.Parameters.Add("pMontoasignado", SqlDbType.Money).Value = 0; }
            else
            { comm.Parameters.Add("pMontoasignado", SqlDbType.Money).Value = Convert.ToDecimal(iMontoasignado); }
            comm.Parameters.Add("pCodcliente", SqlDbType.Int).Value = iCodcliente;
            comm.Parameters.Add("pCodinterno", SqlDbType.Int).Value = iCodinterno;
            comm.Parameters.Add("pNroTarjeta", SqlDbType.Int).Value = iNroTarjeta;
            comm.Parameters.Add("pEstado", SqlDbType.Int).Value = iEstado;
            comm.Parameters.Add("pEmail", SqlDbType.VarChar).Value = iEmail;

            SqlParameter output = new SqlParameter("@responseMessage", SqlDbType.VarChar, 5000);
            output.Direction = ParameterDirection.Output;
            comm.Parameters.Add(output);

            comm.ExecuteNonQuery();
            SQLConnection.ConnectionClose();
            return output.Value.ToString();

        }

        [WebMethod]
        public bool TestIsValidToken(string token)
        {
            return IsTokenValid(token);
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet GetSaldoActualizado()
        {

            if (CheckSecurityHeader(SecurityToken) == true)
            {
                string token = SecurityToken.SecurityToken;
                string email = GetEmailFromToken(token);

                DataSet ds = new DataSet();
                DB SQLConnection = new DB();

                SQLConnection.ConnectionOpen();
                SqlCommand comm = new SqlCommand();
                comm.Connection = DB.connection;
                comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
                comm.CommandText = "SELECT saldo_actualizado FROM Transaccion_ultimo WHERE userid = (SELECT userid FROM Usuario WHERE email = (@email))";
                SqlDataAdapter adp = new SqlDataAdapter();
                adp.SelectCommand = comm;
                adp.Fill(ds);
                SQLConnection.ConnectionClose();
                return ds;
            }
            else
            {
                return showError(ErrorMsg2);
            }
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet NewTransaction(int tipoTransaccion, string montoTransaccion)
        {
            if (CheckSecurityHeader(SecurityToken) == true)
            {
                string token = SecurityToken.SecurityToken;
                string email = GetEmailFromToken(token);

                int numRows = 0;

                DataTable table = new DataTable();

                decimal mTransaccion = 0;
                mTransaccion = System.Convert.ToDecimal(montoTransaccion);

                DB SQLConnection = new DB();
                SQLConnection.ConnectionOpen();

                SqlCommand comm = new SqlCommand();
                comm.Connection = DB.connection;
                comm.CommandText = "SP_Inserta_Transaccion";
                comm.CommandType = CommandType.StoredProcedure;

                comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
                comm.Parameters.Add("tipoTransaccion", SqlDbType.Int).Value = tipoTransaccion;
                comm.Parameters.Add("montoTransaccion", SqlDbType.Money).Value = mTransaccion;


                numRows = comm.ExecuteNonQuery();
                table.TableName = "Table";
                table.Columns.Add("Rows", typeof(int));
                table.Rows.Add(numRows);
                DataSet ds = new DataSet();
                ds.Tables.Add(table);
                SQLConnection.ConnectionClose();
                return ds;
            }
            else
            {
                return showError(ErrorMsg2);
            }
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet GetVendicontMAC(string serie)
        {
            if (CheckSecurityHeader(SecurityToken) == true)
            {

                DataSet ds = new DataSet();
                DB SQLConnection = new DB();

                SQLConnection.ConnectionOpen();

                SqlCommand comm = new SqlCommand();
                comm.Connection = DB.connection;
                comm.Parameters.Add("serie", SqlDbType.VarChar).Value = serie;
                comm.CommandText = "SELECT vendicontMAC FROM Maquinas WHERE serie = (@serie)";

                SqlDataAdapter adp = new SqlDataAdapter();
                adp.SelectCommand = comm;
                adp.Fill(ds);
                SQLConnection.ConnectionClose();
                return ds;
            }
            else
            {
                return showError(ErrorMsg2);
            }
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet GetUserTransaction(int count)
        {
            if (CheckSecurityHeader(SecurityToken) == true)
            {
                string token = SecurityToken.SecurityToken;
                string email = GetEmailFromToken(token);

                DataSet ds = new DataSet();
                DB SQLConnection = new DB();

                SQLConnection.ConnectionOpen();
                SqlCommand comm = new SqlCommand();
                comm.Connection = DB.connection;
                comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
                comm.Parameters.Add("count", SqlDbType.Int).Value = count;
                comm.CommandText = "SELECT TOP (@count) Transaccion.tipoTransaccion_id, Transaccion.montoTransaccion, Transaccion.FechaHora, Transaccion.SaldoDespuesTransaccion, Usuario.nombre FROM dbo.Transaccion, dbo.Usuario WHERE Transaccion.userid = (SELECT Usuario.userid FROM dbo.Usuario WHERE Usuario.email = (@email)) AND Usuario.email = (@email)";
                SqlDataAdapter adp = new SqlDataAdapter();
                adp.SelectCommand = comm;
                adp.Fill(ds);
                SQLConnection.ConnectionClose();
                return ds;
            }
            else
            {
                return showError(ErrorMsg2);
            }
        }
    }
}
