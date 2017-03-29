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
using System.Text;

namespace WebServiceProject
{
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

		DAL dal = new DAL();
		BLL bll = new BLL();

		#region Error messages
		const string ErrorMsg1 = "Wrong Service Credentials";
        const string ErrorMsg2 = "Wrong Token";
        const string ErrorMsg3 = "Wrong Service Credentials Header";
        const string ErrorMsg4 = "Invalid Email or Password";
		#endregion


		[WebMethod]
        [SoapHeader("ServiceCredentials")]
        public string AuthenticateUser()
        {
            if (ServiceCredentials == null)
                return ErrorMsg3;
            if (string.IsNullOrEmpty(ServiceCredentials.Email) || string.IsNullOrEmpty(ServiceCredentials.Password))
                return ErrorMsg1;
            // Are the credentials valid?
            if (!dal.IsUserValid(ServiceCredentials.Email, ServiceCredentials.Password))
                return ErrorMsg4;

            string token = null;

            token = dal.GenerateToken(ServiceCredentials.Email);
            return token;
        }

        [WebMethod]
        public string NewUser(string iNombre, string iRut, string iPassword, string iMontoasignado, int iCodcliente, int iCodinterno, int iNroTarjeta, int iEstado, string iEmail)
        {

            //Сделать проверки (email и т.д., кол-во символов, фильтрацию входящих данных)
            if (String.IsNullOrEmpty(iNombre) || (String.IsNullOrEmpty(iPassword)) || (String.IsNullOrEmpty(iEmail)))
            { return "Wrong Input Data!"; }

			return dal.NewUser(iNombre, iRut, iPassword, iMontoasignado, iCodcliente, iCodinterno, iNroTarjeta, iEstado, iEmail);
        }

        [WebMethod]
        public bool TestIsValidToken(string token)
        {
            return dal.IsTokenValid(token);
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet GetSaldoActualizado()
        {

            if (dal.CheckSecurityHeader(SecurityToken) == true)
            {
                string token = SecurityToken.SecurityToken;
                string email = dal.GetEmailFromToken(token);

				return dal.GetSaldoActualizado(email);
            }
            else
            {
                return bll.showError(ErrorMsg2);
            }
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet NewTransaction(int tipoTransaccion, decimal montoTransaccion)
        {
            if (dal.CheckSecurityHeader(SecurityToken) == true)
            {
                string token = SecurityToken.SecurityToken;
                string email = dal.GetEmailFromToken(token);

				return dal.NewTransaction(tipoTransaccion, montoTransaccion, email);
            }
            else
            {
                return bll.showError(ErrorMsg2);
            }
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet GetVendicontMAC(string serie)
        {
            if (dal.CheckSecurityHeader(SecurityToken) == true)
            {
				return dal.GetVendicontMAC(serie);
            }
            else
            {
                return bll.showError(ErrorMsg2);
            }
        }

        [WebMethod]
        [System.Web.Services.Protocols.SoapHeader("SecurityToken")]
        public DataSet GetUserTransaction(int pageSize, int pageIndex)
        {
            if (dal.CheckSecurityHeader(SecurityToken) == true)
            {
                string token = SecurityToken.SecurityToken;
                string email = dal.GetEmailFromToken(token);

				return dal.GetUserTransaction(email, pageSize, pageIndex);
            }
            else
            {
                return bll.showError(ErrorMsg2);
            }
        }
    }
}
