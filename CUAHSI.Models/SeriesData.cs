using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using ServerSideHydroDesktop.ObjectModel;

namespace CUAHSI.Models
{
    [DataContract]
    public class SeriesData
    {
        /// <summary>
        /// SeriesID used for client-side indexing
        /// </summary>
        [DataMember]
        public int SeriesID { get; set; }        

        /// <summary>
        /// Indicates whether the web service is confidant of the time stamps provided in values.
        /// </summary>
        [DataMember]
        public Boolean HasConfirmedTimeStamp { get; set; }

        /// <summary>
        /// Contains descriptions of issues resolving time stamps.
        /// </summary>
        [DataMember]
        public String TimeStampMessage { get; set; }

        /// <summary>
        /// List of DateTime, Double pairs. DateTime data is represented as UTC.
        /// </summary>
        [DataMember]
        public List<DataValue> values { get; set; }

        /// <summary>
        /// Contains values from HISCentral SeriesCatalog record, may not be part of the ontology if values were not made into searchable facets. May be deprecated if deemed unnecessary (client will typically, but not always have metadata from Discovery API interactions local).
        /// </summary>
        [DataMember]
        public SeriesMetadata myMetadata { get; set; }        

        /// <summary>
        /// Collection of HIS Ontology descriptors of this series.
        /// </summary>
        [DataMember]
        public List<OntologyItem> ontology { get; set; }

        /// <summary>
        /// Collection of user-submitted HydroTags of this series.
        /// </summary>
        [DataMember]
        public List<HydroTag> tags { get; set; }

        /// <summary>
        /// List of Sources (MS) 
        /// </summary>
        [DataMember]
		public List<Source> mySources { get; set; }

        /// <summary>
        /// List of Variables (MS) 
        /// </summary>
        [DataMember]
        public Variable myVariable { get; set; }

        /// <summary>
        /// Concatenates the HIS Ontology canonical names, and the site name, to produce a single name that shold be unique within HIS at the time that the search is conducted (no guarantee in the future).
        /// Format is {SiteCode}, {ConceptName1}, {ConceptName2}, ...
        /// </summary>
        /// <returns></returns>
        public String GetOntologyName()
        {
            StringBuilder str = new StringBuilder();
            string template = ", {{0}}";
            str.Append(string.Format("{{0}}", myMetadata.SiteCode));
            foreach (OntologyItem o in ontology.OrderBy(r => r.FacetID).OrderBy(r => r.Id))
            {
                str.Append(string.Format(template, o.ConceptName));
            }
            return str.ToString();
        }

        [DataMember]
        public string unit { get; set; }

        [DataMember]
        public string unitAbbrev { get; set; }

		[DataMember]
		public List<Method> myMethods { get; set; }

		[DataMember]
		public List<QualityControlLevel> myQualityControlLevels { get; set; }

        [DataMember] 
        public string VerticalDatum { get; set; }

        [DataMember] 
        public double Elevation_m { get; set; }

        /// <summary>
        /// Public URI of CSV data file of series while it is cached by the CUAHSI Water Data Center.
        /// </summary>
        [DataMember]
        public string wdcCache { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SeriesData()
        {
            values = new List<DataValue>();
            ontology = new List<OntologyItem>();
            tags = new List<HydroTag>();

			mySources = new List<Source>();
			myMethods = new List<Method>();
			myQualityControlLevels = new List<QualityControlLevel>();
        }

        public SeriesData(int seriesId, SeriesMetadata myMeta, IList<DataValue> dataValues, Variable variable, 
						  List<Source> sources, List<Method> methods, List<QualityControlLevel> qualityControlLevels) : this()
        {
            myMetadata = myMeta;
            SeriesID = seriesId;
            HasConfirmedTimeStamp = true;
            TimeStampMessage = string.Empty;
            myVariable = variable;           
            values = dataValues.ToList();
            ontology = new List<OntologyItem>();
            tags = new List<HydroTag>();
			if (null != sources)
			{
				mySources = sources;
			}

			if (null != methods)
			{
				myMethods = methods;
			}

			if (null != qualityControlLevels)
			{
				myQualityControlLevels = qualityControlLevels;
			}
        }
    }
}