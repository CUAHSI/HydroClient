namespace CUAHSI.DataExport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CUAHSI.Common;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Reference to a data file for download
    /// </summary>
    public class DataExportRequest : TableEntity
    {
        /// <summary>
        /// Time of the request.
        /// </summary>
        public DateTimeOffset RequestTime { get; set; }

        /// <summary>
        /// Start time of the data.
        /// </summary>
        public DateTimeOffset BeginTimeUtc { get; set; }

        /// <summary>
        /// End time of the data.
        /// </summary>
        public DateTimeOffset EndTimeUtc { get; set; }

        /// <summary>
        /// Publicly-accessible address of requested data file. CUAHSI Wata Data Center.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Guid identifer for this particular download. Can be used, along with SeriesID, to get the blob of the cached WaterOneFlow response.
        /// </summary>
        public string DownloadGuid { get; set; }

        /// <summary>
        /// HIS Central Series ID identifying Series persisted.
        /// </summary>
        public long SeriesId { get; set; }

        /// <summary>
        /// The metadata service URL needed to correctly identify the parsing engine dependency of the client.
        /// </summary>
        public string ServUrl { get; set; }

        public DataExportRequest()
        { }

        public DataExportRequest(DateTimeOffset requestTime, DateTimeOffset beginTimeUtc, DateTimeOffset endTimeUtc, string uri, int seriesId, string servUrl)
            : base(DataExportRequest.GeneratePartitionKeyFromSeriesMetadata(seriesId), requestTime.ToDateTimeDescending())
        {
            this.RequestTime = requestTime; this.BeginTimeUtc = beginTimeUtc; this.EndTimeUtc = endTimeUtc; this.Uri = uri; this.ServUrl = servUrl;   
        }        

        /// <summary>
        /// Generate partition key of data export request. Should create a distinct partition per request resource.
        /// </summary>
        /// <param name="seriesId"></param>
        /// <returns></returns>
        public static string GeneratePartitionKeyFromSeriesMetadata(int seriesId)
        {
            return string.Format("{0}", seriesId);
        }
    }
}
