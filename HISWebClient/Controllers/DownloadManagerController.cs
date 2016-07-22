using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using HISWebClient.Models;
using HISWebClient.Models.DownloadManager;

using Newtonsoft.Json;

namespace HISWebClient.Controllers
{
    public class DownloadManagerController : Controller
    {

		private HydroClientDbContext _hcDbContext;

		//Constructors - instantiate the db context singleton for later reference...
		public DownloadManagerController() : this(new HydroClientDbContext()) { }

		public DownloadManagerController(HydroClientDbContext hcdc)
		{
			_hcDbContext = hcdc;
		}

		//GET DownloadManager/{userEmail}
		[HttpGet]
		public ActionResult Get(String id) 		//MUST use 'id' here to match the identifier used in the MapRoute call!!
		{
			//Validate/initialize input parameters
			string userEmail = id;
			if (String.IsNullOrWhiteSpace(userEmail))
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Retrieve the exports tasks, if any
			var etd = _hcDbContext.ExportTaskDataSet.Where(e => (e.UserEmail.Equals(userEmail))).ToList();

			if (null == etd)
			{
				//Failure - return 'Not Found'...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, String.Format("Requested record(s) for user: ({0}) not found.", userEmail).ToString());
			}

			//Check time stamp values - if SQL Server minimum value change to DateTime.MinValue...
			DateTime sqlMin = new DateTime(1753, 1, 1);
			foreach (var exportTask in etd)
			{
				if (sqlMin == exportTask.BlobTimeStamp)
				{
					exportTask.BlobTimeStamp = DateTime.MinValue;
				}
			}

			//Success - convert retrieved data to JSON and return...
			var json = JsonConvert.SerializeObject(etd);

			//Processing complete - return 
			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json", JsonRequestBehavior.AllowGet);
		}
	
		//Delete the database row per the input RequestId...
		[HttpDelete]
		public ActionResult Delete( string id)
		{
			//Validate/initialize input parameters
			string requestId = id;
			if (String.IsNullOrWhiteSpace(requestId))
			{
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid parameter(s)");
			}

			//Delete input values from database, if indicated...
			ExportTaskData etd = _hcDbContext.ExportTaskDataSet.SingleOrDefault(e => e.RequestId.Equals(id));
			if (null == etd)
			{
				//Not found - return error...
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound, "Requested task not found");
			}
			else
			{
				//Found - delete the row...
				_hcDbContext.ExportTaskDataSet.Remove(etd);
				_hcDbContext.SaveChanges();
			}

			//Success - convert input data to JSON and return (so to avoid a parsing error on the client...)
			// Source: http://stackoverflow.com/questions/25173727/syntax-error-unexpected-end-of-input-parsejson
			var responseData = new { requestId = id };
			var json = JsonConvert.SerializeObject(responseData);

			Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
			return Json(json, "application/json");
		}
	}
}