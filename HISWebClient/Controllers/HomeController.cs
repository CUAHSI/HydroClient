using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HISWebClient.BusinessObjects;
using HISWebClient.DataLayer;
using TB.ComponentModel;
using HISWebClient.MarkerClusterer;
using System.Configuration;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Net;
using ServerSideHydroDesktop;
using System.Threading.Tasks;
using System.IO;
using CUAHSI.Models;
using HISWebClient.Models;
using System.Xml;
using System.Text.RegularExpressions;
using System.Reflection;

using System.Net.Http;

using log4net.Core;
using HISWebClient.Util;
using HISWebClient.Models.ClientFilterAndSearchCriteria;

namespace HISWebClient.Controllers
{
	public class HomeController : Controller
	{
		private readonly CUAHSI.DataExport.IExportEngine wdcStore;

		private DbLogContext dblogcontext = new DbLogContext("DBLog", "AdoNetAppenderLog", "local-DBLog", "deploy-DBLog");

		private DbErrorContext dberrorcontext = new DbErrorContext("DBError", "AdoNetAppenderError", "local-DBLog", "deploy-DBLog");

		private List<WebServiceNode> webServiceList = getWebServiceList();

		private ExportController exportController = new ExportController();

		private static bool bFirstCall = true;

		private static List<WebServiceNode> getWebServiceList()
		{
			var dataWorker = new DataWorker();

			return dataWorker.getWebServiceList();
		}

		public string LookupValueFromTitle( clientDataSource cds, TimeSeriesViewModel tsvm )
		{
			string value = String.Empty;

			//Validate/initialize input parameters
			if (null == cds || null == tsvm)
			{
				return value;	//Invalid parameter(s) - return early
			}

			switch (cds.title)
			{
				case "Service URL":		//Data Service URL (NOT the WSDL URL)

					//On the client's Data page, the DataTable's Service URL column lists the organization name.  The entry contains a link to the DescriptionUrl.
 					// On a search, the DataTable references the organization name value not the DescriptionUrl value...  
					//value = webServiceList.Find(wsn => wsn.ServiceCode.Equals(tsvm.ServCode, StringComparison.CurrentCultureIgnoreCase)).DescriptionUrl;
					value = tsvm.Organization;
					break;
				default:
					//Unknown title, take no action...
					break;
			}

			//Processing complete - return
			return value;
		}

		public ActionResult Index()
		{
			ViewBag.Message = "CUAHSI Hydrologic Data Services";
			
			if (Session["sessionGuid"] == null)
			{
				string sessionguid = Guid.NewGuid().ToString();
				Session["sessionGuid"] = sessionguid;
				ViewBag.ThisSessionGuid = sessionguid;
			}
			else
			{
				ViewBag.ThisSessionGuid = Session["sessionGuid"].ToString();
			}

			var ontologyHelper = new OntologyHelper();

			return View();
		}

		public ActionResult Index2()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

        public ActionResult Quickstart()
        {
            return View();
        }
		public ActionResult About()
		{
			ViewBag.Message = "About";

			return View();
		}
		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
		public ActionResult Sidebar()
		{
			ViewBag.Message = "Your Sideabar page.";

			return View();
		}

		public ActionResult Fancytreetest()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public ActionResult MapTabtest()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}


