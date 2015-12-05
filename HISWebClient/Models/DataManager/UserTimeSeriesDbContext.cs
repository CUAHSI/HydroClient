using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data.Entity;

namespace HISWebClient.Models.DataManager
{
	public class UserTimeSeriesDbContext : DbContext
	{

		//Default constructor - pass connection string to base constructor
		public UserTimeSeriesDbContext() : base("DefaultConnection")
        {
        }

		public DbSet<UserTimeSeries> UserTimeSeriesSet { get; set; }
		public DbSet<DM_TimeSeries> DM_TimeSeriesSet { get; set; }
	}
}