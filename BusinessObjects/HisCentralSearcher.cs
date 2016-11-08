using HISWebClient.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using System.Configuration;
using System.Net.Http;

using log4net.Core;
using HISWebClient.Util;

namespace HISWebClient.DataLayer
{
	/// <summary>
	/// Search for data series using HIS Central 
	/// </summary>
	public class HISCentralSearcher : SeriesSearcher
	{
		#region Fields

		private readonly string _hisCentralUrl;
		private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

		#endregion

		#region Constructor

		/// <summary>
		/// Create a new HIS Central Searcher which connects to the HIS Central web
		/// services
		/// </summary>
		/// <param name="hisCentralUrl">The URL of HIS Central</param>
		public HISCentralSearcher(string hisCentralUrl)
		{
			hisCentralUrl = hisCentralUrl.Trim();
			if (hisCentralUrl.EndsWith("?WSDL", StringComparison.OrdinalIgnoreCase))
			{
				hisCentralUrl = hisCentralUrl.ToUpperInvariant().Replace("?WSDL", "");
			}
			_hisCentralUrl = hisCentralUrl;
		}

		#endregion

		#region Public methods

		public string GetWebServicesXml(string xmlFileName, int? codePage = null)
		{
			Encoding encoding = Encoding.ASCII;	//Default to ASCII encoding

			if ( null != codePage)
			{
				//Other than 'ASCII' encodings...
				Dictionary<int, Encoding> encodings = new Dictionary<int,Encoding>()
													  {
														  {Encoding.UTF7.CodePage, Encoding.UTF7},							//UTF-7
														  {Encoding.UTF8.CodePage, Encoding.UTF8},							//UTF-8
														  {Encoding.BigEndianUnicode.CodePage, Encoding.BigEndianUnicode},	//UTF-16
														  {Encoding.UTF32.CodePage, Encoding.UTF32},						//UTF-32
													  };

				foreach (int cp in encodings.Keys)
				{
					if (cp == codePage)
					{
						encoding = encodings[cp];
						break;
					}
				}
			}

			HttpWebResponse response = null;
			
			try
			{
				var url = _hisCentralUrl + "/GetWaterOneFlowServiceInfo";

				StringBuilder sb = new StringBuilder();

				byte[] buf = new byte[8192];

				//do get request
				HttpWebRequest request = (HttpWebRequest)
					WebRequest.Create(url);


				response = (HttpWebResponse)
					request.GetResponse();


				Stream resStream = response.GetResponseStream();

				string tempString = null;
				int count = 0;
				//read the data and print it
				do
				{
					count = resStream.Read(buf, 0, buf.Length);
					if (count != 0)
					{
						tempString = encoding.GetString(buf, 0, count);

						sb.Append(tempString);
					}
				}
				while (count > 0);

				return sb.ToString();

			}
			finally
			{
				if (response != null)
				{
					response.Close();
				}
			}
		}

		public string GetOntologyTreeXML(string conceptKeyword)
		{
			HttpWebResponse response = null;
			WebClient myWebClient = new WebClient();
			
			try
			{
				var url = _hisCentralUrl + "/getOntologyTree";

				string data = "conceptKeyword=" + conceptKeyword;
				byte[] dataStream = Encoding.UTF8.GetBytes(data);

				StringBuilder sb = new StringBuilder();

				byte[] buf = new byte[8192];

				//do get request
				HttpWebRequest request = (HttpWebRequest)
					WebRequest.Create(url);

				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = dataStream.Length;

				Stream newStream = request.GetRequestStream();

				// Send the data.
				newStream.Write(dataStream, 0, dataStream.Length);
				newStream.Close();

				response = (HttpWebResponse)
					request.GetResponse();


				Stream resStream = response.GetResponseStream();

				string tempString = null;
				int count = 0;
				//read the data and print it
				do
				{
					count = resStream.Read(buf, 0, buf.Length);
					if (count != 0)
					{
						tempString = Encoding.UTF8.GetString(buf, 0, count);

						sb.Append(tempString);
					}
				}
				while (count > 0);

				return sb.ToString();
		   
			}
			finally
			{
				if (response != null)
				{
					response.Close();
				}
			}
		}

		#endregion

		#region Private methods

