using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using CUAHSI.Common;

namespace HISWebClient.Util
{
	public static class FileContext
	{
		/// <summary>
		/// Format: <requestName>-YYYY-MM-DD-<milliseconds since midnight>.<extension> --OR--
		///			YYYY-MM-DD-<milliseconds since midnight>.<extension>
		/// </summary>
		/// <returns></returns>
		public static string GenerateFileName( string requestName, string extension = ".csv" )
		{
			//Validate/initialize input parmeters...
			if ('.' != extension[0])
			{
				extension = extension.Insert(0, ".");
			}

			//For the current date/time...
			DateTime now = DateTime.Now;
			TimeSpan ts = new TimeSpan(0, now.Hour, now.Minute, now.Second, now.Millisecond);

			//Generate data and extension string
			string strFileDateAndExtension = String.Format("{0}-{1}{2}", now.ToString("yyyy-MM-dd"), ts.TotalMilliseconds, extension);  

			//Generate filename
			string fileName = String.IsNullOrWhiteSpace(requestName) ? strFileDateAndExtension : requestName.SanitizeAndUrlEscapeForFilename() + "-" + strFileDateAndExtension;

			//Processing complete - return
			return fileName;
		}
	}
}