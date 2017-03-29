using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace WebServiceProject
{
	public class DAL
	{
		private const string _alg = "HmacSHA256";
		string constr = ConfigurationManager.ConnectionStrings["BDTRansactionConnectionString"].ConnectionString;

		public string NewUser(string iNombre, string iRut, string iPassword, string iMontoasignado, int iCodcliente, int iCodinterno, int iNroTarjeta, int iEstado, string iEmail)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{

				SqlCommand com = new SqlCommand("AddUser", con);
				com.CommandType = System.Data.CommandType.StoredProcedure;

				com.Parameters.Add("pNombre", SqlDbType.NVarChar).Value = iNombre;
				if (String.IsNullOrEmpty(iRut))
				{
					com.Parameters.Add("pRut", SqlDbType.VarChar).Value = 0;
				}
				else
				{ com.Parameters.Add("pRut", SqlDbType.VarChar).Value = iRut; }
				com.Parameters.Add("pPassword", SqlDbType.VarChar).Value = iPassword;
				if (String.IsNullOrEmpty(iRut))
				{ com.Parameters.Add("pMontoasignado", SqlDbType.Money).Value = 0; }
				else
				{ com.Parameters.Add("pMontoasignado", SqlDbType.Money).Value = Convert.ToDecimal(iMontoasignado); }
				com.Parameters.Add("pCodcliente", SqlDbType.Int).Value = iCodcliente;
				com.Parameters.Add("pCodinterno", SqlDbType.Int).Value = iCodinterno;
				com.Parameters.Add("pNroTarjeta", SqlDbType.Int).Value = iNroTarjeta;
				com.Parameters.Add("pEstado", SqlDbType.Int).Value = iEstado;
				com.Parameters.Add("pEmail", SqlDbType.VarChar).Value = iEmail;

				SqlParameter output = new SqlParameter("@responseMessage", SqlDbType.VarChar, 5000);
				output.Direction = ParameterDirection.Output;
				com.Parameters.Add(output);
				try
				{
					con.Open();
					com.ExecuteNonQuery();
					con.Close();

				}
				catch { }
				return output.Value.ToString();
			}
		}

		public DataSet GetSaldoActualizado(string email)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{
				DataSet ds = new DataSet();
				SqlCommand com = new SqlCommand("GetSaldoActualizado", con);
				com.CommandType = System.Data.CommandType.StoredProcedure;
				com.Parameters.Add("pEmail", SqlDbType.VarChar).Value = email;
				SqlDataAdapter adp = new SqlDataAdapter();
				try
				{
					adp.SelectCommand = com;
					adp.Fill(ds);

				}
				catch { }
				return ds;

			}
		}

		public DataSet NewTransaction(int tipoTransaccion, decimal montoTransaccion,string email)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{
				int numRows = 0;
				DataSet ds = new DataSet();
				DataTable table = new DataTable();
				//decimal mTransaccion = 0;
				//mTransaccion = System.Convert.ToDecimal(montoTransaccion);

				SqlCommand com = new SqlCommand("SP_Inserta_Transaccion",con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("email", SqlDbType.VarChar).Value = email;
				com.Parameters.Add("tipoTransaccion", SqlDbType.Int).Value = tipoTransaccion;
				com.Parameters.Add("montoTransaccion", SqlDbType.Money).Value = montoTransaccion;
				SqlDataAdapter adp = new SqlDataAdapter();
				try
				{
					//adp.SelectCommand = com;
					//adp.Fill(ds);
					con.Open();
					numRows = com.ExecuteNonQuery();
					con.Close();
					table.TableName = "Table";
					table.Columns.Add("Rows", typeof(int));
					table.Rows.Add(numRows);
					ds.Tables.Add(table);
				}
				catch { }
				return ds;

			}
		}

		public DataSet GetVendicontMAC(string serie)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{
				DataSet ds = new DataSet();

				SqlCommand com = new SqlCommand("GetMAC",con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("serie", SqlDbType.VarChar).Value = serie;				
				SqlDataAdapter adp = new SqlDataAdapter();
				try
				{
					adp.SelectCommand = com;
					adp.Fill(ds);
				}
				catch { }
				return ds;
			}
		}

		public DataSet GetUserTransaction(string email, int pageSize, int pageIndex)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{
				DataSet ds = new DataSet();

				SqlCommand com = new SqlCommand("SelectPartialData", con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("email", SqlDbType.VarChar).Value = email;
				com.Parameters.Add("pageSize", SqlDbType.Int).Value = pageSize;
				com.Parameters.Add("pageIndex", SqlDbType.Int).Value = pageIndex;
				SqlDataAdapter adp = new SqlDataAdapter();
				try
				{
					adp.SelectCommand = com;
					adp.Fill(ds);
				}
				catch { }
				return ds;
			}
		}

		private void UpdateTokenTime(string token)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{
				SqlCommand com = new SqlCommand("UpdateTokenTime",con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("@pToken", SqlDbType.NVarChar, 200).Value = token;
				try
				{
					con.Open();
					com.ExecuteNonQuery();
					con.Close();
				}
				catch { }
			}
		}

		public bool IsTokenValid(string token)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{
				bool result = false;

				SqlCommand com = new SqlCommand("IsTokenValid",con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("token", SqlDbType.VarChar).Value = token;
				try
				{
					con.Open();
					result = ((int)com.ExecuteScalar()) > 0 ? true : false;
					con.Close();
				}
				catch { }

				if (result == true)
					UpdateTokenTime(token);

				return result;
			}
		}

		public bool IsUserValid(string email, string password)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{

				SqlCommand com = new SqlCommand("AuthenticateUser",con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("@pEmail", SqlDbType.VarChar).Value = email;
				com.Parameters.Add("@pPassword", SqlDbType.VarChar).Value = password;
				SqlParameter output = new SqlParameter("@responseMessage", SqlDbType.Int);
				output.Direction = ParameterDirection.Output;
				com.Parameters.Add(output);
				try
				{
					con.Open();
					com.ExecuteNonQuery();
					con.Close();
				}
				catch { }
				if (output == null)
				{
					//Обдумать
					return false;
				}
				int res = int.Parse(output.Value.ToString());
				if (res == 1)
				{

					return true;
				}
				else { return false; }

			}
		}

		public string GenerateToken(string email)
		{
			using (SqlConnection con = new SqlConnection(constr))
			{

				SqlCommand com = new SqlCommand("GetPassAndSalt",con);
				com.CommandType = CommandType.StoredProcedure;
				com.Parameters.Add("@pEmail", SqlDbType.VarChar).Value = email;
				con.Open();
				SqlDataReader reader;
				reader = com.ExecuteReader();

				string password = "";
				string salt = "";

				while (reader.Read())
				{
					salt = reader.GetValue(0).ToString();
					password = reader.GetValue(1).ToString();

				}
				reader.Close();

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
				try
				{
					com = new SqlCommand("AddToken", con);
					com.CommandType = CommandType.StoredProcedure;
					com.Parameters.Add("@pToken", SqlDbType.NVarChar, 200).Value = token;
					
					com.ExecuteNonQuery();
					
				}
				catch { }
				return token;
			}
		}
		private static string GetHashedPassword(string password, string salt)
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

		public string GetEmailFromToken(string token)
		{
			string key = Encoding.UTF8.GetString(Convert.FromBase64String(token));
			string[] parts = key.Split(new char[] { ':' });
			string email = parts[1];
			return email;
		}

		public bool CheckSecurityHeader(Token securityToken)
		{
			if ((securityToken == null) || (string.IsNullOrEmpty(securityToken.SecurityToken)))
			{
				return false;
			}
			if (IsTokenValid(securityToken.SecurityToken))
			{
				return true;
			}
			else return false;
		}
	}
}