		protected override IEnumerable<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> GetSeriesCatalogForBox(double xMin, double xMax, double yMin,
																			  double yMax, string keyword,
																			  DateTime startDate, DateTime endDate,
																			  int[] networkIDs,
																			  //IProgressHandler bgWorker, 
																			  long currentTile, long totalTilesCount)
		{
			var url = new StringBuilder();
			url.Append(_hisCentralUrl);
			url.Append("/GetSeriesCatalogForBox2");
			url.Append("?xmin=");
			url.Append(Uri.EscapeDataString(xMin.ToString(_invariantCulture)));
			url.Append("&xmax=");
			url.Append(Uri.EscapeDataString(xMax.ToString(_invariantCulture)));
			url.Append("&ymin=");
			url.Append(Uri.EscapeDataString(yMin.ToString(_invariantCulture)));
			url.Append("&ymax=");
			url.Append(Uri.EscapeDataString(yMax.ToString(_invariantCulture)));

			//to append the keyword
			url.Append("&conceptKeyword=");
			if (!String.IsNullOrEmpty(keyword))
			{
				url.Append(Uri.EscapeDataString(keyword));
			}

			//to append the list of networkIDs separated by comma
			url.Append("&networkIDs=");
			if (networkIDs != null)
			{
				var serviceParam = new StringBuilder();
				for (int i = 0; i < networkIDs.Length - 1; i++)
				{
					serviceParam.Append(networkIDs[i]);
					serviceParam.Append(",");
				}
				if (networkIDs.Length > 0)
				{
					serviceParam.Append(networkIDs[networkIDs.Length - 1]);
				}
				url.Append(Uri.EscapeDataString(serviceParam.ToString()));
			}

			//to append the start and end date
			url.Append("&beginDate=");
			url.Append(Uri.EscapeDataString(startDate.ToString("MM/dd/yyyy")));
			url.Append("&endDate=");
			url.Append(Uri.EscapeDataString(endDate.ToString("MM/dd/yyyy")));

			var keywordDesc = string.Format("[{0}. Tile {1}/{2}]",
											String.IsNullOrEmpty(keyword) ? "All" : keyword, currentTile,
											totalTilesCount);

			// Try to send request several times (in case, when server returns timeout)
			const int tryCount = 5;
			for (int i = 0; i < tryCount; i++)
			{
				try
				{
					var request = WebRequest.Create(url.ToString());
					request.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["requestTimeoutMilliseconds"].ToString());
					using (var response = request.GetResponse())
					{
						using (var stream = response.GetResponseStream())
						{
							using (MemoryStream ms = new MemoryStream())
							{
								stream.CopyTo(ms);
								ms.Seek(0, SeekOrigin.Begin);
#if (DEBUG)
								writeMemoryStreamToFile(@"C:\CUAHSI\ResponseFromGetSeriesCatalogForBox2.xml", ms);
								ms.Seek(0, SeekOrigin.Begin);
#endif
								using (var reader = XmlReader.Create(ms))
								{
									return ParseSeries(reader, startDate, endDate);
								}
							}
						}
					}
				}
				catch (WebException wex)
				{
					if (wex.Status == WebExceptionStatus.Timeout)
					{
						//Timeout error - continue 
						continue;
					}

					throw wex;
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}

			WebException wex1 = new WebException("Timeout. Please limit search area and/or Keywords.", WebExceptionStatus.Timeout);

			throw wex1; 
		}

