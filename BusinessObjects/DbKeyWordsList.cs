using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    class DbKeywordsList : IOntologyReader
    {
        public OntologyDesc GetOntologyDesc()
        {
            // Keywords
            //var searcher = new MetadataCacheSearcher();
            var list = new List<string>();
            var keywordsList = list;//searcher.GetKeywords();
            keywordsList.Add(Constants.RootName);
            var sortedKeywords = new SortedSet<string>(keywordsList);

            // Ontology tree
            var tree = new OntologyTree();
            var parentNode = new OntologyNode(Constants.RootName);
            foreach (var keyword in keywordsList.Where(keyword => keyword != Constants.RootName))
            {
                parentNode.children.Add(new OntologyNode(keyword));
            }
            tree.Nodes.Add(parentNode);

            // Return result
            var result = new OntologyDesc
            {
                Keywords = sortedKeywords,
                OntoloyTree = tree,
            };
            return result;
        }
    }
}
