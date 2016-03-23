using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;

namespace HISWebClient.Models
{
	[Flags]
    public enum TimeSeriesFormat	//OR-able...
    {
        [Description("CSV...")]
        CSV = 0x0001,
        [Description("CSV Merged...")]
        CSVMerged = 0x0004,
        [Description("WaterOneFlow...")]
        WaterOneFlow = 0x0002
    }

    public class TimeSeriesRequest
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public TimeSeriesRequest() {

			TimeSeriesIds = new List<int>();
		}

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