		//BCC - 07-Sep-2016 - Added new method for call to GetSeriesCatalogForBox3...
		protected override IEnumerable<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> GetSeriesCatalogForBox( double xMin, 
																														  double xMax, 
																														  double yMin,
																													      double yMax,
																														  string sampleMedium,
																														  string dataType,
																														  string valueType,
																														  string keyword, 
																														  DateTime startDate, 
																														  DateTime endDate,
																														  int[] networkIDs,
																														  long currentTile, 
																														  long totalTilesCount)
		{
			var url = new StringBuilder();
			url.Append(_hisCentralUrl);
			url.Append("/GetSeriesCatalogForBox3");
			url.Append("?xmin=");
			url.Append(Uri.EscapeDataString(xMin.ToString(_invariantCulture)));
			url.Append("&xmax=");
			url.Append(Uri.EscapeDataString(xMax.ToString(_invariantCulture)));
			url.Append("&ymin=");
			url.Append(Uri.EscapeDataString(yMin.ToString(_invariantCulture)));
			url.Append("&ymax=");
			url.Append(Uri.EscapeDataString(yMax.ToString(_invariantCulture)));

			url.Append("&sampleMedium=");
			if (!String.IsNullOrEmpty(sampleMedium))
			{
				url.Append(Uri.EscapeDataString(sampleMedium));
			}
			url.Append("&dataType=");
			if (!String.IsNullOrEmpty(dataType))
			{
				url.Append(Uri.EscapeDataString(dataType));
			}
			url.Append("&valueType=");
			if (!String.IsNullOrEmpty(valueType))
			{
				url.Append(Uri.EscapeDataString(valueType));
			}

			//to append the keyword
			url.Append("&conceptKeyword=");
			if (!String.IsNullOrEmpty(keyword))
			{
				url.Append(Uri.EscapeDataString(keyword));
			}

			//to append the list of networkIDs separated by comma
			url.Append("&networkIDs=");
			if (networkIDs != null)
			{
				var serviceParam = new StringBuilder();
				for (int i = 0; i < networkIDs.Length - 1; i++)
				{
					serviceParam.Append(networkIDs[i]);
					serviceParam.Append(",");
				}
				if (networkIDs.Length > 0)
				{
					serviceParam.Append(networkIDs[networkIDs.Length - 1]);
				}
				url.Append(Uri.EscapeDataString(serviceParam.ToString()));
			}

			//to append the start and end date
			url.Append("&beginDate=");
			url.Append(Uri.EscapeDataString(startDate.ToString("MM/dd/yyyy")));
			url.Append("&endDate=");
			url.Append(Uri.EscapeDataString(endDate.ToString("MM/dd/yyyy")));

			var keywordDesc = string.Format("[{0}. Tile {1}/{2}]",
											String.IsNullOrEmpty(keyword) ? "All" : keyword, currentTile,
											totalTilesCount);

			// Try to send request several times (in case, when server returns timeout)
			const int tryCount = 5;
			for (int i = 0; i < tryCount; i++)
			{
				try
				{
					var request = WebRequest.Create(url.ToString());
					request.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["requestTimeoutMilliseconds"].ToString());
					using (var response = request.GetResponse())
					{
						using (var stream = response.GetResponseStream())
						{
							using (MemoryStream ms = new MemoryStream())
							{
								stream.CopyTo(ms);
								ms.Seek(0, SeekOrigin.Begin);
#if (DEBUG)
								writeMemoryStreamToFile(@"C:\CUAHSI\ResponseFromGetSeriesCatalogForBox3.xml", ms);
								ms.Seek(0, SeekOrigin.Begin);
#endif
								using (var reader = XmlReader.Create(ms))
								{
						return ParseSeries(reader, startDate, endDate);
					}
				}
						}
					}
				}
				catch (WebException wex)
				{
					if (wex.Status == WebExceptionStatus.Timeout)
					{
						//Timeout error - continue 
						continue;
					}

					throw wex;
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}

			WebException wex1 = new WebException("Timeout. Please limit search area and/or Keywords.", WebExceptionStatus.Timeout);

			throw wex1; 
		}

