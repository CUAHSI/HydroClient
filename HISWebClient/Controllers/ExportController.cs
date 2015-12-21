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
using System.Net.Http;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;


using log4net.Core;

using HISWebClient.Util;

//using ServerSideHydroDesktop.ObjectModel;

namespace HISWebClient.Controllers
{
	//BCC - 16-Oct-2015 - Testing push to QA branch on GitHub...

	//This is a change in HydrodataTools
	//Does the Git client display it?

	public class ExportController : Controller
	{
		//Reference AzureContext singleton...
		//Source: http://stackoverflow.com/questions/24626749/azure-table-storage-best-practice-for-asp-net-mvc-webapi

		private AzureContext _ac;

		//Task status dictionary - keyed by request Id
		private static IDictionary<string, TaskData> _dictTaskStatus = new Dictionary<string, TaskData>();

		private static Object lockObject = new Object();

		private DbLogContext dblogcontext = new DbLogContext("DBLog", "AdoNetAppenderLog", "local-DBLog", "deploy-DBLog");

		private DbErrorContext dberrorcontext = new DbErrorContext("DBError", "AdoNetAppenderError", "local-DBLog", "deploy-DBLog");

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
				ArgumentNullException ane = new ArgumentNullException("Empty input parameter(s)!!!");

				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "IsTaskCancelled(string requestId, TimeSeriesRequestStatus tsrsIn, string statusMessage)",
											  ane,
											  ane.Message);

