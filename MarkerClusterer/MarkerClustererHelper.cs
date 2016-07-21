using HISWebClient.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.MarkerClusterer
{
    public class MarkerClustererHelper
    {
        public int CLUSTERWIDTH; //Cluster region width, all pin within this area are clustered
        public int CLUSTERHEIGHT; //Cluster region height, all pin within this area are clustered

        public MarkerClustererHelper()
        {
          CLUSTERWIDTH = 5; //Cluster region width, all pin within this area are clustered
          CLUSTERHEIGHT = 5; //Cluster region height, all pin within this area are clustered
        }

        /// <summary>
        /// Create geojson from markers (http://www.geojson.org/)
        /// </summary>
        /// <param name="pins"></param>
        /// <param name="zoomlevel"></param>
        /// <param name="centerPoint"></param>
        /// <param name="legend"></param>
        /// <param name="searchParameters"></param>
        /// <param name="assessmentCount"></param>
        /// <returns></returns>
        public string createMarkersGEOJSON(List<ClusteredPin> pins, int zoomlevel, LatLong centerPoint, string legend)
        {
            StringBuilder sb = new StringBuilder();

            //legend = "test";
            //sb.Append("<locations>");
            //sb.Append("{ \"locations\": [");
            //Add zoomlevel information only relevant for forst timt after wards it is 0;
            //sb.Append("<setting zoomlevel=\"" + zoomlevel + "\"  />");

            //var appServiceLayerResponseObject = new SerializedAppResponse();
            //ByBoolRequest bObj = new ByBoolRequest();

            ////get Database connectionstring name
            //bObj.DBConnName = ConfigurationManager.AppSettings["DbConnName"];
            ////get userId for Authenticated user 
            //FormsIdentity formsIdentity = (FormsIdentity)HttpContext.Current.User.Identity;
            //bObj.AuthenticatedUserId = Convert.ToInt32(formsIdentity.Ticket.Name.Split(';')[1]);

            //bObj.BoolParam = true;

            //Hashtable ht = MapClientLibrary.AppServiceLayerObjects.GetAllVisibleFromCustom(bObj);

            int i = 0;
            StringBuilder pinsString = new StringBuilder();
            StringBuilder clusteredpinsString = new StringBuilder();
            StringBuilder polylineString = new StringBuilder();
            StringBuilder polygonString = new StringBuilder();

            foreach (ClusteredPin pin in pins)
            {

                if (pin.PinType.ToString().Contains("point")) //point and clusterpoint 
                {
                    //sb.Append("\"pin\" :[{");
                    if (pin.Count <= 1)
                    {
                        pinsString.Append("{");
                        pinsString.Append("\"type\": \"Feature\",");
                        pinsString.Append("\"id\": \"" + pin.assessmentids[0].ToString() + "\",");
                        pinsString.Append("\"geometry\": {");
                        pinsString.Append("\"type\": \"Point\",");
                        pinsString.Append("\"coordinates\": [" + pin.Loc.Lon.ToString() + "," + pin.Loc.Lat.ToString() + "]},");
                        pinsString.Append("\"properties\": {");
                        pinsString.Append("\"clusterid\":\"" + i.ToString() + "\", ");
                        //pinsString.Append("\"guid\":\"" + pin.AssessmentHeaderData["guid"].ToString() + "\", ");
                        //pinsString.Append("\"sector\":\"" + pin.AssessmentHeaderData["sector"].ToString() + "\", ");
                        //pinsString.Append("\"assessment\":\"" + pin.AssessmentHeaderData["assessment"].ToString() + "\", ");
                        //pinsString.Append("\"dateassessed\":\"" + pin.AssessmentHeaderData["dateassessed"].ToString() + "\", ");
                        //pinsString.Append("\"dateposted\":\"" + pin.AssessmentHeaderData["dateposted"].ToString() + "\", ");
                        //pinsString.Append("\"icontype\":\"" + pin.AssessmentHeaderData["icontype"].ToString() + "\" ");

						//Add service code/title pairs to properties... 
						pinsString.Append("\"serviceCodeToTitle\": {");
						foreach (var serviceCode in pin.ServiceCodeToTitle)
						{
							pinsString.Append(String.Format("\"{0}\": \"{1}\",", serviceCode.Key, serviceCode.Value));
						}
						//Remove trailing ',', if indicated 
						if ( ',' == pinsString[pinsString.Length - 1])
						{
							pinsString.Remove(pinsString.Length - 1, 1);
						}
						pinsString.Append("}");

                        pinsString.Append("}},");
                        ////if ((pins.Count > 1)) sb.Append(",");
                    }
                    else
                    {
                        clusteredpinsString.Append("{");
                        clusteredpinsString.Append("\"type\": \"Feature\",");
                        clusteredpinsString.Append("\"id\": \"" + pin.assessmentids[0].ToString() + "\",");
                        clusteredpinsString.Append("\"geometry\": {");
                        clusteredpinsString.Append("\"type\": \"Point\",");
                        clusteredpinsString.Append("\"coordinates\": [" + pin.Loc.Lon.ToString() + "," + pin.Loc.Lat.ToString() + "]},");
                        clusteredpinsString.Append("\"properties\": {");

                        clusteredpinsString.Append("\"clusterid\":\"" + i.ToString() + "\", ");
                        clusteredpinsString.Append("\"count\":\"" + pin.Count + "\", ");
                        clusteredpinsString.Append("\"icontype\":\"cluster\", ");

						//Add service code/title pairs to properties... 
						clusteredpinsString.Append("\"serviceCodeToTitle\": {");
						foreach (var serviceCode in pin.ServiceCodeToTitle)
						{
							clusteredpinsString.Append(String.Format("\"{0}\": \"{1}\",", serviceCode.Key, serviceCode.Value));
						}
						//Remove trailing ',', if indicated 
						if (',' == clusteredpinsString[clusteredpinsString.Length - 1])
						{
							clusteredpinsString.Remove(clusteredpinsString.Length - 1, 1);
						}
						clusteredpinsString.Append("}");

                        clusteredpinsString.Append("}},");
                    }

                }
                if (pin.PinType.ToString() == "polyline")
                { }
                //if (pin.PinType.ToString() == "polygon")
                //{
                //    //parse answertext to get points
                //    //fix for not having a semicolon on end
                //   // string answertext = pin.AssessmentHeaderData["answertext"];
                //    //if (!answertext.EndsWith(";"))
                //    //{
                //    //    answertext = answertext + ";";
                //    //}

                //    //string[] pts = answertext.Split(';');
                //    string vertices = null;
                //    string[] pt = null;
                //    vertices += "\"coordinates\": [";
                //    for (int j = 0; j < pts.Length - 1; j++)
                //    {
                //        pt = pts[j].Split(',');
                //        vertices += "[" + pt[1].ToString() + ", " + pt[0].ToString() + "],";
                //    }

                //    //add the first one again to complete area.
                //    pt = pts[0].Split(',');
                //    vertices += "[" + pt[1].ToString() + ", " + pt[0].ToString() + "] ";
                //    vertices += " ],";

                //    polygonString.Append("{");
                //    polygonString.Append("\"type\": \"Feature\",");
                //    polygonString.Append("\"id\": \"" + pin.assessmentids[0].ToString() + "\",");
                //    polygonString.Append("\"geometry\": {");
                //    polygonString.Append("\"type\": \"Polygon\",");
                //    polygonString.Append(vertices);
                //    //polygonString.Append("\"coordinates\": [" + pin.Loc.Lat.ToString() + "," + pin.Loc.Lon.ToString() + "]},");
                //    polygonString.Append("\"properties\": {");

                //    polygonString.Append("\"guid\":\"" + pin.AssessmentHeaderData["guid"].ToString() + "\", ");
                //    polygonString.Append("\"sector\":\"" + pin.AssessmentHeaderData["sector"].ToString() + "\", ");
                //    polygonString.Append("\"assessment\":\"" + pin.AssessmentHeaderData["assessment"].ToString() + "\", ");
                //    polygonString.Append("\"dateassessed\":\"" + pin.AssessmentHeaderData["dateassessed"].ToString() + "\", ");
                //    polygonString.Append("\"dateposted\":\"" + pin.AssessmentHeaderData["dateposted"].ToString() + "\", ");
                //    polygonString.Append("\"answertext\":\"" + pin.AssessmentHeaderData["answertext"].ToString() + "\", ");
                //    polygonString.Append("\"strokeColor\": \"#000000\", ");
                //    polygonString.Append("\"strokeWeight\": \"2\", ");
                //    polygonString.Append("\"strokeOpacity\": \"0.8\", ");
                //    polygonString.Append("\"fillColor\": \"#EBEC78\", ");
                //    polygonString.Append("\"fillOpacity\": \"0.50\" ");
                //    polygonString.Append("}}},");

                //    //polygonString.Append("\"polygon\": [{ \"id\": \"" + pin.assessmentids[0].ToString() + "\", " +
                //    //            "\"answertext\": \"" + answertext.ToString() + "\", " +
                //    //            "\"guid\": \"" + pin.AssessmentHeaderData["guid"].ToString() + "\", " +
                //    //            "\"sector\": \"" + pin.AssessmentHeaderData["sector"].ToString() + "\", " +
                //    //            "\"assessment\": \"" + pin.AssessmentHeaderData["assessment"].ToString() + "\", " +
                //    //            "\"dateassessed\": \"" + pin.AssessmentHeaderData["dateassessed"].ToString() + "\", " +
                //    //            "\"dateposted\":\"" + pin.AssessmentHeaderData["dateposted"].ToString() + "\", " +
                //    //            "\"strokeColor\": \"#000000\", " +
                //    //            "\"strokeWeight\": \"2\", " +
                //    //            "\"strokeOpacity\": \"0.8\", " +
                //    //            "\"fillColor\": \"#EBEC78\", " +
                //    //            "\"fillColor\": \"#EBEC78\", " +
                //    //             vertices +
                //    //            " }]");

                //}
                i++;
            }

            //if (pinsString.Length > 0)
            //{
            //    pinsString.Remove(pinsString.Length - 1, 1); // remove last comma
            //    sb.Append(",\"pins\" : [ " + pinsString.ToString() + " ]");
            //}

            //if (clusteredpinsString.Length > 0)
            //{
            //    clusteredpinsString.Remove(clusteredpinsString.Length - 1, 1); // remove last comma
            //    sb.Append(",\"clusteredpins\" : [ " + clusteredpinsString.ToString() + " ]");
            //}


            //built json
            sb.Append("{");
            sb.Append("\"type\": \"FeatureCollection\",");
            sb.Append("\"features\": [");
            //add point features
            // remove last comma if no clustered markers
            if (pinsString.Length > 0) if (clusteredpinsString.Length == 0) { pinsString.Remove(pinsString.Length - 1, 1); }
            //append string for single markers
            sb.Append(pinsString);

            if (clusteredpinsString.Length > 0) if (polygonString.Length == 0) { clusteredpinsString.Remove(clusteredpinsString.Length - 1, 1); }
            //Append String for clustered markers
            //if ((pinsString.Length > 0) && (pins.Count > 1)) sb.Append(",");
            sb.Append(clusteredpinsString);
            if (polygonString.Length > 0) { polygonString.Remove(polygonString.Length - 1, 1); }
            sb.Append(polygonString);
            sb.Append("],");
            sb.Append("\"properties\": {");
            //string lblprogramTitle = (ht["program"] != null) ? ht["program"].ToString() : "program";
            sb.Append("\"zoomlevel\": \"" + zoomlevel + "\",");
            sb.Append("\"centerPoint\": \"" + centerPoint.Lat + " " + centerPoint.Lon + "\",");
            //sb.Append("\"programname\":\"" + lblprogramTitle + ": " + searchParameters.ProgramName + "\", ");

            ////legend = "<i>legend</i>";
            sb.Append("\"legend\" : \"" + legend + "\"");
            sb.Append("}");
            sb.Append("}");


            //sb.Append("]}");
            //sb.Append("</locations>");
            //sb.Replace("& ", "&amp;#38 ");
            string s = sb.ToString();
            //string s = Server.HtmlEncode(sb.ToString());
            return s;
        }

        /// <summary>
        /// Calulate zoomlever for bounds       
        /// </summary>
        /// <param name="maxLat"></param>
        /// <param name="maxLon"></param>
        /// <param name="minLat"></param>
        /// <param name="minLon"></param>
        /// <param name="mapScreenWidth"></param>
        /// <param name="mapScreenHeight"></param>
        /// <returns></returns>
        public int GetZoomlevelForBounds(double maxLat, double maxLon, double minLat, double minLon, int mapScreenWidth, int mapScreenHeight)
        {
            int d = 1; //min resolution
            int e = 17; //min resolution
            int zoomLevel = 0;

            for (int h = e; h > d; h--)
            {
                double Screen_maxlat = Utilities.LatitudeToYAtZoomGoogle(maxLat, h);
                //double Screen_maxlat1 = Utilities.LatitudeToYAtZoomGoogle(maxLat, h);
                double Screen_maxlon = Utilities.LongitudeToXAtZoomGoogle(maxLon, h);
                //double Screen_maxlon1 = Utilities.LongitudeToXAtZoomGoogle(maxLon, h);
                double Screen_minlat = Utilities.LatitudeToYAtZoomGoogle(minLat, h);
                double Screen_minlon = Utilities.LongitudeToXAtZoomGoogle(minLon, h);
                if ((Math.Abs(Screen_maxlat - Screen_minlat) <= mapScreenHeight) && (Math.Abs(Screen_maxlon - Screen_minlon) < mapScreenWidth))
                { zoomLevel = h; break; }
            }
            return zoomLevel;
        }

        /// <summary>
        /// Calulate zoomlevel for bounds
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="mapScreenWidth"></param>
        /// <param name="mapScreenHeight"></param>
        /// <returns></returns>
        public int GetZoomlevelForBounds(Bounds bounds, int mapScreenWidth, int mapScreenHeight)
        {
            int d = 1; //min resolution
            int e = 17; //min resolution
            int zoomLevel = 1;

            for (int h = e; h > d; h--)
            {
                double Screen_maxlat = Utilities.LatitudeToYAtZoomGoogle(bounds.NW.Lat, h);
                //double Screen_maxlat1 = Utilities.LatitudeToYAtZoomGoogle(maxLat, h);
                double Screen_maxlon = Utilities.LongitudeToXAtZoomGoogle(bounds.SE.Lon, h);
                //double Screen_maxlon1 = Utilities.LongitudeToXAtZoomGoogle(maxLon, h);
                double Screen_minlat = Utilities.LatitudeToYAtZoomGoogle(bounds.SE.Lat, h);
                double Screen_minlon = Utilities.LongitudeToXAtZoomGoogle(bounds.NW.Lon, h);
                if ((Math.Abs(Screen_maxlat - Screen_minlat) <= mapScreenHeight) && (Math.Abs(Screen_maxlon - Screen_minlon) < mapScreenWidth))
                { zoomLevel = h; break; }
            }
            return zoomLevel;
        }
        /// <summary>
        /// Get Centerpoint For Bounds
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public LatLong GetCenterpointForBounds(Bounds bounds)
        {
            var latLong = new LatLong();

            latLong.Lat = (bounds.NW.Lat + bounds.SE.Lat) / 2;
            latLong.Lon = (bounds.SE.Lon + bounds.NW.Lon) / 2;


            return latLong;
        }
        /// <summary>
        /// Calculate Bounds for given answertext polygon
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public Bounds GetBoundsFromAnswertext(string txt)
        {
            string[] c = txt.Split(';');
            Bounds polygonbounds = new Bounds();
            if (c.Length > 2)
            {
                for (int i = 0; i < c.Length; i++)
                {
                    string[] d = c[i].Split(',');//order is lng lat

                    if (d[0] != String.Empty) polygonbounds.IncludeInBounds(new Bounds(new LatLong(Convert.ToDouble(d[1]), Convert.ToDouble(d[0])), new LatLong(Convert.ToDouble(d[1]), Convert.ToDouble(d[0]))));

                }
            }
            return polygonbounds;
        }

        //public static List<ClusteredPin> CreatePinsForDatatable(DataTable dt, out Bounds bounds)
        //{
        //    List<ClusteredPin> assessmentpins = new List<ClusteredPin>();
        //    int ordinalReportHeaderId = dt.Columns.IndexOf("ReportHeaderId");
        //    int ordinalGuid = dt.Columns.IndexOf("ReportHeaderGuid");
        //    int ordinalGpslon = dt.Columns.IndexOf("gpslon");
        //    int ordinalGpslat = dt.Columns.IndexOf("gpslat");
        //    int ordinalIcon = -1;
        //    if (dt.Columns.IndexOf("icontype") != -1) ordinalIcon = (dt.Columns.IndexOf("icontype"));
        //    if (dt.Columns.IndexOf("sectorIcon") != -1) ordinalIcon = (dt.Columns.IndexOf("sectorIcon"));
        //    int ordinalSector = dt.Columns.IndexOf("Sectorname");
        //    int ordinalAssessment = dt.Columns.IndexOf("Assessmentname");
        //    int ordinalDateassessed = dt.Columns.IndexOf("dateassessed");
        //    int ordinalDateposted = dt.Columns.IndexOf("dateposted");
        //    //int ordinalquestiontype = dt.Columns.IndexOf("questiontype");
        //    int ordinalAnswertext = dt.Columns.IndexOf("answertext");

        //    //Add to Pins
        //    double
        //        lat = 0,
        //        lon = 0;

        //    int id = 0;

        //    bounds = new Bounds();
        //    int i = 0;
        //    string s = null;
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        if (dr[ordinalAnswertext].ToString() != String.Empty)// polygon
        //        {
        //            NameValueCollection polygonheaderData = new NameValueCollection();
        //            id = Convert.ToInt32(dr[ordinalReportHeaderId].ToString());
        //            polygonheaderData.Add("guid", dr[ordinalGuid].ToString());
        //            polygonheaderData.Add("sector", dr[ordinalSector].ToString());
        //            polygonheaderData.Add("assessment", dr[ordinalAssessment].ToString());
        //            //convert to user spec time zone 5/13/10
        //            DateTime da = Convert.ToDateTime(dr[ordinalDateassessed].ToString());
        //            polygonheaderData.Add("dateassessed", timeZoneAware.ConvertDateTimeFromUtc(da).ToString(timeZoneAware.DateTimeFormat));
        //            DateTime dp = Convert.ToDateTime(dr[ordinalDateposted].ToString());
        //            polygonheaderData.Add("dateposted", timeZoneAware.ConvertDateTimeFromUtc(dp).ToString(timeZoneAware.DateTimeFormat));
        //            //
        //            polygonheaderData.Add("answertext", dr[ordinalAnswertext].ToString());

        //            Bounds polygonbounds = new Bounds();
        //            polygonbounds = GIS.GisHelper.GetBoundsFromAnswertext(dr[ordinalAnswertext].ToString());

        //            assessmentpins.Add(new ClusteredPin(new LatLong(0, 0),
        //                                               polygonbounds, id, "polygon", polygonheaderData));

        //        }
        //        else
        //        {


        //            NameValueCollection headerData = new NameValueCollection();
        //            id = Convert.ToInt32(dr[ordinalReportHeaderId].ToString());
        //            headerData.Add("guid", dr[ordinalGuid].ToString());
        //            headerData.Add("sector", dr[ordinalSector].ToString());
        //            headerData.Add("assessment", dr[ordinalAssessment].ToString());
        //            //convert to user spec time zone 5/13/10
        //            DateTime da = Convert.ToDateTime(dr[ordinalDateassessed].ToString());
        //            headerData.Add("dateassessed", timeZoneAware.ConvertDateTimeFromUtc(da).ToString(timeZoneAware.DateTimeFormat));
        //            DateTime dp = Convert.ToDateTime(dr[ordinalDateposted].ToString());
        //            headerData.Add("dateposted", timeZoneAware.ConvertDateTimeFromUtc(dp).ToString(timeZoneAware.DateTimeFormat));
        //            //
        //            headerData.Add("icontype", dr[ordinalIcon].ToString());
        //            lat = Convert.ToDouble(dr[ordinalGpslat]);
        //            lon = Convert.ToDouble(dr[ordinalGpslon]);



        //            assessmentpins.Add(new ClusteredPin(new LatLong(lat, lon),
        //                                                new Bounds(new LatLong(lat, lon),
        //                                                           new LatLong(lat, lon)), id, "point", headerData));

        //            s += assessmentpins.Count.ToString() + ";" + assessmentpins[i].AssessmentHeaderData[2].ToString();
        //            i++;
        //            ////Determine bounds 

        //            bounds.NW.Lat = Math.Max(bounds.NW.Lat, lat);
        //            bounds.SE.Lon = Math.Max(bounds.SE.Lon, lon);
        //            bounds.SE.Lat = Math.Min(bounds.SE.Lat, lat);
        //            bounds.NW.Lon = Math.Min(bounds.NW.Lon, lon);

        //        }
        //    }
        //    return assessmentpins;
        //}

        /// <summary>
        /// Cluster markers geographically and visually  
        /// </summary>
        /// <param name="pins"></param>
        /// <param name="clusterWidth"></param>
        /// <param name="clusterHeight"></param>
        /// <param name="clusterIncrement"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="maxClusterCount"></param>
        /// <param name="minClusterDistance"></param>
        /// <returns></returns>
        public List<ClusteredPin> clusterPins(List<ClusteredPin> pins, int clusterWidth, int clusterHeight, int clusterIncrement, int zoomLevel, int maxClusterCount, int minClusterDistance)
        {
        
            List<ClusteredPin> clusteredpins = new List<ClusteredPin>();
            for (int i = 1; i < 100; i++)
            {
                CLUSTERWIDTH = i * clusterWidth;
                CLUSTERHEIGHT = i * clusterHeight;
                clusteredpins = cluster(pins, zoomLevel, CLUSTERWIDTH, CLUSTERHEIGHT);

                if (clusteredpins.Count < maxClusterCount)
                {
                    //reduce the density if clusters are to close together
                    clusteredpins = reduceClusterDensity(clusteredpins, zoomLevel, minClusterDistance);

                    break;
                }
                else 
                { 
                    
                    markAllPinsAsNotClustered(pins);  //reset the pins to use unaltered pins  
                }
            }
            return clusteredpins;
        }
        /// <summary>
        /// clusters retrieved pins geographically 
        /// </summary>
        /// <param name="pins"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="clusterWidth"></param>
        /// <param name="clusterHeight"></param>
        /// <returns></returns>
        public List<ClusteredPin> cluster(List<ClusteredPin> pins, int zoomLevel, int clusterWidth, int clusterHeight)
        {
            //sort pins - must be ordered correctly.
            PinXYComparer pinComparer = new PinXYComparer();
            pins.Sort(pinComparer);

            List<ClusteredPin> clusteredPins = new List<ClusteredPin>();

            for (int index = 0; index < pins.Count; index++)
            {
               
                    if ((!pins[index].IsClustered)) //skip already clusted pins
                    {
                        ClusteredPin currentClusterPin = new ClusteredPin();
                        //create our cluster object and add the first pin
                        currentClusterPin.AddPin(pins[index]);
                        pins[index].IsClustered = true;

                        //look backwards in the list for any points within the range that are not already grouped, as the points are in order we exit as soon as it exceeds the range.  
                        addPinsWithinRange(pins, index, -1, currentClusterPin, zoomLevel, clusterWidth, clusterHeight);

                        //look forwards in the list for any points within the range, again we short out.  
                        addPinsWithinRange(pins, index, 1, currentClusterPin, zoomLevel, clusterWidth, clusterHeight);

                        clusteredPins.Add(currentClusterPin);
                    }
                

            }
            return clusteredPins;
        }
        /// <summary>
        /// Adds pins to nearby cluster
        /// </summary>
        /// <param name="pins"></param>
        /// <param name="index"></param>
        /// <param name="direction"></param>
        /// <param name="currentClusterPin"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="clusterWidth"></param>
        /// <param name="clusterHeight"></param>
        private void addPinsWithinRange(List<ClusteredPin> pins, int index, int direction, ClusteredPin currentClusterPin, int zoomLevel, int clusterWidth, int clusterHeight)
        {
            bool finished = false;
            int searchindex;
            searchindex = index + direction;
            while (!finished)
            {
                if (searchindex >= pins.Count || searchindex < 0)
                {
                    finished = true;
                }
                else
                {
                    if (!pins[searchindex].IsClustered)
                    {
                        if (Math.Abs(pins[searchindex].GetPixelX(zoomLevel) - pins[index].GetPixelX(zoomLevel)) < clusterWidth) //within the same x range
                        {
                            if (Math.Abs(pins[searchindex].GetPixelY(zoomLevel) - pins[index].GetPixelY(zoomLevel)) < clusterHeight) //within the same y range = cluster needed
                            {
                                //add to cluster
                                currentClusterPin.AddPin(pins[searchindex]);

                                //stop any further clustering
                                pins[searchindex].IsClustered = true;
                            }
                        }
                        else
                        {
                            finished = true;
                        }
                    };
                    searchindex += direction;
                }
            }
        }
        /// <summary>
        /// Adds pins to nearby cluster for theme
        /// </summary>
        /// <param name="pins"></param>
        /// <param name="index"></param>
        /// <param name="direction"></param>
        /// <param name="currentClusterPin"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="themeParameter"></param>
        private void addPinsWithinRange(List<ClusteredPin> pins, int index, int direction, ClusteredPin currentClusterPin, int zoomLevel, string themeParameter)
        {
            bool finished = false;
            int searchindex;
            searchindex = index + direction;
            while (!finished)
            {
                if (searchindex >= pins.Count || searchindex < 0)
                {
                    finished = true;
                }
                else
                {
                    if (!pins[searchindex].IsClustered)
                    {
                       // if (pins[searchindex].ThemeParameter == themeParameter)//only cluster if parameter are the same 
                        //{
                            if (Math.Abs(pins[searchindex].GetPixelX(zoomLevel) - pins[index].GetPixelX(zoomLevel)) < CLUSTERWIDTH) //within the same x range
                            {
                                if (Math.Abs(pins[searchindex].GetPixelY(zoomLevel) - pins[index].GetPixelY(zoomLevel)) < CLUSTERHEIGHT) //within the same y range = cluster needed
                                {
                                    //add to cluster
                                    currentClusterPin.AddPin(pins[searchindex]);

                                    //stop any further clustering
                                    pins[searchindex].IsClustered = true;
                                }
                            }
                            else
                            {
                                finished = true;
                            }
                        //}
                    };
                    searchindex += direction;
                }
            }
        }
        /// <summary>
        /// Visual clustering. 
        /// </summary>
        /// <param name="pins"></param>
        /// <param name="index"></param>
        /// <param name="direction"></param>
        /// <param name="currentClusterPin"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="minClusterDistance"></param>
        private void addClusteredPinsWithinRange(List<ClusteredPin> pins, int index, int direction, ClusteredPin currentClusterPin, int zoomLevel, int minClusterDistance)
        {
            bool finished = false;
            int searchindex;
            searchindex = index + direction;
            while (!finished)
            {
                if (searchindex >= pins.Count || searchindex < 0)
                {
                    finished = true;
                }
                else
                {
                    if (!pins[searchindex].IsClustered)
                    {
                        if (Math.Abs(pins[searchindex].GetPixelX(zoomLevel) - pins[index].GetPixelX(zoomLevel)) < minClusterDistance) //within the same x range
                        {
                            if (Math.Abs(pins[searchindex].GetPixelY(zoomLevel) - pins[index].GetPixelY(zoomLevel)) < minClusterDistance) //within the same y range = cluster needed
                            {
                                //add to cluster
                                currentClusterPin.CombineClusteredPins(pins[searchindex]);

                                //stop any further clustering
                                pins[searchindex].IsClustered = true;
                            }
                        }
                        else
                        {
                            finished = true;
                        }
                    };
                    searchindex += direction;
                }
            }
        }

        /// <summary>
        /// clusteres visually 
        /// </summary>
        /// <param name="clusteredpins"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="minClusterDistance"></param>
        /// <returns></returns>
        private List<ClusteredPin> reduceClusterDensity(List<ClusteredPin> clusteredpins, int zoomLevel, int minClusterDistance)
        {

            PinXYComparer pinComparer = new PinXYComparer();
            clusteredpins.Sort(pinComparer);
            List<ClusteredPin> pins = new List<ClusteredPin>();
            //reset flag
            //Only cluster already clusteredpoints and do not cluster Alerts
            for (int j = 0; j < clusteredpins.Count; j++)
            {
                if (clusteredpins[j].PinType == "clusterpoint")
                {
                    clusteredpins[j].IsClustered = false;
                }
                else
                {
                    clusteredpins[j].IsClustered = true;
                    pins.Add(clusteredpins[j]);
                }
            }
            for (int i = 0; i < clusteredpins.Count; i++)
            {

                if ((!clusteredpins[i].IsClustered)) //skip already clusted pins
                {
                    ClusteredPin currentClusterPin = new ClusteredPin();
                    //create our cluster object and add the first pin
                    currentClusterPin.AddPin(clusteredpins[i]);


                    //look backwards in the list for any points within the range that are not already grouped, as the points are in order we exit as soon as it exceeds the range.  
                    addClusteredPinsWithinRange(clusteredpins, i, -1, clusteredpins[i], zoomLevel, minClusterDistance);

                    //look forwards in the list for any points within the range, again we short out.  
                    addClusteredPinsWithinRange(clusteredpins, i, 1, clusteredpins[i], zoomLevel, minClusterDistance);

                    clusteredpins[i].IsClustered = true;

                    pins.Add(clusteredpins[i]);
                }

            }
            return pins;
        }
        /// <summary>
        /// resets clustered flag for markers after clustering and the resulting clusters are to many
        /// </summary>
        /// <param name="pins"></param>
        private void markAllPinsAsNotClustered(List<ClusteredPin> pins)
        {
            for (int index = 0; index < pins.Count; index++)//runs through all pins
            {
                pins[index].IsClustered = false;
            }
        }

       

    }
}
   