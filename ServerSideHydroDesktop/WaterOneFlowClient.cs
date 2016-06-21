using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CUAHSIDataStorage;
using ServerSideHydroDesktop.ObjectModel;

namespace ServerSideHydroDesktop
{
    public class WaterOneFlowClient
    {
        #region Fields

		private readonly string _serviceURL;
		private readonly IWaterOneFlowParser _parser;

		//the directory where downloaded files are stored
		private string _downloadDirectory;

		//the object containing additional metadata information
		//about the web service including service version
		private readonly DataServiceInfo _serviceInfo;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new instance of a WaterOneFlow web service client
		/// which communicates with the specified web service.
		/// </summary>
		/// <param name="serviceInfo">The object with web service information</param>
		/// <remarks>Throws an exception if the web service is not a valid
		/// WaterOneFlow service</remarks>
		public WaterOneFlowClient(DataServiceInfo serviceInfo)
		{
			_serviceURL = serviceInfo.EndpointURL;

			//find out the WaterOneFlow version of this web service
			_serviceInfo = serviceInfo;
            _serviceInfo.Version = WebServiceHelper.GetWaterOneFlowVersion(_serviceURL);
                        
            //assign the waterOneFlow parser
            _parser = new ParserFactory().GetParser(ServiceInfo);
            
            SaveXmlFiles = false; // false on webserver
		}

		/// <summary>
		/// Creates a new instance of a WaterOneFlow web service client.
		/// The constructor will throw an exception if the url is an invalid
		/// WaterOneFlow web service url.
		/// </summary>
		/// <param name="asmxURL">The url of the .asmx web page</param>
		/// <remarks>Throws an exception if the web service is not a valid
		/// WaterOneFlow service</remarks>
        public WaterOneFlowClient(string asmxURL) :
            this(new DataServiceInfo(asmxURL, asmxURL.Replace(@"http://", "")))
		{
		}

	    #endregion

		#region Properties

		/// <summary>
		/// Gets information about the web service used by this web service client
		/// </summary>
		public DataServiceInfo ServiceInfo
		{
			get { return _serviceInfo; }
		}

		/// <summary>
		/// Gets or sets the name of the directory where 
		/// downloaded xml files are stored
		/// </summary>
		public string DownloadDirectory
		{
            get
            {
                if (string.IsNullOrWhiteSpace(_downloadDirectory))
                {
                    DownloadDirectory = Path.Combine(Path.GetTempPath(), "HydroDesktop");
                }

                return _downloadDirectory;
            }
			set
			{
                if (!Directory.Exists(value))
                {
                    try
                    {
                        Directory.CreateDirectory(value);
                    }
                    catch
                    {
                        value = Path.GetTempPath();
                    }
                }
			    _downloadDirectory = value;
			}
		}

        /// <summary>
        /// Gets or sets boolean indicated that WaterOneFlowClient should save temporary xml files.
        /// </summary>
        /// <seealso cref="DownloadDirectory"/>
        public bool SaveXmlFiles { get; set; }

	    #endregion

		#region Public Methods

		/// <summary>
		/// Get the data values for the specific site, variable and time range as a list of Series objects
		/// </summary>
		/// <param name="siteCode">the full site code (networkPrefix:siteCode)</param>
		/// <param name="variableCode">the full variable code (vocabularyPrefix:variableCode)</param>
		/// <param name="startTime">the start date/time</param>
		/// <param name="endTime">the end date/time</param>
		/// <returns>The data series. Each data series object includes a list of data values, 
		/// site, variable, method, source and quality control level information.</returns>
		/// <remarks>Usually the list of Series returned will only contain one series. However in some
		/// cases, it will contain two or more series with the same site code and variable code, but
		/// with a different method or quality control level</remarks>
        public IList<Series> GetValues(string siteCode, string variableCode, DateTime startTime, DateTime endTime)
		{
		    IList<Series> result;

		    var req = WebServiceHelper.CreateGetValuesRequest(_serviceURL, siteCode, variableCode, startTime, endTime);

            using (var resp = (HttpWebResponse) req.GetResponse())
		    {
		        using (var stream = resp.GetResponseStream())
		        {
		            result = _parser.ParseGetValues(stream);
		        }
		    }
		    

		    return result;
		}

