using HISWebClient.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HISWebClient.DataLayer
{
    public class DataWorker
    {
        public string hisCentralUrl;
        public string webServicesFilename;

        public DataWorker()
        {
            hisCentralUrl = System.Configuration.ConfigurationManager.AppSettings["ServiceUrl"]; // "http://hiscentral.cuahsi.org/webservices/hiscentral.asmx";
            webServicesFilename = "HisServicesList.xml";
        }

        public List<WebServiceNode> getWebServiceList()
        {
            var searcher = new HISCentralSearcher(hisCentralUrl);

            //Create instance
            //var hisCentralWebServicesList = new HisCentralWebServicesList(webServicesFilename);
            //hisCentralWebServicesList.RefreshListFromHisCentral(searcher);
            var xmlData = searcher.GetWebServicesXml(webServicesFilename);

            var webserviceNodeList = getWebserviceNodeList(xmlData);

            return webserviceNodeList;
        }

        public string getOntologyTree(string conceptKeyword)
        {
             var searcher = new HISCentralSearcher(hisCentralUrl);

             var xmlData = searcher.GetOntologyTreeXML(conceptKeyword);

             return xmlData;
        }

        public List<WebServiceNode> filterWebservices (List<WebServiceNode> webserviceNodeList, List<int> serviceIDs)
         {
            var filteredWebserviceNodeList = new List<WebServiceNode>();
            if (serviceIDs != null)
            {
                filteredWebserviceNodeList = (from p in webserviceNodeList
                        where serviceIDs.Contains(p.ServiceID)
                        select p).ToList();
                 

            }
            return filteredWebserviceNodeList;

         }

        public List<BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> getSeriesData(Box extentBox, string[] keywords, double tileWidth, double tileHeight,
                                                        DateTime startDate, DateTime endDate, List<WebServiceNode> serviceList)
        {
                              
            
           
            //filter list allways contains initial element            

            SeriesSearcher seriesSearcher = new HISCentralSearcher(hisCentralUrl);

            var series = seriesSearcher.GetSeriesInRectangle(extentBox, keywords.ToArray(), tileWidth, tileHeight,
                                                              startDate,
                                                              endDate,
                                                              serviceList.ToArray());
            

            return series;
        }
        private List<WebServiceNode> getWebserviceNodeList(string xmlData)
        {
            var list = new List<WebServiceNode>();

            var xmlReaderSettings = new XmlReaderSettings
            {
                CloseInput = true,
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };

            using (var reader = XmlReader.Create(new StringReader(xmlData), xmlReaderSettings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "ServiceInfo")
                        {
                            string desciptionUrl = null;
                            string serviceUrl = null;
                            string title = null;
                            int serviceID = -1;
                            string serviceCode = null;
                            string organization = null;

                            int variables = -1, values = -1, sites = -1;
                            double xmin = double.NaN, xmax = double.NaN, ymin = double.NaN, ymax = double.NaN;

                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "ServiceInfo")
                                {
                                    break;
                                }

                                if (reader.NodeType == XmlNodeType.Element)
                                {
                                    switch (reader.Name)
                                    {
                                        case "Title":
                                            if (!reader.Read()) continue;
                                            title = reader.Value.Trim();
                                            break;
                                        case "ServiceID":
                                            if (!reader.Read()) continue;
                                            serviceID = Convert.ToInt32(reader.Value.Trim());
                                            break;
                                        case "ServiceDescriptionURL":
                                            if (!reader.Read()) continue;
                                            desciptionUrl = reader.Value.Trim();
                                            break;
                                        case "organization":
                                            if (!reader.Read()) continue;
                                            organization = reader.Value.Trim();
                                            break;
                                        case "servURL":
                                            if (!reader.Read()) continue;
                                            serviceUrl = reader.Value.Trim();
                                            break;
                                        case "valuecount":
                                            if (!reader.Read()) continue;
                                            values = Convert.ToInt32(reader.Value.Trim());
                                            break;
                                        case "variablecount":
                                            if (!reader.Read()) continue;
                                            variables = Convert.ToInt32(reader.Value.Trim());
                                            break;
                                        case "sitecount":
                                            if (!reader.Read()) continue;
                                            sites = Convert.ToInt32(reader.Value.Trim());
                                            break;
                                        case "NetworkName":
                                            if (!reader.Read()) continue;
                                            serviceCode = reader.Value.Trim();
                                            break;
                                        case "minx":
                                            if (!reader.Read()) continue;
                                            double.TryParse(reader.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture,
                                                            out xmin);
                                            break;
                                        case "maxx":
                                            if (!reader.Read()) continue;
                                            double.TryParse(reader.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture,
                                                            out xmax);
                                            break;
                                        case "miny":
                                            if (!reader.Read()) continue;
                                            double.TryParse(reader.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture,
                                                            out ymin);
                                            break;
                                        case "maxy":
                                            if (!reader.Read()) continue;
                                            double.TryParse(reader.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture,
                                                            out ymax);
                                            break;
                                    }
                                }
                            }

                            var boundingBox = (Box)null;
                            if (!double.IsNaN(xmin) && !double.IsNaN(xmax) && !double.IsNaN(ymin) && !double.IsNaN(ymax))
                                boundingBox = new Box(xmin, xmax, ymin, ymax);

                            var node = new WebServiceNode(title, serviceCode, serviceID, desciptionUrl, serviceUrl, boundingBox, organization, sites, variables, values);
                            list.Add(node);
                        }
                    }
                }
            }

            return list;

        }

        
    }

}
