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

using System.IO.Compression;
using System.Threading;
using System.Web.Script.Serialization;

using log4net;

using HISWebClient.Util;

namespace HISWebClient.Controllers
{

    public class ExportController : Controller
    {
        //Reference AzureContext singleton...
        //Source: http://stackoverflow.com/questions/24626749/azure-table-storage-best-practice-for-asp-net-mvc-webapi

        private AzureContext _ac;

        //Task status dictionary - keyed by request Id
        private static IDictionary<string, TaskData> _dictTaskStatus = new Dictionary<string, TaskData>();

        private static Object lockObject = new Object();

        private static readonly ILog logger
            = LogManager.GetLogger("QueryLog");

        //Constructors - instantiate the AzureContext singleton for later reference...
        public ExportController() : this(new AzureContext()) { }

        public ExportController( AzureContext ac)
        {
            _ac = ac;
        }

        //Return task cancellation status
        private bool IsTaskCancelled(string requestId, TimeSeriesRequestStatus tsrsIn, string statusMessage)
        {
            //Validate/initialize input parameters...
            if (String.IsNullOrWhiteSpace(requestId) || String.IsNullOrWhiteSpace(statusMessage))
            {
                throw new ArgumentNullException("Empty input parameter(s)!!!");
            }

            bool bCancelled = false;    //Assume task in NOT cancelled...

            //Thread-safe access to dictionary
            lock (lockObject)
            {
                if (_dictTaskStatus.ContainsKey(requestId))
                {
                    //Task entry found - check task cancellation request
                    CancellationToken ct = _dictTaskStatus[requestId].CTS.Token;
                    if (ct.IsCancellationRequested)
                    {
                        //Cancellation requested - set indicator, update task status
                        bCancelled = true;
                        _dictTaskStatus[requestId].RequestStatus = tsrsIn;
                        _dictTaskStatus[requestId].Status = statusMessage;
                    }
                }
            }

            //Processing complete - return indicator
            return bCancelled;
        }

        //Update a task status
        private void UpdateTaskStatus(string requestId, TimeSeriesRequestStatus tsrsIn, string statusMessage)
        {
            //Validate/initialize input parameters...
            if (String.IsNullOrWhiteSpace(requestId) || String.IsNullOrWhiteSpace(statusMessage))
            {
                throw new ArgumentNullException("Empty input parameter(s)!!!");
            }

            //Thread-safe access to dictionary
            lock (lockObject)
            {
                if (_dictTaskStatus.ContainsKey(requestId))
                {
                    //Task entry found - update task status
                    _dictTaskStatus[requestId].RequestStatus = tsrsIn;
                    _dictTaskStatus[requestId].Status = statusMessage;
                }
            }
        }

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

            logger.Info("DownloadFile Starts for: " + filename );

