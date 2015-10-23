using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;

namespace HISWebClient.Models
{

	public enum TimeSeriesFormat
	{
		[Description("CSV...")]
		CSV = 1,

		[Description("WaterOneFlow...")]
		WaterOneFlow = 2
	}

    public class TimeSeriesRequest
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public TimeSeriesRequest() { }

        public TimeSeriesRequest(string requestNameIn, string requestIdIn, int[] timeSeriesIdsIn, TimeSeriesFormat timeSeriesFormatIn = TimeSeriesFormat.CSV )
        {
            RequestName = requestNameIn;

            RequestId = requestIdIn;

            TimeSeriesIds = new List<int>(timeSeriesIdsIn);

			RequestFormat = timeSeriesFormatIn;
        }
 
        /// <summary>
        /// Request name
        /// </summary>
        public string RequestName { get; set; }

        /// <summary>
        /// Request Id
        /// ASSUMPTION: The request Id value, as received from the client, is unique for all requests submitted by all clients  
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// TimeSeriesIds
        /// </summary>
        public List<int> TimeSeriesIds { get; set; }

		/// <summary>
		/// RequestFormat
		/// </summary>
		public TimeSeriesFormat RequestFormat { get; set; }
    }
}