		public ActionResult Maptest()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}
        public ActionResult Maptest2()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        public ActionResult datatablestest()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}
		public ActionResult viztest()
		{
			ViewBag.Message = "viztest";

			return View();
		}
		public ActionResult updateMarkers(FormCollection collection)
		{
			if ( bFirstCall)
			{
				//First time called since last web site restart - log selected <appSettings> values...
				bFirstCall = false;

				dblogcontext.clearParameters();
				dblogcontext.clearReturns();

				dblogcontext.addParameter("ServiceUrl", ConfigurationManager.AppSettings["ServiceUrl"]);
				dblogcontext.addParameter("ServiceUrl_1_1_EndPoint", ConfigurationManager.AppSettings["ServiceUrl1_1_EndPoint"]);
				dblogcontext.addParameter("ByuUrl", ConfigurationManager.AppSettings["ByuUrl"]);
				dblogcontext.addParameter("MaxClustercount", ConfigurationManager.AppSettings["MaxClustercount"].ToString());
				dblogcontext.addParameter("maxAllowedTimeseriesReturn", ConfigurationManager.AppSettings["maxAllowedTimeseriesReturn"].ToString());
				dblogcontext.addParameter("maxCombinedExportValues", ConfigurationManager.AppSettings["maxCombinedExportValues"].ToString());
				dblogcontext.addParameter("blobContainer", ConfigurationManager.AppSettings["blobContainer"]);
				dblogcontext.addParameter("aspnet:MaxJsonDeserializerMembers", ConfigurationManager.AppSettings["aspnet:MaxJsonDeserializerMembers"].ToString());
				dblogcontext.addParameter("currentVersion", ConfigurationManager.AppSettings["currentVersion"].ToString());

				DateTime dtNow = DateTime.UtcNow;
				dblogcontext.createLogEntry(System.Web.HttpContext.Current, dtNow, dtNow, "updateMarkers(...)", "first call - selected appSettings values...", Level.Info);
			}

			var searchSettings = new SearchSettings();
			string markerjSON = string.Empty;
		   
			//get map geometry
			double xMin, xMax, yMin, yMax;
			int zoomLevel;

			int CLUSTERWIDTH = 50; //Cluster region width, all pin within this area are clustered
			int CLUSTERHEIGHT = 50; //Cluster region height, all pin within this area are clustered
			int CLUSTERINCREMENT = 5; //increment for clusterwidth 
			int MINCLUSTERDISTANCE = 25;
			int MAXCLUSTERCOUNT = Convert.ToInt32(ConfigurationManager.AppSettings["MaxClustercount"].ToString()); //maximum ammount of clustered markers

			UniversalTypeConverter.TryConvertTo<double>(collection["xMin"], out xMin);
			UniversalTypeConverter.TryConvertTo<double>(collection["xMax"], out xMax);
			UniversalTypeConverter.TryConvertTo<double>(collection["yMin"], out yMin);
			UniversalTypeConverter.TryConvertTo<double>(collection["yMax"], out yMax);
			Box box = new Box(xMin, xMax, yMin, yMax);
			UniversalTypeConverter.TryConvertTo<int>(collection["zoomLevel"], out zoomLevel);
			var activeWebservices = new List<WebServiceNode>();

			//Attempt to retrieve filtered timeseries ids, if indicated
			string strFilterAndSearchCriteria = collection["filterAndSearchCriteria"];

			clientFilterAndSearchCriteria filterAndSearchCriteria = null;

			if (!String.IsNullOrWhiteSpace(strFilterAndSearchCriteria))
			{
				filterAndSearchCriteria = JsonConvert.DeserializeObject<clientFilterAndSearchCriteria>(strFilterAndSearchCriteria);
			}

			//if it is a new request
			if (collection["isNewRequest"].ToString() == "true")
			{

				bool canConvert = false;

				var keywords = Regex.Split(collection["keywords"], @"##");
				var tileWidth = 10;
				var tileHeight = 10;
				List<int> webServiceIds = null;
				try
				{
					//Increase date range by one day to accomodate one day searches, if indicated...
					//Set begin date time to 00:00:00, set end date time to 23:59:59
					searchSettings.DateSettings.StartDate = Convert.ToDateTime(collection["startDate"]);
					searchSettings.DateSettings.StartDate = searchSettings.DateSettings.StartDate.Date.AddHours(0).AddMinutes(0).AddSeconds(0);

					searchSettings.DateSettings.EndDate = Convert.ToDateTime(collection["endDate"]);
					if (searchSettings.DateSettings.StartDate.Date == searchSettings.DateSettings.EndDate.Date)
					{
						searchSettings.DateSettings.EndDate = searchSettings.DateSettings.EndDate.Date.AddDays(1);
					}
					searchSettings.DateSettings.EndDate = searchSettings.DateSettings.EndDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
					
					//Convert to int Array
					if (collection["services"].Length > 0)
					{
						webServiceIds = collection["services"].Split(',').Select(s => Convert.ToInt32(s)).ToList();
					}

					var dataWorker = new DataWorker();

					var allWebservices = dataWorker.getWebServiceList();


					//filter list
					if (webServiceIds != null)
					{
						activeWebservices = dataWorker.filterWebservices(allWebservices, webServiceIds);
					}
					Session["webServiceList"] = allWebservices;

					//Clear parameters...
					dblogcontext.clearParameters();
					dberrorcontext.clearParameters();

					//Add call parameters...
					dblogcontext.addParameter("box", box);
					dberrorcontext.addParameter("box", box);
					if (1 == keywords.Length && "" == keywords[0])
					{
						dblogcontext.addParameter("keywords", "All");
						dberrorcontext.addParameter("keywords", "All");
					}
					else
					{
						StringBuilder sb1 = new StringBuilder();
						foreach (string keyword in keywords)
						{
							sb1.AppendFormat("{0}, ", keyword);
						}

						dblogcontext.addParameter("keywords", sb1.ToString());
						dberrorcontext.addParameter("keywords", sb1.ToString());
					}

					dblogcontext.addParameter("tileWidth", tileWidth);
					dberrorcontext.addParameter("tileWidth", tileWidth);

					dblogcontext.addParameter("tileHeight", tileHeight);
					dberrorcontext.addParameter("tileHeight", tileHeight);

					dblogcontext.addParameter("startDate", searchSettings.DateSettings.StartDate);
					dberrorcontext.addParameter("startDate", searchSettings.DateSettings.StartDate);

					dblogcontext.addParameter("endDate", searchSettings.DateSettings.EndDate);
					dberrorcontext.addParameter("endDate", searchSettings.DateSettings.EndDate);

					if (0 >= activeWebservices.Count)
					{
						dblogcontext.addParameter("activeWebServices", "All");
						dberrorcontext.addParameter("activeWebServices", "All");
					}
					else
					{
						StringBuilder sb1 = new StringBuilder();
						activeWebservices.ForEach(wsn => sb1.AppendFormat("{0},", wsn.Title));

						dblogcontext.addParameter("activeWebServices", sb1.ToString());
						dberrorcontext.addParameter("activeWebServices", sb1.ToString());
					}

					DateTime startDtUtc = DateTime.UtcNow;

					var series = dataWorker.getSeriesData(box, keywords.ToArray(), tileWidth, tileHeight,
																	 searchSettings.DateSettings.StartDate,
																	  searchSettings.DateSettings.EndDate,
																	  activeWebservices);
					DateTime endDtUtc = DateTime.UtcNow;

					//Clear returns
					dblogcontext.clearReturns();

					//Add returned series count...
					dblogcontext.addReturn("seriesCount", series.Count);

					//Create log entry...
					dblogcontext.createLogEntry(System.Web.HttpContext.Current, startDtUtc, endDtUtc, "updateMarkers(...)", "calls dataWorker.getSeriesData(...)", Level.Info);

					var list = new List<TimeSeriesViewModel>();

					if (!EnvironmentContext.LocalEnvironment())
					{
						//Non-local environment - do not attempt to create debug files...
						//Watch out for similar loop code in 'else' block...
						if (series.Count > 0)
						{
							for (int i = 0; i < series.Count; i++)
							{
								var tvm = new TimeSeriesViewModel();
								tvm = mapDataCartToTimeseries(series[i], i);
								list.Add(tvm);
							}
						}
					}
					else
					{
						//Local environment - create debug files, if indicated...
#if (DEBUG)
						//BCC - Test - write data cart and time series objects to files...
						using (System.IO.StreamWriter swSdc = System.IO.File.CreateText(@"C:\CUAHSI\SeriesDataCart.json"))
						{
							using (System.IO.StreamWriter swTsvm = System.IO.File.CreateText(@"C:\CUAHSI\TimeSeriesViewModel.json"))
							{
								JsonSerializer jsonser = new JsonSerializer();

								swSdc.Write('[');	//Write start of array...
								swTsvm.Write('[');
#endif
								//Watch out for similar loop code in 'if' block...
								if (series.Count > 0)
								{
									for (int i = 0; i < series.Count; i++)
									{
										var tvm = new TimeSeriesViewModel();
										tvm = mapDataCartToTimeseries(series[i], i);
										list.Add(tvm);
#if (DEBUG)
										jsonser.Serialize(swSdc, series[i]);
										jsonser.Serialize(swTsvm, tvm);

										if ((i + 1) < series.Count)
										{
											swSdc.Write(',');	//Separate array element...
											swTsvm.Write(',');
										}
#endif
									}
								}
#if (DEBUG)
								swSdc.Write(']');	//Write end of array...
								swTsvm.Write(']');
							}
						}
#endif
					}

					var markerClustererHelper = new MarkerClustererHelper();

					//save list for later
					Session["Series"] = list;

					//transform list int clusteredpins
					var pins = transformSeriesDataCartIntoClusteredPin(list, filterAndSearchCriteria);

					var clusteredPins = markerClustererHelper.clusterPins(pins, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
					Session["ClusteredPins"] = clusteredPins;

					var centerPoint = new LatLong(0, 0);
					markerjSON = markerClustererHelper.createMarkersGEOJSON(clusteredPins, zoomLevel, centerPoint, "");
				}
			   
				catch (Exception ex)
				{
					//NOTE: Override 'standard' IIS error handling since we are using 'standard' HTTP error codes - RequestEntityTooLarge and RequestTimeout...
					//Sources:	http://stackoverflow.com/questions/22071211/when-performing-post-via-ajax-bad-request-is-returned-instead-of-the-json-resul
					//			http://stackoverflow.com/questions/3993941/in-iis7-5-what-module-removes-the-body-of-a-400-bad-request/4029197#4029197
					//			http://weblog.west-wind.com/posts/2009/Apr/29/IIS-7-Error-Pages-taking-over-500-Errors

					//Find the 'inner-most' exception...
					while (null != ex.InnerException)
					{
						ex = ex.InnerException;
					}

					if ( typeof (WebException) == ex.GetType())
					{
						//Web exception - return the error message...
						WebException wex = (WebException) ex;

						Response.StatusCode = (int) wex.Status;
						Response.StatusDescription = wex.Message;
						Response.TrySkipIisCustomErrors = true;	//Tell IIS to use your error text not the 'standard' error text!!
						//ALSO clues jQuery to add the parsed responseJSON object to the jqXHR object!!
						dberrorcontext.createLogEntry(System.Web.HttpContext.Current, DateTime.UtcNow, "updateMarkers(...)", wex, "Web Exception: " + wex.Message);

						return Json(new { Message = wex.Message }, "application/json");

					}
					else if ( typeof (System.InvalidOperationException) == ex.GetType() )
					{
						//Recover the returned error message...
						Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
						Response.StatusDescription = ex.Message;
						Response.TrySkipIisCustomErrors = true;	//Tell IIS to use your error text not the 'standard' error text!!
																//ALSO clues jQuery to add the parsed responseJSON object to the jqXHR object!!
						dberrorcontext.createLogEntry(System.Web.HttpContext.Current, DateTime.UtcNow, "updateMarkers(...)", ex, "NON-Web Exception" + ex.Message);

						return Json(new { Message = ex.Message }, "application/json");
					}
					else
					{
						//Assume a timeout has occurred...
						string message = "The execution of the search took too long. Please limit search area and/or Keywords.";
						Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
						Response.StatusDescription = message;
						Response.TrySkipIisCustomErrors = true;	//Tell IIS to use your error text not the 'standard' error text!!
																//ALSO clues jQuery to add the parsed responseJSON object to the jqXHR object!!
						dberrorcontext.createLogEntry(System.Web.HttpContext.Current, DateTime.UtcNow, "updateMarkers(...)", ex, "Defaults to timeout exception: " + message);

						return Json(new { Message = message }, "application/json");
					}
				}

				//var session2 =(List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>) Session["Series"];
			}
			else
			{
             	//BCC - 19-Nov-2015 - GitHub Issues #67 - Application unresponsive after session timeout and zoom out...
				var retrievedSeries = (List<TimeSeriesViewModel>)Session["Series"];

				if (null != retrievedSeries)	//If a session timeout has occurred, the new session object will not contain the 'Series' element!!
				{
					var markerClustererHelper = new MarkerClustererHelper();
					//transform list int clusteredpins
					var pins = transformSeriesDataCartIntoClusteredPin(retrievedSeries, filterAndSearchCriteria);

					var clusteredPins = markerClustererHelper.clusterPins(pins, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
					Session["ClusteredPins"] = clusteredPins;

					var centerPoint = new LatLong(0, 0);
					markerjSON = markerClustererHelper.createMarkersGEOJSON(clusteredPins, zoomLevel, centerPoint, "");
				}
				else
				{
					//Likely session timeout - return a Request Timeout error (408)...
					string message = "User session has expired!!";

					Response.StatusCode = (int) HttpStatusCode.RequestTimeout;
					Response.StatusDescription = message;
					Response.TrySkipIisCustomErrors = true;	//Tell IIS to use your error text not the 'standard' error text!!

					return Json(new { Message = message }, "application/json");
				}

			}
			return Json(markerjSON);

		}

		//GET home/{service code}
		//NOTE: The hiscentral.cuahsi.org/getIcon service returns either a unique icon defined for the input service code --OR--
		//		 the default icon (if the service has not defined a custom icon).  In providing the default icon, the  
		//		 service returns the following:
		//		
		//		 - HTTP Status Code 302 - the requested resource resides temporarily under a different URI
		//		 - The temporary URI in the response's Location field
		//
		//		Upon receipt of this response the browser (or HttpClient instance) performs a re-direct to 
		//		the temporary URI.  Thus comparing the final URI contained in the response to the default icon's
		//		URI provides a convenient means to know whether or not a re-direct has occurred.  
		//
		//		Can't really do anything like this in the browser since: 
		//
		//		 - The <img> tag's 'load' event does not provide an HTTP status code or final URI value
		//		 - JavaScript's Image object does not support any events
		//		 - Ajax calls to the getIcon service encounter 'cross-domain' security issues...
		[HttpGet]
		public async Task<ActionResult> getIcon(string id )		//MUST use 'id' here to match the identifier used in the MapRoute call!!
		{
			//Create Uris for default icon and CUAHSI logo...
			Uri uriDefaultIcon = new Uri(ConfigurationManager.AppSettings["uriDefaultIcon"], UriKind.Absolute);
			Uri uriCuahsiLogo = new Uri(ConfigurationManager.AppSettings["uriCuahsiLogo"], UriKind.Relative);

			string uriIcon = String.Format("{0}{1}", ConfigurationManager.AppSettings["getIconUrl"] + "/getIcon.aspx?name=", id);

			//Download the icon to determine the final URI...
			HttpResponseMessage response = await DownloadIcon(uriIcon);
			Uri uriRequest = GetRequestUri(response);

			//Compare default icon URI to final URI
			if (CompareUris(uriDefaultIcon, uriRequest))
			{
				//Match - no custom icon defined for service - set URI to CUAHSI logo
				uriRequest = uriCuahsiLogo;
			}

			//Re-direct to the final URI...
			return Redirect( uriRequest.IsAbsoluteUri ? uriRequest.AbsoluteUri : uriRequest.OriginalString);
		}

		//NOTE: The use of '+'s in URLs - like /Content/Google+/... results in IIS error message: 
		//			'The request filtering module is configured to deny a request that contains a double escape sequence.'
		//		While this error checking can be disabled via configuration parameters, doing so can decrease site security.
		//		The current controller method is provided to allow retention of file/directory name(s) as received from Google...
		//			
		[HttpGet]
		public ActionResult getGoogleIcon(string id)			//MUST use 'id' here to match the identifier used in the MapRoute call!!
		{
			//Validate/initialize input parameters
			string fileName = id;
			if (String.IsNullOrWhiteSpace(fileName))
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Establish file path...
			string filePathAndName = Server.MapPath("~/Content/Images/Google+/" + fileName.ToString());

			//Attempt to read file contents... 
			try
			{
				FileStream fs = new FileStream( filePathAndName, FileMode.Open);
				fs.Seek(0, SeekOrigin.Begin);

				//Success - return a file stream result...
				FileStreamResult fsr = new FileStreamResult(fs, "image/png");

				return fsr;

			}
			catch( Exception ex)
			{
				//Error - return a 'Not Found'...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, String.Format("Requested Google icon: {0} not found.", fileName).ToString());
			}
			
		}

		//For the input Icon URI, perform a response-header GET.  Return the response...
		async static Task<HttpResponseMessage> DownloadIcon(string uri)
		{
			HttpResponseMessage response = null;

			try
			{
			using (HttpClient client = new HttpClient())
			{
				response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

					return response;
			}
			}
			catch (Exception ex)
			{
				//Take no action...
				return response;
			}
		}

		//Return the response's final RequestUri
		//NOTE: As explained here: http://stackoverflow.com/questions/17758667/c-sharp-how-to-get-last-url-from-httpclient (Answer 11)
		//		 the RequestUri property contains the final URI returned (after all re-directs)
		//		Also, one can configure HttpClient to prevent any re-directs as explained here: 
		//		 http://stackoverflow.com/questions/14731980/using-httpclient-how-would-i-prevent-automatic-redirects-and-get-original-statu
		static Uri GetRequestUri(HttpResponseMessage response)
		{

			if (null != response && null != response.RequestMessage && null != response.RequestMessage.RequestUri)
			{
				return response.RequestMessage.RequestUri;
			}

			return new Uri("unknown.gif", UriKind.Relative);
		}

		//Compare the input Uri, using the overloaded equality operator...
		static bool CompareUris(Uri uriLHS, Uri uriRHS)
		{
			return (null == uriLHS || null == uriRHS) ? false : (uriLHS == uriRHS);
		}

		public string getSeriesDataForViz()
		{
			StringBuilder sb  = new StringBuilder();
			sb.Append("[");
			sb.Append("[1,10,100],");
			sb.Append("[2,20,80],");
			sb.Append("[3,50,60],");
			sb.Append("[4,70,80]");
			sb.Append("]");

			return sb.ToString();
		}
		
		public string getOntologyMainCategories()
		{
			var sb =  new StringBuilder();
			sb.Append("[");
			sb.Append("{\"key\": \"2\", \"title\": \"Physical\",\"folder\": true, \"lazy\":true},");
			sb.Append("{\"key\": \"3\", \"title\": \"Chemical\",\"folder\": true, \"lazy\":true},");
			sb.Append("{\"key\": \"4\", \"title\": \"Biological\",\"folder\": true, \"lazy\":true}");
			sb.Append("]");

			return sb.ToString();
		}

		public string getOntologyByCategory(string id)
		{
			var ontologyHelper = new OntologyHelper();
			var ontologyJson = ontologyHelper.getOntology(id);

			return ontologyJson;
		}

		public string getDetailsForMarker(int id)
		{
			//get all clustered pins form cache
			var clusteredPins = (List<ClusteredPin>)Session["ClusteredPins"];
			if (clusteredPins != null)
			{
				var currentCluster = clusteredPins[id];
				var sb = new StringBuilder();
				var allRetrievedSeries = (List<TimeSeriesViewModel>)Session["Series"];
				
				var seriesInCluster = new List<TimeSeriesViewModel>();

				var allRetrievedSeriesArray = allRetrievedSeries.ToArray();

				foreach(var s in currentCluster.assessmentids)
				{
					int i = Convert.ToInt32(s);
					var obj = (TimeSeriesViewModel)allRetrievedSeriesArray[i];
					
					if (obj != null) seriesInCluster.Add(obj);
				}

				//Order returned series by Organization, Service Title, Keyword
				var json = JsonConvert.SerializeObject(seriesInCluster.OrderBy(tsvm => tsvm.Organization)
																	  .ThenBy(tsvm => tsvm.ServTitle)
																	  .ThenBy(tsvm => tsvm.ConceptKeyword));
				json = "{ \"data\":" + json + "}";
				return json;
			}
			else
			{
				throw new NullReferenceException();
			}


		}

		public string getTimeseries()
		{
			 
			var sb = new StringBuilder();
			var allRetrievedSeries = (List<TimeSeriesViewModel>)Session["Series"];

			if (allRetrievedSeries != null)
			{
				//Order returned series by Organization, Service Title, Keyword
				//var allRetrievedSeriesArray = allRetrievedSeries.ToArray();
				var allRetrievedSeriesArray = allRetrievedSeries.OrderBy(tsvm => tsvm.Organization)
																.ThenBy(tsvm => tsvm.ServTitle)
																.ThenBy(tsvm => tsvm.ConceptKeyword)
																.ToArray();

				var seriesInCluster = new List<TimeSeriesViewModel>();

				for (int i = 0; i< allRetrievedSeriesArray.Length ;i++ )
				{
					
					var obj = (TimeSeriesViewModel)allRetrievedSeriesArray[i];

					if (obj != null) seriesInCluster.Add(obj);
				}
				
				var json = JsonConvert.SerializeObject(seriesInCluster);
				json = "{ \"data\":" + json + "}";
				return json;
			}
			else
			{
				throw new NullReferenceException();
			}

		}

		public List<ClusteredPin> transformSeriesDataCartIntoClusteredPin(List<TimeSeriesViewModel> series, clientFilterAndSearchCriteria filterAndSearchCriteria)
		{
			List<ClusteredPin> clusterPins = new List<ClusteredPin>();
			bool bUseFilter = ! (null == filterAndSearchCriteria || filterAndSearchCriteria.isEmpty());

			if (  bUseFilter) {
				//Retrieve COPIES of all time series which match filter criteria...
				series = filterTimeSeries(series, filterAndSearchCriteria);
			}

			for (int i = 0; i < series.Count; i++)
			{

				var cl = new ClusteredPin();

				cl.Loc = new LatLong(series[i].Latitude, series[i].Longitude);

				cl.assessmentids.Add(series[i].SeriesId);
				cl.PinType = "point";

				//Retain the service code, title and highest value count for later reference...
				string key = series[i].ServCode;
				string title = series[i].ServTitle;

				cl.ServiceCodeToTitle[key] = title;
	
				clusterPins.Add(cl);
			}

			return clusterPins;
		}

		//Determine whether or not to filter the input time series per the input criteria - 
		private List<TimeSeriesViewModel> filterTimeSeries(List<TimeSeriesViewModel> series, clientFilterAndSearchCriteria filterAndSearchCriteria)
		{
			//Validate/initialize input parameters...
			if (null == series || 0 >= series.Count || filterAndSearchCriteria.isEmpty())
			{
				//Null or empty time series list and/or criteria - return early
				return series;
			}

			//Prior to applying the criteria - copy the input time series 
			List<TimeSeriesViewModel> listFiltered = null;

			//Initialize the LINQ predicate...
			System.Linq.Expressions.Expression<Func<TimeSeriesViewModel, bool>> predicate;

			//Apply search string (case-insensitive), if indicated...
			//NOTE: The search string main contain space-delimited sub strings (e.g., 'SNOTEL 784' )
			string search = filterAndSearchCriteria.search;
			if (! String.IsNullOrWhiteSpace(search))
			{
				//Initialize the LINQ predicate...
				//NOTE: Initializing the predicate to always return false and then adding ORs 
				//		 will select only those timeseries with values matching the search criteria
				predicate = LinqPredicateBuilder.Create<TimeSeriesViewModel>(item => false);

				//Scan datasources...
				StringComparer comparer = StringComparer.CurrentCultureIgnoreCase;
				foreach ( var datasource in filterAndSearchCriteria.dataSources)
				{
					if (! datasource.searchable)
					{
						continue;	//NOT searchable - continue
					}

					//Searchable... add value to predicate...
					if (null != datasource.dataSource)
					{
						//Value in TimeSeriesViewModel property...
						var source = datasource.dataSource;

						//Add TimeSeriesViewModel property to LINQ predicate 
						//NOTE: MUST specify a case-insensitive contains...
						//		Use null coalescing operator (??) to avoid null reference errors...
						//		source: http://stackoverflow.com/questions/16490509/how-to-assign-empty-string-if-the-value-is-null-in-linq-query
						predicate = predicate.Or(item => (item.GetType().GetProperty(source).GetValue(item, null) ?? String.Empty).ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase, new SearchStringComparer()));
					}
					else if (null != datasource.title)
					{
						//Lookup value from title...
						//		Use null coalescing operator (??) to avoid null reference errors...
						//		source: http://stackoverflow.com/questions/16490509/how-to-assign-empty-string-if-the-value-is-null-in-linq-query
						predicate = predicate.Or(item => (LookupValueFromTitle(datasource, item) ?? String.Empty).ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase, new SearchStringComparer()));
					}
				}

				//Apply search criteria to current timeseries
				var queryable = series.AsQueryable();

				try
				{
					listFiltered = queryable.Where(predicate).ToList();
				}
				catch (Exception ex)
				{
					string msg = ex.Message;
					
					//Error in filtering - set filtered list to unfiltered list...
					listFiltered = series;
				}
			}

			//Apply filters (case-insensitive), if indicated...
			List<clientFilter> filters = filterAndSearchCriteria.filters;
			if (0 < filters.Count)
			{
				//Initialize the LINQ predicate...
				//NOTE: Initializing the predicate to always return true and then adding ANDs 
				//		 will select only those timeseries with values matching the filter(s)
				predicate = LinqPredicateBuilder.Create<TimeSeriesViewModel>(item => true);

				//Set the queryable collection - listFiltered (if search criteria already applied, otherwise series)
				var queryable = (null != listFiltered) ? listFiltered.AsQueryable() : series.AsQueryable(); 
				foreach (var filter in filters)
				{
					var source = filter.dataSource;
					var value = filter.value;

					//Add TimeSeriesViewModel property to LINQ predicate
					//NOTE: MUST specify a case-insensitive equals...
					predicate = predicate.And(item => item.GetType().GetProperty(source).GetValue(item, null).ToString().Equals(value, StringComparison.CurrentCultureIgnoreCase));
				}

				//Apply search criteria to current timeseries
				listFiltered = queryable.Where(predicate).ToList();
			}

			//Processing complete - return
			return listFiltered;
		}

		private TimeSeriesViewModel mapDataCartToTimeseries(BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart dc, int id)
		{
			var allWebservices = (List < WebServiceNode >) Session["webServiceList"];

			var dict = allWebservices.Distinct().ToDictionary(i => i.ServiceCode, i => i.Organization);

			var dict1 = allWebservices.Distinct().ToDictionary(i => i.ServiceCode, i => i.Title);

			var obj = new TimeSeriesViewModel();           
			
			obj.SeriesId = id;
			obj.ServCode = dc.ServCode;
			obj.ServTitle = dict1.FirstOrDefault(x => x.Key == dc.ServCode).Value;
			obj.ServURL = dc.ServURL;
			obj.SiteCode = dc.SiteCode;
			obj.VariableCode = dc.VariableCode;
			obj.SiteName = dc.SiteName;
			obj.VariableName = dc.VariableName;
			obj.BeginDate = dc.BeginDate;
			obj.EndDate = dc.EndDate;
			obj.ValueCount = dc.ValueCount;
			obj.Latitude = dc.Latitude;
			obj.Longitude = dc.Longitude;
			obj.DataType = dc.DataType;
			obj.ValueType = dc.ValueType;
			obj.SampleMedium = dc.SampleMedium;
			//BCC - 08-Aug-2016 - Assign view model general category...
			obj.GeneralCategory = dc.GeneralCategory;
			obj.TimeUnit = dc.TimeUnit;
			obj.TimeSupport = dc.TimeSupport;
			obj.ConceptKeyword = dc.ConceptKeyword;
			//BCC - 15-Oct-2015 -  Suppress display of IsRegular
			//obj.IsRegular = dc.IsRegular;
			obj.VariableUnits = dc.VariableUnits;
			obj.Organization = dict.FirstOrDefault(x => x.Key == dc.ServCode).Value;

			//BCC - 07-Sep-2016 - Add additional fields for use with GetSeriesCatalogForBox3...
			obj.QCLID = dc.QCLID;
			obj.QCLDesc = dc.QCLDesc;

			obj.SourceId = dc.SourceId;
			obj.SourceOrg = dc.SourceOrg;

			obj.MethodId = dc.MethodId;
			obj.MethodDesc = dc.MethodDesc;

			return obj;
		}

		public string getServiceList()
		{
			var dataWorker = new DataWorker();

			var allWebservices = dataWorker.getWebServiceList();

			WebServiceNode wsn5588 = null;
			int count = 0;
			foreach (WebServiceNode wsn in allWebservices)
			{
				Console.WriteLine(String.Format("{0} - {1}", count++, wsn.ServiceID));
				if (5588 == wsn.ServiceID)
				{
					wsn5588 = wsn;
				}
			}

			if (allWebservices != null)
			{
				var json = JsonConvert.SerializeObject(allWebservices);
				json = "{ \"data\":" + json + "}";
				return json;
			}
			else
			{
				throw new NullReferenceException();
			}

		}

	   

		public PartialViewResult CreatePartialView()
		{
			return PartialView("_DownloadTimeseries");
		}

		[HttpPost]
		public ActionResult Progress()
		{
			string StatusMessage = "Processing started " + DateTime.Now.ToLocalTime().ToString();
			if (Session["Uri"] != null)
			{
				StatusMessage = Session["Uri"].ToString();
			}

			return Json(StatusMessage);
		}

	   
		
		public string downloadFile()
		{
			var dir = "~/Files/";
			var filename = "downloadtest.csv";
			var filePath = Server.MapPath(dir + filename);
			var fileType = "text/csv";
			return "downloadtest.csv";
		}

		public void setdownloadIds(FormCollection collection)
		{

		}

	}
}