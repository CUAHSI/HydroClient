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


namespace HISWebClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly CUAHSI.DataExport.IExportEngine wdcStore;      

        public ActionResult Index()
        {

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
                var tileWidth = 5;
                var tileHeight = 5;
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

                    var series = dataWorker.getSeriesData(box, keywords.ToArray(), tileWidth, tileHeight,
                                                                     searchSettings.DateSettings.StartDate,
                                                                      searchSettings.DateSettings.EndDate,
                                                                      activeWebservices);
                    var list = new List<TimeSeriesViewModel>();

                    if (series.Count> 0)
                    {
                        for (int i=0;i< series.Count; i++)
                        {
                            var tvm = new TimeSeriesViewModel();
                            tvm = mapDataCartToTimeseries(series[i], i);
                            list.Add(tvm);
                        }
                    }
                    var markerClustererHelper = new MarkerClustererHelper();

                    //save list for later
                    Session["Series"] = list;
                    // Session["test"] = "test";// series;

                    //transform list int clusteredpins
                    var pins = transformSeriesDataCartIntoClusteredPin(list);

                    var clusteredPins = markerClustererHelper.clusterPins(pins, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
                    Session["ClusteredPins"] = clusteredPins;

                    var centerPoint = new LatLong(0, 0);
                    markerjSON = markerClustererHelper.createMarkersGEOJSON(clusteredPins, zoomLevel, centerPoint, "");
                }
               
                catch (Exception ex)
                {
                    if (ex.InnerException.ToString().ToLower().Contains("operationcanceledexception"))
                    {
                        Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                        var maxAllowedTimeseriesReturn = Convert.ToInt32(ConfigurationManager.AppSettings["maxAllowedTimeseriesReturn"].ToString()); //maximum ammount of number of timeseries that are returned
 
                        return Json(new {Message="Search returned more than " + maxAllowedTimeseriesReturn + "timeseries and was canceled. Please limit search area and/or Keywords." });
                        //throw new WebException("Timeout. Try to decrease Search Area, or Select another Keywords.", WebExceptionStatus.Timeout);
                    }
                    else{
                         Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                         return Json(new { Message = "The execution of the search took too long. Please limit search area and/or Keywords." });                    
                    }
                }


                //var session2 =(List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>) Session["Series"];
            }
            else
            {
                //var s = (string)Session["test"];             
                var retrievedSeries = (List<TimeSeriesViewModel>)Session["Series"];

                var markerClustererHelper = new MarkerClustererHelper();
                //transform list int clusteredpins
                var pins = transformSeriesDataCartIntoClusteredPin(retrievedSeries);

                var clusteredPins = markerClustererHelper.clusterPins(pins, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
                Session["ClusteredPins"] = clusteredPins;

                var centerPoint = new LatLong(0, 0);
                markerjSON = markerClustererHelper.createMarkersGEOJSON(clusteredPins, zoomLevel, centerPoint, "");

            }
            return Json(markerjSON);

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

        public List<ClusteredPin> transformSeriesDataCartIntoClusteredPin(List<TimeSeriesViewModel> series)
        {
            List<ClusteredPin> clusterPins = new List<ClusteredPin>();

            for (int i = 0; i < series.Count; i++)
            {
                var cl = new ClusteredPin();

                cl.Loc = new LatLong(series[i].Latitude, series[i].Longitude);

                cl.assessmentids.Add(series[i].SeriesId);
                cl.PinType = "point";
                clusterPins.Add(cl);
            }

            return clusterPins;
        }

        private TimeSeriesViewModel mapDataCartToTimeseries(BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart dc, int id)
        {
            var allWebservices = (List < WebServiceNode >) Session["webServiceList"];

            var dict = allWebservices.Distinct().ToDictionary(i => i.ServiceCode, i => i.Organization);

            

            var obj = new TimeSeriesViewModel();           
            
            obj.SeriesId = id;
            obj.ServCode = dc.ServCode;
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
            obj.IsRegular = dc.IsRegular;
            //obj.VariableUnits = dc.VariableUnits;
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
                    Convert.ToInt32(10000));
        }

    }
}