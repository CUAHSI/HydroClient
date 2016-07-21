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
    public class OntologyItem
    {
        /// <summary>
        /// Null constructor for default result
        /// </summary>
        public OntologyItem()
        { }

        /// <summary>
        /// Data controller => used to bind REST data request params for use in query string. Expected format: 'Id-FacetID'
        /// </summary>
        /// <param name="id"></param>
        /// <param name="facetid"></param>
        public OntologyItem(string conceptAndFacetIDs)
        {
            string[] data = conceptAndFacetIDs.Split('-');
            Id = Convert.ToInt32(data[0]);
            ConceptName = String.Empty;
            FacetID = Convert.ToInt32(data[1]);
            FacetName = String.Empty;
            Synonyms = new List<string>();
        }

        /// <summary>
        /// General constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="conceptName"></param>
        /// <param name="facetID"></param>
        /// <param name="facetName"></param>
        public OntologyItem(int id, string conceptName, int facetID, string facetName)
        {
            Id = id;
            ConceptName = conceptName;
            FacetID = facetID;
            FacetName = facetName;
            Synonyms = new List<string>();
        }
        
        public OntologyItem(int id, string conceptName, int facetID, string facetName, List<string> synonyms, int seriesCount, int valueCount)
        {
            Id = id;
            ConceptName = conceptName;
            FacetID = facetID;
            FacetName = facetName;
            Synonyms = synonyms;
            SeriesCount = seriesCount;
            ValueCount = valueCount;
        }

        [Obsolete("Deprecated in favor of async reader result, which is an object array")]
        
        /// <summary>
        /// Maps to single-row result of query with parameter SQL.HISONTOLOGYRESULTS       
        /// Concepts.ConceptID, 0        
        /// Concepts.ConceptName, 1
        /// Synonyms.Synonym, 2
        /// Concepts.IsSearchable, 3
        /// Concepts.QueryStructureTblID AS FacetID, 4
        /// SeriesQueryStructureID.FacetName 5
        /// </summary>
        /// <param name="r"></param>
        public OntologyItem(DataRow r)
        {
            Id = Convert.ToInt32(r.ItemArray[0]);
            ConceptName = r.ItemArray[1].ToString();
            FacetID = Convert.ToInt32(r.ItemArray[4]);
            FacetName = r.ItemArray[5].ToString();
            Synonyms = new List<string>();
            if (r.ItemArray[2] != null)
            {
                Synonyms.Add(r.ItemArray[2].ToString());
            }
        }

        /// <summary>
        /// Maps to single-row result of query with parameter SQL.HISONTOLOGYRESULTS       
        /// Concepts.ConceptID, 0        
        /// Concepts.ConceptName, 1
        /// Synonyms.Synonym, 2
        /// Concepts.IsSearchable, 3
        /// Concepts.QueryStructureTblID AS FacetID, 4
        /// SeriesQueryStructureID.FacetName 5
        /// </summary>
        /// <param name="r"></param>
        /// <param name="hasCounts">Identifies object array schema as including series and value count parameters</param>
        public OntologyItem(object[] ItemArray, Boolean hasCounts)
        {
            Id = Convert.ToInt32(ItemArray[0]);
            ConceptName = ItemArray[1].ToString();
            FacetID = Convert.ToInt32(ItemArray[3]);
            FacetName = ItemArray[4].ToString();
            Synonyms = new List<string>();
            if (ItemArray[2] != null)
            {
                Synonyms.Add(ItemArray[2].ToString());
            }
            if (hasCounts)
            {
                SeriesCount = Convert.ToInt32(ItemArray[5]);
                ValueCount = Convert.ToInt32(ItemArray[6]);
            }
        }


        /// <summary>
        /// Maps to multi-row result (multiple synonyms) of query with parameter SQL.HISONTOLOGYRESULTS
        /// Concepts.ConceptID, 0       
        /// Concepts.ConceptName, 1
        /// Synonyms.Synonym, 2
        /// Concepts.IsSearchable, 3
        /// Concepts.QueryStructureTblID AS FacetID, 4
        /// SeriesQueryStructureID.FacetName 5
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="hasCounts">Identifies object array schema as including series and value count parameters</param>
        public OntologyItem(List<object[]> rs, Boolean hasCounts)
        {
            object[] ItemArray = rs.OrderBy(x => x.ElementAt(0)).FirstOrDefault(); // assumes all aggregation done on ConceptID; Synonym records should have identical Count values
            Id = Convert.ToInt32(ItemArray[0]);
            ConceptName = ItemArray[1].ToString();
            FacetID = Convert.ToInt32(ItemArray[3]);
            FacetName = ItemArray[4].ToString();
            Synonyms = new List<string>();
            foreach (object[] ir in rs)
            {
                string s = ir[2].ToString();
                if (s.Length > 0) // not all canonical names have synonyms
                {
                    Synonyms.Add(s);
                }
            }
            if (hasCounts)
            {
                SeriesCount = Convert.ToInt32(ItemArray[5]);
                ValueCount = Convert.ToInt32(ItemArray[6]);
            }
        }

        [DataMember]
        /// <summary>
        /// ConceptID of the 
        /// </summary>        
        public int Id { get; set; }

        [DataMember]
        /// <summary>
        /// Canonical name of this concept. Synonyms are attached in MySynonyms.
        /// </summary>        
        public string ConceptName { get; set; }

        [DataMember]
        /// <summary>
        /// ID of orthogonal dimension or container of metadata this OntologyItem belongs to in the CUAHSI managed vocabulary
        /// </summary>
        public int FacetID { get; set; }

        [DataMember]
        /// <summary>
        /// Name of orthogonal dimension or container of metadata this OntologyItem belongs to in the CUAHSI managed vocabulary
        /// </summary>
        public string FacetName { get; set; }

        [DataMember]
        /// <summary>
        /// List of alternative names for this OntologyItems (i.e. not the ConceptName)
        /// </summary>        
        public List<string> Synonyms { get; set; }

        [DataMember]
        /// <summary>
        /// Number of series represented by this OntologyItem. Requires "counts" verbosity parameter value to be populated.
        /// </summary>
        public int SeriesCount { get; set; }

        [DataMember]
        /// <summary>
        /// Number of data values in all of the series represented by this OntologyItem, matching the constraints at the time of the request. Requires "counts" verbosity parameter value to be populated.
        /// </summary>
        public int ValueCount { get; set; }
    }

    [DataContract]
    public class Synonym
    {        
        public Synonym(int canonicalID, string name)
        {
            CanonicalNameID = canonicalID;
            SynonymName = name;
        }

        [DataMember]
        /// <summary>
        /// ID of the OntologyElement of which this Synoym is a synonym
        /// </summary>
        public int CanonicalNameID { get; set; }

        [DataMember]
        /// <summary>
        /// Alternative name for an OntologyElement. Fully equivalent data reference (search is conducted via the CanonicalNameID).
        /// </summary>
        public string SynonymName { get; set; }
    }    
}