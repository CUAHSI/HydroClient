using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models.ClientFilterAndSearchCriteria
{
	public class clientFilter
	{
		public clientFilter() { }

		public string dataSource { get; set; }

		public string title { get; set; }

		public string value { get; set; }
	}
}