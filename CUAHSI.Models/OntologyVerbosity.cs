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
    /// Represents available views of OntologyItems. Implements type-safe-enum pattern.   
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/424366/c-sharp-string-enums"/>
    public sealed class OntologyVerbosity
    {
        private readonly String name;
        private readonly int value;

        /// <summary>
        /// ConceptID, ConceptName, FacetID, FacetName, Synonyms
        /// </summary>
        public static readonly OntologyVerbosity basic = new OntologyVerbosity(1, "basic");

        /// <summary>
        /// ConceptID, ConceptName, FacetID, FacetName, Synonyms, SeriesCount, ValueCount
        /// </summary>
        public static readonly OntologyVerbosity counts = new OntologyVerbosity(2, "counts");

        [Obsolete("Deprecated in favor of splitting site-plus-ontology response into a separate endpoint and object, because it has different aggregation semantics (i.e. Site-level instead of Concept-level)than basic OntologyItems queries")]
        /// <summary>
        /// ConceptID, ConceptName, FacetID, FacetName, Synonyms, SiteID, Latitude, Longitude, SeriesCount, ValueCount
        /// </summary>
        public static readonly OntologyVerbosity spatialcounts = new OntologyVerbosity(3, "spatialcounts");

        /// <summary>
        /// Same as basic, but batches all geohashes in single request to each cluster. Used to explore tradeoffs between clustering activities.
        /// </summary>
        public static readonly OntologyVerbosity basicbatch = new OntologyVerbosity(4, "basicbatch");

        private OntologyVerbosity(int value, String name)
        {
            this.name = name;
            this.value = value;
        }

        public override String ToString()
        {
            return name;
        }

        /// <summary>
        /// String-specific implementation of equals useful for web services parameter matching
        /// </summary>
        /// <param name="verbosity"></param>
        /// <returns></returns>
        public bool Equals(String verbosity)
        {
            return verbosity.Equals(this.name);
        }

        /// <summary>
        /// Override of default comparison mechanism that enforces String-based comparison
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj.Equals(this.name);
        }
    }
}