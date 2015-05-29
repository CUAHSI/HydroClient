using CUAHSI.Common;
using CUAHSI.Models;
using HISWebClient.Models;
using Kent.Boogaart.KBCsv;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using MvcDemo.Common;
using ServerSideHydroDesktop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HISWebClient.Controllers
{
    public class ExportController : Controller
    {
        // GET: Export
        public ActionResult Index()
        {
            return View();
        }

        private CloudStorageAccount cloudStorageAccount;

        [HttpGet, FileDownload]
        public async Task<FileStreamResult> DownloadFile(int id)
        {
           
           
            //var filePath = Server.MapPath(dir + filename);
            

            //cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cuahsidataexport;AccountKey=yydsRROjUZa9+ShUCS0hIxZqU98vojWbBqAPI22SgGrXGjomphIWxG0cujYrSiyfNU86YeVIXICPAP8IIPuT4Q==");

            var seriesMetaData = getSeriesMetadata(id);
            var filename = GenerateFileName(seriesMetaData);
            var fileType = "text/csv";
            try
            {
                var result = await this.getStream(id);
                //var memoryStream = new MemoryStream(result);



                //filestream.Write(result, 0, result.Count);
                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = filename };
            }
            catch (Exception ex)
            {
                string input = "An error occured downloading file: " + filename;

                byte[] result = Encoding.ASCII.GetBytes(input);

                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = "ERROR " + filename };

                 
                
            }
            //return base.File(filePath, "text/csv", filename);
        }

        public async Task<byte[]> getStream(int SeriesID)
        {
            DateTimeOffset requestTime = DateTimeOffset.UtcNow;
            //for test
            double lat = 0;
            double lng = 0;

           
                //get series from wateroneflow and return response
                Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID);

                //var seriesMetaData = getSeriesMetadata(SeriesID);

                //Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await SeriesAndStreamOfSeriesID(seriesMetaData);
                string nameGuid = Guid.NewGuid().ToString();

                var s = await this.getCSVResultByteArray(data.Item2, nameGuid, requestTime);

                return s;
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
                        dataResult.Variable, dataResult.Source));
                    //return new Tuple<Stream, SeriesData>(data.Item1, new SeriesData(meta.SeriesID, meta, dataValues, (IList < ServerSideHydroDesktop.ObjectModel.Series >) dataResult));
                
                }
            }
        }

        public async Task<Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>>> SeriesAndStreamOfSeriesID(SeriesMetadata meta)
        {
            var requestTimeout = 60000;
            WaterOneFlowClient client = new WaterOneFlowClient(meta.ServURL);
            return await client.GetValuesAndRawStreamAsync(
                    meta.SiteCode,
                    meta.VarCode,
                    meta.StartDate,
                    DateTime.UtcNow,
                    Convert.ToInt32(requestTimeout));
        }


        public async Task<byte[]> getCSVResultByteArray(SeriesData data, string nameGuid, DateTimeOffset requestTime)
        {
            //SeriesDownload result = new SeriesDownload() { SeriesID = data.SeriesID };

            //assumes series is not already in storage                        
            using (MemoryStream ms = new MemoryStream())
            {
                //write data to memory stream as csv
                await WriteDataToMemoryStreamAsCsv(data, ms);

                if (ms.Length > 0)
                {
                    // persist memory stream as blob
                    ms.Position = 0;
                    var csa = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
                    CloudBlockBlob blob = await WriteMemoryStreamToBlobInGuidDirectory(data, ms, csa);

                }           


                return ms.ToArray();
            }
        }

        private async Task<CloudBlockBlob> WriteMemoryStreamToBlobInGuidDirectory(SeriesData data, MemoryStream ms, CloudStorageAccount csa)
        {
            CloudBlobClient bClient = csa.CreateCloudBlobClient();
            
            CloudBlobContainer container = bClient.GetContainerReference(DiscoveryStorageTableNames.SeriesDownloads);
            string fileName = GenerateBlobName(data);
            CloudBlockBlob blob = container.GetDirectoryReference(Session.SessionID.ToString()).GetBlockBlobReference(fileName);
            blob.Properties.ContentType = "text/csv; utf-8";
            blob.Properties.ContentDisposition = string.Format("attachment; filename = {0}", fileName);
            await blob.DeleteIfExistsAsync();
            await blob.UploadFromStreamAsync(ms, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions()
            {
                RetryPolicy = new ExponentialRetry()
            }, null);
            return blob;
        }

        private async Task WriteDataToMemoryStreamAsCsv(SeriesData data, MemoryStream ms)
        {
            using (var csvwrtr = new CsvWriter(ms, Encoding.UTF8, true))
            {
                //write metadata
                csvwrtr.ValueSeparator = Char.Parse(",");
                csvwrtr.WriteRecord(new List<string>() {  

                "Organization",
                "SourceDescription",
                "SourceLink",
                "VariableName",                              
                "VarUnits",
                "SampleMedium",
                "MethodDescription",
                "QualityControlLevelCode",
                "DataType",
                "ValueType",
                "IsRegular",
                "TimeSupport",
                "TimeUnits",
                "SiteName",
                "SiteCode",
                "Latitude",
                "Longitude",
                "GeneralCategory",
                "NoDataValue",
                "Citation",


                });
                csvwrtr.WriteRecord(new List<string>() {  
                
                data.mySource.Organization,
                data.mySource.Description,
                data.mySource.Link,
                data.myMetadata.VariableName,
                data.myVariable.VariableUnit.Name,
                "SampleMedium",	
                "MethodDescription",                            
                "QualityControlLevelCode",		
                data.myVariable.DataType,
                data.myVariable.ValueType,
                data.myVariable.IsRegular.ToString(),
                data.myVariable.TimeSupport.ToString(),
                data.myVariable.TimeUnit.ToString(),
                data.myMetadata.SiteName,
                data.myMetadata.SiteCode,
                data.myMetadata.Latitude.ToString(),
                data.myMetadata.Longitude.ToString(),
                "GeneralCategory",
                data.myVariable.NoDataValue.ToString(),                
                data.mySource.Citation,
                //"UTCOffset",                
                //"ValueAccuracy",
                //"CensorCode",
                //"OffsetValue",	
                //"OffsetDescription",	
                //"OffsetUnits",	
                //"QualifierCode",

                });

                //LocalDateTime 
                //DateTimeUTC
                //DataValues

                //csvwrtr.ValueSeparator = Char.Parse(",");
                csvwrtr.WriteRecord(new List<string>() { "TimeStamp"
                        ,"Value","OffsetType","OffsetValue", "ValueAccuracy",
                        "Qualifier","CensorCode", "UTCOffset" });

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
                    //values.Add(value.);
                    csvwrtr.WriteRecord(values);
                }
                await csvwrtr.FlushAsync();
            }
        }

        private string GenerateBlobName(SeriesData data)
        {
            return string.Format("series-{0}-{1}.csv", data.myMetadata.SiteName.SanitizeForFilename(), data.myMetadata.VariableName.SanitizeForFilename());
        }
        private string GenerateFileName(SeriesMetadata meta)
        {
            return string.Format("{0}-{1}-{2}.csv", meta.ServCode.SanitizeForFilename(), meta.SiteName.SanitizeForFilename(), meta.VariableName.SanitizeForFilename());
        }

        public SeriesMetadata getSeriesMetadata(int SeriesId)
        {
            var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);

            var retrievedSeries = (List<TimeSeriesViewModel>)httpContext.Session["Series"];

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