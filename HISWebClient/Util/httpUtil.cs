using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Net;
using System.Web.Mvc;

namespace HISWebClient.Util
{
	public static class httpUtil
	{

		//Set a response status code 
		public static void setHttpResponseStatusCode( ref HttpResponseBase httpResponseBase, HttpStatusCode httpStatusCode, bool bTrySkipIisCustomErrors = true)
		{
			if (null != httpStatusCode)
			{
				HttpStatusCodeResult httpResult = new HttpStatusCodeResult(httpStatusCode);

				httpResponseBase.StatusCode = httpResult.StatusCode;
				httpResponseBase.StatusDescription = httpResult.StatusDescription;
				httpResponseBase.TrySkipIisCustomErrors = bTrySkipIisCustomErrors;	//If true, tell IIS to use your error text not the 'standard' error text!!
			}
		}

		//This is another test - do my changes go to the correct branch in GitHub - HydrodataTools???


	}
}