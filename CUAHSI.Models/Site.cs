using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CUAHSI.Models
{
    /// <summary>
    /// Representation of a location at which hydrologic observations occur.
    /// </summary>
    [DataContract]
    public class Site
    {
        /// <summary>
        /// Unique identifier of the Site
        /// </summary>
        [DataMember]
        public int SiteID { get; set; }
        
        /// <summary>
        /// Name of the site as defined by its Organization
        /// </summary>
        [DataMember]
        public string SiteName { get; set; }
        
        /// <summary>
        /// Name of institution responsible for operating this observation location..
        /// </summary>
        [DataMember]
        public string OrganizationName { get; set; }

        /// <summary>
        /// WGS-84 Latitude coordinates of this Site
        /// </summary>
        [DataMember]
        public double Lat { get; set; }

        /// <summary>
        /// WGS-84 Longitude coordinates of this Site
        /// </summary>
        [DataMember]
        public double Lng { get; set; }

        /// <summary>
        /// Number of series at this site. Can represent total count or partial count given ontology and temporal constraints, depending on the request querystring.
        /// </summary>
        [DataMember]
        public int SeriesCount { get; set; }

        /// <summary>
        /// Cumulative number of values across all Series included in the SeriesCount.
        /// </summary>
        [DataMember]
        public int ValueCount { get; set; }

        /// <summary>
        /// Initialize site object given ADO.NET object results
        /// </summary>
        /// <param name="data"></param>
        public Site(object[] data)
        {
            SiteID = Convert.ToInt32(data[0]);
            SiteName = data[1].ToString();
            OrganizationName = data[2].ToString();
            Lat = Convert.ToDouble(data[3]);
            Lng = Convert.ToDouble(data[4]);
            SeriesCount = Convert.ToInt32(data[5]);
            ValueCount = Convert.ToInt32(data[6]);
        }

    }
}