using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.WebHost;
using System.Web.Routing;
using System.Web.SessionState;

namespace HydroServerTools
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);


			//BCC - 13-Oct-2015 - Custom route for RESTful Api
			//		callable as: http://<base URL>/CUAHSI/HydroClient/WaterOneFlowArchive/{id}
			//		 where <base URL> can be, for example, 'localhost:nnnnn' or 'data.cuahsi.org'
			//Source: http://stackoverflow.com/questions/10766157/how-to-make-more-maphttproutes-for-mvc-4-api
			config.Routes.MapHttpRoute(
				name: "WaterOneFlowApi",
				//routeTemplate: "CUAHSI/HydroClient/WaterOneFlowArchive/{id}",		//Remove the controller and action from here - since they are listed in the defaults...
				routeTemplate: "CUAHSI/HydroClient/WaterOneFlowArchive/{fileName}/{fileExtension}",		//Remove the controller and action from here - since they are listed in the defaults...
				defaults: new { controller = "RESTfulInterface", action = "Get", fileExtension = "zip" });

            RouteTable.Routes.MapHttpRoute(
               name: "DefaultApi",
				//routeTemplate: "api/{controller}/{id}",
			   routeTemplate: "api/{controller}/{action}/{id}",		//BCC - Changing the template to avoid conflicts with MVC routes...
               defaults: new { id = RouteParameter.Optional }
               ).RouteHandler = new SessionRouteHandler();
        }
        //added to support Session
        public class SessionControllerHandler : HttpControllerHandler, IRequiresSessionState
        {
            public SessionControllerHandler(RouteData routeData)
                : base(routeData)
            { }
        }
        public class SessionRouteHandler : IRouteHandler
        {
            IHttpHandler IRouteHandler.GetHttpHandler(RequestContext requestContext)
            {
                return new SessionControllerHandler(requestContext.RouteData);
            }
        }
    }
}
