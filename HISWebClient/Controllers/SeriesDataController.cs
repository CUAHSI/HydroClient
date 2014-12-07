using CUAHSI.DataExport;
using CUAHSI.Models;
using Kent.Boogaart.KBCsv;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ServerSideHydroDesktop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using CUAHSI.Common;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace HISWebClient.Controllers
{
    /// <summary>
    /// Transducer for WaterML 1.* to JSON object. Useful for data visualization and thin client scenarios.
    /// </summary>
    public class SeriesDataController : ApiController
    {

        /// <summary>
        /// Means of executing queries for data partitioned by atomic units.
        /// </summary>
        private readonly IMetadataQuery discoveryService;

        private CloudStorageAccount csa;

        /// <summary>
        /// Means of executing data export workflows to create public references to files.
        /// </summary>
        private readonly CUAHSI.DataExport.IExportEngine wdcStore;

        public SeriesDataController() { }
        public SeriesDataController(IMetadataQuery repository, CUAHSI.DataExport.IExportEngine exportEngine)
        {
            this.discoveryService = repository;
            this.wdcStore = exportEngine;
        }

        /// <summary>
        /// Retrieves data from WaterOneFlow services, persists a publicly-accessible copy as a CSV-formatted document
        /// (see <a href="http://www.ietf.org/rfc/rfc4180.txt">rfc-4180</a> and <a href="http://www.creativyst.com/Doc/Articles/CSV/CSV01.htm">this discussion</a>), 
        /// and returns a web-formatted response including a URI to the reference link.
        /// </summary>
        /// <param name="SeriesID"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns></returns>       
        public async Task<SeriesData> Get(int SeriesID, double lat, double lng)
        {
            DateTimeOffset requestTime = DateTimeOffset.UtcNow;

            DataExportRequest cachedResult = await wdcStore.CheckIfSeriesExistsInStorage(CUAHSIDataStorage.LogHelper.GetCUAHSIDataStorage(), SeriesID, requestTime);
            if (cachedResult == null)
            {
                //get series from wateroneflow and return response
                Tuple<Stream, SeriesData> data = await discoveryService.GetSeriesDataObjectAndStreamFromSeriesTriple(SeriesID, lat, lng);
                string nameGuid = Guid.NewGuid().ToString();
                var dl = wdcStore.PersistSeriesData(data.Item2, nameGuid, requestTime);

                //fire and forget => no reason to require consistency with user download.
                var persist = wdcStore.PersistSeriesDocumentStream(data.Item1, SeriesID, nameGuid, DateTime.UtcNow);

                await Task.WhenAll(new List<Task>() { dl, persist });

                data.Item2.wdcCache = dl.Result.Uri;
                return data.Item2;
            }
            else
            {

                //get series from cloud cache and return response
                ServerSideHydroDesktop.ObjectModel.Series dataResult = await wdcStore.GetWaterOneFlowFromCloudCache(SeriesID.ToString(), cachedResult.DownloadGuid, cachedResult.ServUrl);
                SeriesMetadata meta = await discoveryService.SeriesMetaDataOfSeriesID(SeriesID, lat, lng);

                IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();
                return new SeriesData(meta.SeriesID, meta, dataResult.QualityControlLevel.IsValid, dataValues,
                    dataResult.Variable.VariableUnit.Name, dataResult.Variable.VariableUnit.Abbreviation, dataResult.Site.VerticalDatum, dataResult.Site.Elevation_m);
            }
            // SeriesData data = await discoveryService.GetSeriesDataObjectFromSeriesTriple(SeriesID, lat, lng);            
        }

        public string test1()
        {
            return "t";
        }

        public async Task<SeriesData> test(int SeriesID)
        {
            DateTimeOffset requestTime = DateTimeOffset.UtcNow;
            //for test
            double lat = 0;
            double lng = 0;

            DataExportRequest cachedResult = null;//await wdcStore.CheckIfSeriesExistsInStorage(CUAHSIDataStorage.LogHelper.GetCUAHSIDataStorage(), SeriesID, requestTime);
            if (cachedResult == null)
            {
                //get series from wateroneflow and return response
                Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID);

                //var seriesMetaData = getSeriesMetadata(SeriesID);

                //Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await SeriesAndStreamOfSeriesID(seriesMetaData);
                string nameGuid = Guid.NewGuid().ToString();

                var s = await getCSVResultAsMemoryStream(data.Item2);

                //var dl = PersistSeriesData(data.Item2, nameGuid, requestTime);
               
                //fire and forget => no reason to require consistency with user download.
                //var persist = wdcStore.PersistSeriesDocumentStream(data.Item1, SeriesID, nameGuid, DateTime.UtcNow);

                //await Task.WhenAll(new List<Task>() { dl, persist });

                //data.Item2.wdcCache = dl.Result.Uri;
                return data.Item2;
            }
            else
            {

                //get series from cloud cache and return response
                ServerSideHydroDesktop.ObjectModel.Series dataResult = await wdcStore.GetWaterOneFlowFromCloudCache(SeriesID.ToString(), cachedResult.DownloadGuid, cachedResult.ServUrl);
                SeriesMetadata meta = await discoveryService.SeriesMetaDataOfSeriesID(SeriesID, lat, lng);

                IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();
                return new SeriesData(meta.SeriesID, meta, dataResult.QualityControlLevel.IsValid, dataValues,
                    dataResult.Variable.VariableUnit.Name, dataResult.Variable.VariableUnit.Abbreviation, dataResult.Site.VerticalDatum, dataResult.Site.Elevation_m);
            }
            // SeriesData data = await discoveryService.GetSeriesDataObjectFromSeriesTriple(SeriesID, lat, lng);            
        }

        public async Task<Tuple<Stream, SeriesData>> GetSeriesDataObjectAndStreamFromSeriesID(int seriesId)
        {
            SeriesMetadata meta = getSeriesMetadata(seriesId);
            // SeriesMetadata meta = await QueryHelpers.QueryHelpers.SeriesMetaDataOfSeriesID(SeriesID);
            if (meta == null)
            {
                throw new NullReferenceException();
            }
            else
            {
                Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await this.SeriesAndStreamOfSeriesID(meta);

                if (data == null || data.Item2.FirstOrDefault() == null)
                {
                    throw new KeyNotFoundException();
                }
                else
                {
                    var dataResult = data.Item2.FirstOrDefault();
                    IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();
                    return new Tuple<Stream, SeriesData>(data.Item1, new SeriesData(meta.SeriesID, meta, dataResult.QualityControlLevel.IsValid, dataValues,
                        dataResult.Variable.VariableUnit.Name, dataResult.Variable.VariableUnit.Abbreviation, dataResult.Site.VerticalDatum, dataResult.Site.Elevation_m));
                }
            }
        }

        public async Task<Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>>> SeriesAndStreamOfSeriesID(SeriesMetadata meta)
        {
            WaterOneFlowClient client = new WaterOneFlowClient(meta.ServURL);
            return await client.GetValuesAndRawStreamAsync(
                    meta.SiteCode,
                    meta.VarCode,
                    meta.StartDate,
                    DateTime.UtcNow,
                    Convert.ToInt32(10000));
        }

        public async Task<MemoryStream> getCSVResultAsMemoryStream(SeriesData data)
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
                   
                }
                return ms;
            }            
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
            await blob.UploadFromStreamAsync(ms, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions()
            {
                RetryPolicy = new ExponentialRetry()
            }, null);
            return blob;
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
        public SeriesMetadata getSeriesMetadata(int SeriesId)
        {
            var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);

            var retrievedSeries = (List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>)httpContext.Session["Series"];

            var d = retrievedSeries[SeriesId];

            object[] metadata = new object[13];
            metadata[0] = d.ServCode;
            metadata[1] = d.ServURL;
            metadata[2] = d.SiteCode;
            metadata[3] = d.VariableCode;
            metadata[4] = d.SiteName;
            metadata[5] = d.VariableName;
            metadata[6] = d.BeginDate;
            metadata[7] = d.EndDate;
            metadata[8] = d.ValueCount;
            metadata[9] = d.Latitude;
            metadata[10] = d.Longitude;
            metadata[11] = 0;
            metadata[12] = 0;
            //metadata[13] = split[13];


            return new SeriesMetadata(metadata);
        }
    }
}