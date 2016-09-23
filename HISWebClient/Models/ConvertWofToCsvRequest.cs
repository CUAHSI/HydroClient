using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
	public class WofIds
	{
		public string WofId { get; set; }
 
		public string QCLID { get; set;}

		public string MethodId { get; set; } 

		public string SourceId { get; set;}
	}

	public class ConvertWaterMlToCsvRequest
	{
        /// <summary>
        /// Constructors
        /// </summary>
		public ConvertWaterMlToCsvRequest()
		{
			WofIds = new List<WofIds>();
		}

		public ConvertWaterMlToCsvRequest(string requestNameIn, string requestIdIn, WofIds[] wofIdsIn)
        {
            RequestName = requestNameIn;

            RequestId = requestIdIn;

            WofIds = new List<WofIds>(wofIdsIn);
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
		/// WaterOneFlow Ids
		/// </summary>
		public List<WofIds> WofIds { get; set; }
	}
}