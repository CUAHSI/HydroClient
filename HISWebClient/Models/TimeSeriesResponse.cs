using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;

using HISWebClient.Util;

namespace HISWebClient.Models
{

    public enum TimeSeriesRequestStatus
    {
        [Description("Starting...")]
        Starting = 1,

        [Description("Started!!")]
        Started = 2,

        [Description("Client submitted cancellation request...")]
        ClientSubmittedCancelRequest = 3,

        [Description("Canceled per client request!!")]
        CanceledPerClientRequest = 4,

        [Description("Completed!!")]
        Completed = 5,

        [Description("Processing Time Series ID: ")]
        ProcessingTimeSeriesId = 6,

        [Description("Saving Zip archive...")]
        SavingZipArchive = 7,

        [Description("Processing error: ")]
        ProcessingError = 8,

        [Description("Unknown Task!!")]
        UnknownTask = 9,

        [Description("Request Time Series Error")]
        RequestTimeSeriesError = 10,

        [Description("Check Task Error")]
        CheckTaskError = 11,

        [Description("End Task Error")]
        EndTaskError = 12,
    
		[Description("Not Started")]
		NotStarted = 13
   }


    public class TimeSeriesResponse
    {
        /// <summary>
        /// Constructors...
        /// </summary>
        public TimeSeriesResponse() { }

		//C# 'feature' - new DateTime() same value as DateTIme.MinValue - but you can't use DateTime.MinValue as a default argument!!!
        public TimeSeriesResponse(string requestIdIn, TimeSeriesRequestStatus tsrsIn, string statusIn, string blobUriIn = "", DateTime blobTimeStampIn = new DateTime())  
        {
            RequestId = requestIdIn;
            RequestStatus = tsrsIn;
            Status = statusIn;
            BlobUri = blobUriIn;
			BlobTimeStamp = blobTimeStampIn;

			SeriesIdsToVariableUnits = new Dictionary<int, string>();
        }

        /// <summary>
        /// Request Id
        /// ASSUMPTION: The request Id value, as received from the client, is unique for all requests submitted by all clients  
        /// </summary>
        public String RequestId { get; set; }

        /// <summary>
        /// Time Series Request Status
        /// </summary>
        public TimeSeriesRequestStatus RequestStatus { get; set; }

        /// <summary>
        /// Status 
        /// </summary>
        public String Status { get; set; }

        /// <summary>
        /// BlobUri - for blob files...
        /// </summary>
        public String BlobUri {get; set; }

		/// <summary>
		/// BlobTimeStamp - for blob files...
		/// </summary>
		public DateTime BlobTimeStamp { get; set; }



		public Dictionary<int, string> SeriesIdsToVariableUnits { get; set; }
	}
}