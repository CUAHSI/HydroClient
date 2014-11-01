using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    public class OntologyPath
    {
        public int? ConceptID { get; set; }
        public string SearchableKeyword { get; set; }
        public string ConceptName { get; set; }
        public string ConceptPath { get; set; }
    }
}
