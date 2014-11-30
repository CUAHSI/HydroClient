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
       
    }
}