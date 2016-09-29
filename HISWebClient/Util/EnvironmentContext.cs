using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Util
{
	public static class EnvironmentContext
	{
		//Return values: True - local environment, false otherwise
		public static bool LocalEnvironment()
		{
			return String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
		}
	}
}