using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.MarkerClusterer
{
    [Serializable]
    public class LatLong
    {
        private double lat;
        private double lon;

        public LatLong()
        {
        }

        public LatLong(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public double Lat
        {
            get { return lat; }
            set { lat = value; }
        }

        public double Lon
        {
            get { return lon; }
            set { lon = value; }
        }
    }
}
