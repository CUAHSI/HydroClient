using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Configuration;
using System.Net;

using log4net;
using log4net.Appender;
using log4net.Core;

using HISWebClient.Models.DataManager;

namespace HISWebClient.Util
{
	/// <summary>
	/// A derived class for use with the AdoNetAppender...
	/// </summary>
	public class DbLogContext : DbBaseContext
	{
		//members...
		private Dictionary<string, string> m_dictReturns = new Dictionary<string,string>();

		//Initializing Constructor
		public DbLogContext(string loggerName, string adoNetAppenderName, string localConnectionStringKey, string deployConnectionStringKey) 
					: base(loggerName, adoNetAppenderName, localConnectionStringKey, deployConnectionStringKey) {}

		//interface...
		public void clearReturns()
		{
			m_dictReturns.Clear();
		}

		public void addReturn<Type>(string name, Type returnIn)
		{
			if (!String.IsNullOrWhiteSpace(name) && null != returnIn)
			{
				m_dictReturns.Add(name, returnIn.ToString());
			}
		}


		//Write and entry to the log table (for use when an HttpContext is not available...)
		public void createLogEntry(string sessionId, string userIpAddress, string domainName, string userEMailAddress, DateTime startDtUtc, DateTime endDtUtc, string methodName, string message, Level logLevel)
		{
			//Validate/initialize input parameters...
			if ( String.IsNullOrWhiteSpace(sessionId) ||
				 String.IsNullOrWhiteSpace(userIpAddress) ||
				 String.IsNullOrWhiteSpace(domainName) ||
				 String.IsNullOrWhiteSpace(userEMailAddress) ||
				 null == startDtUtc ||
				 null == endDtUtc ||
				 String.IsNullOrWhiteSpace(methodName) ||
				 String.IsNullOrWhiteSpace(message) ||
				 null == logLevel)
			{
				return;		//Invalid parameter - return early...
			}

			//Write derived and input values to MDC...
			MDC.Clear();

			MDC.Set("SessionId", sessionId);
			MDC.Set("IPAddress", userIpAddress);
			MDC.Set("Domain", domainName);
			MDC.Set("EmailAddress", userEMailAddress);

			MDC.Set("StartDateTime", startDtUtc.ToString());
			MDC.Set("EndDateTime", endDtUtc.ToString());
			MDC.Set("MethodName", methodName);
			MDC.Set("Message", message);
			MDC.Set("LogLevel", logLevel.DisplayName);

			//Convert parameters to JSON and write to MDC...
			//Source: http://stackoverflow.com/questions/23729477/converting-dictionary-string-string-to-json-string
			if (0 < m_dictParams.Count)
			{
				var kvs = m_dictParams.Select(kvp => string.Format("\"{0}\":\"{1}\"", kvp.Key, string.Join(",", kvp.Value)));
				string json = string.Concat("{", string.Join(",", kvs), "}");

				MDC.Set("Parameters", json);
			}

			//Convert returns to JSON and write to MDC...
			if (0 < m_dictReturns.Count)
			{
				var kvs = m_dictReturns.Select(kvp => string.Format("\"{0}\":\"{1}\"", kvp.Key, string.Join(",", kvp.Value)));
				string json = string.Concat("{", string.Join(",", kvs), "}");

				MDC.Set("Returns", json);
			}

			//Write to the log per the input level...
			string logMessage = "log message";	//NOTE: Due to MDC usage and AdoNetAppender usage, this message is not logged..
			if (Level.Debug == logLevel)
			{
				m_loggerDB.Debug(logMessage);
			}
			else if (Level.Error == logLevel)
			{
				m_loggerDB.Error(logMessage);
			}
			else if (Level.Fatal == logLevel)
			{
				m_loggerDB.Fatal(logMessage);
			}
			else if (Level.Info == logLevel)
			{
				m_loggerDB.Info(logMessage);
			}
			else if (Level.Warn == logLevel)
			{
				m_loggerDB.Warn(logMessage);
			}

			//Processing complete - return
			return;


		}


		//Write an entry to the log table
		public void createLogEntry(HttpContext httpcontextCurrent, DateTime startDtUtc, DateTime endDtUtc, string methodName, string message, Level logLevel)
		{
			//Validate/initialize input parameters...
			if ( null == startDtUtc ||
				 null == endDtUtc ||
				 String.IsNullOrWhiteSpace(methodName) ||
				 String.IsNullOrWhiteSpace(message) ||
				 null == logLevel )
			{
				return;		//Invalid parameter - return early...
			}

			//Retrieve session id, IP address and domain name...
			string sessionId = String.Empty;
			string userIpAddress = String.Empty;
			string domainName = String.Empty;

			getIds(httpcontextCurrent, ref sessionId, ref userIpAddress, ref domainName);

			//If current user authenticated, retrieve user's e-mail address...
			var httpContext = new HttpContextWrapper(System.Web.HttpContext.Current);
			CurrentUser cu = httpContext.Session[httpContext.Session.SessionID] as CurrentUser;

			string eMailAddress = (null != cu && cu.Authenticated) ? cu.UserEmail : "unknown";

			createLogEntry(sessionId, userIpAddress, domainName, eMailAddress, startDtUtc, endDtUtc, methodName, message, logLevel);

			return;
		}

	}
}