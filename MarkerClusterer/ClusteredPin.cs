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
        private string themeParameter = null;

        //private NameValueCollection assessmentHeaderData;

        #endregion

        public ClusteredPin()
        {
            pixelX = -1;
            pixelY = -1;
            ClusterArea = new Bounds();
        }

        public ClusteredPin(LatLong loc, Bounds clusterArea)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
        }
        public ClusteredPin(LatLong loc, Bounds clusterArea, int assessmentid)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
            assessmentids.Add(assessmentid);

        }
        public ClusteredPin(LatLong loc, Bounds clusterArea, int assessmentid, NameValueCollection assessmentHeaderData)
        {
            pixelX = -1;
            pixelY = -1;
            Loc = loc;
            ClusterArea = clusterArea;
            assessmentids.Add(assessmentid);
          //  AssessmentHeaderData = assessmentHeaderData;

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

        }


        public ClusteredPin(NameValueCollection assessmentHeaderData)
        {

            //AssessmentHeaderData = assessmentHeaderData;

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

        }
        #region Interfaces
        // Return a copy of the current object.





        #endregion

    }
}