				throw ane;
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
				ArgumentNullException ane = new ArgumentNullException("Empty input parameter(s)!!!");

				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "UpdateTaskStatus(string requestId, TimeSeriesRequestStatus tsrsIn, string statusMessage)",
											  ane,
											  ane.Message);

				throw ane;
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
			//BCC - Testing server timeout errors...
			//Session.Timeout = 1;
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
			catch( Exception ex )
			{
				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current, 
											  DateTime.UtcNow, "DownloadFile(int id)", 
											  ex, 
											  "DownloadFile Errors for: " + filename + " message: " + ex.Message);

				string input = "An error occured downloading file: " + filename;

				byte[] result = Encoding.ASCII.GetBytes(input);

				return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = "ERROR " + filename };

				 
				
			}
			//return base.File(filePath, "text/csv", filename);
		}



		private async Task<FileStreamResult> DownloadFile(int id, List<TimeSeriesViewModel> currentSeries, TimeSeriesFormat timeSeriesFormat = TimeSeriesFormat.CSV)
		{

			var seriesMetaData = getSeriesMetadata(id, currentSeries);
			var filename = GenerateFileName(seriesMetaData, timeSeriesFormat);
			var fileType = TimeSeriesFormat.CSV == timeSeriesFormat ? "text/csv" : "application/xml";

			try
			{
				if (TimeSeriesFormat.CSV == timeSeriesFormat)
				{
				var result = await this.getStream(id, currentSeries);
					return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = filename };
				}
				else if (TimeSeriesFormat.WaterOneFlow == timeSeriesFormat)
				{
					var result = await this.getWOFStream(id, currentSeries);

					//BCC - TEST - Commenting out seek calls to avoid exception: This stream does not support seek operations...
					//using (System.IO.FileStream outfile = new System.IO.FileStream(@"c:\CUAHSI\DownloadFile.xml", FileMode.Create))
					//{
					//	result.Seek(0, SeekOrigin.Begin);
					//	result.CopyTo(outfile);
					//}
					
					result.Seek(0, SeekOrigin.Begin);

					FileStreamResult fsr = new FileStreamResult(result, fileType) { FileDownloadName = filename };

					return fsr;
				}
				else
				{
					throw new ArgumentException(String.Format("Unknown TimeSeriesFormat value: {0}", timeSeriesFormat.ToString()));
				}

			}
			catch( Exception ex )
			{
				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "DownloadFile(int id, List<TimeSeriesViewModel> currentSeries)",
											  ex,
											  "DownloadFile Errors for: " + filename + " message: " + ex.Message);

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

				//Get user data...
				//string userIpAddress = ContextUtil.GetIPAddress(System.Web.HttpContext.Current);
				DateTime requestTimeStamp = httpContext.Timestamp;

				List<TimeSeriesViewModel> currentSeries = new List<TimeSeriesViewModel>();
				//int total = tsrIn.TimeSeriesIds.Count;
				LogRequestIds(tsrIn.RequestId, tsrIn.RequestName, retrievedSeries, currentSeries, tsrIn.TimeSeriesIds);

				//Retrieve values for use in async task...
				string sessionId = String.Empty;
				string userIpAddress = string.Empty;
				string domainName = string.Empty;

				dblogcontext.getIds(System.Web.HttpContext.Current, ref sessionId, ref userIpAddress, ref domainName);

				var dblogcontextRef = dblogcontext;
				var dberrorcontextRef = dberrorcontext;
				var sessionIdRef = sessionId;
				var userIpAddressRef = userIpAddress;
				var domainNameRef = domainName;

				Task.Run(async () =>
				{

					try
					{
						DateTime startDtUtc = DateTime.UtcNow;

						//Allocate a memory stream for later use...
						var memoryStream = new MemoryStream();  //ASSUMPTION: IDispose called during FileStreamResult de-allocation
						//string cancellationMessage = "Cancelled per client request!!";
						TimeSeriesRequestStatus cancellationEnum = TimeSeriesRequestStatus.CanceledPerClientRequest;
						string cancellationMessage = TimeSeriesRequestStatus.CanceledPerClientRequest.GetEnumDescription();

						//Allocate a zip archive for later use...
						using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
						{
							int bufSize = 4096;
							int i = 0;
							//if (bUseRequestIds)
							//{
								//For each time series id...
								int count = tsrIn.TimeSeriesIds.Count;
								foreach (int timeSeriesId in tsrIn.TimeSeriesIds)
								{
									//Check for cancellation...
									if (IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
									{
										break;
									}

									UpdateTaskStatus(requestId, TimeSeriesRequestStatus.ProcessingTimeSeriesId,
										TimeSeriesRequestStatus.ProcessingTimeSeriesId.GetEnumDescription() +
																		  timeSeriesId.ToString() +
																		  " (" + (++i).ToString() + " of " + count.ToString() + ")");

									//Retrieve the time series data in csv format
									FileStreamResult filestreamresult = await DownloadFile(timeSeriesId, currentSeries, tsrIn.RequestFormat);

                                    //MS added to check for failed downloads. TO DO: Needs to be put into a proper status message 
                                    if (filestreamresult.FileDownloadName.Contains("ERROR"))
                                    {
                                        UpdateTaskStatus(requestId, TimeSeriesRequestStatus.Completed, TimeSeriesRequestStatus.RequestTimeSeriesError.GetEnumDescription());
                                        throw new System.ApplicationException();
                                    }
                                    else
                                    {

                                        //Copy file contents to zip archive...
                                        //ASSUMPTION: FileStreamResult instance properly disposes of FileStream member!!
                                        var zipArchiveEntry = zipArchive.CreateEntry(filestreamresult.FileDownloadName);
                                        using (var zaeStream = zipArchiveEntry.Open())
                                        {
                                            await filestreamresult.FileStream.CopyToAsync(zaeStream, bufSize);
                                        }
                                        //add
                                    }
								}
							//}
						}

#if NEVER_DEFINED
						//Simulate a server error... divide by zero
						int zero = 0;
						int nonZero = 5;

						int testResult = nonZero / zero;2
#endif
						//Time series processing complete - check for cancellation
						if (!IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
						{
							
                            UpdateTaskStatus(requestId, TimeSeriesRequestStatus.SavingZipArchive, TimeSeriesRequestStatus.SavingZipArchive.GetEnumDescription());

							//Reposition to start of memory stream...
							memoryStream.Seek(0, SeekOrigin.Begin);

							//Upload zip archive...
							blobUri = await _ac.UploadFromMemoryStreamAsync(memoryStream, requestName, ct); ;

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

								dblogcontextRef.clearParameters();
								dblogcontextRef.clearReturns();

								dblogcontextRef.addParameter("requestId", tsrIn.RequestId);
								dblogcontextRef.addParameter("requestName", tsrIn.RequestName);

								dblogcontext.addReturn("requestId", requestId);
								dblogcontext.addReturn("requestStatus", requestStatus);
								dblogcontext.addReturn("status", status);
								dblogcontext.addReturn("blobUri", blobUri);

								dblogcontextRef.createLogEntry(sessionIdRef, userIpAddressRef, domainNameRef, startDtUtc, DateTime.UtcNow, "RequestTimeSeries(...)", "zip archive creation complete.", Level.Info);
							}
						}
					}
					catch (Exception ex)
					{
						//Error - log...
						dberrorcontextRef.clearParameters();
						dberrorcontextRef.createLogEntry(sessionIdRef, 
														 userIpAddressRef, 
														 domainNameRef,
														 DateTime.UtcNow,
														 "RequestTimeSeries(TimeSeriesRequest tsrIn)",
														 ex,
														"RequestTimeSeries error for Id: " + requestId + " message: " + ex.Message);
						
						//Update request status
						UpdateTaskStatus(requestId, TimeSeriesRequestStatus.RequestTimeSeriesError, TimeSeriesRequestStatus.RequestTimeSeriesError.GetEnumDescription() + ": " + ex.Message);
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

#if NEVER_DEFINED
			//Simulate a 500 error - Internal Server Error...
			//NOTE: Need to assign a temporary here to avoid compiler error: CS0206 - A property or indexer may not be passed as an out or ref parameter.
			HttpResponseBase httpResponseBase = Response;
			httpUtil.setHttpResponseStatusCode(ref httpResponseBase, HttpStatusCode.InternalServerError);
#endif				
			//Processing complete - return 
			return Json(json, "application/json");
		}


		//Find the version of the input water one flow stream...
		private string GetWaterOneFlowVersion( MemoryStream msWaterOneFlow, ref MemoryStream msWaterOneFlowDecompressed )
		{
			string version = String.Empty;

			msWaterOneFlow.Seek(0, SeekOrigin.Begin);

			ZipArchive ziparchive = new ZipArchive(msWaterOneFlow, ZipArchiveMode.Read);

			var entries = ziparchive.Entries;

			//Assume one entry per zip archive...
			if (0 >= entries.Count)
			{
				return version;
			}

			ZipArchiveEntry entry = entries[0];

			Stream zaeStream = entry.Open();

			zaeStream.CopyTo(msWaterOneFlowDecompressed);

			zaeStream.Close();

			zaeStream = entry.Open();

			XElement root = XElement.Load(zaeStream);

			IEnumerable<XNode> desc = root.DescendantNodes();

			foreach (XNode xnode in desc)
			{
				if (XmlNodeType.Element == xnode.NodeType)
				{
					XElement xelement = xnode as XElement;
					if (null != xelement)
					{
						if (xelement.Name.LocalName.Equals("timeseriesresponse", StringComparison.OrdinalIgnoreCase))
							{
							string namespacename = xelement.Name.NamespaceName;
							version = namespacename.Contains("1.1") ? "1.1" : namespacename.Contains("1.0") ? "1.0" : String.Empty; 
							break;
						}
					}
				}
			}

			return version;
		}

		//Log the input request ids and related information
		private void LogRequestIds(string requestId, string requestName, List<TimeSeriesViewModel> retrievedSeries, List<TimeSeriesViewModel> currentSeries, List<int> timeSeriesIds ) 
		{
			int current = 0;
			int total = timeSeriesIds.Count;

			foreach (TimeSeriesViewModel tsvm in retrievedSeries)
			{
				currentSeries.Add(new TimeSeriesViewModel(tsvm));

				foreach (int id in timeSeriesIds)
				{
					if (tsvm.SeriesId == id)
					{
						//Requested time series found - log identifiers...
						dblogcontext.clearParameters();
						dblogcontext.clearReturns();

						dblogcontext.addParameter("requestId", requestId);
						dblogcontext.addParameter("requestName", requestName);

						dblogcontext.addReturn("organization", tsvm.Organization);
						dblogcontext.addReturn("conceptKeyword", tsvm.ConceptKeyword);
						dblogcontext.addReturn("beginDate", tsvm.BeginDate);
						dblogcontext.addReturn("endDate", tsvm.EndDate);
						dblogcontext.addReturn("serviceCode", tsvm.ServCode);
						dblogcontext.addReturn("siteCode", tsvm.SiteCode);
						dblogcontext.addReturn("valueType", tsvm.ValueType);
						dblogcontext.addReturn("valueCount", tsvm.ValueCount);

						DateTime dtNow = DateTime.UtcNow;
						string message = String.Format("Requested time series: {0} of {1}", ++current, total);
						dblogcontext.createLogEntry(System.Web.HttpContext.Current, dtNow, dtNow, "RequestTimeSeries(...)", message, Level.Info);
					}
				}
			}
		}

		//Return status for the input task id...
		private TimeSeriesResponse checkTask(string Id)
		{
			TimeSeriesResponse tsr = new TimeSeriesResponse(Id, TimeSeriesRequestStatus.UnknownTask, TimeSeriesRequestStatus.UnknownTask.GetEnumDescription());

			if (String.IsNullOrWhiteSpace(Id))
			{
				return tsr;		//Invalid input parameter, return early
			}

			//Thread-safe access to dictionary
			lock (lockObject)
			{
				if (_dictTaskStatus.Keys.Contains(Id))
				{
					tsr.RequestStatus = _dictTaskStatus[Id].RequestStatus;
					tsr.Status = _dictTaskStatus[Id].Status;
					tsr.BlobUri = _dictTaskStatus[Id].BlobUri;

					//If task is cancelled or completed, remove associated entry from dictionary
					if (TimeSeriesRequestStatus.Completed == tsr.RequestStatus ||
						 TimeSeriesRequestStatus.CanceledPerClientRequest == tsr.RequestStatus ||
						TimeSeriesRequestStatus.EndTaskError == tsr.RequestStatus ||
						TimeSeriesRequestStatus.RequestTimeSeriesError == tsr.RequestStatus)
					{
						_dictTaskStatus.Remove(Id);
					}
				}
			}

			//Processing complete - return status instance
			return tsr;
		}

		//BC - Check status of a long running task
		[HttpGet]
		public JsonResult CheckTask(String Id)
		{
			TimeSeriesResponse tsr = checkTask(Id); 

			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(tsr);

			//Processing complete - return 
			return Json(json, "application/json", JsonRequestBehavior.AllowGet);
		}

		//BC - Check status of multiple, long running tasks
		[HttpPost]
		public JsonResult CheckTasks( List<string> Ids)
		{
			List<TimeSeriesResponse> tsrList = new System.Collections.Generic.List<TimeSeriesResponse>();

			//Thread-safe access to dictionary
			lock (lockObject)
			{
				foreach (var Id in Ids)
				{
					TimeSeriesResponse tsr = checkTask(Id);
					tsrList.Add(tsr);
				}
			}

			//Processing complete - return JSON result
			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(tsrList);

			//Processing complete - return 
			return Json(json, "application/json");
		}

		//BC - End long running task
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

#if NEVER_DEFINED
						//Simulate a server error... divide by zero
						int zero = 0;
						int nonZero = 5;

						int testResult = nonZero / zero;
#endif
						tsr.RequestStatus = TimeSeriesRequestStatus.ClientSubmittedCancelRequest;
						tsr.Status = TimeSeriesRequestStatus.ClientSubmittedCancelRequest.GetEnumDescription();
					}
					catch (Exception ex)
					{
						//Error - update request status
						dberrorcontext.clearParameters();
						dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
													  DateTime.UtcNow, "EndTask(String Id)",
													  ex,
													  "EndTask error for Id: " + Id + " message: " + ex.Message);

						tsr.RequestStatus = TimeSeriesRequestStatus.EndTaskError;
						tsr.Status = TimeSeriesRequestStatus.EndTaskError.GetEnumDescription() + ": " + ex.Message;

						UpdateTaskStatus(Id, tsr.RequestStatus, tsr.Status);
					}
				}
			}

			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(tsr);

