using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Web.Script.Serialization;

using Newtonsoft.Json;

///
/// A simple Entity Framework class representing one tabel row on the Exports page...
/// 
namespace HISWebClient.Models.DataManager
{
	[Table("ExportTaskData")]
	public class ExportTaskData
	{
		[Key]
		public string RequestId { get; set; }

		//Foreign key...
		//[ForeignKey("UserEmail")]
		public string UserEmail { get; set; }

		//Members...
		public int RequestStatus { get; set; }

		public string Status { get; set; }

		public string BlobUri { get; set; }

		public DateTime BlobTimeStamp { get; set;}

		//Reference to associated user in AspNetUsers
		//public virtual ApplicationUser ApplicationUser { get; set; }
	}
}