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
    public enum HydroWizardType
    {
        /// <summary>
        /// Returns entire collection of metadata in all facets describing series objects 
        /// </summary>
        [DataMember]
        FacetedSearch,

        /// <summary>
        /// Returns smallest collection of metadata that needs disambiguation yet has at least 2 options left. This drives the 
        /// </summary>
        [DataMember]
        SimplestOntologyCollectionAboveThreshold,

        /// <summary>
        /// Deprecated: Returns smallest collection of metadata that needs disambiguation yet has at least minimum bound options left, where the minimum bound decreases one for each facet specified in the user request.
        /// </summary>
        [DataMember]
        SimplestOntologyCollectionAboveDynamicThreshold,

        /// <summary>
        /// NOT Implemented: Chooses facet that represents the greatest data values collectively.
        /// </summary>
        [DataMember]
        HighestDataCoverageByDataValueCount,

        /// <summary>
        /// NOT Implemented: Chooses facet that has the greatest average data values per option.
        /// </summary>
        [DataMember]
        HighestAverageDataValueCountPerFacet,

        /// <summary>
        /// Not implemented: uses user groups to prioritize facet options
        /// </summary>
        [DataMember]
        GreatestSocialConnection,

        /// <summary>
        /// Returns Ontology options in the facet requested by the UI
        /// </summary>
        [DataMember]
        ClientRequestedFacet

    };
}