#if NEVER_DEFINED
			//Simulate a 500 error - Internal Server Error...
			//NOTE: Need to assign a temporary here to avoid compiler error: CS0206 - A property or indexer may not be passed as an out or ref parameter.
			HttpResponseBase httpResponseBase = Response;
			httpUtil.setHttpResponseStatusCode(ref httpResponseBase, HttpStatusCode.InternalServerError);
#endif
			//Processing complete - return 
			return Json(json, "application/json", JsonRequestBehavior.AllowGet);
		}


		//Call the BYU API for a list of interfaced apps
		[HttpGet]
		public async Task<ActionResult> GetHydroshareAppsList()
		{
			try
			{
				//Add call to avoid error: "The underlying connection was closed: Could not establish trust relationship for the SSL/TLS secure channel."
				//Source: http://stackoverflow.com/questions/536352/webclient-https-issues
				ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

				HttpClient httpClient = new HttpClient();
                //Prod
                httpClient.BaseAddress = new Uri("https://apps.hydroshare.org/");
				

                //dev
				//httpClient.BaseAddress = new Uri("http://appsdev.hydroshare.org/");
				//httpClient.BaseAddress = new Uri("https://appsdev.hydroshare.org/");

				HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("apps/timeseries-viewer/api/list_apps/");

				string strJSON = String.Empty;
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					strJSON = httpResponseMessage.Content.ReadAsStringAsync().Result;
				}
				else
				{
					var javaScriptSerializer = new JavaScriptSerializer();
					strJSON = javaScriptSerializer.Serialize("{'Apps': []}");
				}


				//Processing complete - return 
				//return Json(json, "application/json", JsonRequestBehavior.AllowGet);

				return new ContentResult { Content = strJSON, ContentType = "application/json" };
			}
			catch (Exception ex)
			{
				string strJSON = String.Empty;

				var javaScriptSerializer = new JavaScriptSerializer();
				strJSON = javaScriptSerializer.Serialize("{'Apps': []}");

				return new ContentResult { Content = strJSON, ContentType = "application/json" };
			}
		}


		public async Task<byte[]> getStream(int SeriesID, List<TimeSeriesViewModel> currentSeries = null)
		{
			DateTimeOffset requestTime = DateTimeOffset.UtcNow;
		   
				//get series from wateroneflow and return response
				Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID, currentSeries);

				//var seriesMetaData = getSeriesMetadata(SeriesID);

				//Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await SeriesAndStreamOfSeriesID(seriesMetaData);
				string nameGuid = Guid.NewGuid().ToString();

				var s = await this.getCSVResultByteArray(data.Item2, nameGuid, requestTime);

				return s;
		}

		//Similar to getStream(...) but returns the WaterOneFlow stream instead of the CSV byte array...
		public async Task<Stream> getWOFStream(int SeriesID, List<TimeSeriesViewModel> currentSeries = null)
		{
			DateTimeOffset requestTime = DateTimeOffset.UtcNow;

			//get series from wateroneflow and return response
			Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID, currentSeries);

			return (data.Item1);

		}



		public async Task<Tuple<Stream, SeriesData>> GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)
		{
			SeriesMetadata meta = getSeriesMetadata(seriesId, currentSeries);
			// SeriesMetadata meta = await QueryHelpers.QueryHelpers.SeriesMetaDataOfSeriesID(SeriesID);
			if (meta == null)
			{
				NullReferenceException nre = new NullReferenceException();

				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)",
											  nre,
											  "meta == null");

				throw nre;
			}
			else
			{
				Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await this.SeriesAndStreamOfSeriesID(meta);

				if (data == null || data.Item2.FirstOrDefault() == null)
				{
					KeyNotFoundException knfe = new KeyNotFoundException();

					dberrorcontext.clearParameters();
					dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
												  DateTime.UtcNow, "GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)",
												  knfe,
												  "data == null");


					throw knfe;
				}
				else
				{
					var dataResult = data.Item2.FirstOrDefault();
					IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();

					SeriesData sd = new SeriesData(meta.SeriesID, meta, dataResult.Method.Description.ToString(), dataResult.QualityControlLevel.Definition, dataValues,
						dataResult.Variable, dataResult.Source);

					return new Tuple<Stream, SeriesData>(data.Item1, sd);
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
                    meta.EndDate,
					//DateTime.UtcNow,
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
					//CloudBlockBlob blob = await WriteMemoryStreamToBlobInGuidDirectory(data, ms, csa);

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
				//BCC - 15-Oct-29015 -  Suppress display of IsRegular
				//"IsRegular",
				"TimeSupport",
				"TimeUnits",
				"Speciation",
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
				//BCC - 15-Oct-29015 -  Suppress display of IsRegular
				//data.myVariable.IsRegular.ToString(),
				data.myVariable.TimeSupport.ToString(),
				//BCC - 18-Nov-2015 - GitHub issue #65 - Time Units is displaying abbreviation in download instead of full name
				(! String.IsNullOrWhiteSpace(data.myVariable.TimeUnit.Name)) ? data.myVariable.TimeUnit.Name.ToString() :
					(! String.IsNullOrWhiteSpace(data.myVariable.TimeUnit.Abbreviation)) ? data.myVariable.TimeUnit.Abbreviation.ToString() : 
						ServerSideHydroDesktop.ObjectModel.Unit.UnknownTimeUnit.Name.ToString(),
				//data.myVariable.TimeUnit.Name.ToString(),
				data.myVariable.Speciation.ToString(),
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

				//10-Aug-2015 - BCC - GitHub Issue #33 - Include Qualifier Description in downloaded time series data
				csvwrtr.WriteRecord(new List<string>() 
						{ 
							"UTCTimeStamp", 
							"LocalTimestamp",
							"UTCOffset",
							"Value",
							"ValueAccuracy",                            
							"CensorCode", 
							"OffsetValue", 
							"OffsetDescription",
							"OffsetUnit",
							"Qualifier",
							"QualifierDescription"
						});

				foreach (DataValue value in data.values)
				{
					List<string> values = new List<string>();
                    values.Add(value.UTCTimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    values.Add(value.LocalTimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    values.Add(value.UTCOffset.ToString());
                    values.Add(value.Value.ToString());
                    values.Add(value.ValueAccuracy.ToString());
                    values.Add(value.CensorCode);                    
					values.Add(value.OffsetValue.ToString());
					values.Add(value.OffsetDescription);
					values.Add(value.OffsetUnit);
					values.Add(value.Qualifier);
					values.Add(value.QualifierDescription);
                    
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
		private string GenerateFileName(SeriesMetadata meta, TimeSeriesFormat timeSeriesFormat = TimeSeriesFormat.CSV)
		{

			string fileName = string.Format("{0}-{1}-{2}", meta.ServCode.SanitizeForFilename(), meta.SiteName.SanitizeForFilename(), meta.VariableName.SanitizeForFilename());
			string extension = String.Empty;

			if (TimeSeriesFormat.WaterOneFlow == timeSeriesFormat)
			{
				extension = ".xml";
			}
			else if (TimeSeriesFormat.CSV == timeSeriesFormat)
		{
			//NOTE: Microsoft Excel restricts file path + name + extension to 218 characters max.  Truncate file name if indicated...

				extension = ".csv";

			while (218 < (fileName.Length + extension.Length))
			{
				fileName = fileName.Substring(0, (fileName.Length - 1));
				}				
			}

			return string.Format("{0}{1}", fileName, extension);

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



/*
 
							if (bUseWaterOneFlow)
							{
								int count = tsrIn.WaterOneFlowIds.Count;
								foreach (string waterOneFlowId in tsrIn.WaterOneFlowIds)
								{
									//Check for cancellation...
									if (IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
									{
										break;
									}

									UpdateTaskStatus(requestId, TimeSeriesRequestStatus.ProcessingTimeSeriesId,
										TimeSeriesRequestStatus.ProcessingTimeSeriesId.GetEnumDescription() +
																		  waterOneFlowId +
																		  " (" + (++i).ToString() + " of " + count.ToString() + ")");


									//Retrieve WaterOneFlow file from azure blob storage as stream
									MemoryStream ms = new MemoryStream();
									bool bFound = await _ac.RetrieveBlobAsync(waterOneFlowId, ct, ms);

									if ( ! IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
									{
										if (bFound)
										{
											//Retrieve the water one flow version...
											MemoryStream msDecompress = new MemoryStream();

											string version = GetWaterOneFlowVersion(ms, ref msDecompress);
											if ( ! String.IsNullOrWhiteSpace(version))
											{
												IWaterOneFlowParser parser;
												switch (version)
												{
													case "1.0":
														parser = new WaterOneFlow10Parser();
														break;
													case "1.1":
														parser = new WaterOneFlow11Parser();
														break;
													default:
														//Take no action...
														parser = null;
														break;
												}

												if (null != parser)
												{
													//Parser found - parse water one flow contents...
													msDecompress.Seek(0, SeekOrigin.Begin);
													IList<ServerSideHydroDesktop.ObjectModel.Site> listSites = parser.ParseGetSites(msDecompress);

													msDecompress.Seek(0, SeekOrigin.Begin);
													IList<ServerSideHydroDesktop.ObjectModel.SeriesMetadata> listMeta = parser.ParseGetSiteInfo(msDecompress);

													msDecompress.Seek(0, SeekOrigin.Begin);
													IList<ServerSideHydroDesktop.ObjectModel.Series> listSeries = parser.ParseGetValues(msDecompress);

													//			 parse file contents into stream
													//			 add stream as a zip archive entry...
													int n = 5;

													++n;
									
												}
												else
												{
													//TO DO - parser not found - make an error stream, add stream as a zip archive entry...

												}
											}
											else
											{
												//TO DO - version not found - make an error stream, add stream as a zip archive entry...
											}
										}
										else
										{
											//DO DO - //If not found, make an error stream, add stream as a zip archive entry...
										}
									}
								}
							}

 */


