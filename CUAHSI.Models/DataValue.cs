using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CUAHSI.Models
{
    [DataContract]
    public class DataValue
    {
        [DataMember]
        public DateTime TimeStamp { get; set; }

        [DataMember]
        public Double Value { get; set; }

        [DataMember]
        public string OffsetType { get; set; }

        [DataMember]
        public double OffsetValue { get; set; }

        [DataMember]
        public double ValueAccuracy { get; set; }

        /// <summary>
        /// Provides data-values level Q/A feature. Used by USGS. Available in ODM for users to comment on their own data. 
        /// </summary>
        [DataMember]
        public string Qualifier { get; set; }

        [DataMember]
        public string CensorCode { get; set; }

        /// <summary>
        /// Default constructor. Sets properties to architecture-specific constant minima values.
        /// </summary>
        public DataValue()
        { 
            TimeStamp = DateTime.MinValue;
            Value = Double.MinValue;
            Qualifier = String.Empty;
            CensorCode = String.Empty;
            ValueAccuracy = 0;
            OffsetType = String.Empty;
        }

        /// <summary>
        /// Given TimeStamp, value, and .NET-compliant timeZoneID. Based on http://msdn.microsoft.com/en-us/library/bb397769.aspx. For timeZoneIDs supported by a particular implementation (note that server implementation is all that matters, as the client receives all values from the web service in UTC): http://msdn.microsoft.com/en-us/library/bb384272.aspx
        /// </summary>
        /// <param name="measureTime">DateTime of the record.</param>        
        /// <param name="measureTimeZoneID">An instance of the .NET-compliant timeZoneID titles specifying the timezone of the measurement.</param>
        public DataValue(DateTime measureTime, string measureTimeZoneID)
        {
            TimeStamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(measureTime, measureTimeZoneID, "UTC"); // returns value in UTC
        }

        /// <summary>
        /// Extracts a Faceted API DataValue object from a HydroDesktop DataValue object
        /// </summary>
        /// <param name="v"></param>
        public DataValue(ServerSideHydroDesktop.ObjectModel.DataValue v)
        {
            TimeStamp = v.DateTimeUTC;
            Value = v.Value;
            ValueAccuracy = v.ValueAccuracy;
            if (v.Qualifier != null)
            {
                Qualifier = v.Qualifier.Code;
            }
            if (v.OffsetType != null)
            {
                OffsetType = v.OffsetType.Description;
            }
            
            OffsetValue = v.OffsetValue;
        }
    }
}