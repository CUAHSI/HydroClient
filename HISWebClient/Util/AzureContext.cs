using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

using CUAHSI.Common;

namespace HISWebClient.Util
{
    //Wrapper class for singleton Azure context...
    //Source: http://stackoverflow.com/questions/24626749/azure-table-storage-best-practice-for-asp-net-mvc-webapi
    public class AzureContext
    {
        private static CloudStorageAccount _cloudStorageAccount;
        private static CloudBlobClient _cloudBlobClient;
        private static CloudBlobContainer _cloudBlobContainer;

        //Lock object - allocate at first class instance initialization
        private static object lockObject = new Object();

        //Default constructor...
        public AzureContext() : this("BlobStorageViaPrimaryAccessKey") { }

        //Initializing constructor...
        public AzureContext(string appSettingsKey) 
        {
            if ( null == _cloudStorageAccount ) //ASSUMPTION: This member is ALWAYS allocated first...
            {
                lock (lockObject)
                {
                    if ( null == _cloudStorageAccount )
                    {
                        string connectionString = ConfigurationManager.AppSettings[appSettingsKey];
                        _cloudStorageAccount = CloudStorageAccount.Parse(connectionString);

                        _cloudBlobClient = _cloudStorageAccount.CreateCloudBlobClient();

                        _cloudBlobContainer = _cloudBlobClient.GetContainerReference(ConfigurationManager.AppSettings["blobContainer"]);

                        //ASSUMPTION: container access settings: public read access blobs only...
                        BlobContainerPermissions bcp = _cloudBlobContainer.GetPermissions();

                        Debug.Assert(BlobContainerPublicAccessType.Blob == bcp.PublicAccess);
                    }
                }
            }
        }

        public async Task<String> UploadFromMemoryStreamAsync(MemoryStream ms,  string requestName, CancellationToken ct)
        {
            //Validate/initialize input parameters...
            if (null == ms)
            {
                throw new ArgumentNullException("ms", "Input memory stream must NOT be null!!");
            }

			//Format the file name...
			requestName = FileContext.GenerateFileName(requestName, ".zip");

            if( null == ct)
            {
                ct = CancellationToken.None;
            }

            //Retrieve a Cloud Block Blob Reference...
            CloudBlockBlob ccb;
            lock (lockObject)
            {
                ccb = _cloudBlobContainer.GetBlockBlobReference(requestName);
            }

            //Create blob URI...
            string sasBlobToken = ccb.GetSharedAccessSignature(sharedAccessBlobPolicy);
            string blobUri = ccb.Uri + sasBlobToken;

            //Write the zip archive to the Azure blob storage collection...
            await ccb.UploadFromStreamAsync(ms, ct);

            //Return the blobURI
            return (blobUri);
        }

		//Retrieve a blob in a file stream per the input file name...
		public async Task<bool> RetrieveBlobAsync(string fileName, CancellationToken ct, MemoryStream ms)
		{
			//Retrieve a Cloud Block Blob Reference...
			CloudBlockBlob ccb;
			lock (lockObject)
			{
				ccb = _cloudBlobContainer.GetBlockBlobReference(fileName);
			}

			if (! ccb.Exists())
			{
				return false;	//Blob not found - return early
			}

			//BCC - 14-Oct-2015
			//NOTE: To prevent this call from hanging, add the ConfigureAwait(false) call to tell .NET: Do not attempt to run in the same synchronization context...
			//Source(s): http://stackoverflow.com/questions/28526249/azure-downloadtostreamasync-method-hangs
			//			 http://code.jonwagner.com/2012/09/04/deadlock-asyncawait-is-not-task-wait/
			//
			//NOTE 2: While the ConfigureAwait(false) call solves the hanging problem, its use causes the method to return before the download is complete...
			//		  So for right now, call the synchronous DownloadToSteam(...) 
			//await ccb.DownloadToStreamAsync(ms, ct).ConfigureAwait(false);
			//ccb.DownloadToStream(ms);

			//Another approach... WORKS!!!
			//Source: http://stackoverflow.com/questions/18050836/getting-return-value-from-task-run
			ms = WrapDownloadToStreamAsync(ccb, ms, ct).Result;

			//Processing complete - return 
			return true;
		}

		//Source: http://stackoverflow.com/questions/18050836/getting-return-value-from-task-run
		private async Task<MemoryStream> WrapDownloadToStreamAsync(CloudBlockBlob ccb, MemoryStream ms, CancellationToken ct)
		{
			await Task.Run(() => ccb.DownloadToStreamAsync(ms, ct));
			return ms;
		}


		//Purge all blobs older than the input time period (compared to the current date/time) 
		public void PurgeBlobsOlderThan(TimeSpan ts)
		{
			if (null != ts) 
			{
				//For each item in the container...
				DateTimeOffset dtoUtcNow = DateTimeOffset.Now;
				foreach (IListBlobItem item in _cloudBlobContainer.ListBlobs(null, false))
				{
					if (item.GetType() == typeof(CloudBlockBlob))
					{
						CloudBlockBlob ccb = (CloudBlockBlob)item;

						//Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);
						string name = ccb.Name;

						DateTimeOffset? dto = ccb.Properties.LastModified;

						if (null != dto)
						{
							if ( ts <= (dtoUtcNow - dto))
							{
								//Current blob older than input time span - delete...
								ccb.Delete();
							}
						}
					}
				}

			}
		}

        //Shared Access Blob Policy - allocate a new policy - access: readonly, duration: 30 days from now
        public SharedAccessBlobPolicy sharedAccessBlobPolicy
        {
            get
            {
                return (new SharedAccessBlobPolicy()
                             {
								 //BCC - 13-Oct-2015 - Increase expiration date to 30 days in the future...
                                 //SharedAccessExpiryTime = DateTime.UtcNow.AddDays(1.0),
								 SharedAccessExpiryTime = DateTime.UtcNow.AddDays(30.0),
                                 Permissions = SharedAccessBlobPermissions.Read
                             });
            }
        }
    }
}