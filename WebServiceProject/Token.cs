using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;

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
}