using HISWebClient.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    class DbWebServicesList : IWebServicesList
    {
        public IEnumerable<WebServiceNode> GetWebServices()
        {
            //added to fix compile problem needs tobe implemented correctly!!
            //var searcher = new HISCentralSearcher("");

            var result = new List<WebServiceNode>();

            string desciptionUrl = null;
            string serviceUrl = null;
            string title = null;
            int serviceID = -1;
            string serviceCode = null;
            string organization = null;

            int variables = -1, values = -1, sites = -1;
            double xmin = double.NaN, xmax = double.NaN, ymin = double.NaN, ymax = double.NaN;

            var boundingBox = (Box)null;

            var node = new WebServiceNode(title, serviceCode, serviceID, desciptionUrl, serviceUrl, boundingBox, organization, sites, variables, values);
            result.Add(node);

            return result.ToList();
            //return new MetadataCacheSearcher().GetWebServices().Select(
            //    service =>
            //    new WebServiceNode(service.ServiceTitle,
            //                       service.ServiceCode, (int)service.Id, service.DescriptionURL, service.EndpointURL,
            //                       new Box(service.WestLongitude, service.EastLongitude,
            //                               service.SouthLatitude, service.NorthLatitude), service.SiteCount, service.VariableCount, (int)service.ValueCount)).ToList();
        }
    }
}
