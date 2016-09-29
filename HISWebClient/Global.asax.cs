using HydroServerTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using System.IO;

using System.Diagnostics;

using log4net;
using log4net.Config;

//Apply to the entire assembly...
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "HISWebClient.log4net", Watch = true)]
namespace HISWebClient
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register); // NEW way

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //BC TEST - configure log4net here..
            //Debugger.Launch();
            //var fileinfo = new System.IO.FileInfo("HISWebClient.log4net");
            //var l = log4net.Config.XmlConfigurator.Configure(fileinfo);  
     
            //'Wake Up' log4net...
            //XmlConfigurator.Configure();
            //var fileinfo = new System.IO.FileInfo("HISWebClient.log4net");
            //var l = log4net.Config.XmlConfigurator.Configure(fileinfo);  

            //BC - Test - write a trace message...
//            Trace.TraceInformation("{0} {1} Application started...", DateTime.UtcNow, "Application_Start");
//            Trace.Flush();
        }
    }
}
