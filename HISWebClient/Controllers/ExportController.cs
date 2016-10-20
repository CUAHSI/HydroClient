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

using System.Runtime.Remoting.Messaging;

using Newtonsoft.Json;

using log4net.Core;

using HISWebClient.Models.DataManager;
using HISWebClient.Util;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using HISWebClient.Models.DownloadManager;


//using ServerSideHydroDesktop.ObjectModel;

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

		//Variable units dictionary - keyed by Site Code, Variable Code
		private static Dictionary<Tuple<String, String, String>, String> _dictVarUnits = new Dictionary<Tuple<String, String, String>, String>();
		private static Object lockObject_VU = new Object();


		private DbLogContext dblogcontext = new DbLogContext("DBLog", "AdoNetAppenderLog", "local-DBLog", "deploy-DBLog");

		private DbErrorContext dberrorcontext = new DbErrorContext("DBError", "AdoNetAppenderError", "local-DBLog", "deploy-DBLog");

		private Random _random = new Random(DateTime.Now.Millisecond);

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

		//Get a task status
		private TimeSeriesRequestStatus GetTaskStatus(string requestId)
		{
			TimeSeriesRequestStatus tsrs = 0;	//Indicates unknown request...

			//Thread-safe access to dictionary
			lock (lockObject)
			{
				if (_dictTaskStatus.ContainsKey(requestId))
				{
					//Task entry found - update task status
					tsrs = _dictTaskStatus[requestId].RequestStatus;
				}
			}

			return tsrs;
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
			var seriesMetaData = getSeriesMetadata(id);
			var filename = FileContext.GenerateFileName(seriesMetaData);
			var fileType = "text/csv";

			try
			{
				var result = await this.getStream(id);

				return new FileStreamResult(new MemoryStream(result.Item1), fileType) { FileDownloadName = filename };
			}
			catch( Exception ex )
			{
				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current, 
											  DateTime.UtcNow, "DownloadFile(int id)", 
											  ex, 
											  "DownloadFile Errors for: " + filename + " message: " + ex.Message);

				string input = "An error occured downloading file: " + filename;

				byte[] result = Encoding.UTF8.GetBytes(input);

				return new FileStreamResult(new MemoryStream(result), fileType) { FileDownloadName = "ERROR " + filename };
			}
		}



		private async Task<Tuple<FileStreamResult, SeriesData, Exception>> DownloadFile(int id, List<TimeSeriesViewModel> currentSeries, TimeSeriesFormat timeSeriesFormat = TimeSeriesFormat.CSV)
		{

			var seriesMetaData = getSeriesMetadata(id, currentSeries);
			var filename = FileContext.GenerateFileName(seriesMetaData, timeSeriesFormat);
			var fileType = TimeSeriesFormat.CSV == timeSeriesFormat ? "text/csv" : "application/xml";

			try
			{
				if (TimeSeriesFormat.CSV == timeSeriesFormat)
				{
					var result = await this.getStream(id, currentSeries);
					return new Tuple<FileStreamResult, SeriesData, Exception>( new FileStreamResult(new MemoryStream(result.Item1), fileType) { FileDownloadName = filename }, result.Item2, null);
				}
				else if (TimeSeriesFormat.WaterOneFlow == timeSeriesFormat)
				{
					var result = await this.getWOFStream(id, currentSeries);

					result.Item1.Seek(0, SeekOrigin.Begin);

					FileStreamResult fsr = new FileStreamResult(result.Item1, fileType) { FileDownloadName = filename };

					return new Tuple<FileStreamResult, SeriesData, Exception>( fsr, result.Item2, null);
				}
				else
				{
					throw new ArgumentException(String.Format("Unknown TimeSeriesFormat value: {0}", timeSeriesFormat.ToString()));
				}

			}
			catch( Exception ex )
			{
				//Log error...
				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "DownloadFile(int id, List<TimeSeriesViewModel> currentSeries, timeSeriesFormat)",
											  ex,
											  "DownloadFile Errors for: " + filename + " message: " + ex.Message);

				string input = "An error occured downloading file: " + filename;

				byte[] result = Encoding.UTF8.GetBytes(input);

				//Return 'error' file
				return new Tuple<FileStreamResult, SeriesData, Exception>( new FileStreamResult(new MemoryStream(result), "text/csv") { FileDownloadName = "ERROR " + filename }, null,  ex);
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
            var RequestFormat = tsrIn.RequestFormat;
			TimeSeriesRequestStatus requestStatus = TimeSeriesRequestStatus.Starting;
			string status = requestStatus.GetEnumDescription();
			string blobUri = "Not yet available...";
			DateTime blobTimeStamp = DateTime.MinValue;
			CancellationToken ct;
			bool bNewTask = false;  //Assume time series retrieval task already exists...

			var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);
			CurrentUser cu = httpContext.Session[httpContext.Session.SessionID] as CurrentUser;

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
					blobTimeStamp = _dictTaskStatus[requestId].BlobTimeStamp;
				}
				else
				{
					//New task - allocate a task status instance - add to dictionary
					var taskData = new TaskData(requestStatus, status, new CancellationTokenSource(), blobUri, blobTimeStamp);

					_dictTaskStatus.Add(requestId, taskData);
					ct = taskData.CTS.Token;
					bNewTask = true;

					WriteTaskDataToDatabase(cu, requestId, requestStatus, "", blobUri, blobTimeStamp);	
				}
			}

			//Start the async time series retrieval task, if indicated
			if (bNewTask)
			{
				//Copy the currently requested time series for use in the task...
				var retrievedSeries = (List<TimeSeriesViewModel>)httpContext.Session["Series"];

				//Get user data...
				DateTime requestTimeStamp = httpContext.Timestamp;

				List<TimeSeriesViewModel> currentSeries = new List<TimeSeriesViewModel>();
				//int total = tsrIn.TimeSeriesIds.Count;
				int selectedValues = LogRequestIds(tsrIn.RequestId, tsrIn.RequestName, retrievedSeries, currentSeries, tsrIn.TimeSeriesIds);

				//Retrieve values for use in async task...
				string sessionId = String.Empty;
				string userIpAddress = string.Empty;
				string domainName = string.Empty;

				dblogcontext.getIds(System.Web.HttpContext.Current, ref sessionId, ref userIpAddress, ref domainName);

				//If user authenticated, retrieve the e-mail address..
				var userEMailAddressRef = (null != cu && cu.Authenticated) ? cu.UserEmail : "unknown";

				var dblogcontextRef = dblogcontext;
				var dberrorcontextRef = dberrorcontext;
				var sessionIdRef = sessionId;
				var userIpAddressRef = userIpAddress;
				var domainNameRef = domainName;

                #region MyRegion run task

				//Generate a unique ID...
				int uniqueId = _random.Next();

				//Save ids to log and error contexts...
				dblogcontextRef.saveIds(uniqueId, sessionIdRef, userIpAddressRef, domainNameRef);
				dberrorcontextRef.saveIds(uniqueId, sessionIdRef, userIpAddressRef, domainNameRef);

                Task.Run(async () =>
                {
					//Set unique ID into the async call context (used by DbBaseContext.getIds when null == httpcontext as happens on async calls!!)
					//Source: http://stackoverflow.com/questions/14176028/why-does-logicalcallcontext-not-work-with-async - answer 10
					CallContext.LogicalSetData("uniqueId", uniqueId);
				
					try
					{
						DateTime startDtUtc = DateTime.UtcNow;
						//Allocate a memory stream for later use...
						var memoryStream = new MemoryStream();  //ASSUMPTION: IDispose called during FileStreamResult de-allocation
						TimeSeriesRequestStatus cancellationEnum = TimeSeriesRequestStatus.CanceledPerClientRequest;
						string cancellationMessage = TimeSeriesRequestStatus.CanceledPerClientRequest.GetEnumDescription();

						//Allocate a zip archive for later use...
						using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
						{
							int bufSize = 4096;
							int i = 0;
                            var listOfValuesWithMetadata = new List<DataValueCsvViewModel>();

							int maxCombinedExportValues = Convert.ToInt32(ConfigurationManager.AppSettings["maxCombinedExportValues"]);
							if (TimeSeriesFormat.CSVMerged == tsrIn.RequestFormat && selectedValues > maxCombinedExportValues)
							{
								throw new ValidationException( String.Format("The number of values in the export exceeds the allowed maximum of {0:#,###0}.", maxCombinedExportValues));
							}

							//For each time series id...
							int count = tsrIn.TimeSeriesIds.Count;
							foreach (int timeSeriesId in tsrIn.TimeSeriesIds)
							{
								//Check for cancellation...
								if (IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
								{
									break;
								}

								string statusMessage = TimeSeriesRequestStatus.ProcessingTimeSeriesId.GetEnumDescription() +
																								timeSeriesId.ToString() +
																								" (" + (++i).ToString() + " of " + count.ToString() + ")";
								UpdateTaskStatus(requestId, TimeSeriesRequestStatus.ProcessingTimeSeriesId, statusMessage);
								WriteTaskDataToDatabase(cu, requestId, TimeSeriesRequestStatus.ProcessingTimeSeriesId, statusMessage, blobUri, blobTimeStamp);

								if (TimeSeriesFormat.CSV == tsrIn.RequestFormat || TimeSeriesFormat.WaterOneFlow == tsrIn.RequestFormat)
								{
									//Retrieve the time series data in the input format: CSV --OR-- XML
 									Tuple<FileStreamResult, SeriesData, Exception> downloadResult = await DownloadFile(timeSeriesId, currentSeries, tsrIn.RequestFormat);
									FileStreamResult filestreamresult = downloadResult.Item1;
									Exception exDownload = downloadResult.Item3;

									//Check for failed/empty downloads.
									Exception ex = null;
									Exception exLog = null;
									if (filestreamresult.FileDownloadName.Contains("ERROR", StringComparison.CurrentCultureIgnoreCase) || 0 >= filestreamresult.FileStream.Length)
									{
										//BCC - 25-Jan-2016 - set task status error here - log error message - throw...
										//BCC - 08-Mar-2016 - Revised logic to log the error only - do not want to throw (i.e., stop the entire task) for problem(s) with a single file...

										if (filestreamresult.FileDownloadName.Contains("ERROR", StringComparison.CurrentCultureIgnoreCase))
										{
#if (DEBUG)
											ex = new Exception("Error in time series generation: " +
															   exDownload.Message );
#else
											ex = new Exception("Error in time series generation: The external server was unable to complete the request.  " +  
															   "Please limit search area, date range and/or keywords." );
#endif
											exLog = new Exception("Error in time series generation: " + exDownload.Message);
										}
										else
										{
											ex = new Exception("Empty time series...");
											exLog = new Exception("Empty time series...");
										}

										dberrorcontextRef.clearParameters();
										dberrorcontextRef.createLogEntry(sessionIdRef,
																			userIpAddressRef,
																			domainNameRef,
																			DateTime.UtcNow,
																			"RequestTimeSeries(TimeSeriesRequest tsrIn)",
																			exLog,
																			"RequestTimeSeries error for Id: " + requestId + " message: " + exLog.Message);
									}

									//ALWAYS Copy file contents to zip archive - Good results, error in results generation or empty results...
									//ASSUMPTION: FileStreamResult instance properly disposes of FileStream member!!
									var zipArchiveEntry = zipArchive.CreateEntry(filestreamresult.FileDownloadName);
									using (var zaeStream = zipArchiveEntry.Open())
									{
										await filestreamresult.FileStream.CopyToAsync(zaeStream, bufSize);
									}

									//Throw exception if request deals with a single timeseries...
									if ( null != ex && 1 >= tsrIn.TimeSeriesIds.Count )
									{
										throw ex;
									}
								}
								else
								{
									if (tsrIn.RequestFormat == TimeSeriesFormat.CSVMerged)
									{
										try
										{
											//Retrieve data as list of objects that we can serialize to CSV
											//get objects an serialze to CSV once all series have returned
											//var list = new List<TimeSeriesViewModel>();
											var list = await DownloadFileInList(timeSeriesId, currentSeries, tsrIn.RequestFormat);
											;
											if (list.Count < maxCombinedExportValues)
											{
												listOfValuesWithMetadata.AddRange(list);
												list.Clear();
											}
											else
											{
												throw new ValidationException( String.Format("The number of values in the export exceeds the allowed maximum of {0:#,###0}.", maxCombinedExportValues));
											}
										}
										catch (Exception ex)
										{
											throw ex;
										}
									}
								}
                            }

                            if (tsrIn.RequestFormat == TimeSeriesFormat.CSVMerged)
                            {
								var filestreamresult = WriteCsvToFileStream(listOfValuesWithMetadata, cu);

								using (filestreamresult.FileStream) 
								{
									var zipArchiveEntry = zipArchive.CreateEntry(filestreamresult.FileDownloadName);
									using (var zaeStream = zipArchiveEntry.Open())
									{
										await filestreamresult.FileStream.CopyToAsync(zaeStream, bufSize);
									}
								}
							}
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
							string archiveRequestName = (TimeSeriesFormat.CSVMerged == tsrIn.RequestFormat) ? "combined_" + requestName : requestName;
							blobUri = await _ac.UploadFromMemoryStreamAsync(memoryStream, archiveRequestName, ct);
							blobTimeStamp = DateTime.Now;
							WriteTaskDataToDatabase(cu, requestId, TimeSeriesRequestStatus.SavingZipArchive, "", blobUri, blobTimeStamp);	

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
									_dictTaskStatus[requestId].BlobTimeStamp = blobTimeStamp;
								}

								//Record the final status in the database, if indicated
								WriteTaskDataToDatabase(cu, requestId, TimeSeriesRequestStatus.Completed, "", blobUri, blobTimeStamp);

								dblogcontextRef.clearParameters();
								dblogcontextRef.clearReturns();

								dblogcontextRef.addParameter("requestId", tsrIn.RequestId);
								dblogcontextRef.addParameter("requestName", tsrIn.RequestName);

								dblogcontextRef.addReturn("requestId", requestId);
								dblogcontextRef.addReturn("requestStatus", requestStatus);
								dblogcontextRef.addReturn("status", status);
								dblogcontextRef.addReturn("blobUri", blobUri);
								dblogcontextRef.addReturn("blobTimeStamp", blobTimeStamp);

								dblogcontextRef.createLogEntry(sessionIdRef, userIpAddressRef, domainNameRef, userEMailAddressRef, startDtUtc, DateTime.UtcNow, "RequestTimeSeries(...)", "zip archive creation complete.", Level.Info);
							}
#if NEVER_DEFINED
		//BCC - 07-Apr-2016 - Group decision - DO NOT include e-mail messages in Release 1.2
                            sendEmail(cu, blobUri);
#endif
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
						string errorMessage = TimeSeriesRequestStatus.RequestTimeSeriesError.GetEnumDescription() + ": " + ex.Message;
						UpdateTaskStatus(requestId, TimeSeriesRequestStatus.RequestTimeSeriesError, errorMessage);
						WriteTaskDataToDatabase(cu, requestId, TimeSeriesRequestStatus.RequestTimeSeriesError, errorMessage, blobUri, blobTimeStamp);	

						//Set status variables...
						requestStatus = TimeSeriesRequestStatus.RequestTimeSeriesError;
						//BCC - 07-Jun-2016 - Include error message in status string...
						status = requestStatus.GetEnumDescription() + ": " + ex.Message;
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

						//Remove ids from log and error contexts...
						dblogcontextRef.removeIds(uniqueId);
						dberrorcontextRef.removeIds(uniqueId);
					}
				});

                #endregion
			}

			//Return a TimeSeriesResponse in JSON format...
			var result = new TimeSeriesResponse(tsrIn.RequestId, requestStatus, status, blobUri, blobTimeStamp);

			var json = JsonConvert.SerializeObject(result);

