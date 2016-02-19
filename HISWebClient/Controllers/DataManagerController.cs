﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Web.Script.Serialization;

using HISWebClient.Models.DataManager;

using Newtonsoft.Json;

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
             var uts = _utsDbContext.DM_TimeSeriesSet.Where(u => (u.UserEmail.Equals(id))).ToList();
          
			if (null == uts)
			{
				//Failure - return 'Not Found'...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, String.Format("Requested record(s) for user: ({0}) not found.", userEmail).ToString());
			}

			//Check time stamp values - if SQL Server minimum value change to DateTime.MinValue...
			DateTime sqlMin = new DateTime(1753, 1, 1);
			foreach (var timeSeries in uts)
			{
				if (sqlMin == timeSeries.WaterOneFlowTimeStamp)
				{
					timeSeries.WaterOneFlowTimeStamp = DateTime.MinValue;
				}
			}


			//Success - convert retrieved data to JSON and return...
			//NOTE: Use of NewtonSoft JsonCovert here results in a 'circular dependency' error given the definition of class: DM_TimeSeries
			//		Happily, use of JavaScriptSerializer avoids this problem but does introduce a difference in date handling.  Specifically,
			//		dates are rendered in 'JSON' format: '/Date(nnnnnnnnnnnnn)/'
			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(uts);

			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json", JsonRequestBehavior.AllowGet );
		}

		//Post DataManager/{dmTimeSeries}
		[HttpPost]
		public ActionResult Post(UserTimeSeries userTimeSeries)
		{
			//Validate/initialize input parameters
			if (null == userTimeSeries)
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Check time stamp values - if DateTime.MinValue change to SQL Server minimum value to avoid a 'value out of range' error...
			foreach ( var timeSeries in userTimeSeries.TimeSeries)
			{
				if (DateTime.MinValue == timeSeries.WaterOneFlowTimeStamp)
				{
					timeSeries.WaterOneFlowTimeStamp = new DateTime(1753, 1, 1);
				}
			}

			//Load input values to database
			UserTimeSeries uts = _utsDbContext.UserTimeSeriesSet.SingleOrDefault(u => u.UserEmail.Equals(userTimeSeries.UserEmail));
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

			//Success - convert input data to JSON and return (so to avoid a parsing error on the client...)
			// Source: http://stackoverflow.com/questions/25173727/syntax-error-unexpected-end-of-input-parsejson
			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(userTimeSeries);

			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json");
		}

		//Delete DataManager/{dmTimeSeries}
		[HttpDelete]
		public ActionResult Delete(UserTimeSeries userTimeSeries)
		{
			//Validate/initialize input parameters
			if (null == userTimeSeries)
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Delete input values from database, if indicated...
			UserTimeSeries uts = _utsDbContext.UserTimeSeriesSet.SingleOrDefault(u => u.UserEmail.Equals(userTimeSeries.UserEmail));
			if (null == uts)
			{
				//Not found - return error...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, "Requested timeseries not found");
			}
			else
			{
				//Found - delete time series only...
				foreach (var timeseries in userTimeSeries.TimeSeries)
				{
					DM_TimeSeries timeseriesFound = _utsDbContext.DM_TimeSeriesSet.SingleOrDefault(d => d.TimeSeriesRequestId.Equals(timeseries.TimeSeriesRequestId));
					if (null != timeseriesFound)
					{
						_utsDbContext.DM_TimeSeriesSet.Remove(timeseriesFound);
					}
				}
				_utsDbContext.SaveChanges();
		}

			//Success - convert input data to JSON and return (so to avoid a parsing error on the client...)
			// Source: http://stackoverflow.com/questions/25173727/syntax-error-unexpected-end-of-input-parsejson
			var javaScriptSerializer = new JavaScriptSerializer();
			var json = javaScriptSerializer.Serialize(userTimeSeries);

			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json");
		}
    }
}