            try
            {
                var result = await this.getStream(id);
                //var memoryStream = new MemoryStream(result);


                logger.Info("DownloadFile returns for: " + filename);

                //filestream.Write(result, 0, result.Count);
                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = filename };
            }
            catch( Exception ex )
            {
                logger.Info("DownloadFile Errors for: " + filename + " message: " + ex.Message );

                string input = "An error occured downloading file: " + filename;

                byte[] result = Encoding.ASCII.GetBytes(input);

                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = "ERROR " + filename };

                 
                
            }
            //return base.File(filePath, "text/csv", filename);
        }



        private async Task<FileStreamResult> DownloadFile(int id, List<TimeSeriesViewModel> currentSeries)
        {
           
            //var filePath = Server.MapPath(dir + filename);
            

            //cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cuahsidataexport;AccountKey=yydsRROjUZa9+ShUCS0hIxZqU98vojWbBqAPI22SgGrXGjomphIWxG0cujYrSiyfNU86YeVIXICPAP8IIPuT4Q==");

            var seriesMetaData = getSeriesMetadata(id, currentSeries);
            var filename = GenerateFileName(seriesMetaData);
            var fileType = "text/csv";

            logger.Info("DownloadFile Starts for: " + filename );

            try
            {
                var result = await this.getStream(id, currentSeries);
                //var memoryStream = new MemoryStream(result);


                logger.Info("DownloadFile returns for: " + filename);

                //filestream.Write(result, 0, result.Count);
                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = filename };
            }
            catch( Exception ex )
            {
                logger.Info("DownloadFile Errors for: " + filename + " message: " + ex.Message );

                string input = "An error occured downloading file: " + filename;

                byte[] result = Encoding.ASCII.GetBytes(input);

                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = "ERROR " + filename };
            }
            //return base.File(filePath, "text/csv", filename);
        }

        //Source - http://stackoverflow.com/questions/16469094/starting-and-forgetting-an-async-task-in-mvc-action
        [HttpPost]
        public async Task<JsonResult> RequestTimeSeries(TimeSeriesRequest tsrIn)
        {
            //#pragma warning disable 4014 // Fire and forget.
//            Task.Run(async () =>
//            {
//                await Task.Delay(60000);
//            }).ConfigureAwait(false);

            //Retrieve the input request Id
            var requestId = tsrIn.RequestId;
            var requestName = tsrIn.RequestName;
            TimeSeriesRequestStatus requestStatus = TimeSeriesRequestStatus.Starting;
            string status = requestStatus.GetEnumDescription();
            string blobUri = "Not yet available...";
            CancellationToken ct;
            bool bNewTask = false;  //Assume time series retrieval task already exists... 

            //Thread-safe access to dictionary
            lock (lockObject) 
            {
                //Check/Create Time Series Retrival Task...
                if (_dictTaskStatus.ContainsKey(requestId))
                {
                    //Task already exists - retrieve current status
                    requestStatus = _dictTaskStatus[requestId].RequestStatus;
                    status = _dictTaskStatus[requestId].Status;
                    blobUri = _dictTaskStatus[requestId].BlobUri;
                }
                else
                {
                    //New task - allocate a task status instance - add to dictionary
                    var taskData = new TaskData(requestStatus, status, new CancellationTokenSource(), blobUri);

                    _dictTaskStatus.Add(requestId, taskData);
                    ct = taskData.CTS.Token;
                    bNewTask = true;
                }
            }

            //Start the async time series retrieval task, if indicated
            if (bNewTask)
            {
                //Copy the currently requested time series for use in the task...
                var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);
                var retrievedSeries = (List<TimeSeriesViewModel>)httpContext.Session["Series"];

                List<TimeSeriesViewModel> currentSeries = new List<TimeSeriesViewModel>();
                foreach (TimeSeriesViewModel tsvm in retrievedSeries)
                {
                    currentSeries.Add(new TimeSeriesViewModel(tsvm));
                }

                Task.Run( async () =>
                {
/*
                    //await Task.Delay(60000);
                    for (var i = 0; i <= 100; i++)
                    {
                        //Thread-safe access to dictionary
                        lock (lockObject)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                _dictTaskStatus[requestId].Status = "Cancelled per client request!!";
                                break;
                            }

                            _dictTaskStatus[requestId].Status = "Processing iteration: " + i.ToString();
                        }

                        //Thread.Sleep(1000);
                        await Task.Delay(1000);
                    }
*/

                    try
                    {
                        //Allocate a memory stream for later use...
                        var memoryStream = new MemoryStream();  //ASSUMPTION: IDispose called during FileStreamResult de-allocation
                        //string cancellationMessage = "Cancelled per client request!!";
                        TimeSeriesRequestStatus cancellationEnum = TimeSeriesRequestStatus.CanceledPerClientRequest;
                        string cancellationMessage = TimeSeriesRequestStatus.CanceledPerClientRequest.GetEnumDescription();

                        //Allocate a zip archive for later use...
                        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                        {
                            //For each time series id...
                            int bufSize = 4096;
                            int count = tsrIn.TimeSeriesIds.Count;
                            for (int i = 0; i < count; ++i)
                            {
                                //Check for cancellation...
                                if (IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
                                {
                                    break;
                                }

                                UpdateTaskStatus(requestId, TimeSeriesRequestStatus.ProcessingTimeSeriesId,
                                    TimeSeriesRequestStatus.ProcessingTimeSeriesId.GetEnumDescription() + tsrIn.TimeSeriesIds[i].ToString());

                                //Retrieve the time series data in csv format
                                FileStreamResult filestreamresult = await DownloadFile(tsrIn.TimeSeriesIds[i], currentSeries);

                                //Copy file contents to zip archive...
                                //ASSUMPTION: FileStreamResult instance properly disposes of FileStream member!!
                                var zipArchiveEntry = zipArchive.CreateEntry(filestreamresult.FileDownloadName);
                                using (var zaeStream = zipArchiveEntry.Open())
                                {
                                    await filestreamresult.FileStream.CopyToAsync(zaeStream, bufSize);
                                }
                            }
                        }

                        //Time series processing complete - check for cancellation
                        if (!IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
                        {
                            UpdateTaskStatus(requestId, TimeSeriesRequestStatus.SavingZipArchive, TimeSeriesRequestStatus.SavingZipArchive.GetEnumDescription());

                            //Reposition to start of memory stream...
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            //Upload zip archive...
                            blobUri = await _ac.UploadFromMemoryStreamAsync(memoryStream, requestName, ct);

                            //Upload complete - check for cancellation...
                            if (!IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
                            {
                                //Task complete - set status and blobUri...
                                //Thread-safe access to dictionary
                                lock (lockObject)
                                {
                                    _dictTaskStatus[requestId].RequestStatus = TimeSeriesRequestStatus.Completed;
                                    _dictTaskStatus[requestId].Status = TimeSeriesRequestStatus.Completed.GetEnumDescription();
                                    _dictTaskStatus[requestId].BlobUri = blobUri;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Error - update request status
                        UpdateTaskStatus(requestId, TimeSeriesRequestStatus.ProcessingError, TimeSeriesRequestStatus.ProcessingError.GetEnumDescription() + ex.Message);
                    }
                    finally
                    {
                        //Thread-safe access to dictionary
                        lock (lockObject)
                        {
                            //Dispose of the cancellation token source
                            CancellationTokenSource cts = _dictTaskStatus[requestId].CTS;
                            cts.Dispose();
                        }
                    }
                }).ConfigureAwait(false);
            }

            //Return a TimeSeriesResponse in JSON format...
            var result = new TimeSeriesResponse(tsrIn.RequestId, requestStatus, status, blobUri);

            var javaScriptSerializer = new JavaScriptSerializer();
            var json = javaScriptSerializer.Serialize(result);

            //Processing complete - return 
            return Json(json, "application/json");
        }

        //BC - Test - Check status of a long running task
        [HttpGet]
        public JsonResult CheckTask(String Id)
        {
            TimeSeriesResponse tsr = new TimeSeriesResponse(Id, TimeSeriesRequestStatus.UnknownTask, TimeSeriesRequestStatus.UnknownTask.GetEnumDescription());

            //Thread-safe access to dictionary
            lock (lockObject)
            {
                if (_dictTaskStatus.Keys.Contains(Id))
                {
                    tsr.RequestStatus = _dictTaskStatus[Id].RequestStatus;
                    tsr.Status = _dictTaskStatus[Id].Status;
                    tsr.BlobUri = _dictTaskStatus[Id].BlobUri;

                    //If task is cancelled or completed, remove associated entry from dictionary
                    if ( TimeSeriesRequestStatus.Completed == tsr.RequestStatus || 
                         TimeSeriesRequestStatus.CanceledPerClientRequest == tsr.RequestStatus)
                    {
                        _dictTaskStatus.Remove(Id);
                    }
                }
            }

            var javaScriptSerializer = new JavaScriptSerializer();
            var json = javaScriptSerializer.Serialize(tsr);

            //Processing complete - return 
            return Json(json, "application/json", JsonRequestBehavior.AllowGet);
        }

        //BC - Test - End long running task
        [HttpGet]
        public JsonResult EndTask(String Id)
        {
            TimeSeriesResponse tsr = new TimeSeriesResponse(Id, TimeSeriesRequestStatus.UnknownTask, TimeSeriesRequestStatus.UnknownTask.GetEnumDescription());

            //Thread-safe access to dictionary
            lock (lockObject)
            {
                if (_dictTaskStatus.Keys.Contains(Id))
                {
                    CancellationTokenSource cts = _dictTaskStatus[Id].CTS;

                    try
                    {
                        cts.Cancel();

                        tsr.RequestStatus = TimeSeriesRequestStatus.ClientSubmittedCancelRequest;
                        tsr.Status = TimeSeriesRequestStatus.ClientSubmittedCancelRequest.GetEnumDescription();
                    }
                    catch (Exception ex)
                    {
                        //Error - update request status
                        UpdateTaskStatus(Id, TimeSeriesRequestStatus.ProcessingError, TimeSeriesRequestStatus.ProcessingError.GetEnumDescription() + ex.Message);
                    }
                }
            }

            var javaScriptSerializer = new JavaScriptSerializer();
            var json = javaScriptSerializer.Serialize(tsr);

            //Processing complete - return 
            return Json(json, "application/json", JsonRequestBehavior.AllowGet);
        }

/*
        [HttpPost]
        public async Task<JsonResult> RequestTimeSeries(TimeSeriesRequest tsrIn)
        {

            //This returns immediately and does not block thread pool thread...
//            new Thread(() =>
//            {
//                for (var i = 0; i <= 1000; i++)
//                {
//                    Thread.Sleep(100);     
//                }
//            }).Start();

            //This does not return immediately and blocks thread pool thread...
            //await Task.Delay(60000);

            int n = 5;

            ++n;

            //Try again but do not capture/restore the context of the original thread 
            //Task task = Task.Delay(60000);

            //await task;

            //Method returns here.  When the delay period ends, the method resumes after this point - as explained in ASP.NET documentation
            //BUT - another map selection blocks until this method resumes and finishes...
            //    - an attempt to navigate to another web page - like 'About' also blocks until this method resumes and ends...
            await Task.Delay(60000).ConfigureAwait(false);

            //await Sleeper();

            //This does not return immediately and blocks thread pool thread...
            //await Task.Run(() => Thread.Sleep(60000));

            //Simulate the blob URI for now
            string blobURI = "This is the blob URI";

            //Return a TimeSeriesResponse in JSON format...
            string status = DateTime.Now.ToString();
            var result = new TimeSeriesResponse(tsrIn.RequestId, status, blobURI);

            var javaScriptSerializer = new JavaScriptSerializer();
            var json = javaScriptSerializer.Serialize(result);

            //Processing complete - return 
            return Json(json, "application/json");
        }

        private Task Sleeper()
        {
            return  new Task(() =>  
              new Thread(async () =>
              {
                for (var i = 0; i <= 1000; i++)
                {
                    //Thread.Sleep(100);
                    await Task.Delay(1);
                }
              }).Start() );
        }
*/
/*
        [HttpGet, FileDownload]
        public async Task<FileStreamResult> DownloadTimeSeries(TimeSeriesRequest tsrIn)
        {
            Dictionary<string, byte[]> dictByteArrays = new Dictionary<string, byte[]>();
            var fileType = "text/csv";

            //Retrieve request name for later reference
            string requestName = tsrIn.RequestName;

            logger.Info("DownloadTimeSeries Starts for: " + requestName);

            //For each time series id...
            try
            {
                foreach (int tsId in tsrIn.TimeSeriesIds)
                {
                    var seriesMetaData = getSeriesMetadata(tsId);
                    var filename = GenerateFileName(seriesMetaData);

                    var byteArray = await this.getStream(tsId);

                    dictByteArrays.Add(filename, byteArray);
                }

                



                logger.Info("DownloadTimeSeries Returns for: " + requestName);
            }
            catch (Exception ex )
            {
                string input = "An error occured while processing request: " + requestName;

                byte[] result = Encoding.ASCII.GetBytes(input);

                return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = "ERROR " + requestName };
            }
        }

*/
        public async Task<byte[]> getStream(int SeriesID, List<TimeSeriesViewModel> currentSeries = null)
        {
            DateTimeOffset requestTime = DateTimeOffset.UtcNow;
            //for test
            double lat = 0;
            double lng = 0;

           
                //get series from wateroneflow and return response
                Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID, currentSeries);

                //var seriesMetaData = getSeriesMetadata(SeriesID);

                //Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await SeriesAndStreamOfSeriesID(seriesMetaData);
                string nameGuid = Guid.NewGuid().ToString();

                var s = await this.getCSVResultByteArray(data.Item2, nameGuid, requestTime);

                return s;
        }

        public async Task<Tuple<Stream, SeriesData>> GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)
        {
            SeriesMetadata meta = getSeriesMetadata(seriesId, currentSeries);
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
                    return new Tuple<Stream, SeriesData>(data.Item1, new SeriesData(meta.SeriesID, meta, dataResult.Method.Description.ToString(), dataResult.QualityControlLevel.Definition, dataValues,
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
                "QualityControlLevel",
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
                data.myMetadata.SampleMedium,	
                data.MethodDescription,                            
                data.QualityControlLevelDefinition,		
                data.myVariable.DataType,
                data.myVariable.ValueType,
                data.myVariable.IsRegular.ToString(),
                data.myVariable.TimeSupport.ToString(),
                data.myVariable.TimeUnit.ToString(),
                data.myMetadata.SiteName,
                data.myMetadata.SiteCode,
                data.myMetadata.Latitude.ToString(),
                data.myMetadata.Longitude.ToString(),
                data.myMetadata.GeneralCategory,
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
                csvwrtr.WriteRecord(new List<string>() { "UTCTimeStamp"
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
                    values.Add(value.UTCOffset.ToString());
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

        public SeriesMetadata getSeriesMetadata(int SeriesId, List<TimeSeriesViewModel> currentSeries = null)
        {
            List<TimeSeriesViewModel> retrievedSeries = currentSeries;

            if (null != System.Web.HttpContext.Current)
            {
                //Called with an Http context - retrieve list from current session data...
            var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);

                retrievedSeries = (List<TimeSeriesViewModel>)httpContext.Session["Series"];
            }

            var d = retrievedSeries[SeriesId];

            object[] metadata = new object[15];
            metadata[0] = d.ServCode;
            metadata[1] = d.ServURL;
            metadata[2] = d.SiteCode;
            metadata[3] = d.VariableCode;
            metadata[4] = d.SiteName;
            metadata[5] = d.VariableName;
            metadata[6] = d.SampleMedium;
            metadata[7] = d.GeneralCategory;            
            metadata[8] = d.BeginDate;
            metadata[9] = d.EndDate;
            metadata[10] = d.ValueCount;
            metadata[11] = d.Latitude;
            metadata[12] = d.Longitude;
            metadata[13] = 0;
            metadata[14] = 0;
            //metadata[13] = split[13];


            return new SeriesMetadata(metadata);
        }
      

    }
}