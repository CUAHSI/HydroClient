using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Configuration;
using System.Net;

using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace HISWebClient.Util
{

	/// <summary>
	/// A base class for use with the log4net AdoNetAppender...
	/// </summary>
	public abstract class DbBaseContext
	{

		//members...
		protected Dictionary<string, string> m_dictParams = new Dictionary<string, string>();

		protected ILog m_loggerDB;

		protected AdoNetAppender m_appender;

		protected string m_localConnectionString;

		protected string m_deployConnectionString;

		//Initializing constructor
		protected DbBaseContext(string loggerName, string adoNetAppenderName, string localConnectionStringKey, string deployConnectionStringKey)
		{
			//Validate/initialize input parameters...
			if ( String.IsNullOrWhiteSpace(loggerName) || 
				 String.IsNullOrWhiteSpace(adoNetAppenderName) ||
				 String.IsNullOrWhiteSpace(localConnectionStringKey) ||
				 String.IsNullOrWhiteSpace(deployConnectionStringKey) )
			{
				throw new ArgumentNullException("Empty parameter!!");
			}

			//Retrieve the logger instance...
			m_loggerDB = LogManager.GetLogger(loggerName);
			if (null == m_loggerDB)
			{
				throw new KeyNotFoundException(String.Format("Log4net logger: {0} NOT found!!", loggerName)); //Logger not found!!
			}

			//Retrieve the appender instance...
			m_appender = findAppender(adoNetAppenderName);
			if (null == m_appender)
			{
				throw new KeyNotFoundException(String.Format("Log4net AdoNetAppender: {0} NOT found!!", adoNetAppenderName)); //Appender not found!!
			}

			//Retrieve the local connection string...
			ConnectionStringSettings css = ConfigurationManager.ConnectionStrings[localConnectionStringKey];
			if (null != css)
			{
				m_localConnectionString = css.ConnectionString;
			}
			else
			{
				throw new KeyNotFoundException(String.Format("Connection String Key: {0} NOT found!!", localConnectionStringKey)); //Local connection string not found!!
			}

			//Retrieve the deploy connection string...
			css = ConfigurationManager.ConnectionStrings[deployConnectionStringKey];
			if (null != css)
			{
				m_deployConnectionString = css.ConnectionString;
			}
			else
			{
				throw new KeyNotFoundException(String.Format("Connection String Key: {0} NOT found!!", deployConnectionStringKey)); //Deploy connection string not found!!
			}

			//Set the appender's connection string...
			m_appender.ConnectionString = GetConnectionString();
			m_appender.ActivateOptions();
		}

		//methods

		//Source: http://mylifeandcode.blogspot.com/2012/12/setting-log4net-adonetappender.html
		private AdoNetAppender findAppender(string appenderName)
		{
			//Validate/initialize input parameters...
			if (String.IsNullOrWhiteSpace(appenderName))
			{
				return null;
			}

			Hierarchy hierarchy = LogManager.GetRepository() as Hierarchy;
			if (null != hierarchy)
			{
				AdoNetAppender appender = (AdoNetAppender)hierarchy.GetAppenders()
											.Where(x => x.GetType() == typeof(AdoNetAppender) && appenderName == x.Name)
											.FirstOrDefault();
				return appender;
			}

			return null;
		}

		//Retrieve connection string per current run-time environment...
		//source: http://cloudmonix.com/blog/how-to-check-if-code-is-running-on-azure-webapps/
		//		  Other Azure environment variables of possible interest:
		//			WEBSITE_HOSTNAME
		//			WEBSITE_IIS_SITE_NAME
		//			WEBSITE_OWNER_NAME
		private string GetConnectionString()
		{
			if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
			{
				//Local environment...
				return m_localConnectionString;
			}
			else
			{
				//Deployed environment...
				return m_deployConnectionString;
			}

//			return m_deployConnectionString;
		}

		public void getIds(HttpContext httpcontextCurrent, ref string sessionId, ref string userIpAddress, ref string domainName)
		{
			if ( null == httpcontextCurrent)
			{
				return;	//Invalid parameter - return early...
			}

			//Retrieve Session Id
			var httpContext = new HttpContextWrapper(httpcontextCurrent);
			sessionId = httpContext.Session.SessionID;

			//Retrieve user's IP address and domain name...
			userIpAddress = ContextUtil.GetIPAddress(System.Web.HttpContext.Current);

			try
			{
				IPHostEntry host = Dns.GetHostEntry(userIpAddress);
				domainName = host.HostName;
			}
			catch (Exception exceptionDNS)
			{
				domainName =  exceptionDNS.Message;
			}

			//Processing complete - return
			return;
		}


		//interface...
		public void clearParameters()
		{
			m_dictParams.Clear();
		}

		public void addParameter<Type>(string name, Type parameter)
		{
			if (!String.IsNullOrWhiteSpace(name) && null != parameter)
			{
				m_dictParams.Add(name, parameter.ToString());
			}
		}
	}
}