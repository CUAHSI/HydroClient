using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

using System;
using System.Configuration;
using HISWebClient.Util;

namespace BlobPurge
{
	public class Functions
	{

		private static AzureContext _ac = new AzureContext();


		// This function will be triggered based on the schedule you have set for this WebJob
		// This function will enqueue a message on an Azure Queue called queue
		[NoAutomaticTrigger]
		//public static void ManualTrigger(TextWriter log, int value, [Queue("queue")] out string message)
		public static void ManualTrigger(TextWriter log, int value /*, [Queue("queue")] out string message */)
		{
			// Microsoft code - not needed here...
			//log.WriteLine("Function is invoked with value={0}", value);
			//message = value.ToString();
			//log.WriteLine("Following message will be written on the Queue={0}", message);

			//Purge blobs...
			string purgeAfterDays = ConfigurationManager.AppSettings["purgeAfterDays"];
			TimeSpan ts = new TimeSpan(int.Parse(purgeAfterDays), 0, 0, 0);

			_ac.PurgeBlobsOlderThan(ts);

		}
	}
}
