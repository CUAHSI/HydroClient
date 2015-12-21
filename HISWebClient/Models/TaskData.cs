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
        public TaskData() { }

        public TaskData( TimeSeriesRequestStatus tsrsIn, string statusIn, CancellationTokenSource ctsIn, String blobUriIn = "")
        {
            RequestStatus = tsrsIn;
            Status = statusIn;
            CTS = ctsIn;
            BlobUri = blobUriIn;
        }

        /// <summary>
        /// Time Series Request Status
        /// </summary>
        public TimeSeriesRequestStatus RequestStatus { get; set; }

        public String Status { get; set; }

        public CancellationTokenSource CTS { get; set; }

        public String BlobUri { get; set; }
	}
}