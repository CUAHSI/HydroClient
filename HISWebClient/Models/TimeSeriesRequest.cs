using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
    public class TimeSeriesRequest
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public TimeSeriesRequest() { }

        public TimeSeriesRequest(string requestNameIn, string requestIdIn, int[] timeSeriesIdsIn )
        {
            RequestName = requestNameIn;

            RequestId = requestIdIn;

            TimeSeriesIds = new List<int>(timeSeriesIdsIn);
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
    }
}