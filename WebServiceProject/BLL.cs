using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace WebServiceProject
{
	public class BLL
	{
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

	}
}