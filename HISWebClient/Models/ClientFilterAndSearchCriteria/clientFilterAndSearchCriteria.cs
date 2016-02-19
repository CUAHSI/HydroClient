using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models.ClientFilterAndSearchCriteria
{
	public class clientFilterAndSearchCriteria
	{
		public clientFilterAndSearchCriteria()
		{
			dataSources = new List<clientDataSource>();

			filters = new List<clientFilter>();
		}

		public List<clientDataSource> dataSources { get; set; }

		public List<clientFilter> filters { get; set; }

		public string search { get; set; }

		public bool isEmpty()
		{
			return ( String.IsNullOrWhiteSpace(search) && (0 >= filters.Count));
		}
	}
}