#if NEVER_DEFINED
			//Simulate a 500 error - Internal Server Error...
			//NOTE: Need to assign a temporary here to avoid compiler error: CS0206 - A property or indexer may not be passed as an out or ref parameter.
			HttpResponseBase httpResponseBase = Response;
			httpUtil.setHttpResponseStatusCode(ref httpResponseBase, HttpStatusCode.InternalServerError);
#endif				
			//Processing complete - return 
			return Json(json, "application/json");
		}

		//Convert the input compressed WaterML files to CSV format...
		[HttpPost]
		public async Task<JsonResult> ConvertWaterMlToCsv(ConvertWaterMlToCsvRequest crIn)
		{
			//Retrieve the input request Id
			var requestId = crIn.RequestId;
			var requestName = crIn.RequestName;
			TimeSeriesRequestStatus requestStatus = TimeSeriesRequestStatus.Starting;
			string status = requestStatus.GetEnumDescription();
			string blobUri = "Not yet available...";
			DateTime blobTimeStamp = DateTime.MinValue;
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
					blobTimeStamp = _dictTaskStatus[requestId].BlobTimeStamp;
				}
				else
				{
					//New task - allocate a task status instance - add to dictionary
					var taskData = new TaskData(requestStatus, status, new CancellationTokenSource(), blobUri, blobTimeStamp);

					_dictTaskStatus.Add(requestId, taskData);
					ct = taskData.CTS.Token;
					bNewTask = true;
				}
			}

			//Start the async conversion task, if indicated
			if (bNewTask)
			{
				var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);
				CurrentUser cu = httpContext.Session[httpContext.Session.SessionID] as CurrentUser;

				//Get user data...
				DateTime requestTimeStamp = httpContext.Timestamp;

				var wofIds = crIn.WofIds.Select( w => w.WofId);
				LogRequestIds(crIn.RequestId, crIn.RequestName, wofIds.ToList<string>());

				//Retrieve values for use in async task...
				string sessionId = String.Empty;
				string userIpAddress = string.Empty;
				string domainName = string.Empty;

				dblogcontext.getIds(System.Web.HttpContext.Current, ref sessionId, ref userIpAddress, ref domainName);

				//If user authenticated, retrieve the e-mail address..
				var userEMailAddressRef = (null != cu && cu.Authenticated) ? cu.UserEmail : "unknown";

				var dblogcontextRef = dblogcontext;
				var dberrorcontextRef = dberrorcontext;
				var sessionIdRef = sessionId;
				var userIpAddressRef = userIpAddress;
				var domainNameRef = domainName;

				//Generate a unique ID...
				int uniqueId = _random.Next();

				//Save ids to log and error contexts...
				dblogcontextRef.saveIds(uniqueId, sessionIdRef, userIpAddressRef, domainNameRef);
				dberrorcontextRef.saveIds(uniqueId, sessionIdRef, userIpAddressRef, domainNameRef);

				Task.Run(async () =>
				{
					//Set unique ID into the async call context (used by DbBaseContext.getIds when null == httpcontext as happens on async calls!!)
					//Source: http://stackoverflow.com/questions/14176028/why-does-logicalcallcontext-not-work-with-async - answer 10
					CallContext.LogicalSetData("uniqueId", uniqueId);

					try
					{
						DateTime startDtUtc = DateTime.UtcNow;

						//Allocate a memory stream for later use...
						var memoryStream = new MemoryStream();  //ASSUMPTION: IDispose called during FileStreamResult de-allocation
						TimeSeriesRequestStatus cancellationEnum = TimeSeriesRequestStatus.CanceledPerClientRequest;
						string cancellationMessage = TimeSeriesRequestStatus.CanceledPerClientRequest.GetEnumDescription();

						//Allocate a zip archive for later use...
						using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
						{
							int bufSize = 4096;
							int i = 0;

							int count = crIn.WofIds.Count;
							foreach (WofIds currentWofIds in crIn.WofIds)
								{
								//Check for cancellation...
								if (IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
								{
									break;
								}

								UpdateTaskStatus(requestId, TimeSeriesRequestStatus.ProcessingTimeSeriesId,
									TimeSeriesRequestStatus.ProcessingTimeSeriesId.GetEnumDescription() +
																		currentWofIds.WofId +
																		" (" + (++i).ToString() + " of " + count.ToString() + ")");

								//Retrieve WaterOneFlow file from azure blob storage as stream
								using (MemoryStream ms = new MemoryStream())
								{
									bool bFound = await _ac.RetrieveBlobAsync(currentWofIds.WofId, ct, ms);
									if (bFound)
									{
										//Retrieve the water one flow version...
										MemoryStream msDecompress = new MemoryStream();

										string version = GetWaterOneFlowVersion(ms, ref msDecompress);
										if (!String.IsNullOrWhiteSpace(version))
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

												//NOTE: This call returns zero entries...
												//msDecompress.Seek(0, SeekOrigin.Begin);
												//IList<ServerSideHydroDesktop.ObjectModel.Site> listSites = parser.ParseGetSites(msDecompress);

												//NOTE: This call returns zero entries...
												//msDecompress.Seek(0, SeekOrigin.Begin);
												//IList<ServerSideHydroDesktop.ObjectModel.SeriesMetadata> listMeta = parser.ParseGetSiteInfo(msDecompress);

												//Parse the stream for series entries...
												//NOTE: ParseGetValues can produce a number of series instances per sorting/grouping logic
												msDecompress.Seek(0, SeekOrigin.Begin);
												IList<ServerSideHydroDesktop.ObjectModel.Series> listSeries = parser.ParseGetValues(msDecompress);

												if ((null != listSeries) && (0 < listSeries.Count))
												{
													//Series entries found - per code in GetSeriesDataObjectAndStreamFromSeriesID(...) process each entry
													SeriesData sd = new SeriesData();
													SeriesMetadata smd = null;

													//Instantiate DataValue list...
													List<DataValue> dvList = new List<DataValue>();

													//For each series in the returned list...
													foreach (var series in listSeries)
													{
														if ((!String.IsNullOrWhiteSpace(currentWofIds.SourceId)) &&
															(!String.IsNullOrWhiteSpace(currentWofIds.MethodId)) &&
															(!String.IsNullOrWhiteSpace(currentWofIds.QCLID)) &&
															(series.Source.IsValid) &&
															(series.Method.IsValid) &&
															(series.QualityControlLevel.IsValid))
														{
															//Comparison values non-empty/valid - Check for a particular time series...
															if (series.Source.OriginId != Int32.Parse(currentWofIds.SourceId) ||
																series.Method.Code != Int32.Parse(currentWofIds.MethodId) ||
																series.QualityControlLevel.OriginId != Int32.Parse(currentWofIds.QCLID))
															{
																//Source, method and/or quality control level do NOT agree - continue...
																continue;
															}
														}

														//Allocate metadata, if indicated...
														if ( null == smd)
														{
															//Load web service list for later reference...
															//NOTE: Since this code runs in a background thread, there is no HTTP context, no way to save the result to the Session object...
															var dataWorker = new DataLayer.DataWorker();
															List<BusinessObjects.WebServiceNode> allWebServices = dataWorker.getWebServiceList();

															//Retrieve the result's service code, scan web service data for a match...
															var serviceCodeLowerCase = series.Site.Code.Split(':')[0].ToLower();
															BusinessObjects.WebServiceNode wsn = null;

															if (!String.IsNullOrWhiteSpace(serviceCodeLowerCase))
															{
																wsn = allWebServices.Find(w => w.ServiceCode.ToLower() == serviceCodeLowerCase);
															}

															//Create a meta data instance
															smd = new SeriesMetadata();

															smd.ServCode = (null != wsn) ? wsn.ServiceCode : "unknown";	//NOTE: 'Unknown' case should never occur!!
															smd.ServURL = (null != wsn) ? wsn.ServiceUrl : "unknown";	//NOTE: 'Unknown' case should never occur!!
															smd.SiteCode = series.Site.Code;
															smd.VarCode = series.Variable.Code;
															smd.SiteName = series.Site.Name;
															smd.VariableName = series.Variable.Name;
															smd.SampleMedium = series.Variable.SampleMedium;
															smd.GeneralCategory = series.Variable.GeneralCategory;
															smd.StartDate = series.BeginDateTime;
															smd.EndDate = series.EndDateTime;
															//Initialize value count...
															smd.ValueCount = 0;
															smd.Latitude = series.Site.Latitude;
															smd.Longitude = series.Site.Longitude;
															//BCC - 08-Aug-2016 - Assign Series, Site and Variable ID's correct values...
															smd.SeriesID = (int) series.Id;
															smd.SiteID = (int) series.Site.Id;
															smd.VariableID = (int) series.Variable.Id;

															sd.SeriesID = smd.SeriesID;
															sd.myMetadata = smd;

															sd.myVariable = series.Variable;
														}


														//Add data values to list...
														IList<DataValue> dataValues = series.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();

														foreach (var dv in dataValues)
														{
															//TO DO - Need to set lab sample code...
															//dv.LabSampleCode = ??
															dv.MethodCode = series.Method.Code.ToString();
															dv.QualityControlLevelCode = series.QualityControlLevel.Code;
															dv.SourceCode = series.Source.OriginId.ToString();

															dv.SeriesData = sd;

															dvList.Add(dv);
														}

														//Add Source instance
														if ((null != series.Source) && 
															(null == sd.mySources.FirstOrDefault(s => s.OriginId == series.Source.OriginId)))
														{
															sd.mySources.Add(series.Source);
														}

														//Add Method instance
														if ((null != series.Method) &&
															(null == sd.myMethods.FirstOrDefault(m => m.Code == series.Method.Code)))
														{
															sd.myMethods.Add(series.Method);
														}

														//Add QualityControlLevel instance
														if ((null != series.QualityControlLevel) &&
															(null == sd.myQualityControlLevels.FirstOrDefault(qcl => qcl.OriginId == series.QualityControlLevel.OriginId)))
														{
															sd.myQualityControlLevels.Add( series.QualityControlLevel);
														}
													}

													//Add values to SeriesData instance...
													sd.values = dvList.OrderBy(a => a.UTCTimeStamp).ToList();

													//Set the value count...
													smd.ValueCount = sd.values.Count;

													//Write data into stream, add stream as a zip archive entry...
													using (MemoryStream ms1 = new MemoryStream())
													{
														await WriteDataToMemoryStreamAsCsv(sd, ms1);
														ms1.Seek(0, SeekOrigin.Begin);

														var filename = FileContext.GenerateFileName(smd, TimeSeriesFormat.CSV);
														var zipArchiveEntry = zipArchive.CreateEntry(filename);
														using (var zaeStream = zipArchiveEntry.Open())
														{
															await ms1.CopyToAsync(zaeStream, bufSize);
														}
													}

												}
											}
										}
									}
								}
							}
		
						}

						//CSV processing complete - check for cancellation
						if (!IsTaskCancelled(requestId, cancellationEnum, cancellationMessage))
						{
							UpdateTaskStatus(requestId, TimeSeriesRequestStatus.SavingZipArchive, TimeSeriesRequestStatus.SavingZipArchive.GetEnumDescription());

							//Reposition to start of memory stream...
							memoryStream.Seek(0, SeekOrigin.Begin);

							//Upload zip archive...
							blobUri = await _ac.UploadFromMemoryStreamAsync(memoryStream, requestName, ct);
							blobTimeStamp = DateTime.Now;

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
									_dictTaskStatus[requestId].BlobTimeStamp = blobTimeStamp;
								}

								dblogcontextRef.clearParameters();
								dblogcontextRef.clearReturns();

								dblogcontextRef.addParameter("requestId", crIn.RequestId);
								dblogcontextRef.addParameter("requestName", crIn.RequestName);

								dblogcontextRef.addReturn("requestId", requestId);
								dblogcontextRef.addReturn("requestStatus", requestStatus);
								dblogcontextRef.addReturn("status", status);
								dblogcontextRef.addReturn("blobUri", blobUri);
								dblogcontextRef.addReturn("blobTimeStamp", blobTimeStamp);

								dblogcontextRef.createLogEntry(sessionIdRef, userIpAddressRef, domainNameRef, userEMailAddressRef, startDtUtc, DateTime.UtcNow, "ConvertWaterMlToCsv(...)", "zip archive creation complete.", Level.Info);
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
														 "ConvertWaterMlToCsv(ConvertWaterMlToCsvRequest crIn)",
														 ex,
														"ConvertWaterMlToCsv error for Id: " + requestId + " message: " + ex.Message);

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

						//Remove ids from log and error contexts...
						dblogcontextRef.removeIds(uniqueId);
						dberrorcontextRef.removeIds(uniqueId);
					}
				});
			}

			//Return a TimeSeriesResponse in JSON format...
			var result = new TimeSeriesResponse(crIn.RequestId, requestStatus, status, blobUri, blobTimeStamp);

			var json = JsonConvert.SerializeObject(result);

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
		private int LogRequestIds(string requestId, string requestName, List<TimeSeriesViewModel> retrievedSeries, List<TimeSeriesViewModel> currentSeries, List<int> timeSeriesIds ) 
		{
			int current = 0;
			int total = timeSeriesIds.Count;
			int selectedValues = 0;

			foreach (TimeSeriesViewModel tsvm in retrievedSeries)
			{
				currentSeries.Add(new TimeSeriesViewModel(tsvm));

				foreach (int id in timeSeriesIds)
				{
					if (tsvm.SeriesId == id)
					{
						//Requested time series found - increment selected values count...
						selectedValues += tsvm.ValueCount;
						
						//Log identifiers...
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

			//Processing complete - return selected values count...
			return selectedValues;
		}

		private void LogRequestIds(string requestId, string requestName, List<string> wofIds)
		{
			int current = 0;
			int total = wofIds.Count;

			foreach (string id in wofIds)
			{
				dblogcontext.clearParameters();
				dblogcontext.clearReturns();

				dblogcontext.addParameter("requestId", requestId);
				dblogcontext.addParameter("requestName", requestName);

				dblogcontext.addReturn("waterML", id);

				DateTime dtNow = DateTime.UtcNow;
				string message = String.Format("Requested WaterML: {0} of {1}", ++current, total);
				dblogcontext.createLogEntry(System.Web.HttpContext.Current, dtNow, dtNow, "RequestWaterMl(...)", message, Level.Info);
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
					tsr.BlobTimeStamp = _dictTaskStatus[Id].BlobTimeStamp;

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

			var json = JsonConvert.SerializeObject(tsr);

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
			var json = JsonConvert.SerializeObject(tsrList);

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

			var json = JsonConvert.SerializeObject(tsr);

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
				httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["ByuUrl"]);

				HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("apps/timeseries-viewer/api/list_apps/");

				string strJSON = String.Empty;
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					strJSON = httpResponseMessage.Content.ReadAsStringAsync().Result;
				}
				else
				{
					//Serialize an anonymous object...
					var jsonObj = new { apps = new List<string>() };
					strJSON = JsonConvert.SerializeObject(jsonObj);
				}

				//Processing complete - return 

				return new ContentResult { Content = strJSON, ContentType = "application/json" };
			}
			catch (Exception ex)
			{
				string strJSON = String.Empty;

				//Serialize an anonymous object...
				var jsonObj = new { apps = new List<string>() };
				strJSON = JsonConvert.SerializeObject(jsonObj);

				return new ContentResult { Content = strJSON, ContentType = "application/json" };
			}
		}

		public async Task<Tuple<byte[], SeriesData>> getStream(int SeriesID, List<TimeSeriesViewModel> currentSeries = null)
		{
			DateTimeOffset requestTime = DateTimeOffset.UtcNow;
		   
			//get series from wateroneflow and return response
			Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID, currentSeries);

			string nameGuid = Guid.NewGuid().ToString();

			var s = await this.getCSVResultByteArray(data.Item2, nameGuid, requestTime);

			return new Tuple<byte[], SeriesData>(s, data.Item2);
		}

        public async Task<List<DataValueCsvViewModel>> getCSVStream(int SeriesID, List<TimeSeriesViewModel> currentSeries = null, TimeSeriesFormat timeSeriesFormat = TimeSeriesFormat.CSV)
        {
            DateTimeOffset requestTime = DateTimeOffset.UtcNow;
            var list = new List<DataValueCsvViewModel>();
            //get series from wateroneflow and return response
            Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID, currentSeries);

            string nameGuid = Guid.NewGuid().ToString();
            if (timeSeriesFormat == TimeSeriesFormat.CSVMerged)
            {
                list = await this.getCSVResultAsList(data.Item2, nameGuid, requestTime, SeriesID);
            }
            return list;
            
        }

        private Task<List<DataValueCsvViewModel>> getCSVResultAsList(SeriesData seriesData, string nameGuid, DateTimeOffset requestTime, int seriesId)
        {
            return Task.Run(() =>
                {
                    var list = new List<DataValueCsvViewModel>();

					for (int i = 0; i < seriesData.values.Count; i++)
					{
						var dvm = new DataValueCsvViewModel();
						var value = seriesData.values[i];

						dvm.SeriesID = seriesId.ToString();
						dvm.SiteName = seriesData.myMetadata.SiteName;
						dvm.VariableName = seriesData.myMetadata.VariableName;
						dvm.LocalDateTime = value.LocalTimeStamp.ToString("yyyy-MM-dd HH:mm:ss");
						dvm.DataValue = value.Value.ToString();
						dvm.VarUnits = seriesData.myVariable.VariableUnit.Name;
						dvm.DataType = seriesData.myVariable.DataType;
						dvm.SiteID = seriesData.myMetadata.SiteID.ToString();
						dvm.SiteCode = seriesData.myMetadata.SiteCode;
						dvm.VariableID = seriesData.myVariable.Id.ToString();
						dvm.VariableCode = seriesData.myVariable.Code;

						//Retrieve organization, source description, sourcelink and citation references...
						dvm.Organization = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
						dvm.SourceDescription = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
						dvm.SourceLink = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
						dvm.Citation = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();

						var source = value.SeriesData.mySources.FirstOrDefault(s => s.OriginId == Convert.ToInt32(value.SourceCode));
						if (null != source)
						{
							dvm.Organization = source.Organization; ;
							dvm.SourceDescription = source.Description;
							dvm.SourceLink = source.Link;
							dvm.Citation = source.Citation;
						}

						dvm.ValueType = seriesData.myVariable.ValueType;
						dvm.TimeSupport = seriesData.myVariable.TimeSupport.ToString();
						dvm.TimeUnits = seriesData.myVariable.TimeUnit.Name;
						dvm.IsRegular = seriesData.myVariable.IsRegular.ToString();
						dvm.NoDataValue = seriesData.myVariable.NoDataValue.ToString();
						dvm.UTCOffset = seriesData.values[i].UTCOffset.ToString();
						//BCC - 27-Apr-2016 - values[i].UTCTimeStamp holds a valid date 
						//					  values[i].TimeStampUTC holds the minimum date
						//
						//	I know these facts should be obvious but they somehow escape me...
						dvm.DateTimeUTC = seriesData.values[i].UTCTimeStamp.ToString("yyyy-MM-dd HH:mm:ss");
						dvm.Latitude = seriesData.myMetadata.Latitude.ToString();
						dvm.Longitude = seriesData.myMetadata.Longitude.ToString();
						dvm.ValueAccuracy = seriesData.values[i].ValueAccuracy.ToString();
						dvm.CensorCode = seriesData.values[i].CensorCode.ToString();

						//Retrieve method code...
						dvm.MethodDescription = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();

						var method = value.SeriesData.myMethods.FirstOrDefault( m => m.Code == Convert.ToInt32(value.MethodCode));
						if (null != method)
						{
							dvm.MethodDescription = method.Description;
						}

						//Retrieve quality control level...
						dvm.QualityControlLevelCode = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();

						var qualityControlLevel = value.SeriesData.myQualityControlLevels.FirstOrDefault(qcl => qcl.Code == value.QualityControlLevelCode);
						if (null != qualityControlLevel)
						{
							dvm.QualityControlLevelCode = qualityControlLevel.Definition;
						}

						dvm.SampleMedium = seriesData.myMetadata.SampleMedium;
						dvm.GeneralCategory = seriesData.myVariable.GeneralCategory;
						dvm.OffsetValue = seriesData.values[i].OffsetValue.ToString();
						dvm.OffsetDescription = seriesData.values[i].OffsetDescription;
						dvm.OffsetUnits = seriesData.values[i].OffsetUnit;
						dvm.QualifierCode = seriesData.values[i].QualifierDescription;

						list.Add(dvm);
					}

					return list;
                });
        }

		//Similar to getStream(...) but returns the WaterOneFlow stream instead of the CSV byte array...
		public async Task<Tuple<Stream, SeriesData>> getWOFStream(int SeriesID, List<TimeSeriesViewModel> currentSeries = null)
		{
			DateTimeOffset requestTime = DateTimeOffset.UtcNow;

			//get series from wateroneflow and return response
			Tuple<Stream, SeriesData> data = await GetSeriesDataObjectAndStreamFromSeriesID(SeriesID, currentSeries);

			return (data);

		}

		public async Task<Tuple<Stream, SeriesData>> GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)
		{
			SeriesMetadata meta = getSeriesMetadata(seriesId, currentSeries);
			if (meta == null)
			{
				NullReferenceException nre = new NullReferenceException();

				dberrorcontext.clearParameters();
				dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)",
											  nre,
											  "null metadata");

				throw nre;
			}
			else
			{
				try
				{
					Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await this.SeriesAndStreamOfSeriesID(meta);

					//BCC - 06-Jun-2016 - Added stream null and length checks...
					if (data == null || null == data.Item2 || data.Item2.FirstOrDefault() == null || null == data.Item1 || 0 >= data.Item1.Length)
					{
						string msg = data == null ? "null series and stream" :
											 null == data.Item2 ? "null series" :
											 data.Item2.FirstOrDefault() == null ? "empty series" :
											 null == data.Item1 ? "null stream" : "empty stream";
						KeyNotFoundException knfe = new KeyNotFoundException(msg);

						dberrorcontext.clearParameters();
						dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
													  DateTime.UtcNow, "GetSeriesDataObjectAndStreamFromSeriesID(int seriesId, List<TimeSeriesViewModel> currentSeries)",
													  knfe,
													  msg);


						throw knfe;
					}
					else
					{
						//Debug code - multiple series returned 
#if (DEBUG)
						if ( 1 < data.Item2.Count)
						{
							int n = 5;

							n++;
						}
#endif
						//var dataResults = data.Item2;
						var listSeries = data.Item2;

						//Assign SeriesMetaData SiteID and VariableID values from first item in Series list...
						var series1 = listSeries.FirstOrDefault();

						meta.SiteID = (int) series1.Site.Id;
						meta.VariableID = (int) series1.Variable.Id;
						SeriesData sd = new SeriesData();

						sd.SeriesID = meta.SeriesID;
						sd.myMetadata = meta;
						sd.myVariable = series1.Variable;

						//Instantiate DataValue list...
						List<DataValue> dvList = new List<DataValue>();

						//Check for a particular time series (i.e, one source, method and quality control level combination)?
						TimeSeriesViewModel tsvmTarget = null;
						if ( null != currentSeries)
						{
							tsvmTarget = currentSeries.FirstOrDefault(tsvm => seriesId == tsvm.SeriesId);
						}

						foreach (var series in listSeries)
						{
							if ( (null != tsvmTarget) && 
								 (!String.IsNullOrWhiteSpace(tsvmTarget.SourceId)) && 
								 (!String.IsNullOrWhiteSpace(tsvmTarget.MethodId)) && 
								 (!String.IsNullOrWhiteSpace(tsvmTarget.QCLID)) &&
								 (series.Source.IsValid) &&
								 (series.Method.IsValid) &&
								 (series.QualityControlLevel.IsValid))
							{
 								//Comparison values non-empty/valid - Check for a particular time series...
								if (series.Source.OriginId != Int32.Parse(tsvmTarget.SourceId) ||
									series.Method.Code != Int32.Parse(tsvmTarget.MethodId) ||
									series.QualityControlLevel.OriginId != Int32.Parse(tsvmTarget.QCLID))
								{
									//Source, method and/or quality control level do NOT agree - continue...
									continue;
								}
							}

							//Add data values to list...
							IList<DataValue> dataValues = series.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();

							foreach( var dv in dataValues)
							{
								//TO DO - Need to set lab sample code...
								//dv.LabSampleCode = ??
								dv.MethodCode = series.Method.Code.ToString();
								dv.QualityControlLevelCode = series.QualityControlLevel.Code;
								dv.SourceCode = series.Source.OriginId.ToString();

								dv.SeriesData = sd;

								dvList.Add(dv);
							}

							//Add Source instance
							if ((null != series.Source) &&
								(null == sd.mySources.FirstOrDefault(s => s.OriginId == series.Source.OriginId)))
							{
								sd.mySources.Add(series.Source);
							}

							//Add Method instance
							if ((null != series.Method) &&
								(null == sd.myMethods.FirstOrDefault(m => m.Code == series.Method.Code)))
							{
								sd.myMethods.Add(series.Method);
							}

							//Add QualityControlLevel instance
							if ((null != series.QualityControlLevel) &&
								(null == sd.myQualityControlLevels.FirstOrDefault(qcl => qcl.OriginId == series.QualityControlLevel.OriginId)))
							{
								sd.myQualityControlLevels.Add(series.QualityControlLevel);
							}
						}

						//Add values to SeriesData instance...
						sd.values = dvList.OrderBy(a => a.UTCTimeStamp).ToList();

						//Update the value count...
						sd.myMetadata.ValueCount = sd.values.Count;

						return new Tuple<Stream, SeriesData>(data.Item1, sd);
					}
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}
		}



		public async Task<Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>>> SeriesAndStreamOfSeriesID(SeriesMetadata meta)
		{
			//var requestTimeout = 60000;
			var requestTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["requestTimeoutMilliseconds"].ToString());

			WaterOneFlowClient client = new WaterOneFlowClient(meta.ServURL);
			try
			{
				return await client.GetValuesAndRawStreamAsync(
						meta.SiteCode,
						meta.VarCode,
						meta.StartDate,
						meta.EndDate,
						Convert.ToInt32(requestTimeout));
			}
			catch (Exception ex)
			{
				string reason = httpUtil.getWebExceptionMessage(ex);

				if (!String.IsNullOrWhiteSpace(reason))
				{
					throw new Exception(reason);
				}

				throw ex;
			}
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
					// re-wind, if indicated...
					ms.Position = 0;
				}

				ms.Seek(0, SeekOrigin.Begin);
				return ms.ToArray();
			}
		}

		private async Task<CloudBlockBlob> WriteMemoryStreamToBlobInGuidDirectory(SeriesData data, MemoryStream ms, CloudStorageAccount csa)
		{
			CloudBlobClient bClient = csa.CreateCloudBlobClient();
			CloudBlobContainer container = bClient.GetContainerReference(DiscoveryStorageTableNames.SeriesDownloads);
			string fileName = FileContext.GenerateBlobName(data);
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
					"VariableName",                              
					"VarUnits",
					"SampleMedium",
					"DataType",
					"ValueType",
					//BCC - 15-Oct-2015 -  Suppress display of IsRegular
					//"IsRegular",
					"TimeSupport",
					"TimeUnits",
					"Speciation",
					"SiteName",
					"SiteCode",
					"Latitude",
					"Longitude",
					"GeneralCategory",
					"NoDataValue"
				});
				csvwrtr.WriteRecord(new List<string>() {  
					data.myMetadata.VariableName,
					data.myVariable.VariableUnit.Name,
					data.myMetadata.SampleMedium,	
					data.myVariable.DataType,
					data.myVariable.ValueType,
					//BCC - 15-Oct-29015 -  Suppress display of IsRegular
					//data.myVariable.IsRegular.ToString(),
					data.myVariable.TimeSupport.ToString(),
					//BCC - 18-Nov-2015 - GitHub issue #65 - Time Units is displaying abbreviation in download instead of full name
					//BCC - 21-Jul-2016 - Add null check for TimeUnit...
					((null != data.myVariable.TimeUnit) && (! String.IsNullOrWhiteSpace(data.myVariable.TimeUnit.Name))) ? data.myVariable.TimeUnit.Name.ToString() :
						((null != data.myVariable.TimeUnit) && (! String.IsNullOrWhiteSpace(data.myVariable.TimeUnit.Abbreviation))) ? data.myVariable.TimeUnit.Abbreviation.ToString() : 
							ServerSideHydroDesktop.ObjectModel.Unit.UnknownTimeUnit.Name.ToString(),
					data.myVariable.Speciation.ToString(),
					data.myMetadata.SiteName,
					data.myMetadata.SiteCode,
					data.myMetadata.Latitude.ToString(),
					data.myMetadata.Longitude.ToString(),
					data.myVariable.GeneralCategory,
					data.myVariable.NoDataValue.ToString()    
				});

				//10-Aug-2015 - BCC - GitHub Issue #33 - Include Qualifier Description in downloaded time series data
				csvwrtr.WriteRecord(new List<string>() 
						{ 
							"UTCTimeStamp", 
							"LocalTimestamp",
							"UTCOffset",
							"Value",
							"MethodDescription",
							"QualityControlLevel",
							"ValueAccuracy",                            
							"CensorCode", 
							"OffsetValue", 
							"OffsetDescription",
							"OffsetUnit",
							"Qualifier",
							"QualifierDescription",
							"SourceDescription",
							"SourceLink",
							"Citation"
						});

				foreach (DataValue value in data.values)
				{
					List<string> values = new List<string>();
					values.Add(value.UTCTimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
					values.Add(value.LocalTimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
					values.Add(value.UTCOffset.ToString());
					values.Add(value.Value.ToString());
					
					//Retrieve method description
					var methodDescription = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();

					var method = value.SeriesData.myMethods.FirstOrDefault( m => m.Code == Convert.ToInt32(value.MethodCode));
					if (null != method)
					{
						methodDescription = method.Description;
					}

					values.Add(methodDescription.ToString());

					//Retrieve quality control level description
					var qualityDefinition = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
					
					var qualityControlLevel = value.SeriesData.myQualityControlLevels.FirstOrDefault(qcl => qcl.Code == value.QualityControlLevelCode);
					if (null != qualityControlLevel)
					{
						qualityDefinition = qualityControlLevel.Definition;
					}

					values.Add(qualityDefinition.ToString());

					values.Add(value.ValueAccuracy.ToString());
					values.Add(value.CensorCode);
					values.Add(value.OffsetValue.ToString());
					values.Add(value.OffsetDescription);
					values.Add(value.OffsetUnit);
					values.Add(value.Qualifier);
					values.Add(value.QualifierDescription);

					//Retrieve source description, link and citation
					var sourceDescription = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
					var sourceLink = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
					var citation = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();

					var source = value.SeriesData.mySources.FirstOrDefault(s => s.OriginId == Convert.ToInt32(value.SourceCode));
					if (null != source)
					{
						sourceDescription = source.Organization;
						sourceLink = source.Link;
						citation = source.Citation;
					}

					values.Add(sourceDescription.ToString());
					values.Add(sourceLink.ToString());
					values.Add(citation.ToString());

					csvwrtr.WriteRecord(values);
				}
				await csvwrtr.FlushAsync();
			}
		}

        public FileStreamResult WriteCsvToFileStream<T>(List<T> records, CurrentUser cu = null)
        {
			string fileExtension = "csv";
			string fileName = null != cu ? FileContext.GenerateFileName( cu.UserEmail.Replace("@", "_"), fileExtension) : FileContext.GenerateFileName(null, fileExtension);
			string filePathAndName = Server.MapPath("~/Files/" + fileName.ToString());

			 //Create 'Files' sub-directory, if indicated...
			if ( ! Directory.Exists(Server.MapPath("~/Files/")))
			{
				Directory.CreateDirectory(Server.MapPath("~/Files/"));
			}

			//Use a large buffer here...
			FileStream fileStream = new FileStream(filePathAndName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 512000, FileOptions.DeleteOnClose );

            var streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("iso-8859-1"));
            var csvWriter = new CsvHelper.CsvWriter(streamWriter);
            
            //Get the properties for type T for the headers
            PropertyInfo[] propInfos = typeof(T).GetProperties();

            foreach (var prop in propInfos)
            {
                var attribs = prop.GetCustomAttributes();
                if (attribs.OfType<DisplayAttribute>().Count() == 0)
                {
                    csvWriter.WriteField(prop.Name);
                }
            }
            csvWriter.NextRecord();
            if (records != null)
            {

                //Loop through the collection, then the properties and add the values
				int nFlushCount = 0;
                for (int i = 0; i <= records.Count - 1; i++)
                {

                    T item = records[i];
                    //csvWriter..WriteField("SiteCode");
                    for (int j = 0; j <= propInfos.Length - 1; j++)
                    {

                        var attribs = propInfos[j].GetCustomAttributes();
                        if (attribs.OfType<DisplayAttribute>().Count() == 0)
                        {
                            object o = item.GetType().GetProperty(propInfos[j].Name).GetValue(item, null);

                            if (o != null)
                            {
                                string value = o.ToString();

                                csvWriter.WriteField(value);
                            }
                            else
                            {
                                csvWriter.WriteField(string.Empty);
							}
                        }
                    }
                    csvWriter.NextRecord();

					if (10000 <= ++nFlushCount)
					{
						//Flush every 10,000 records...
						nFlushCount = 0;
						streamWriter.Flush();
					}
                }
            }

            streamWriter.Flush();

			fileStream.Seek(0, SeekOrigin.Begin);
			return new FileStreamResult(fileStream, "text/plain") { FileDownloadName = fileName };
            
        }

		private void WriteTaskDataToDatabase(CurrentUser cu, String requestId, TimeSeriesRequestStatus requestStatus, String status, String blobUri, DateTime blobTimeStamp)
		{
			//Verify/initialize input parameters
			if ((null != cu) && cu.Authenticated && (!String.IsNullOrWhiteSpace(cu.UserEmail)) && (!String.IsNullOrWhiteSpace(requestId)))
			{

				if (String.IsNullOrWhiteSpace(status))
				{
					status = requestStatus.GetEnumDescription();
				}

				//using statement here ensure proper disposal of the dbcontext...
				//Source: http://www.davepaquette.com/archive/2013/03/27/managing-entity-framework-dbcontext-lifetime-in-asp-net-mvc.aspx
				using (HydroClientDbContext hcdbc = new HydroClientDbContext())
				{
					//Check time stamp values - if DateTime.MinValue change to SQL Server minimum value to avoid a 'value out of range' error...
					if (DateTime.MinValue == blobTimeStamp)
					{
						blobTimeStamp = new DateTime(1753, 1, 1);
					}

					//ExportTaskData etd = _hcdbc.ExportTaskDataSet.FirstOrDefault(e => e.RequestId.Equals(requestId) && e.UserEmail.Equals(cu.UserEmail));
					ExportTaskData etd = hcdbc.ExportTaskDataSet.FirstOrDefault(e => e.RequestId.Equals(requestId) && e.UserEmail.Equals(cu.UserEmail));
					if (null == etd)
					{
						//Record NOT found - add to database...
						etd = new ExportTaskData();

						etd.RequestId = requestId;
						etd.RequestStatus = (int) requestStatus;
						etd.Status = status;
						etd.UserEmail = cu.UserEmail;
						etd.BlobUri = blobUri;
						etd.BlobTimeStamp = blobTimeStamp;

						hcdbc.ExportTaskDataSet.Add(etd);
					}
					else
					{
						//Record found - update database...
						etd.RequestStatus = (int)requestStatus;
						etd.Status = status;
						etd.BlobUri = blobUri;
						etd.BlobTimeStamp = blobTimeStamp;
					}

					try
					{
						//hcdbc.SaveChanges();
						hcdbc.SaveChangesAsync();
					}
					catch (Exception ex)
					{
						var msg = ex.Message;
					}
				}
			}
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

			object[] metadata = new object[16];
			metadata[0] = d.ServCode;
			metadata[1] = d.ServURL;
			metadata[2] = d.SiteCode;
			metadata[3] = d.VariableCode;
			metadata[4] = d.SiteName;
			metadata[5] = d.VariableName;
			metadata[6] = d.SampleMedium;
			metadata[7] = d.GeneralCategory;
            //BCC - 27-Sep-2016 - Adjust times on begin and end dates...
			//metadata[8] = d.BeginDate;
			//metadata[9] = d.EndDate;

			//Set begin date time to 00:00:00
			DateTime beginDt = d.BeginDate;
			metadata[8] = beginDt.Date.AddHours(0).AddMinutes(0).AddSeconds(0);

			DateTime endDt = d.EndDate;
 			if (beginDt.Date == endDt.Date)
			{
				//Equal begin and end dates - increase end date by one day...
				endDt = endDt.Date.AddDays(1);
			}
			//Set end date time to 23:59:59
			metadata[9] = endDt.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

			metadata[10] = d.ValueCount;
			metadata[11] = d.Latitude;
			metadata[12] = d.Longitude;
			metadata[13] = SeriesId;
			metadata[14] = 0;			//SiteID - to be obtained later...
			metadata[15] = 0;			//VariableID - to be obtained later... 

			return new SeriesMetadata(metadata);
		}

        private async Task<List<DataValueCsvViewModel>> DownloadFileInList(int timeSeriesId, List<TimeSeriesViewModel> currentSeries, TimeSeriesFormat timeSeriesFormat)
        {

            var seriesMetaData = getSeriesMetadata(timeSeriesId, currentSeries);
            var filename = FileContext.GenerateFileName(seriesMetaData, timeSeriesFormat);
            var fileType = TimeSeriesFormat.CSV == timeSeriesFormat || TimeSeriesFormat.CSVMerged == timeSeriesFormat ? "text/csv" : "application/xml";
            var dataValuewithMetadaListForId = new List<DataValueCsvViewModel>();
            try
            {
                if (TimeSeriesFormat.CSVMerged == timeSeriesFormat)
                {
                    dataValuewithMetadaListForId = await this.getCSVStream(timeSeriesId, currentSeries, timeSeriesFormat);
                }                
                else
                {
                    throw new ArgumentException(String.Format("Unknown TimeSeriesFormat value: {0}", timeSeriesFormat.ToString()));
                }
                return dataValuewithMetadaListForId;
            }
            catch (Exception ex)
            {
                dberrorcontext.clearParameters();
                dberrorcontext.createLogEntry(System.Web.HttpContext.Current,
											  DateTime.UtcNow, "DownloadFileInList(int id, List<TimeSeriesViewModel> currentSeries, timeSeriesFormat)",
                                              ex,
                                              "DownloadFile Errors for: " + filename + " message: " + ex.Message);

                string input = "An error occured downloading file: " + filename;

				//Append an error entry to the list and return...
				var errorVal = new DataValueCsvViewModel();

				errorVal.SeriesID = timeSeriesId.ToString();
				errorVal.SiteName = input;

				dataValuewithMetadaListForId.Add(errorVal);
				return dataValuewithMetadaListForId;
			}
        }
	}
}


