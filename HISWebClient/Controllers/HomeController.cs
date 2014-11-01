using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HISWebClient.BusinessObjects;
using HISWebClient.DataLayer;
using TB.ComponentModel;


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

            UniversalTypeConverter.TryConvertTo<double>(collection["xMin"], out xMin);
            UniversalTypeConverter.TryConvertTo<double>(collection["xMax"], out xMax);
            UniversalTypeConverter.TryConvertTo<double>(collection["yMin"], out yMin);
            UniversalTypeConverter.TryConvertTo<double>(collection["yMax"], out yMax);
            Box box = new Box(xMin, xMax, yMin, yMax);

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
            dataWorker.getData(box, keywords.ToArray(), tileWidth, tileHeight,
                                                             searchSettings.DateSettings.StartDate,
                                                              searchSettings.DateSettings.EndDate,
                                                              webServiceIds);
            return View();

        }
    }
}