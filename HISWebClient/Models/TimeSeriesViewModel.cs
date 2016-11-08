﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Text;

namespace HISWebClient.Models
{
	[Serializable]
    public class TimeSeriesViewModel
    {
        //Initializing constructor
        public TimeSeriesViewModel() {}

        //Copy constructor...
        public TimeSeriesViewModel(TimeSeriesViewModel tsvm)
        {
            SeriesId = tsvm.SeriesId;
            ServCode = tsvm.ServCode;
			ServTitle = tsvm.ServTitle;
            ServURL = tsvm.ServURL;
            SiteCode = tsvm.SiteCode;
            VariableCode = tsvm.VariableCode;
            VariableName = tsvm.VariableName;
            BeginDate = tsvm.BeginDate;
            EndDate = tsvm.EndDate;
            ValueCount = tsvm.ValueCount;
            SiteName = tsvm.SiteName;
            Latitude = tsvm.Latitude;
            Longitude = tsvm.Longitude;
            DataType = tsvm.DataType;
            ValueType = tsvm.ValueType;
            SampleMedium = tsvm.SampleMedium;
            TimeUnit = tsvm.TimeUnit;
			//BCC - 08-Aug-2016 - Enable general category assignment
            GeneralCategory = tsvm.GeneralCategory;
            TimeSupport = tsvm.TimeSupport;
            ConceptKeyword = tsvm.ConceptKeyword;
			//BCC - 15-Oct-29015 -  Suppress display of IsRegular
			//IsRegular = tsvm.IsRegular;
            VariableUnits = tsvm.VariableUnits;
			Citation = tsvm.Citation;
            Organization = tsvm.Organization;

			QCLID = tsvm.QCLID;

			QCLDesc = tsvm.QCLDesc;

			SourceOrg = tsvm.SourceOrg;

			SourceId = tsvm.SourceId;

			MethodId = tsvm.MethodId;

			MethodDesc = tsvm.MethodDesc;

        }

        public int SeriesId { get; set; }
        /// <summary>
        /// code of the web service
        /// </summary>
        public string ServCode { get; set; }
        /// <summary>
		/// title of the web service
		/// </summary>
		public string ServTitle { get; set; }
        /// <summary>
        /// URL of the web service (WSDL)
        /// </summary>
        public string ServURL { get; set; }
        /// <summary>
        /// The full site code (NetworkCode:SiteCode)
        /// </summary>
        public string SiteCode { get; set; }
        /// <summary>
        /// The full variable code (VocabularyPrefix:Variable)
        /// </summary>
        public string VariableCode { get; set; }
        /// <summary>
        /// Name of the variable
        /// </summary>
        public string VariableName { get; set; }
        /// <summary>
        /// Begin date (date of first available observation in the series)
        /// </summary>
        public DateTime BeginDate { get; set; }
        /// <summary>
        /// End date (date of last available observation in the series)
        /// </summary>
        public DateTime EndDate { get; set; }
        /// <summary>
        /// Total number of DataValues provided by the service
        /// </summary>
        public int ValueCount { get; set; }
        /// <summary>
        /// Then name of the site
        /// </summary>
        public string SiteName { get; set; }
        /// <summary>
        /// Latitude of the site
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// Longitude of the site
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// Data type of the values in the series (average, minimum, maximum..)
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// Value Type
        /// </summary>
        public string ValueType { get; set; }
        /// <summary>
        /// Sample Medium (water, air, other, not applicable)
        /// </summary>
        public string SampleMedium { get; set; }
        /// <summary>
        /// The time unit of the time support period
        /// </summary>
        public string TimeUnit { get; set; }
        /// <summary>
        /// The general category
        /// </summary>
        public string GeneralCategory { get; set; }
        /// <summary>
        /// The time support. This is the length
        /// of the period following the value DateTime
        /// for which the value is valid
        /// </summary>
        public double TimeSupport { get; set; }
        /// <summary>
        /// This is the concept keyword returened by HIS Central
        /// If the variable is not registered, then an empty
        /// keyword is returned.
        /// </summary>
        public string ConceptKeyword { get; set; }

		//BCC - 15-Oct-29015 -  Suppress display of IsRegular
		//public bool IsRegular { get; set; }

        public string VariableUnits { get; set; }

        public string Citation { get; set; }

        //Organization to be retrived from service metadata
        public string Organization { get; set; }

		//BCC - 07-Sep-2016 - Add additional fields for use with GetSeriesCatalogForBox3...
		public string QCLID { get; set; }

		public string QCLDesc { get; set; }

		public string SourceOrg { get; set; }

		public string SourceId { get; set; }

		public string MethodId { get; set; }

		public string MethodDesc { get; set; }

        //Override ToString method for logging...
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("SeriesId: {0} ~" +
                            "ServCode: {1} ~" +
							"ServTitle: {2} ~" +
                            "ServURL: {3} ~" +
                            "SiteCode: {4} ~" +
                            "VariableCode: {5} ~" +
                            "VariableName: {6} ~" +
                            "BeginDate: {7} ~" +
                            "EndDate: {8} ~" +
                            "ValueCount: {9} ~" +
                            "SiteName: {10} ~" +
                            "Latitude: {11} ~" +
                            "Longitude: {12} ~" +
                            "DataType: {13} ~" +
                            "ValueType: {14} ~" +
                            "SampleMedium: {15} ~" +
                            "TimeUnit: {16} ~" +
                            "GeneralCategory: {17} ~" +
                            "TimeSupport: {18} ~" +
                            "ConceptKeyword: {19} ~" +
							//BCC - 15-Oct-29015 -  Suppress display of IsRegular
							//"IsRegular: {20} ~" +
                            "VariableUnits: {21} ~" +
                            "Citation: {22} ~" +
                            "Organization: {23}", 
                            SeriesId,
                            ServCode,
							ServTitle,
                            ServURL,
                            SiteCode,
                            VariableCode,
                            VariableName,
                            BeginDate,
                            EndDate,
                            ValueCount,
                            SiteName,
                            Latitude,
                            Longitude,
                            DataType,
                            ValueType,
                            SampleMedium,
                            TimeUnit,
                            GeneralCategory,
                            TimeSupport,
                            ConceptKeyword,
							//BCC - 15-Oct-29015 -  Suppress display of IsRegular
							//IsRegular,
                            VariableUnits,
                            Citation,
                            Organization );

            return (sb.ToString());
        }
    }
}