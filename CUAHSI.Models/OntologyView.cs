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
    /// Represents different endpoints types internally. Used to assist generic methods for SQL generation => represents every grouping interested in filtering by ontology.
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/424366/c-sharp-string-enums"/>
    public sealed class OntologyView
    {
        private readonly String name;
        private readonly int value;

        /// <summary>
        /// /api/ontologyitems/
        /// </summary>
        public static readonly OntologyView ontologyitems = new OntologyView(1, "ontologyitems");
        
        /// <summary>
        /// /api/sites/
        /// </summary>
        public static readonly OntologyView sites = new OntologyView(2, "sites");
        
        /// <summary>
        /// /api/seriesmetadata/
        /// </summary>
        public static readonly OntologyView series = new OntologyView(3, "series");

        /// <summary>
        /// /api/facets/
        /// </summary>
        public static readonly OntologyView facets = new OntologyView(4, "facets");

        private OntologyView(int value, String name)
        {
            this.name = name;
            this.value = value;
        }

        public override string ToString()
        {
            return name;
        }

        public bool Equals(String endpoint)
        {
            return endpoint.Equals(this.name);
        }

        public override bool Equals(object obj)
        {
            return obj.Equals(this.name);
        }
    }
}