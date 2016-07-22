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

		//Format a message from a System.Net.WebException instance
		public static string getWebExceptionMessage(Exception ex)
		{
			string message = String.Empty;

			//Attempt to cast the input Exception instance...
			System.Net.WebException webex = ex as System.Net.WebException;
			if (null != webex)
			{
				//Success - format message...
				//ASSUMPTION - Status value always exists...
				message = webex.Status.GetEnumDescription();

				if (null != webex.Response)
				{
					//Response exists - retrieve per type...
					System.Net.HttpWebResponse httpres = webex.Response as System.Net.HttpWebResponse;
					if (null != httpres)
					{
						//Http Response...
						message = String.Format("{0} : {1}", ((int)httpres.StatusCode).ToString(), httpres.StatusDescription);
					}
					else
					{
						System.Net.FtpWebResponse ftpres = webex.Response as System.Net.FtpWebResponse;
						if (null != ftpres)
						{
							//Ftp Response...
							message = String.Format("{0 (ftp)} : {1}", ((int)ftpres.StatusCode).ToString(), ftpres.StatusDescription);
						}
					}
				}
			}

			//Processing complete - return message
			return message;
		}

	}
}