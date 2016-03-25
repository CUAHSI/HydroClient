using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models.ClientFilterAndSearchCriteria
{
	public class clientDataSource
	{
		public clientDataSource() { }

		public string dataSource { get; set;}
  
		public bool searchable { get; set;}

		public string title { get; set; }
	}
}