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

namespace SecureWebService
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
    /// Сводное описание для WebService
    /// </summary>
    [WebService(Namespace = "http://www.vendomatica.cl/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Чтобы разрешить вызывать веб-службу из скрипта с помощью ASP.NET AJAX, раскомментируйте следующую строку. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {
        public Authentication ServiceCredentials;
        public Token SecurityToken;

        #region GenerateToken and GetHashedPassword
        private const string _alg = "HmacSHA256";
        private const string _salt = "Ysj2hZAVx2kSut2VO92r"; //https://www.random.org/strings/
        private const int _expirationMinutes = 5;

        public static string GenerateToken(string email, string password)
        {
            DateTime currentDate = DateTime.UtcNow;
            long ticks = currentDate.Ticks;

            string hash = string.Join(":", new string[] { email, ticks.ToString() });
            string hashLeft = "";
            string hashRight = "";
            using (HMAC hmac = HMACSHA256.Create(_alg))
            {
                hmac.Key = Encoding.UTF8.GetBytes(GetHashedPassword(password));
                hmac.ComputeHash(Encoding.UTF8.GetBytes(hash));
                hashLeft = Convert.ToBase64String(hmac.Hash);
                hashRight = string.Join(":", new string[] { email, ticks.ToString() });
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(":", hashLeft, hashRight)));
        }

        public static string GenerateToken(string email, string password, long ticks)
        {
            string hash = string.Join(":", new string[] { email, ticks.ToString() });
            string hashLeft = "";
            string hashRight = "";
            using (HMAC hmac = HMACSHA256.Create(_alg))
            {
                hmac.Key = Encoding.UTF8.GetBytes(GetHashedPassword(password));
                hmac.ComputeHash(Encoding.UTF8.GetBytes(hash));
                hashLeft = Convert.ToBase64String(hmac.Hash);
                hashRight = string.Join(":", new string[] { email, ticks.ToString() });
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(":", hashLeft, hashRight)));
        }




        public static string GetHashedPassword(string password)
        {
            string key = string.Join(":", new string[] { password, _salt });
            using (HMAC hmac = HMACSHA256.Create(_alg))
            {
                // Hash the key.
                hmac.Key = Encoding.UTF8.GetBytes(_salt);
                hmac.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hmac.Hash);
            }
        }
        #endregion


        public static bool IsTokenValid(string token)
        {
            bool result = false;

            try
            {
                // Base64 decode the string, obtaining the token:username:timeStamp.
                string key = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                // Split the parts.
                string[] parts = key.Split(new char[] { ':' });
                if (parts.Length == 3)
                {
                    // Get the hash message, username, and timestamp.
                    string hash = parts[0];
                    string email = parts[1];
                    long ticks = long.Parse(parts[2]);
                    DateTime timeStamp = new DateTime(ticks);
                    // Ensure the timestamp is valid.
                    bool expired = Math.Abs((DateTime.UtcNow - timeStamp).TotalMinutes) > _expirationMinutes; // if token expired?
                    if (!expired)
                    {
                        bool auth = false;

                        DBConn MyConnection = new DBConn();
                        MyConnection.Connection_ToDB();

                        SqlCommand comm = new SqlCommand();
                        comm.Connection = DBConn.conn;

                        comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
                        //comm.Parameters.Add("password", SqlDbType.VarChar).Value = password;

                        comm.CommandText = "SELECT COUNT(*) FROM Usuario WHERE email = @email";

                        auth = ((int)comm.ExecuteScalar()) > 0 ? true : false;
                        comm.ExecuteNonQuery();

                        if (auth)
                        {
                            SqlCommand comm2 = new SqlCommand();
                            comm2.Connection = DBConn.conn;
                            comm2.Parameters.Add("email", SqlDbType.VarChar).Value = email;
                            comm2.CommandText = "SELECT password FROM Usuario WHERE email = @email";
                            var pass = comm2.ExecuteScalar();
                            MyConnection.SqlConnectionClose();

                            string password = pass.ToString();
                            // Hash the message with the key to generate a token.
                            string computedToken = GenerateToken(email, password, ticks);
                            // Compare the computed token with the one supplied and ensure they match.
                            result = (token == computedToken);

                            //add 3 minutes
                            //string[] partTick = key.Split(new char[] { ':' });
                            //long tick = long.Parse(parts[2]);
                            //tick = TimeSpan.TicksPerMinute * 3;
                            //token = GenerateToken(email,password,tick);

                        }
                    }
                }
            }
            catch
            {
            }
            return result;
        }



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
            token = GenerateToken(ServiceCredentials.Email, ServiceCredentials.Password);
            return token;

            /*
			// Create and store the AuthenticatedToken before returning it
			string token = Guid.NewGuid().ToString();

			HttpRuntime.Cache.Add(
				token,
				ServiceCredentials.Email,
				null,
				System.Web.Caching.Cache.NoAbsoluteExpiration,
				TimeSpan.FromMinutes(5),
				System.Web.Caching.CacheItemPriority.NotRemovable,
				null);
			
			return token;
			*/

        }

        private bool IsUserValid(string email, string password)
        {
            bool authenticated = false;

            DBConn MyConnection = new DBConn();
            MyConnection.Connection_ToDB();

            SqlCommand comm = new SqlCommand();
            comm.Connection = DBConn.conn;

            comm.Parameters.Add("email", SqlDbType.VarChar).Value = email;
            comm.Parameters.Add("password", SqlDbType.VarChar).Value = password;

            comm.CommandText = "SELECT COUNT(*) FROM Usuario WHERE email = @email AND password = @password";

            authenticated = ((int)comm.ExecuteScalar()) > 0 ? true : false;
            comm.ExecuteNonQuery();

            MyConnection.SqlConnectionClose();

            return authenticated;

            /*
			SqlDataAdapter adp = new SqlDataAdapter();
			adp.SelectCommand = comm;

			DataSet ds = new DataSet();
			adp.Fill(ds);
			MyConnection.SqlConnectionClose();

			int count = ds.Tables[0].Rows.Count;
			//If count is equal to 1 - OK
			if (count == 1)
			{
				return true;
			}
			else
			{
				return false;
			}
			*/

            // Ask the SQL Memebership to verify the credentials for us
            //   return System.Web.Security.Membership.ValidateUser(Username, Password);
            //if (ServiceCredentials.Email == "anton" && ServiceCredentials.Password == "world")
            //	return true;

            //return false;
        }

        /*
		private bool IsUserValid(Authentication ServiceCredentials)
		{
			if (ServiceCredentials == null)
				return false;

			// Does the token exists in our Cache?
			if (!string.IsNullOrEmpty(ServiceCredentials.Token))
				return (HttpRuntime.Cache[ServiceCredentials.Token] != null);

			return false;
		}
		
			 
		*/

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public string HelloWorld()
        {
            if (SecurityToken == null)
                return ErrorMsg3;
            if (string.IsNullOrEmpty(SecurityToken.SecurityToken))
                return ErrorMsg1;
            // Are the credentials valid?
            if (IsTokenValid(SecurityToken.SecurityToken))
            {
                return "Hello i'm here!";
            }
            else
            {
                return "Please call AuthenitcateUser() first.";
            }
        }


        //[WebMethod]
        //[SoapHeader("ServiceCredentials")]
        //public string Test()
        //{
        //	if (ServiceCredentials == null)
        //	{
        //		return ErrorMsg3;
        //	}
        //	if (string.IsNullOrEmpty(ServiceCredentials.Token))
        //	{
        //		return ErrorMsg2;
        //	}
        //	try
        //	{
        //		if (!HttpRuntime.Cache[ServiceCredentials.Token.ToString()].Equals(ServiceCredentials.Token.ToString()))
        //		{
        //			return string.Format("Hello from Test(). The GUID is {0}", ServiceCredentials.Token);
        //		}
        //		return "Error authentication";
        //	}
        //	catch{
        //		new SoapException("Fault occurred", SoapException.ClientFaultCode);
        //		return "Wrong authentication";
        //		}
        //	finally { }
        //	}

        [WebMethod]
        [SoapHeader("SecurityToken")]
        public DataSet GetSaldoActualizado()
        {
            if (SecurityToken == null)
            {
                return showError(ErrorMsg3);
            }
            if (string.IsNullOrEmpty(SecurityToken.SecurityToken))
            {
                return showError(ErrorMsg1);
            }
            if (IsTokenValid(SecurityToken.SecurityToken))
            {
                string token = SecurityToken.SecurityToken;
                // Base64 decode the string, obtaining the token:username:timeStamp.
                string key = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                // Split the parts.
                string[] parts = key.Split(new char[] { ':' });
                string email = parts[1];

                DBConn MyConnection = new DBConn();
                MyConnection.Connection_ToDB();
                SqlCommand comm2 = new SqlCommand();
                comm2.Connection = DBConn.conn;
                comm2.Parameters.Add("email", SqlDbType.VarChar).Value = email;
                comm2.CommandText = "SELECT password FROM Usuario WHERE email = @email";
                var pass = comm2.ExecuteScalar();
                string password = pass.ToString();


                try
                {
                    DataSet ds = new DataSet();
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
                catch
                {
                    new SoapException("Fault occurred", SoapException.ClientFaultCode);
                    return showError(ErrorMsg1);
                }
                finally { }
            }
            else { return showError(ErrorMsg2); }
        }

        [WebMethod]
        public bool TestIsValidToken(string token)
        {
            return IsTokenValid(token);
        }
    }
}
