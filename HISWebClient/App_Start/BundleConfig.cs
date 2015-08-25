using System.Web;
using System.Web.Optimization;

namespace HISWebClient
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
			bundles.Add(new ScriptBundle("~/bundles/Google").Include(
						"~/Scripts/Google/markerwithlabel.js"));

			bundles.UseCdn = true;   //enable CDN support

			//NO bundle for the Google Map API!!
			//NOTE: 20-Aug-2015 - Use of the local mapsapi.js file can result in a 403 error!!
			//					  Apparently this usage violates Google's policy that the maps API must be downloaded from their servers.  
			// More information: https://developers.google.com/maps/documentation/javascript/error-messages?hl=en
			// Error:			 NotLoadingAPIFromGoogleMapError

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
						"~/Scripts/jquery.dataTables.js",
						"~/Scripts/jquery.slidereveal.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

			//Fancytree bundle...
			bundles.Add(new ScriptBundle("~/bundles/fancytree").Include(
						"~/Scripts/Fancytree/jquery-ui.custom.js",
						"~/Scripts/Fancytree/jquery.fancytree.js",
						"~/Scripts/Fancytree/jquery.fancytree.filter.js",
						"~/Scripts/Fancytree/jquery.fancytree.childcounter.js",
						"~/Scripts/Fancytree/sample.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/bootbox.js",
                      "~/Script/moment.js",
					  "~/Scripts/bootstrap-datepicker.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/Custom").Include(
                      "~/Scripts/Custom/webclient.js",
					  //"~/Scripts/Custom/webclient.min.js",
					  "~/Scripts/Custom/randomId.js",
					  //"~/Scripts/Custom/randomId.min.js",
					  "~/Scripts/Custom/freezeEnum.js",
					  //"~/Scripts/Custom/freezeEnum.min.js",
					  "~/Scripts/Custom/TimeSeriesRequestStatus.js",
					  //"~/Scripts/Custom/TimeSeriesRequestStatus.min.js",
					  "~/Scripts/Custom/jquery-filedownload.js"
                      ));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/bootstrap-datepicker3.css",
					  "~/Content/FancyTree/skin-xp/ui.fancytree.css",
                      "~/Content/site.css"
                     ));

			//add link to DataTables CDN
			//NOTE: The system references the CDN Path when in web.config <compilation debug="false"> is set.
			//		The system references the local .Include(...) path when in web.config <compilation debug="true"> is set!!
			//Source: http://stackoverflow.com/questions/17083864/cdn-path-is-not-working-in-js-bundling-in-mvc
			var datatablesCdnPath = "//cdn.datatables.net/1.10.6/css/jquery.dataTables.min.css";

			bundles.Add(new StyleBundle("~/Content/datatablescdn",
						datatablesCdnPath).Include("~/Content/datatablescdn/jquery.dataTables.min.css"));

            // Set EnableOptimizations to false for debugging. For more information,
            // visit http://go.microsoft.com/fwlink/?LinkId=301862
            BundleTable.EnableOptimizations = false;

			//BundleTable.EnableOptimizations = true;

        }
    }
}
