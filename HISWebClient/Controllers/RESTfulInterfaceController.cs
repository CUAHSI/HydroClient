using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using HISWebClient.Util;


namespace HISWebClient.Controllers
{
    public class RESTfulInterfaceController : ApiController
    {
		//Reference AzureContext singleton...
		//Source: http://stackoverflow.com/questions/24626749/azure-table-storage-best-practice-for-asp-net-mvc-webapi

		private AzureContext _ac;

		//Constructors - instantiate the AzureContext singleton for later reference...
		public RESTfulInterfaceController() : this(new AzureContext()) { }

		public RESTfulInterfaceController(AzureContext ac)
		{
			_ac = ac;
		}

		//GET CUAHSI/HydroClient/WaterOneFlowArchive/{id}
		public async Task<HttpResponseMessage> Get(string fileName, string fileExtension)
		{
			HttpResponseMessage response = new HttpResponseMessage();
		
			//Validate/initialize input parameters
			if (String.IsNullOrWhiteSpace(fileName) || String.IsNullOrWhiteSpace(fileExtension))
			{
				response.StatusCode = HttpStatusCode.BadRequest;	//Missing/invalid parameter(s) - return early
				response.ReasonPhrase = "Invalid parameter(s)";
				return response;
			}
						
			string fileNameAndExtension = String.Format("{0}.{1}", fileName, fileExtension).ToString();
			MemoryStream ms = new MemoryStream();

			bool bFound = await _ac.RetrieveBlobAsync(fileNameAndExtension, CancellationToken.None, ms);

			if (!bFound)
			{
				response.StatusCode = HttpStatusCode.NotFound;	//Blob not found -  return early
				response.ReasonPhrase = String.Format("Requested archive file: ({0}) not found.", fileNameAndExtension.ToString());
				return response;
			}

			//Blob found - stream to client...
			ms.Seek(0, SeekOrigin.Begin);
			var pushStreamContent = new PushStreamContent((stream, content, context) =>
			{
				try
				{
					ms.CopyTo(stream);
					stream.Close(); // After save we close the stream to signal that we are done writing.
					ms.Close();
				}
				catch (Exception ex)
				{
					int n = 5;

					++n;
				}
			}, "application/zip");

			response.StatusCode = HttpStatusCode.OK;
			response.Content = pushStreamContent;
			response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
			{
				FileName = fileNameAndExtension.ToString()
			};

			return response;

		}


    }
}
