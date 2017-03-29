using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Protocols;

namespace WebServiceProject
{
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
}