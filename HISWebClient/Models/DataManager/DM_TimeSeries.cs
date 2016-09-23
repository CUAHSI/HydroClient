using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Web.Script.Serialization;

using Newtonsoft.Json;


///
///A simple Entity Framework class representing one table row on the Data Manager page...
///
namespace HISWebClient.Models.DataManager
{
	[Table("DM_TimeSeries")]
	public class DM_TimeSeries
	{
		[Key]
		public int TimeSeriesId { get; set; }

        //[ForeignKey("Email")]
		public string UserEmail { get; set; }

		//Members...
		public int Status { get; set; }
		public string Organization { get; set; }
		public string ServiceCode { get; set; }
		public string ServiceTitle { get; set; }
		public string Keyword { get; set; }
		public string VariableUnits { get; set; }
		public string DataType { get; set; }
		public string ValueType { get; set; }
		public string SampleMedium { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int ValueCount { get; set; }
		public string SiteName { get; set; }
		public string VariableName { get; set; }
		public double TimeSupport { get; set; }
		public string TimeUnit { get; set; }

		//Members for Quality Control Level, Source and Method...
		public string QCLID { get; set; }

		public string QCLDesc { get; set; }

		public string SourceOrg { get; set; }

		public string SourceId { get; set; }

		public string MethodId { get; set; }

		public string MethodDesc { get; set; }

		public int SeriesId { get; set; }
		public string WaterOneFlowURI { get; set; }
		public DateTime WaterOneFlowTimeStamp { get; set; }
		public string TimeSeriesRequestId { get; set; }

		//Reference to associated user
		//NOTE: To avoid circular references when converting to JSON, you must 
		//		 use the ScriptIgnore attribute as explained here (answer 15)
		//			http://stackoverflow.com/questions/14569207/javascriptserializer-circular-reference-when-using-scriptignore
		[ScriptIgnore(ApplyToOverrides = true)]
		[JsonIgnore]
		public virtual UserTimeSeries UserTimeSeries { get; set; }
	}
}