        /// <summary>
        /// TAP-interface method for acquiring parsed and stream versions of WaterOneFlow document.
        /// </summary>
        /// <param name="siteCode"></param>
        /// <param name="variableCode"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="waterOneflowTimeoutMilliseconds">The number of milliseconds to spend waiting for WaterOneFlow service response before declaring it unavailable.</param>
        /// <returns></returns>
        public async Task<Tuple<Stream, IList<Series>>> GetValuesAndRawStreamAsync(string siteCode, string variableCode, DateTime startTime, DateTime endTime, int waterOneflowTimeoutMilliseconds)
        {
            HttpWebRequest req = WebServiceHelper.CreateGetValuesRequest(_serviceURL, siteCode, variableCode, startTime, endTime);
            req.Timeout = waterOneflowTimeoutMilliseconds;
            try
            {                
				//Task<WebResponse> getTask = Task.Factory.FromAsync(
				//req.BeginGetResponse,
				//asyncResult => req.EndGetResponse(asyncResult),
				//(object)null);

				Task<WebResponse> getTask = Task.Factory.FromAsync<WebResponse>(req.BeginGetResponse, req.EndGetResponse, null);
				
				//Stream s = await getTask
				//	.ContinueWith(t => ReadStreamFromResponse(t.Result));
				//return new Tuple<Stream,IList<Series>>(s, _parser.ParseGetValues(s));
				WebResponse wr = await getTask;
				if (getTask.IsFaulted)
                {
					Exception ex = getTask.Exception.InnerExceptions.First();
					throw ex;
                }

				Stream s = ReadStreamFromResponse(getTask.Result);
				return new Tuple<Stream, IList<Series>>(s, _parser.ParseGetValues(s));
            }
            catch (Exception ex)
            {
                LogHelper.LogGetAsyncDataValuesException(_serviceURL, siteCode, variableCode, startTime, endTime, ex);
				throw ex;
            } 
        }

        /// <summary>
        /// Transform a stream of a WaterOneFlow GetValues response into a Series object suitable for returning via middleware.
        /// </summary>
        /// <param name="s">Stream to deserialize as Series model.</param>
        /// <returns></returns>
        public IList<Series> GetValuesFromStream(Stream s)
        {
            return _parser.ParseGetValues(s);
        }

        /// <summary>
        /// Executes same methods as GetValues only with TAP Task interface support for including in an asynchronous pipeline.
        /// </summary>
        /// <param name="siteCode"></param>
        /// <param name="variableCode"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <see cref="http://stackoverflow.com/questions/10565090/getting-the-response-of-a-asynchronous-httpwebrequest"/>
        /// <returns></returns>
        public async Task<IList<Series>> GetValuesAsync(string siteCode, string variableCode, DateTime startTime, DateTime endTime)
        {
            HttpWebRequest req = WebServiceHelper.CreateGetValuesRequest(_serviceURL, siteCode, variableCode, startTime, endTime);
            //req.Timeout = 30000; // 30-second max download            
			req.Timeout = 60000; // 60-second max download  - BCC - 06-Jun-2016 - Increase timeout interval...
            try
            {
				//Task<WebResponse> task = Task.Factory.FromAsync(
				//req.BeginGetResponse,
				//asyncResult => req.EndGetResponse(asyncResult),
				//(object)null);

				Task<WebResponse> task = Task.Factory.FromAsync<WebResponse>(req.BeginGetResponse, req.EndGetResponse, null);

                //return await task.ContinueWith(t => _parser.ParseGetValues(ReadStreamFromResponse(t.Result)));
				WebResponse wr = await task;
				if (task.IsFaulted)
                {
					Exception ex = task.Exception.InnerExceptions.First();
					throw ex;
                }

				//Parse returned series - save to series cache, if indicated 
				IList<Series> iList = _parser.ParseGetValues(ReadStreamFromResponse(task.Result));
				return iList;
            }
            catch (Exception ex)
                {
				LogHelper.LogGetAsyncDataValuesException(_serviceURL, siteCode, variableCode, startTime, endTime, ex);
                    return new List<Series>();
            }
        }        

        /// <summary>
        /// Extracts a Stream ready for schema parsing from a web response stream. With this and GetValuesAsync, the request executes asynchronously, while the parsing blocks (desired behavior).
        /// http://stackoverflow.com/questions/10565090/getting-the-response-of-a-asynchronous-httpwebrequest
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static Stream ReadStreamFromResponse(WebResponse response)
        {
			Stream stream = response.GetResponseStream();


			//BCC - Test - Here the stream contains data...
			//using (System.IO.FileStream output = new System.IO.FileStream(@"C:\CUAHSI\ReadFromStreamResponse.xml", FileMode.Create))
			//{
			//	stream.CopyTo(output);
			//}

			//BC TEST - Copy stream contents to a new memory stream and return the memory stream...
			MemoryStream ms = new MemoryStream();
			stream.CopyTo(ms);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;


            return stream;
        } 

		#endregion
    }
}