		private IEnumerable<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> ParseSeries(XmlReader reader, DateTime startDate, DateTime endDate)
		{
			var seriesList = new List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>();
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					if (XmlContext.AdvanceReaderPastEmptyElement(reader))
					{
						//Empty element - advance and continue...
						continue;
					}

					var nodeName = reader.Name.ToLower();
					if (nodeName == "seriesrecord" || nodeName == "seriesrecordfull")
					{
						//Read the site information
						var series = ReadSeriesFromHISCentral(reader);

						if (series != null)
						{
#if (DEBUG)
							//Set original value count for debug purposes...
							series.ValueCountOriginal = series.ValueCount;
#endif
								SearchHelper.UpdateDataCartToDateInterval(series, startDate, endDate);
								seriesList.Add(series);
						}
					}
				}
			}
			return seriesList;
		}

		/// <summary>
		/// Read the list of series from the XML that is returned by HIS Central
		/// </summary>
		/// <param name="reader">the xml reader</param>
		/// <returns>the list of intermediate 'SeriesDataCart' objects</returns>
		private BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart ReadSeriesFromHISCentral(XmlReader reader)
		{
			var series = new BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart();
			while (reader.Read())
			{
				var nodeName = reader.Name.ToLower();
				if (reader.NodeType == XmlNodeType.Element)
				{
					if (XmlContext.AdvanceReaderPastEmptyElement(reader))
					{
						//Empty element - advance and continue...
						continue;
					}

					switch (nodeName)
					{
						case "servcode":
							reader.Read();
							series.ServCode = reader.Value;
							break;
						case "servurl":
							reader.Read();
							series.ServURL = reader.Value;
							break;
						case "location":
							reader.Read();
							series.SiteCode = reader.Value;
							break;
						case "varcode":
							reader.Read();
							series.VariableCode = reader.Value;
							break;
						case "varname":
							reader.Read();
							series.VariableName = reader.Value;
							break;
						case "begindate":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.BeginDate = Convert.ToDateTime(reader.Value, _invariantCulture);
							else
								return null;
							break;
						case "enddate":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.EndDate = Convert.ToDateTime(reader.Value, _invariantCulture);
							else
								return null;
							break;
						case "valuecount":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.ValueCount = Convert.ToInt32(reader.Value);
							else
								return null;
							break;
						case "sitename":
							reader.Read();
							series.SiteName = reader.Value;
							break;
						case "latitude":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.Latitude = Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture);
							else
								return null;
							break;
						case "longitude":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.Longitude = Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture);
							else
								return null;
							break;
						case "datatype":
							reader.Read();
							series.DataType = reader.Value;
							break;
						case "valuetype":
							reader.Read();
							series.ValueType = reader.Value;
							break;
						case "samplemedium":
							reader.Read();
							series.SampleMedium = reader.Value;
							break;
						case "timeunits":
							reader.Read();
							series.TimeUnit = reader.Value;
							break;
						case "conceptkeyword":
							reader.Read();
							series.ConceptKeyword = reader.Value;
							break;
						case "gencategory":
							reader.Read();
							series.GeneralCategory = reader.Value;
							break;
						case "timesupport":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.TimeSupport = Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture);
							break;
						case "isregular":
							reader.Read();
							if (!String.IsNullOrWhiteSpace(reader.Value))
								series.IsRegular = Convert.ToBoolean(reader.Value);
							break;
						case "variableunitsabbrev":
							reader.Read();
							series.VariableUnits = reader.Value;
							break;
						//BCC - 07-Sep-2016 - Added additional reads for use with GetSeriesCatalogForBox3...
						case "qclid":
							reader.Read();
							series.QCLID = reader.Value;
							break;
						case "qcldesc":
							reader.Read();
							series.QCLDesc = reader.Value;
							break;
						case "sourceorg":
							reader.Read();
							series.SourceOrg = reader.Value;
							break;
						case "sourceid":
							reader.Read();
							series.SourceId = reader.Value;
							break;
						case "methodid":
							reader.Read();
							series.MethodId = reader.Value;
							break;
						case "methoddesc":
							reader.Read();
							series.MethodDesc = reader.Value;
							break;
					}
				}
				else if (reader.NodeType == XmlNodeType.EndElement && (nodeName == "seriesrecord" || nodeName == "seriesrecordfull"))
				{
					return series;
				}
			}

			return null;
		}

		#endregion

#if (DEBUG)
		private void writeMemoryStreamToFile( string filePathAndName, MemoryStream ms)
		{
			//Validate/initialize input parameters...
			if ( String.IsNullOrWhiteSpace(filePathAndName) || null == ms)
			{
				return;		//Input parameter(s) invalid - return early
			}

			try
			{
				if (EnvironmentContext.LocalEnvironment())
				{
					//Position memory stream...
					ms.Seek(0, SeekOrigin.Begin);

					//Create file...
					using (System.IO.FileStream output = new System.IO.FileStream(filePathAndName, FileMode.OpenOrCreate))
					{
						//Create XmlReader on memory stream...
						using (var reader = XmlReader.Create(ms))
						{
							//Create XmlWriter on file...
							using (XmlWriter writer = XmlWriter.Create(output))
							{
								//Write contents of reader to file and flush...
								writer.WriteNode(reader, true);
								output.Flush();

								//Re-position memory stream...
								ms.Seek(0, SeekOrigin.Begin);
							}
						}
					}
				}

			}
			catch (Exception ex)
			{
				//Take no action...
				string msg = ex.Message;
			}
		}
#endif
	}
}
