using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using HISWebClient.Models;
using CUAHSI.Common;
using CUAHSI.Models;

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

		/// <summary>
		/// Format: series-<sitename>-<variablename>.csv
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string GenerateBlobName(SeriesData data)
		{
			return string.Format("series-{0}-{1}.csv", data.myMetadata.SiteName.SanitizeForFilename(), data.myMetadata.VariableName.SanitizeForFilename());
		}

		/// <summary>
		/// Format: <servicecode>-<sitename>-<variablename>.<extension>
		/// </summary>
		/// <param name="meta"></param>
		/// <param name="timeSeriesFormat"></param>
		/// <returns></returns>
		public static string GenerateFileName(SeriesMetadata meta, TimeSeriesFormat timeSeriesFormat = TimeSeriesFormat.CSV)
		{

			string fileName = string.Format("{0}-{1}-{2}", meta.ServCode.SanitizeForFilename(), meta.SiteName.SanitizeForFilename(), meta.VariableName.SanitizeForFilename());
			string extension = String.Empty;

			if (TimeSeriesFormat.WaterOneFlow == timeSeriesFormat)
			{
				extension = ".xml";
			}
			else if (TimeSeriesFormat.CSV == timeSeriesFormat)
			{
				//NOTE: Microsoft Excel restricts file path + name + extension to 218 characters max.  Truncate file name if indicated...

				extension = ".csv";

				while (218 < (fileName.Length + extension.Length))
				{
					fileName = fileName.Substring(0, (fileName.Length - 1));
				}
			}

			return string.Format("{0}{1}", fileName, extension);

		}

	}
}