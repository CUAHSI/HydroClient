﻿using System;
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
			//BCC - Testing server timeout errors
			//Session.Timeout = 1;

			ViewBag.Message = "CUAHSI Hydrologic Data Services";
			//ViewBag.ServiceDomain = RoleEnvironment.GetConfigurationSettingValue("ServiceDomain");           
			
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
			//LogHelper.LogNewAPIUse(sessionguid);
			//var conceptKeyword = "";
			var ontologyHelper = new OntologyHelper();
			//var s = ontologyHelper.getOntology(conceptKeyword);
			return View();
		}

		public ActionResult Index2()
		{
			ViewBag.Message = "Your application description page.";

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

			//if (collection["sessionGuid"] != null)
			//{
			//    var sessionGuid = collection["sessionGuid"].ToString();
			//}
			

			//if it is a new request
			if (collection["isNewRequest"].ToString() == "true")
			{

				bool canConvert = false;

				//var keywords = collection["keywords"].Split('#');
				var keywords = Regex.Split(collection["keywords"], @"##");
				//replace underscore with comma to align spelling 
				//for (var k =0;k<keywords.Length;k++)
				//{
				//    keywords[k] = keywords[k].Replace("_", ",");
				//}
				var tileWidth = 10;
				var tileHeight = 10;
				List<int> webServiceIds = null;
				try
				{
					searchSettings.DateSettings.StartDate = Convert.ToDateTime(collection["startDate"]);
					searchSettings.DateSettings.EndDate = Convert.ToDateTime(collection["endDate"]);
					//Convert to int Array
					if (collection["services"].Length > 0)
					{
						webServiceIds = collection["services"].Split(',').Select(s => Convert.ToInt32(s)).ToList();
					}
					//for test
					//int[] test = new int[] { 162, 81, 171 };
					// webServiceIds.AddRange(test);

					//var progressHandler = new ProgressHandler(this);
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

					if (series.Count> 0)
					{
						for (int i = 0; i < series.Count; i++)
						{
							var tvm = new TimeSeriesViewModel();
							tvm = mapDataCartToTimeseries(series[i], i);
							list.Add(tvm);
						}

						//TO DO - Transfer logic to tblDataManager loading and copying functions 
						//			The retrieval of the Variable Units is too time consuming to do for every time series...
						//var count = list.Count;
						//var list1 = new List<TimeSeriesViewModel>();
						//for (int i = 0; i < count; ++i)
						//{
						//	int seriesId = list[i].SeriesId;
						//	list1.Add(list[i]);
						//	SeriesData sd = null;

						//	//Task<SeriesData> sd = Task.Run(async () =>
						//	Task.Run(async () =>
						//	{
						//		//SeriesData sd1 = await exportController.GetSeriesDataFromSeriesID(seriesId, list1);
						//		try
						//		{
						//			sd = await exportController.GetSeriesDataFromSeriesID(seriesId, list1);
						//		}
						//		catch (Exception ex)
						//		{
						//			//For now take no action...
						//			sd = null;
						//		}
						//	}).Wait();

						//	if (null != sd)
						//	{
						//		list[i].VariableUnits = sd.myVariable.VariableUnit.Name;
						//	}
						//}
					}

					var markerClustererHelper = new MarkerClustererHelper();

					//save list for later
					Session["Series"] = list;
					// Session["test"] = "test";// series;

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
					if (ex.InnerException.ToString().ToLower().Contains("operationcanceledexception"))
					{
						string maxAllowedTimeseries = ConfigurationManager.AppSettings["maxAllowedTimeseriesReturn"].ToString();
						string message = "Search returned more than " + maxAllowedTimeseries + " timeseries and was canceled. Please limit search area and/or Keywords.";
						Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
						Response.StatusDescription = message;
						Response.TrySkipIisCustomErrors = true;	//Tell IIS to use your error text not the 'standard' error text!!
																//ALSO clues jQuery to add the parsed responseJSON object to the jqXHR object!!

						dberrorcontext.createLogEntry(System.Web.HttpContext.Current, DateTime.UtcNow, "updateMarkers(...)", ex, message);

						return Json(new { Message = message }, "application/json");
					}
					else{
						 string message = "The execution of the search took too long. Please limit search area and/or Keywords.";
						 Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
						 Response.StatusDescription = message;
						 Response.TrySkipIisCustomErrors = true;	//Tell IIS to use your error text not the 'standard' error text!!

						 dberrorcontext.createLogEntry(System.Web.HttpContext.Current, DateTime.UtcNow, "updateMarkers(...)", ex, message);

						 return Json(new { Message = message }, "application/json");
					}
				}


				//var session2 =(List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>) Session["Series"];
			}
			else
			{
				//var s = (string)Session["test"];
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

			string uriIcon = String.Format("{0}{1}", "http://hiscentral.cuahsi.org/getIcon.aspx?name=", id);

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
//		public async Task<ActionResult> getGoogleIcon(string id)			//MUST use 'id' here to match the identifier used in the MapRoute call!!
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
			HttpResponseMessage response;
			using (HttpClient client = new HttpClient())
			{
				response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
			}

			return response;
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
			//sb.Append(",");
			//sb.Append("{");
			//sb.Append(" labels: [ \"x\", \"A\", \"B\" ]");
			//sb.Append("}");

			//return new ContentResult { Content = sb.ToString(), ContentType = "application/json" };
			return sb.ToString();
		}
		
		public string getOntologyMainCategories()
		{
			var sb =  new StringBuilder();
			sb.Append("[");
			//sb.Append("{\"key\": \"1\", \"title\": \"Hydrosphere\", \"folder\": \"true\", \"children\": [");
			sb.Append("{\"key\": \"2\", \"title\": \"Physical\",\"folder\": true, \"lazy\":true},");
			sb.Append("{\"key\": \"3\", \"title\": \"Chemical\",\"folder\": true, \"lazy\":true},");
			sb.Append("{\"key\": \"4\", \"title\": \"Biological\",\"folder\": true, \"lazy\":true}");
			//.Append("]}");	
			sb.Append("]");
			//var json = new JsonResult(s);
			return sb.ToString();
		}

		public string getOntologyByCategory(string id)
		{
			//var json = "[ {\"title\": \"Sub item\", \"lazy\": true }, {\"title\": \"Sub folder\", \"folder\": true, \"lazy\": true } ]";
			//var json = new JsonResult(s);

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
				//var seriesInCluster = retrievedSeries.
				//var w = (List<TimeSeriesViewModel>)retrievedSeries.Select((value, index) => new { value, index }).Where(x => x.index > 50).Select(x => x);
				
				var seriesInCluster = new List<TimeSeriesViewModel>();
				//seriesInCluster = allRetrievedSeries.Where((o=> allRetrievedSeries))

				var allRetrievedSeriesArray = allRetrievedSeries.ToArray();

				foreach(var s in currentCluster.assessmentids)
				{
					int i = Convert.ToInt32(s);
					var obj = (TimeSeriesViewModel)allRetrievedSeriesArray[i];
					
					if (obj != null) seriesInCluster.Add(obj);
				}
				//var json = new JavaScriptSerializer().Serialize(seriesInCluster);
				var json = JsonConvert.SerializeObject(seriesInCluster);
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
				//var seriesInCluster = retrievedSeries.
				//var w = (List<TimeSeriesViewModel>)retrievedSeries.Select((value, index) => new { value, index }).Where(x => x.index > 50).Select(x => x);
			if (allRetrievedSeries != null)
			{
				var allRetrievedSeriesArray = allRetrievedSeries.ToArray();

				var seriesInCluster = new List<TimeSeriesViewModel>();

				for (int i = 0; i< allRetrievedSeriesArray.Length ;i++ )
				{
					
					var obj = (TimeSeriesViewModel)allRetrievedSeriesArray[i];

					if (obj != null) seriesInCluster.Add(obj);
				}
				
				//var json = new JavaScriptSerializer().Serialize(seriesInCluster);
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
				int count = series[i].ValueCount;
				string title = series[i].ServTitle;

				cl.ServiceCodeToTitle[key] = title + " (" + count.ToString() + ")";
	
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
						predicate = predicate.Or(item => item.GetType().GetProperty(source).GetValue(item, null).ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase));
					}
					else if (null != datasource.title)
					{
						//Lookup value from title...
						predicate = predicate.Or(item => (LookupValueFromTitle(datasource, item)).ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase));

					}
				}

				//Apply search criteria to current timeseries
				var queryable = series.AsQueryable();
				listFiltered = queryable.Where(predicate).ToList();
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
			obj.TimeUnit = dc.TimeUnit;
			//obj.GeneralCategory = dc.GeneralCategory;
			obj.TimeSupport = dc.TimeSupport;
			obj.ConceptKeyword = dc.ConceptKeyword;
			//BCC - 15-Oct-29015 -  Suppress display of IsRegular
			//obj.IsRegular = dc.IsRegular;
			obj.VariableUnits = dc.VariableUnits;
			//obj.Citation = dc.Citation;
			obj.Organization = dict.FirstOrDefault(x => x.Key == dc.ServCode).Value;

			return obj;
		}

		public string getServiceList()
		{
			var dataWorker = new DataWorker();

			var allWebservices = dataWorker.getWebServiceList();
			if (allWebservices != null)
			{
				//var s = from a in allWebservices select new [a.ServiceID, a.ServiceCode, a.Organization, a.Sites,a.Variables]
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
			//return base.File(filePath, "text/csv", filename);
			//if (System.IO.File.Exists(filePath))

			//else
			//    return Content("Couldn't find file");        

		}

		//[HttpPost]
		//public string getSeriesValuesAsCSV(FormCollection collection)
		//{
		//    var series = new ServerSideHydroDesktop.ObjectModel.SeriesDataCart();


		//    CUAHSI.Models.SeriesMetadata seriesMetaData = null;

		//    for (int i = 0; i < collection.Count; i++)
		//    {
		//        var split = collection[i].Split(',');
		//        list[i] = new Array(rows[i].ServCode, rows[i].ServURL, rows[i].SiteCode, rows[i].VariableCode, rows[i].BeginDate, rows[i].EndDate);
		//        object[] metadata = new object[13];
		//        metadata[0] = split[0];
		//        metadata[1] = split[1];
		//        metadata[2] = split[2];
		//        metadata[3] = split[3];
		//        metadata[4] = split[4];
		//        metadata[5] = split[5];
		//        metadata[6] = split[6];
		//        metadata[7] = split[7];
		//        metadata[8] = split[8];
		//        metadata[9] = split[9];
		//        metadata[10] = split[10];
		//        metadata[11] = 0;
		//        metadata[12] = 0;
		//        metadata[13] = split[13];
		//        seriesMetaData = new SeriesMetadata(metadata);
		//    }

		//    var url = DoSeriesDownload(seriesMetaData);



		//    return "url";
		//}

		public void setdownloadIds(FormCollection collection)
		{

		}

		//public async Task<string> DoSeriesDownload(SeriesMetadata seriesMetaData)
		//{
		//    Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data = await SeriesAndStreamOfSeriesID(seriesMetaData);
		//    Tuple<Stream, SeriesData> series = null;
		//    DateTimeOffset requestTime = DateTimeOffset.UtcNow;



		//    if (data == null || data.Item2.FirstOrDefault() == null)
		//    {
		//        throw new KeyNotFoundException();
		//    }
		//    else
		//    {
		//        var dataResult = data.Item2.FirstOrDefault();
		//        IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();
		//        series = new Tuple<Stream, SeriesData>(data.Item1, new SeriesData(seriesMetaData.SeriesID, seriesMetaData, dataResult.Method, dataResult.QualityControlLevel.Code, dataValues,
		//            dataResult.Variable.VariableUnit.Name, dataResult.Variable.VariableUnit.Abbreviation, dataResult.Site.VerticalDatum, dataResult.Site.Elevation_m));
		//    }

		//    string nameGuid = Guid.NewGuid().ToString();

		//    var dl = wdcStore.PersistSeriesData(series.Item2, nameGuid, requestTime);

		//    //fire and forget => no reason to require consistency with user download.
		//    var persist = wdcStore.PersistSeriesDocumentStream(data.Item1, 0, nameGuid, DateTime.UtcNow);

		//    await Task.WhenAll(new List<Task>() { dl, persist });

		//    series.Item2.wdcCache = dl.Result.Uri;
		//    //return series.Item2;
		//    Session["StatusMessage"] = dl.Result.Uri;
		//    return dl.Result.Uri;
		//}

		public async Task<Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>>> SeriesAndStreamOfSeriesID(SeriesMetadata meta)
		{
			WaterOneFlowClient client = new WaterOneFlowClient(meta.ServURL);
			return await client.GetValuesAndRawStreamAsync(
					meta.SiteCode,
					meta.VarCode,
					meta.StartDate,
					DateTime.UtcNow,
					Convert.ToInt32(30000));
		}

	}
}