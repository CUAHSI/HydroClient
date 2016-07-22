using HISWebClient.BusinessObjects;
using HISWebClient.DataLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace HISWebClient.Models
{
    public class OntologyHelper
    {
        private SortedSet<string> keywordsList;
        private string json; 
        
        public string getOntology(string conceptKeyword)
        {
            var dataWorker = new DataWorker();
            var ontologytreeJson = string.Empty;
            keywordsList = new SortedSet<string>();
            json = string.Empty;
            //get ontology from hiscentral

            var getOntologytree = dataWorker.getOntologyTree(conceptKeyword);

            XmlDocument ontologytreeXML = new XmlDocument();

            if (getOntologytree != null)
            {

                ontologytreeXML.LoadXml(getOntologytree);
                //string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc.DocumentElement);
            }

            // Ontology tree
            var tree = new OntologyTree();
            //var tmpxmldoc = ReadOntologyXmlFile();
            FillTree(ontologytreeXML.DocumentElement, tree.Nodes);



            //XmlReader rdr = XmlReader.Create(new System.IO.StringReader(ontologytreeXML));
            //while (rdr.Read())
            //{
            //    if (rdr.NodeType == XmlNodeType.Element)
            //    {
            //        Console.WriteLine(rdr.LocalName);
            //    }
            //}

            if (tree != null)
            {
                //    XmlDocument doc = new XmlDocument();
                //    doc.LoadXml(ontologytreeXml);
                //string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc.DocumentElement);
                ontologytreeJson = Newtonsoft.Json.JsonConvert.SerializeObject(tree.Nodes[0].children,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.None
                        // PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    }
                    );
            }
            return ontologytreeJson;

        }

        //from hydrodesktop hisCentralKeywordList.cs
        private void FillTree(XmlNode node, ICollection<OntologyNode> parentnode)
        {
            // End recursion if the node is a text type
            if (node == null || node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                return;

            var tmptreenodecollection = AddNodeToTree(node, parentnode);

            // Add all the children of the current node to the treeview
            foreach (XmlNode tmpchildnode in node.ChildNodes)
            {
                if (tmpchildnode.Name == "childNodes")
                {
                    foreach (XmlNode tmpchildnode2 in tmpchildnode.ChildNodes)
                    {
                        FillTree(tmpchildnode2, tmptreenodecollection);
                    }
                }
            }
        }
        private ICollection<OntologyNode> AddNodeToTree(XmlNode node, ICollection<OntologyNode> parentnode)
        {
            var newchildnode = CreateTreeNodeFromXmlNode(node);
            if (parentnode != null) parentnode.Add(newchildnode);
            return newchildnode.children;
        }

        private OntologyNode CreateTreeNodeFromXmlNode(XmlNode node)
        {
            bool isFolder = true;
            OntologyNode tmptreenode = null;
            if (node.HasChildNodes)
            {
                var text = node.FirstChild.InnerText.Trim();
                var id = node.FirstChild.NextSibling.InnerText.Trim();
                if (text != string.Empty)
                {
                    tmptreenode = new OntologyNode(id, text, isFolder);
                    keywordsList.Add(text);
                }
            }
            return tmptreenode ?? (new OntologyNode());
        }
    }
}