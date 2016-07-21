using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CUAHSI.Models
{
    public class SeriesMetadata
    {
        public SeriesMetadata()
        { }
        
        [Obsolete("Deprecated in favor of async object[] result")]
        /// <summary>
        /// Maps to result of query with parameter SQL.SERIESRESULTS
        /// HISNetworks.NetworkName AS ServCode, 0
        /// HISNetworks.ServiceWSDL AS ServURL,  1
        /// SeriesCatalog.SiteCode,              2
        /// SeriesCatalog.VariableCode,          3
        /// SeriesCatalog.SiteName,              4
        /// SeriesCatalog.VariableName,          5
        /// SeriesCatalog.BeginDateTime AS StartDate,  6
        /// SeriesCatalog.EndDateTime AS EndDate,      7
        /// SeriesCatalog.Valuecount,                  8
        /// Sites.Latitude,                            9
        /// Sites.Longitude,                          10
        /// SeriesCatalog.SeriesID,                   11
        /// Sites.SiteID                              12
        /// </summary>
        /// <param name="r">SeriesMetadata record with matching parameters.</param>
        public SeriesMetadata(DataRow r)
        {
            ServCode = Convert.ToString(r.ItemArray[0]);
            ServURL = Convert.ToString(r.ItemArray[1]);
            SiteCode = Convert.ToString(r.ItemArray[2]);
            VarCode = Convert.ToString(r.ItemArray[3]);
            SiteName = Convert.ToString(r.ItemArray[4]);
            VariableName = Convert.ToString(r.ItemArray[5]);   
       

            StartDate = Convert.ToDateTime(r.ItemArray[6]);
            EndDate = Convert.ToDateTime(r.ItemArray[7]);
            ValueCount = Convert.ToInt32(r.ItemArray[8]);
                                                           
            Latitude = Convert.ToDouble(r.ItemArray[9]);
            Longitude = Convert.ToDouble(r.ItemArray[10]);
            SeriesID = Convert.ToInt32(r.ItemArray[11]);
            SiteID = Convert.ToInt32(r.ItemArray[12]);
        }

        /// <summary>
        /// Maps to result of query with parameter SQL.SERIESRESULTS
        /// HISNetworks.NetworkName AS ServCode, 0
        /// HISNetworks.ServiceWSDL AS ServURL,  1
        /// SeriesCatalog.SiteCode,              2
        /// SeriesCatalog.VariableCode,          3
        /// SeriesCatalog.SiteName,              4
        /// SeriesCatalog.VariableName,          5
        /// SeriesCatalog.BeginDateTime AS StartDate,  6
        /// SeriesCatalog.EndDateTime AS EndDate,      7
        /// SeriesCatalog.Valuecount,                  8
        /// Sites.Latitude,                            9
        /// Sites.Longitude,                          10
        /// SeriesCatalog.SeriesID,                   11
        /// Sites.SiteID                              12
        /// </summary>
        /// <param name="r">SeriesMetadata record with matching parameters.</param>
        public SeriesMetadata(object[] ItemArray)
        {
            ServCode = Convert.ToString(ItemArray[0]);
            ServURL = Convert.ToString(ItemArray[1]);
            SiteCode = Convert.ToString(ItemArray[2]);
            VarCode = Convert.ToString(ItemArray[3]);
            SiteName = Convert.ToString(ItemArray[4]);
            VariableName = Convert.ToString(ItemArray[5]);

            SampleMedium = Convert.ToString(ItemArray[6]); ;
            GeneralCategory = Convert.ToString(ItemArray[7]);

            StartDate = Convert.ToDateTime(ItemArray[8]);
            EndDate = Convert.ToDateTime(ItemArray[9]);
            ValueCount = Convert.ToInt32(ItemArray[10]);

            Latitude = Convert.ToDouble(ItemArray[11]);
            Longitude = Convert.ToDouble(ItemArray[12]);
            SeriesID = Convert.ToInt32(ItemArray[13]);
            SiteID = Convert.ToInt32(ItemArray[14]);
        }

        public Boolean HasValue()
        {
            if (SeriesID > 0) // non-nullable; if not set, will equal zero (TO-DO: cite source)
            {
                return true;
            }
            return false;
        }

        public string ServCode { get; set; }

        
        public string ServURL { get; set; }

        
        public string SiteCode { get; set; }

        
        public string VarCode { get; set; }

        
        public string SiteName { get; set; }

        
        public string VariableName { get; set; }

        public string SampleMedium { get; set; }

        public string GeneralCategory { get; set; }   
        
        public DateTime StartDate { get; set; }

        
        public DateTime EndDate { get; set; }

        
        public int ValueCount { get; set; }        

        
        public Double Latitude { get; set; }

        
        public Double Longitude { get; set; }

        
        public int SeriesID { get; set; }

        
        public int SiteID { get; set; }     
    }
}