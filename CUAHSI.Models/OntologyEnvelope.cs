using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Text;

namespace CUAHSI.Models
{
    [DataContract]
    public class OntologyEnvelope
    {
        /// <summary>
        /// The CUAHSI Managed Ontology elements that describe the series described in the request. Depending on the DiscoveryWizardType of the request, this collection will be differently comprehensive.
        /// </summary>
        [DataMember]
        public List<OntologyItem> ontologyitems { get; set; }

        /// <summary>
        /// User tags of vocabulary that describe the series described in the request.
        /// </summary>
        [DataMember]
        public List<HydroTag> tags { get; set; }

        /// <summary>
        /// The remaining orthogonal facets of ontology from which metadata has not been specified or skipped in the last request.
        /// </summary>
        [DataMember]
        public List<OntologyFacet> availableFacets { get; set; }

        [DataMember]
        /// <summary>
        /// Facets to skip in the return: the user will not be shown any of these facets, nor will they be included as suggested next facets.
        /// </summary>
        public string Skips { get; set; }

        public OntologyEnvelope()
        { }

        public OntologyEnvelope(List<OntologyItem> Selections, List<HydroTag> SelectedTags)
        {
            ontologyitems = new List<OntologyItem>();
            tags = new List<HydroTag>();
            availableFacets = new List<OntologyFacet>();

            if (Selections != null)
            {
                ontologyitems = Selections;
            }

            if (SelectedTags != null)
            {
                tags = SelectedTags;
            }                        
        }

        /// <summary>
        /// Constructor with pass-through for correct skip state persistance => examine implementation for XSS vulnerability
        /// </summary>
        /// <param name="Selections"></param>
        /// <param name="SelectedTags"></param>
        /// <param name="skips"></param>
        public OntologyEnvelope(List<OntologyItem> Selections, List<HydroTag> SelectedTags, List<int> skips)
        {
            ontologyitems = new List<OntologyItem>();
            tags = new List<HydroTag>();
            availableFacets = new List<OntologyFacet>();

            if (Selections != null)
            {
                ontologyitems = Selections;
            }

            if (SelectedTags != null)
            {
                tags = SelectedTags;
            }
            
            int added = 0;
            StringBuilder sb = new StringBuilder();
            foreach (int i in skips)
            {
                if (added < 1)
                {
                    sb.Append(i.ToString());
                }
                else
                {
                    sb.Append(string.Format(",{0}", i));
                }
                added++;
            }
            Skips = sb.ToString();            
        }
    }        
}