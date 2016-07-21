namespace CUAHSI.DataExport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CUAHSI.Common;
    using CUAHSI.Models;
    using Kent.Boogaart.KBCsv;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System.Diagnostics;
    using ServerSideHydroDesktop;

    /// <summary>
    /// Persists data as a CSV file in an Azure blob.
    /// </summary>
    public class CsvExportEngine : IExportEngine
    {
        private CloudStorageAccount csa;

        public CsvExportEngine()
        {
            csa = CUAHSIDataStorage.LogHelper.GetCUAHSIDataStorage();
        }

        /// <summary>
        /// Persists a 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<SeriesDownload> PersistSeriesData(SeriesData data, string urlId, DateTimeOffset requestTime)
        {                        
            SeriesDownload result = new SeriesDownload() { SeriesID = data.SeriesID };

            //assumes series is not already in storage                        
            using (MemoryStream ms = new MemoryStream())
            {
                //write data to memory stream as csv
                await WriteDataToMemoryStreamAsCsv(data, ms);

                if (ms.Length > 0)
                {
                    // persist memory stream as blob
                    ms.Position = 0;
                    CloudBlockBlob blob = await WriteMemoryStreamToBlobInGuidDirectory(data, ms, csa);

                    //persist table storage record                    
                    CloudTable tbl = csa.CreateCloudTableClient().GetTableReference(DiscoveryStorageTableNames.SeriesDownloads);
                    await tbl.ExecuteAsync(TableOperation.InsertOrReplace( //optimistic concurrency
                        new DataExportRequest(requestTime,
                            data.myMetadata.StartDate.ToUniversalTime(),
                            data.myMetadata.EndDate.ToUniversalTime(), blob.Uri.AbsoluteUri, data.SeriesID, data.myMetadata.ServURL)
                        ));

                    //fill result object uri
                    result.Uri = blob.Uri.AbsoluteUri;
                }
            }   
                                    
            return result;            
        }

        public async Task<Boolean> PersistSeriesDocumentStream(Stream s, int seriesId, string urlId, DateTimeOffset requestTime)
        {
            try
            {
                await WriteStreamToBlobInCacheDirectory(s, CUAHSIDataStorage.LogHelper.GetCUAHSIDataStorage(), urlId.ToString(), seriesId);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("PersistSeriesDocumentStream exception given seriesID {0}, Guid {1}, message {2} and inner exception {3}", seriesId, urlId.ToString(), ex.Message, ex.InnerException);
                return false;
            }
        }            

        /// <summary>
        /// Gets a record of a particular series request if one exists in the WDC storage cache already (persists all already-downloaded files).
        /// </summary>
        /// <param name="csa"></param>
        /// <param name="seriesId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="requestTime"></param>
        /// <returns></returns>
        public async Task<DataExportRequest> CheckIfSeriesExistsInStorage(CloudStorageAccount csa, int seriesId, DateTimeOffset requestTime)
        {
            CloudTableClient tableClient = csa.CreateCloudTableClient();
            CloudTable tbl = tableClient.GetTableReference(DiscoveryStorageTableNames.SeriesDownloads);
            string pk = DataExportRequest.GeneratePartitionKeyFromSeriesMetadata(seriesId);
            string rk = requestTime.ToDateTimeDescending();
                        
            TableQuery<DataExportRequest> qry = new TableQuery<DataExportRequest>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, seriesId.ToString()));
            TableQuerySegment<DataExportRequest> result = await tbl.ExecuteQuerySegmentedAsync(qry, null);
            return result.Results.FirstOrDefault();
        }

        /// <summary>
        /// Gets a SeriesData object from 
        /// Todo: Migrate to separate repository specific for blob storage or table storage.
        /// </summary>
        /// <param name="seriesId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task<ServerSideHydroDesktop.ObjectModel.Series> GetWaterOneFlowFromCloudCache(string seriesId, string guid, string servUrl)
        {
            WaterOneFlowClient waterOneFlow = new WaterOneFlowClient(servUrl);
            CloudStorageAccount csa = CUAHSIDataStorage.LogHelper.GetCUAHSIDataStorage();
            CloudBlobClient client = csa.CreateCloudBlobClient();
            Stream s = new MemoryStream();
            await client.GetContainerReference(guid).GetBlockBlobReference(seriesId).DownloadToStreamAsync(s);
            return waterOneFlow.GetValuesFromStream(s).FirstOrDefault();            
        }

        /// <summary>
        /// Writes WaterOneFlow data response as CSV file.
        /// </summary>
        /// <param name="data">WaterOneFlow data reponse to write.</param>
        /// <param name="ms">Memory Stream to write to.</param>
        /// <returns></returns>
        private async Task WriteDataToMemoryStreamAsCsv(SeriesData data, MemoryStream ms)
        {
            using (var csvwrtr = new CsvWriter(ms, Encoding.UTF8, true))
            {
                csvwrtr.ValueSeparator = Char.Parse(",");
                csvwrtr.WriteRecord(new List<string>() { "TimeStamp"
                        ,"Value","OffsetType","OffsetValue", "ValueAccuracy",
                        "Qualifier","CensorCode" });

                foreach (DataValue value in data.values)
                {
                    List<string> values = new List<string>();
                    values.Add(value.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    values.Add(value.Value.ToString());
                    values.Add(value.OffsetType);
                    values.Add(value.OffsetValue.ToString());
                    values.Add(value.ValueAccuracy.ToString());
                    values.Add(value.Qualifier);
                    values.Add(value.CensorCode);
                    csvwrtr.WriteRecord(values);
                }
                await csvwrtr.FlushAsync();
            }
        }

        /// <summary>
        /// Writes data directly to blob storage. Sets Content Type and Content Disposition headers to facilitate in-browser data download.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ms"></param>
        /// <param name="csa"></param>
        /// <returns></returns>
        private async Task<CloudBlockBlob> WriteMemoryStreamToBlobInGuidDirectory(SeriesData data, MemoryStream ms, CloudStorageAccount csa)
        {
            CloudBlobClient bClient = csa.CreateCloudBlobClient();
            CloudBlobContainer container = bClient.GetContainerReference(DiscoveryStorageTableNames.SeriesDownloads);
            string fileName = GenerateBlobName(data);
            CloudBlockBlob blob = container.GetDirectoryReference(new Guid().ToString()).GetBlockBlobReference(fileName);
            blob.Properties.ContentType = "text/csv; utf-8";
            blob.Properties.ContentDisposition = string.Format("attachment; filename = {0}", fileName);            
            await blob.DeleteIfExistsAsync();
            await blob.UploadFromStreamAsync(ms, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions() { 
                RetryPolicy = new ExponentialRetry() 
            }, null);            
            return blob;
        }

        /// <summary>
        /// Writes stream of series to blob storage in Guid-specified directory.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="csa"></param>
        /// <param name="guidId"></param>
        /// <param name="seriesId"></param>
        /// <returns></returns>
        private async Task<Boolean> WriteStreamToBlobInCacheDirectory(Stream s, CloudStorageAccount csa, string guidId, long seriesId)
        {
            try
            {
                CloudBlobClient bClient = csa.CreateCloudBlobClient();
                CloudBlobContainer container = bClient.GetContainerReference(DiscoveryStorageBlobDirectoryNames.SeriesCache);
                string fileName = seriesId.ToString();
                CloudBlockBlob blob = container.GetDirectoryReference(guidId).GetBlockBlobReference(fileName);                                
                await blob.DeleteIfExistsAsync();
                await blob.UploadFromStreamAsync(s, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions()
                {
                    RetryPolicy = new ExponentialRetry()
                }, null);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("WriteStreamToBlobInCacheDirectory faults for Series: {2} with Guid {3} with message {0} and inner ex {1}", ex.Message, ex.InnerException, seriesId, guidId));
                return false;
            }
        }

        /// <summary>
        /// Generates the filename of the data download version of the SeriesData resource.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string GenerateBlobName(SeriesData data)
        {
            return string.Format("series-{0}-{1}.csv", data.SeriesID, data.GetOntologyName().SanitizeForFilename());
        }
    }
}
