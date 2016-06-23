using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
    public class DataValueCsvViewModel
    {
        //Initializing constructor
        public DataValueCsvViewModel() { }

        //Copy constructor...
        public DataValueCsvViewModel(DataValueCsvViewModel tsvm)
        {
			Id = tsvm.Id;
			SiteName = tsvm.SiteName;
			VariableName = tsvm.VariableName;
			LocalDateTime = tsvm.LocalDateTime;        
			DataValue = tsvm.DataValue;
			VarUnits = tsvm.VarUnits;
			DataType = tsvm.DataType;
			SiteID = tsvm.SiteID;
			SiteCode = tsvm.SiteCode;
			VariableID = tsvm.VariableID;
			VariableCode = tsvm.VariableCode;
			Organization = tsvm.Organization;
			SourceDescription = tsvm.SourceDescription;
			SourceLink = tsvm.SourceLink;
			Citation = tsvm.Citation;
			ValueType = tsvm.ValueType;
			TimeSupport = tsvm.TimeSupport;
			TimeUnits = tsvm.TimeUnits;
			IsRegular = tsvm.IsRegular;
			NoDataValue = tsvm.NoDataValue;
			UTCOffset = tsvm.UTCOffset;
			DateTimeUTC = tsvm.DateTimeUTC;
			Latitude = tsvm.Latitude;
			Longitude = tsvm.Longitude;
			ValueAccuracy = tsvm.ValueAccuracy;
			CensorCode = tsvm.CensorCode;
			MethodDescription = tsvm.MethodDescription;
			QualityControlLevelCode = tsvm.QualityControlLevelCode;
			SampleMedium = tsvm.SampleMedium;
			GeneralCategory = tsvm.GeneralCategory;
			OffsetValue = tsvm.OffsetValue;
			OffsetDescription = tsvm.OffsetDescription;
			OffsetUnits = tsvm.OffsetUnits;
			QualifierCode = tsvm.QualifierCode;
        }

        //Seriesid
        public string Id { get; set; }
        // Name of Site
        public string SiteName { get; set; }
        //Name of Variable
        public string VariableName { get; set; }
        //LocalDateTime
        public string LocalDateTime { get; set; }        
        //DataValue	
        public string DataValue { get; set; }
        //VarUnits	
        public string VarUnits { get; set; }
        //DataType	
        public string DataType { get; set; }
        //SiteID	
        public string SiteID { get; set; }
        //SiteCode	
        public string SiteCode { get; set; }
        //VariableID	
        public string VariableID { get; set; }
        //VariableCode	
        public string VariableCode { get; set; }
        //Organization
        public string Organization { get; set; }
        //SourceDescription	
        public string SourceDescription { get; set; }
        //SourceLink	
        public string SourceLink { get; set; }
        //Citation	
        public string Citation { get; set; }
        //ValueType	
        public string ValueType { get; set; }
        //TimeSupport	
        public string TimeSupport { get; set; }
        //TimeUnits	
        public string TimeUnits { get; set; }
        //IsRegular	
        public string IsRegular { get; set; }
        //NoDataValue	
        public string NoDataValue { get; set; }
        //UTCOffset	
        public string UTCOffset { get; set; }
        //DateTimeUTC	
        public string DateTimeUTC { get; set; }
        //Latitude	
        public string Latitude { get; set; }
        //Longitude	
        public string Longitude { get; set; }
        //ValueAccuracy	
        public string ValueAccuracy { get; set; }
        //CensorCode	
        public string CensorCode { get; set; }
        //MethodDescription	
        public string MethodDescription { get; set; }
        //QualityControlLevelCode	
        public string QualityControlLevelCode { get; set; }
        //SampleMedium	
        public string SampleMedium { get; set; }
        //GeneralCategory	
        public string GeneralCategory { get; set; }
        //OffsetValue	
        public string OffsetValue { get; set; }
        //OffsetDescription	
        public string OffsetDescription { get; set; }
        //OffsetUnits	
        public string OffsetUnits { get; set; }
        //QualifierCode
        public string QualifierCode { get; set; }
    }
}