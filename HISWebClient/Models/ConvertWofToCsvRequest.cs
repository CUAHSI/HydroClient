using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
	public class ConvertWaterMlToCsvRequest
	{
        /// <summary>
        /// Constructors
        /// </summary>
		public ConvertWaterMlToCsvRequest()
		{
			WofIds = new List<string>();
		}

		public ConvertWaterMlToCsvRequest(string requestNameIn, string requestIdIn, string[] wofIdsIn)
        {
            RequestName = requestNameIn;

            RequestId = requestIdIn;

            WofIds = new List<string>(wofIdsIn);
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
		public List<string> WofIds { get; set; }
	}
}