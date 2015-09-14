using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using log4net;

namespace HISWebClient.Util
{
	/// <summary>
	/// A derived class for use with the AdoNetAppender...
	/// </summary>
	public class DbErrorContext : DbBaseContext
	{
		//Initializing Constructor
		public DbErrorContext(string loggerName, string adoNetAppenderName, string localConnectionStringKey, string deployConnectionStringKey) 
					: base(loggerName, adoNetAppenderName, localConnectionStringKey, deployConnectionStringKey) {}

		//Write an entry to the log table
		public void createLogEntry(HttpContext httpcontextCurrent, DateTime occurrenceDtUtc, string methodName, Exception exception, string exceptionMessage)
		{
			//Validate/initialize input parameters...
			if ( null == httpcontextCurrent ||
				 null == occurrenceDtUtc ||
				 String.IsNullOrWhiteSpace(methodName) ||
				 null == exception ||
				 String.IsNullOrWhiteSpace(exceptionMessage))
			{
				return;		//Invalid parameter - return early...
			}

			//Retrieve session id, IP address and domain name...
			string sessionId = String.Empty;
			string userIpAddress = String.Empty;
			string domainName = String.Empty;

			getIds(httpcontextCurrent, ref sessionId, ref userIpAddress, ref domainName);

			createLogEntry(sessionId, userIpAddress, domainName, occurrenceDtUtc, methodName, exception, exceptionMessage);

			return;
		}

		//Write and entry to the log table (for use when an HttpContext is not available...)
		public void createLogEntry(string sessionId, string userIpAddress, string domainName, DateTime occurrenceDtUtc, string methodName, Exception exception, string exceptionMessage)
		{
			//Validate/initialize input parameters...
			if (String.IsNullOrWhiteSpace(sessionId) ||
				 String.IsNullOrWhiteSpace(userIpAddress) ||
				 String.IsNullOrWhiteSpace(domainName) ||
				 null == occurrenceDtUtc ||
				 String.IsNullOrWhiteSpace(methodName) ||
				 null == exception ||
				 String.IsNullOrWhiteSpace(exceptionMessage) )
			{
				return;		//Invalid parameter - return early...
			}

			//Write derived and input values to MDC...
			MDC.Clear();

			MDC.Set("SessionId", sessionId);
			MDC.Set("IPAddress", userIpAddress);
			MDC.Set("Domain", domainName);
			MDC.Set("OccurrenceDateTime", occurrenceDtUtc.ToString());
			MDC.Set("MethodName", methodName);
			MDC.Set("ExceptionType", exception.GetType().ToString());
			MDC.Set("ExceptionMessage", exceptionMessage);

			//Convert parameters to JSON and write to MDC...
			//Source: http://stackoverflow.com/questions/23729477/converting-dictionary-string-string-to-json-string
			if (0 < m_dictParams.Count)
			{
				var kvs = m_dictParams.Select(kvp => string.Format("\"{0}\":\"{1}\"", kvp.Key, string.Join(",", kvp.Value)));
				string json = string.Concat("{", string.Join(",", kvs), "}");

				MDC.Set("Parameters", json);
			}

			//Write to the log...
			string logMessage = "log message";	//NOTE: Due to MDC usage and AdoNetAppender usage, this message is not logged..

			m_loggerDB.Error(logMessage);

			//Processing complete - return
			return;
		}

	}
}