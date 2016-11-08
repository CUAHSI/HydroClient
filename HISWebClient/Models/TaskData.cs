using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Threading;

namespace HISWebClient.Models
{
    public class TaskData
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public TaskData() {}

		//C# 'feature' - new DateTime() same value as DateTIme.MinValue - but you can't use DateTime.MinValue as a default argument!!!
        public TaskData( TimeSeriesRequestStatus tsrsIn, string statusIn, 
						 CancellationTokenSource ctsIn, 
						 String blobUriIn = "", 
						 DateTime blobTimeStampIn = new DateTime(),
						 string eMail = "" )
			: this()
        {
            RequestStatus = tsrsIn;
            Status = statusIn;
            CTS = ctsIn;
            BlobUri = blobUriIn;
			BlobTimeStamp = blobTimeStampIn;
			UserEmail = eMail;
        }

        /// <summary>
        /// Time Series Request Status
        /// </summary>
        public TimeSeriesRequestStatus RequestStatus { get; set; }

        public String Status { get; set; }

        public CancellationTokenSource CTS { get; set; }

        public String BlobUri { get; set; }

		public DateTime BlobTimeStamp { get; set; }

		public string UserEmail { get; set; }
	}
}