using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Web.Script.Serialization;

using HISWebClient.Models.DataManager;

namespace HISWebClient.Controllers
{
    public class DataManagerController : Controller
    {

		private UserTimeSeriesDbContext _utsDbContext;

		//Constructors - instantiate the AzureContext singleton for later reference...
		public DataManagerController() : this( new UserTimeSeriesDbContext()) { }

		public DataManagerController(UserTimeSeriesDbContext utsdc)
		{
			_utsDbContext = utsdc;
		}


        // GET: DataManager
		//public ActionResult Index()
		//{
		//	return View();
		//}

		//GET DataManager/{userEmail}
		[HttpGet]
		public ActionResult Get(string id )		//MUST use 'id' here to match the identifier used in the MapRoute call!!
		{

			//HttpStatusCodeResult response = new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);	//Assume Success

			//Validate/initialize input parameters
			string userEmail = id;
			if (String.IsNullOrWhiteSpace(userEmail))
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Lookup user time series, set found indicator...
			UserTimeSeries uts = _utsDbContext.UserTimeSeriesSet.SingleOrDefault(u => u.UserEmail == id);
			if (null == uts)
			{
				//Failure - return 'Not Found'...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, String.Format("Requested record(s) for user: ({0}) not found.", userEmail).ToString());
			}

			//Sucess - convert retrieved data to JSON and return...
			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(uts);

			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json", JsonRequestBehavior.AllowGet );
		}

		//Post DataManager/{dmTimeSeries}
		[HttpPost]
		public ActionResult Post(UserTimeSeries userTimeSeries)
		{

			HttpStatusCodeResult response = new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);	//Assume Success

			//Validate/initialize input parameters
			if (null == userTimeSeries)
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Load input values to database
			UserTimeSeries uts = _utsDbContext.UserTimeSeriesSet.SingleOrDefault(u => u.UserEmail == userTimeSeries.UserEmail);
			if (null == uts)
			{
				//New entry - save
				_utsDbContext.UserTimeSeriesSet.Add(userTimeSeries);
				_utsDbContext.SaveChanges();
			}
			else
			{
				//Existing entry - save time series only...
				_utsDbContext.DM_TimeSeriesSet.AddRange(userTimeSeries.TimeSeries);
				_utsDbContext.SaveChanges();
			}

			return response;
		}


    }
}