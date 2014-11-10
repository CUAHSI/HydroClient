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
        public ActionResult SearchSubmit(FormCollection collection)
        {
            var searchSettings = new SearchSettings();

           
            bool canConvert = false;
            double xMin, xMax, yMin, yMax;
            int zoomLevel;

            UniversalTypeConverter.TryConvertTo<double>(collection["xMin"], out xMin);
            UniversalTypeConverter.TryConvertTo<double>(collection["xMax"], out xMax);
            UniversalTypeConverter.TryConvertTo<double>(collection["yMin"], out yMin);
            UniversalTypeConverter.TryConvertTo<double>(collection["yMax"], out yMax);
            Box box = new Box(xMin, xMax, yMin, yMax);
            UniversalTypeConverter.TryConvertTo<int>(collection["zoomLevel"], out zoomLevel);

            var keywords = collection["keywords"].Split(','); 
            var tileWidth = 1; 
            var tileHeight =  1;
            List<int> webServiceIds = null;
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
            var series = dataWorker.getSeriesData(box, keywords.ToArray(), tileWidth, tileHeight,
                                                             searchSettings.DateSettings.StartDate,
                                                              searchSettings.DateSettings.EndDate,
                                                             webServiceIds);
            var markerClustererHelper = new MarkerClustererHelper();

            int CLUSTERWIDTH = 5; //Cluster region width, all pin within this area are clustered
            int CLUSTERHEIGHT = 5; //Cluster region height, all pin within this area are clustered
            int CLUSTERINCREMENT = 5; //increment for clusterwidth 
            int MINCLUSTERDISTANCE = 25;
            int MAXCLUSTERCOUNT = Convert.ToInt32(ConfigurationSettings.AppSettings["MaxClustercount"].ToString()); //maximum ammount of clustered markers

            clusteredpins = MarkerClustererHelper.clusterPins(series, CLUSTERWIDTH, CLUSTERHEIGHT, CLUSTERINCREMENT, zoomLevel, MAXCLUSTERCOUNT, MINCLUSTERDISTANCE);
            HISWebClient.
            Session["Series"] = series;

            var session2 =(List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>) Session["Series"];

            return View();

        }
    }
}