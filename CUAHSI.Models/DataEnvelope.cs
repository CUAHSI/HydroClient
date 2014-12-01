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
    /// Object container for results of an ontology-driven search. Holds many collections of time series and their associated metadata.
    /// </summary>
    [DataContract]
    public class DataEnvelope
    {
        [DataMember]
        public List<SeriesData> series { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataEnvelope()
        {
            series = new List<SeriesData>();
        }

        /// <summary>
        /// Create container for returning data valeus from one or many different series
        /// </summary>
        /// <param name="datum"></param>
        public DataEnvelope(SeriesData datum)
        {
            series = new List<SeriesData>() { datum }; 
        }

        /// <summary>
        /// Create container for returning data values from many different series
        /// </summary>
        /// <param name="data"></param>
        public DataEnvelope(List<SeriesData> data)
        {
            series = data;
        }
    }
}