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
    public class OntologyFacet
    {
        /// <summary>
        /// Identity of this facet, used as reference in API.
        /// </summary>
        [DataMember]
        public int FacetID { get; set; }

        /// <summary>
        /// Name of this facet, as would appear to users.
        /// </summary>
        [DataMember]
        public string FacetName { get; set; }

        /// <summary>
        /// Web page conveying additional discussion of this Facet.
        /// </summary>
        [DataMember]
        public string FacetURI { get; set; }

        /// <summary>
        /// How this facet of ontology was derived from HIS Central metadata
        /// </summary>
        [DataMember]
        public string FacetDefinition { get; set; }

        /// <summary>
        /// Parameterless constructor for use in REST API as null case
        /// </summary>
        public OntologyFacet()
        { }

        /// <summary>
        /// Initial prototype constructor no additional metadata than name and facet. Definitions should be implemented through data tier.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public OntologyFacet(int id, string name)
        {
            FacetID = id;
            FacetName = name;
            FacetURI = string.Empty;
            switch (name)
            { 
                case "Sample Medium":
                    FacetDefinition = string.Empty;
                    break;
                case "Organization":
                    FacetDefinition = string.Empty;
                    break;
                case "Network":
                    FacetDefinition = string.Empty;
                    break;
                case "Variable Name":
                    FacetDefinition = string.Empty;
                    break;
                case "Data Type":
                    FacetDefinition = string.Empty;
                    break;
                case "Value Type":
                    FacetDefinition = string.Empty;
                    break;
                default:
                    FacetDefinition = string.Empty;
                    break;
            }
        }

        /// <summary>
        /// Constructor for async web api
        /// </summary>
        /// <param name="data">Conforms to response in FacetController.GetFacets()</param>
        public OntologyFacet(object[] data)
        {
            FacetID = Convert.ToInt32(data[0]);
            FacetName = data[1].ToString();
            FacetURI = "http://his.cuahsi.org/documents/HIS_Central-1.2.pdf";
            switch (FacetName)
            {
                case "Sample Medium":
                    FacetDefinition = "Environmental medium that was sampled and analyzed";
                    FacetURI = "http://help.waterdata.usgs.gov/codes-and-parameters";
                    break;
                case "Organization":
                    FacetDefinition = "The organization responsible for data being served";                    
                    break;
                case "Network":
                    FacetDefinition = "The name of the observation network as registered with WaterOneFlow services";
                    break;
                case "Variable Name":
                    FacetDefinition = string.Empty;
                    break;
                case "Data Type":
                    FacetDefinition = string.Empty;
                    break;
                case "Value Type":
                    FacetDefinition = string.Empty;
                    break;
                default:
                    FacetDefinition = string.Empty;
                    break;
            }
        }
    }
}