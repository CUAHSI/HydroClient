using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CUAHSI.Models
{
    /// <summary>
    /// Container for SeriesID, Latitude, Longitude
    /// </summary>
    public class SeriesReferenceTriple
    {
        public SeriesReferenceTriple() { }
        
        /// <summary>
        /// Generate SeriesReference triple from comma-delimited string of format {SeriesID},{Latitude},{Longitude}.
        /// </summary>
        /// <param name="commaDelimitedSeriesReferenceString"></param>
        public SeriesReferenceTriple(string commaDelimitedSeriesReferenceString)
        {
            string[] items = commaDelimitedSeriesReferenceString.Split(',');
            if (items.Length != 3) {
                throw new NotSupportedException("Series must be specified as comma-delimited strings of {SeriesID},{Latitude},{Longitude}");
            }
            int s;
            double lat;
            double lng;
            if (!Int32.TryParse(items[0], out s))
            {
                throw new NotSupportedException("SeriesIDs must be integers");
            }
            if (!Double.TryParse(items[1], out lat))
            {
                throw new NotSupportedException("Latitude must be of type double");
            }
            if (!Double.TryParse(items[2], out lng))
            {
                throw new NotSupportedException("Longitude must be of type double");
            }
            this.SeriesID = s;
            this.Lat = lat;
            this.Lng = lng;
        }

        public int SeriesID { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }

    }
}