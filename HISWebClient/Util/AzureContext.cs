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

            string strFileDateAndExtension = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-ffff") + ".zip";
            if (String.IsNullOrWhiteSpace(requestName))
            {
                requestName = strFileDateAndExtension;
            }
            else
            {
                //requestName += "-" + strFileDateAndExtension;
				requestName = requestName.SanitizeAndUrlEscapeForFilename() + "-" + strFileDateAndExtension;

			}

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

						//Console.WriteLine(name);
						//Console.WriteLine(dto);

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

        //Shared Access Blob Policy - allocate a new policy - access: readonly, duration: 24 hours from now
        public SharedAccessBlobPolicy sharedAccessBlobPolicy
        {
            get
            {
                return (new SharedAccessBlobPolicy()
                             {
                                 SharedAccessExpiryTime = DateTime.UtcNow.AddDays(1.0),
                                 Permissions = SharedAccessBlobPermissions.Read
                             });
            }
        }
    }
}