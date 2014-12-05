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




namespace HISWebClient.Controllers
{
    public class HomeController : Controller
    {
      
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

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
        public  ActionResult updateMarkers(FormCollection collection)
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

           

            //if it is a new request
            if (collection["isNewRequest"] != null)
            {

                bool canConvert = false;
       
                var keywords = collection["keywords"].Split(',');
                var tileWidth = 1;
                var tileHeight = 1;
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
                    var markerClustererHelper = new MarkerClustererHelper();


                    //save list for later
                    Session["Series"] = series;
                    // Session["test"] = "test";// series;

                    //transform list int clusteredpins
                    var pins = markerClustererHelper.transformSeriesDataCartIntoClusteredPin(series);

                    var clusteredPins = markerClustererHelper.clusterPins(pins, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
                    Session["ClusteredPins"] = clusteredPins;

                    var centerPoint = new LatLong(0, 0);
                    markerjSON = markerClustererHelper.createMarkersGEOJSON(clusteredPins, zoomLevel, centerPoint, "");
                }
                catch (Exception ex)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { success = false });
                }

                //var session2 =(List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>) Session["Series"];
            }
            else
            {
                //var s = (string)Session["test"];             
                var retrievedSeries =(List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>) Session["Series"];

                var markerClustererHelper = new MarkerClustererHelper();
                //transform list int clusteredpins
                var pins = markerClustererHelper.transformSeriesDataCartIntoClusteredPin(retrievedSeries);

                var clusteredPins = markerClustererHelper.clusterPins(pins, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
                Session["ClusteredPins"] = clusteredPins;

                var centerPoint = new LatLong(0, 0);
                markerjSON = markerClustererHelper.createMarkersGEOJSON(clusteredPins, zoomLevel, centerPoint, "");

            }
            return Json(markerjSON);

        }

        public string getDetailsForMarker(int id)
        {
           //get all clustered pins form cache
           var clusteredPins = (List<ClusteredPin>)Session["ClusteredPins"];
           if (clusteredPins != null)
           { 
               var currentCluster = clusteredPins[id];
               var sb = new StringBuilder();
               var retrievedSeries = (List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>)Session["Series"];
               //var seriesInCluster = retrievedSeries.
               //var w = (List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>)retrievedSeries.Select((value, index) => new { value, index }).Where(x => x.index > 50).Select(x=>x);
               var seriesInCluster = new List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>();
               foreach (int i in currentCluster.assessmentids)         
               {             
               
                   seriesInCluster.Add(retrievedSeries.ElementAt(i));
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
            string StatusMessage = "Processing started "+ DateTime.Now.ToLocalTime().ToString();
            if (Session["StatusMessage"] != null)
            {
                StatusMessage = Session["StatusMessage"].ToString(); 
            }           
           
            return Json(StatusMessage);
        }

        [HttpPost]
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

        [HttpPost]
        public string getSeriesValuesAsCSV(FormCollection collection)
        {
            //var series = new ServerSideHydroDesktop.ObjectModel.SeriesDataCart();
            
            
            CUAHSI.Models.SeriesMetadata seriesMetaData = null;

            for (int i = 0; i < collection.Count; i++)
            {
                var split = collection[i].Split(',');
                //   list[i] = new Array(rows[i].ServCode, rows[i].ServURL, rows[i].SiteCode, rows[i].VariableCode, rows[i].BeginDate, rows[i].EndDate);
                object[] metadata = new object[13];
                metadata[0] = split[0];
                metadata[1] = split[1];
                metadata[2] = split[2];
                metadata[3] = split[3];
                metadata[4] = split[4];
                metadata[5] = split[5];
                metadata[6] = split[6];
                metadata[7] = split[7];
                metadata[8] = split[8];
                metadata[9] = split[9];
                metadata[10] = split[10];
                metadata[11] = 0;
                metadata[12] = 0;
                //metadata[13] = split[13];
                seriesMetaData = new SeriesMetadata(metadata);
            }
             
           var stream = DoSeriesDownload(seriesMetaData);
        
                          
               
                return "";
        }
        public async Task<string>  DoSeriesDownload(SeriesMetadata seriesMetaData)
        {
            Tuple<Stream, IList<ServerSideHydroDesktop.ObjectModel.Series>> data =  await SeriesAndStreamOfSeriesID(seriesMetaData);
            Tuple<Stream, SeriesData> ser = null;

            if (data == null || data.Item2.FirstOrDefault() == null)
            {
                throw new KeyNotFoundException();
            }
            else
            {
                var dataResult = data.Item2.FirstOrDefault();
                IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();
                ser =  new Tuple<Stream, SeriesData>(data.Item1, new SeriesData(seriesMetaData.SeriesID, seriesMetaData, dataResult.QualityControlLevel.IsValid, dataValues,
                    dataResult.Variable.VariableUnit.Name, dataResult.Variable.VariableUnit.Abbreviation, dataResult.Site.VerticalDatum, dataResult.Site.Elevation_m));
            }

            string nameGuid = Guid.NewGuid().ToString();
            var dl = wdcStore.PersistSeriesData(data.Item2, nameGuid, requestTime);

            //fire and forget => no reason to require consistency with user download.
            var persist = wdcStore.PersistSeriesDocumentStream(data.Item1, SeriesID, nameGuid, DateTime.UtcNow);

            await Task.WhenAll(new List<Task>() { dl, persist });

            data.Item2.wdcCache = dl.Result.Uri;
            return data.Item2;

            return "";
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

    }
}