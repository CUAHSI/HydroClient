using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
	//A simple class representing the contents of one ontology tree node...
	public class OntologyObject
	{
		public string key { get; set; }

		public string title { get; set; }

		public bool folder { get; set; }

		public List<OntologyObject> children { get; set; }
	}

	//A simple class representing the contents of one ontology category
	public class OntologyCategory
	{
		public string key { get; set;  }

		public string title { get; set; }

		public bool folder { get; set; }

		public bool lazy { get; set; }
	}
}