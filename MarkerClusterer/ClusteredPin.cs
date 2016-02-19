using HISWebClient.MarkerClusterer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.MarkerClusterer
{
    /// <summary>
    /// A clustered pin is the basic object required to plot on the map.
    /// It has a location, a type and a bounds that it represents
    /// </summary>
    /// 
    [Serializable]
    public class ClusteredPin
    {
        #region Fields

        private LatLong loc;
        private Bounds clusterArea;
        private bool isClustered;
        private int pixelX;
        private int pixelY;
        private int count = 0;
        private string pinType = null;
        public System.Collections.ArrayList assessmentids = new System.Collections.ArrayList();
        //private string themeParameter = null;

        //private NameValueCollection assessmentHeaderData;

        #endregion

        public ClusteredPin()
        {
            pixelX = -1;
            pixelY = -1;
            ClusterArea = new Bounds();
		
			ServiceCodeToTitle = new Dictionary<string, string>();
        }

        public ClusteredPin(LatLong loc, Bounds clusterArea)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;

			ServiceCodeToTitle = new Dictionary<string, string>();
        }
        public ClusteredPin(LatLong loc, Bounds clusterArea, int assessmentid)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
            assessmentids.Add(assessmentid);

			ServiceCodeToTitle = new Dictionary<string, string>();
        }
        public ClusteredPin(LatLong loc, Bounds clusterArea, int assessmentid, NameValueCollection assessmentHeaderData)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
            assessmentids.Add(assessmentid);
          //  AssessmentHeaderData = assessmentHeaderData;

			ServiceCodeToTitle = new Dictionary<string, string>();
        }
        public ClusteredPin(LatLong loc, Bounds clusterArea, int assessmentid, string pinType, NameValueCollection assessmentHeaderData)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
            assessmentids.Add(assessmentid);
            PinType = pinType;
           // AssessmentHeaderData = assessmentHeaderData;

			ServiceCodeToTitle = new Dictionary<string, string>();
        }

        public ClusteredPin(LatLong loc, Bounds clusterArea, int assessmentid, string pinType, NameValueCollection assessmentHeaderData, string themeParameter)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
            assessmentids.Add(assessmentid);
            PinType = pinType;
          //  AssessmentHeaderData = assessmentHeaderData;
          //  ThemeParameter = themeParameter;

			ServiceCodeToTitle = new Dictionary<string, string>();
        }


        public ClusteredPin(NameValueCollection assessmentHeaderData)
        {

            //AssessmentHeaderData = assessmentHeaderData;

			ServiceCodeToTitle = new Dictionary<string, string>();
        }


        #region Properties

        public LatLong Loc
        {
            get { return loc; }
            set { loc = value; }
        }

        public Bounds ClusterArea
        {
            get { return clusterArea; }
            set { clusterArea = value; }
        }

        public bool IsClustered
        {
            get { return isClustered; }
            set { isClustered = value; }
        }
        public int Count
        {
            get { return count; }
            set { count = value; }
        }
        public string PinType
        {
            get { return pinType; }
            set { pinType = value; }
        }
        //public NameValueCollection AssessmentHeaderData
        //{
        //    get { return assessmentHeaderData; }
        //    set { assessmentHeaderData = value; }
        //}
        //public string ThemeParameter
        //{
        //    get { return themeParameter; }
        //    set { themeParameter = value; }
        //}

		//Maps Service Codes to Service Titles...
		public Dictionary<string, string> ServiceCodeToTitle
		{
			get;

			set;
		}


        #endregion

        /// <summary>
        /// Adds a pin to the cluster
        /// </summary>
        /// <param name="newPin">the pin to add</param>
        public void AddPin(ClusteredPin newPin)
        {
            //This is needed to prevent alert to be sonsidered for clustering
            //if (newPin.AssessmentHeaderData["Sector"].ToLower() == "alert") return;
            if (Loc == null)
            {
                Loc = newPin.Loc;
            }
            ClusterArea.IncludeInBounds(newPin.ClusterArea);

            if ((Count == 0))
            {
               // AssessmentHeaderData = newPin.AssessmentHeaderData;
                pinType = newPin.PinType;
                //themeParameter = newPin.ThemeParameter;
            }
            else
            {
                //AssessmentHeaderData.Clear();
               // AssessmentHeaderData["icontype"] = newPin.AssessmentHeaderData["icontype"];
                pinType = "clusterpoint";
                //themeParameter = newPin.ThemeParameter;
            }

            assessmentids.Add(newPin.assessmentids[0]);
            Count = Count + 1;

			//Merge the two dictionaries into one dictionary
			//Source: http://stackoverflow.com/questions/10559367/combine-multiple-dictionaries-into-a-single-dictionary
			ServiceCodeToTitle = ServiceCodeToTitle.Concat(newPin.ServiceCodeToTitle).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
        }

        /// <summary>
        /// Gets the x pixel location of the pin for the given zoomlevel
        /// location is stored. Assumption is made the zoomlevel does not change for the pin.
        /// </summary>
        /// <param name="zoomLevel">the current zoomlevel of the map</param>
        /// <returns>the x pixel location of the pin</returns>
        public int GetPixelX(int zoomLevel)
        {
            if (pixelX < 0)
            {
                pixelX = Utilities.LongitudeToXAtZoom(Loc.Lon, zoomLevel);
            }
            return pixelX;
        }

        /// <summary>
        /// Gets the y pixel location of the pin for the given zoomlevel
        /// location is stored. Assumption is made the zoomlevel does not change for the pin.
        /// </summary>
        /// <param name="zoomLevel">the current zoomlevel of the map</param>
        /// <returns>the y pixel location of the pin</returns>
        public int GetPixelY(int zoomLevel)
        {
            if (pixelY < 0)
            {
                pixelY = Utilities.LongitudeToXAtZoom(Loc.Lat, zoomLevel);
            }
            return pixelY;
        }

        /// <summary>Combine clustered pins to thin out map
        /// </summary>
        public void CombineClusteredPins(ClusteredPin newPin)
        {
            //This is needed to prevent alert to be considered for clustering            
            //if (newPin.AssessmentHeaderData["Sector"].ToLower() == "alert") return;
            ClusterArea.IncludeInBounds(newPin.ClusterArea);

            if ((Count == 0))
            {
               // AssessmentHeaderData = newPin.AssessmentHeaderData;
                pinType = newPin.PinType;
               // themeParameter = newPin.ThemeParameter;
            }
            else
            {
                //AssessmentHeaderData.Clear();
               // AssessmentHeaderData["icontype"] = newPin.AssessmentHeaderData["icontype"];
                pinType = "clusterpoint";
               // themeParameter = newPin.ThemeParameter;
            }

            assessmentids.AddRange(newPin.assessmentids);
            Count = Count + newPin.Count;

			//Merge the two dictionaries into one dictionary
			//Source: http://stackoverflow.com/questions/10559367/combine-multiple-dictionaries-into-a-single-dictionary
			ServiceCodeToTitle = ServiceCodeToTitle.Concat(newPin.ServiceCodeToTitle).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);

        }
        #region Interfaces
        // Return a copy of the current object.





        #endregion

    }
}
