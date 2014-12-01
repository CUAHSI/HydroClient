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
    /// Representation of hydrologic observation system of Sites
    /// </summary>
    [DataContract]
    public class HISNetwork
    {
        /// <summary>
        /// Unique identifier of HISNetwork.
        /// </summary>
        [DataMember]
        public int NetworkID { get; set; }
        
        [DataMember]
        public string NetworkName { get; set; }

        [DataMember]
        public string NetworkTitle { get; set; }
        
        /// <summary>
        /// WaterOneFlow web services endpoint for acc
        /// </summary>
        [DataMember]
        public string ServiceWSDL { get; set; }

        [DataMember]
        public string ServiceAbstract { get; set; }

        [DataMember]
        public string ContactName { get; set; }

        [DataMember]
        public string ContactEmail { get; set; }

        [DataMember]
        public string ContactPhone { get; set; }

        [DataMember]
        public string OrganizationName { get; set; }
        
        [DataMember]
        public string Website { get; set; }

        [DataMember]
        public Boolean SupportsAllMethods { get; set; }

        [DataMember]
        public string Citation { get; set; }

        [DataMember]
        public Boolean FrequentUpdates { get; set; }

        [DataMember]
        public byte[] logo { get; set; }

        [DataMember]
        public byte[] icon { get; set; }

        [DataMember]
        public double Xmin { get; set; }

        [DataMember]
        public double Xmax { get; set; }

        [DataMember]
        public double Ymin { get; set; }

        [DataMember]
        public double Ymax { get; set; }

        [DataMember]
        public double ValueCount { get; set; }

        [DataMember]
        public double SiteCount { get; set; }
        
    }
}