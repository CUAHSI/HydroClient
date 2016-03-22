using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data.Entity;

using HISWebClient.Models.DataManager;
using HISWebClient.Models.DownloadManager;

namespace HISWebClient.Models
{
	public class HydroClientDbContext : DbContext
	{

		//Default constructor - pass connection string to base constructor
		public HydroClientDbContext() : base("DefaultConnection")
        {
        }

		public DbSet<UserTimeSeries> UserTimeSeriesSet { get; set; }
		public DbSet<DM_TimeSeries> DM_TimeSeriesSet { get; set; }

		public DbSet<ExportTaskData> ExportTaskDataSet { get; set; }

	}
}