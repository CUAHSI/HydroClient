using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

///
///A simple Entity Framework class associating a unique user e-mail (from authentication)
///with one or more Data Manager time series rows...
///
namespace HISWebClient.Models.DataManager
{
	[Table("DM_UserTimeSeries")]
	public class UserTimeSeries
	{
		[Key]
		public string UserEmail { get; set; }

		public virtual ICollection<DM_TimeSeries> TimeSeries { get; set; }
		//public ICollection<DM_TimeSeries> TimeSeries { get; set; }
	}

}