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
            //SeriesID	SiteName	VariableName	LocalDateTime	DataValue	VarUnits	DataType	SiteID	SiteCode	VariableID	
            //VariableCode	Organization	SourceDescription	SourceLink	Citation	ValueType	TimeSupport	TimeUnits	IsRegular	
            //NoDataValue	UTCOffset	DateTimeUTC	Latitude	Longitude	ValueAccuracy	CensorCode	MethodDescription	QualityControlLevelCode	
            //SampleMedium	GeneralCategory	OffsetValue	OffsetDescription	OffsetUnits	QualifierCode
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