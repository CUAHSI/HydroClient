using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using HISWebClient.Util;

namespace CUAHSI.Models
{
    [DataContract]
    public class DataValue
    {
        [DataMember]
        public DateTime TimeStamp { get; set; }

		//BCC - 27-Sep-2016 - Never referenced - commented out...
		//[DataMember]
		//public DateTime TimeStampUTC { get; set; }

		//BCC - 27-Sep-2016 - Never referenced - commented out...
		//[DataMember]
		//public DateTime TimeStampLocal { get; set; }

        [DataMember]
        public DateTime UTCTimeStamp { get; set; }

        [DataMember]
        public DateTime LocalTimeStamp { get; set; }

        [DataMember]
        public double UTCOffset { get; set; }

        [DataMember]
        public Double Value { get; set; }

        [DataMember]
        public string OffsetType { get; set; }

        [DataMember]
        public double OffsetValue { get; set; }

        [DataMember]
        public string OffsetDescription { get; set; }

        [DataMember]
        public string OffsetUnit { get; set; }

        [DataMember]
        public double ValueAccuracy { get; set; }

        /// <summary>
        /// Provides data-values level Q/A feature. Used by USGS. Available in ODM for users to comment on their own data. 
        /// </summary>
        [DataMember]
        public string Qualifier { get; set; }

		[DataMember]
		public string QualifierDescription { get; set; }

		//[DataMember]
        //public string Speciation { get; set; }

        [DataMember]
        public string CensorCode { get; set; }

		//BCC - 12-Aug-2016 - Model quality control level, lab sample, source and method codes as in WaterML...
		public string LabSampleCode { get; set; }

		public string MethodCode { get; set; }

		public string QualityControlLevelCode { get; set; }

		public string SourceCode { get; set; }

		//BCC - Add reference to parent object...
		public SeriesData SeriesData { get; set; } 

		/// <summary>
        /// Default constructor. Sets properties to architecture-specific constant minima values.
        /// </summary>
        public DataValue()
        {
            TimeStamp = DateTime.MinValue;
            Value = Double.MinValue;
            Qualifier = String.Empty;
			QualifierDescription = String.Empty;	//10-Aug-2015 - BCC - GitHub Issue #33 - Include Qualifier Description in downloaded time series data
            CensorCode = String.Empty;
            ValueAccuracy = 0;
            OffsetType = String.Empty;

			LabSampleCode = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
			MethodCode = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
			QualityControlLevelCode = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
			SourceCode = ServerSideHydroDesktop.ObjectModel.ValueTypeCV.Unknown.GetEnumDescription();
		}

        /// <summary>
        /// Given TimeStamp, value, and .NET-compliant timeZoneID. Based on http://msdn.microsoft.com/en-us/library/bb397769.aspx. For timeZoneIDs supported by a particular implementation (note that server implementation is all that matters, as the client receives all values from the web service in UTC): http://msdn.microsoft.com/en-us/library/bb384272.aspx
        /// </summary>
        /// <param name="measureTime">DateTime of the record.</param>        
        /// <param name="measureTimeZoneID">An instance of the .NET-compliant timeZoneID titles specifying the timezone of the measurement.</param>
        public DataValue(DateTime measureTime, string measureTimeZoneID) : this()
        {
            TimeStamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(measureTime, measureTimeZoneID, "UTC"); // returns value in UTC
        }

        /// <summary>
        /// Extracts a Faceted API DataValue object from a HydroDesktop DataValue object
        /// </summary>
        /// <param name="v"></param>
        public DataValue(ServerSideHydroDesktop.ObjectModel.DataValue v, SeriesData sd = null) : this()
        {
            UTCTimeStamp = v.DateTimeUTC;
            UTCOffset = v.UTCOffset;
            LocalTimeStamp = v.LocalDateTime;
            Value = v.Value;
            ValueAccuracy = v.ValueAccuracy;
            if (v.Qualifier != null)
            {
                Qualifier = v.Qualifier.Code;
				//10-Aug-2015 - BCC - GitHub Issue #33 - Include Qualifier Description in downloaded time series data
				QualifierDescription = v.Qualifier.Description;
            }
            if (v.OffsetType != null)
            {
                OffsetDescription = v.OffsetType.Description;
                OffsetUnit = v.OffsetType.Unit.ToString();
            }
            CensorCode = v.CensorCode;
            OffsetValue = v.OffsetValue;

			if (null != v.Series)
			{
				//TO DO - LabSampleCode...
	
				if (null != v.Series.Method)
				{
					MethodCode = v.Series.Method.Code.ToString();
				}

				if (null != v.Series.QualityControlLevel)
				{
					QualityControlLevelCode = v.Series.QualityControlLevel.Code.ToString();
				}

				if (null != v.Series.Source)
				{
					//TO DO - Does this work for WaterML 1.0 and 1.1?
					SourceCode = v.Series.Source.OriginId.ToString();
				}
			}

			SeriesData = sd;
		}
    }
}