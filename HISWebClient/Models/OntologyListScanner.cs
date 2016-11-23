using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
	//A simple class which aids in the scanning of OntologyObject lists...
	public static class OntologyListScanner
	{
		//Find the node in the input list with the input title...
		private static OntologyObject findNode(List<OntologyObject> objects, string nodeTitle)
		{
			//Validate/initialize input parameters...
			if (null == objects || String.IsNullOrWhiteSpace(nodeTitle))
			{
				return null;
			}

			//Scan the input list...
			OntologyObject objReturn = null;

			foreach (var obj in objects)
			{
				if (nodeTitle == obj.title)
				{
					objReturn = obj;
					break;
				}

				if (0 < obj.children.Count)
				{
					objReturn = findNode(obj.children, nodeTitle);
					if (null != objReturn)
					{
						break;
					}
				}
			}

			return objReturn;

		}

		//Find descendant leaves for the input nodes and node title...
		private static void findLeaves(List<OntologyObject> objects, ref List<OntologyObject> leaves)
		{
			//Validate/initialize input parameters...
			if (null == objects)
			{
				return;
			}

			//Scan the input list...
			foreach (var obj in objects)
			{
				bool hasChildern = 0 < obj.children.Count;

				if (hasChildern)
				{
					//Not a leaf - look for leaves...
					findLeaves(obj.children, ref leaves);
				}
				else
				{
					//A leaf - add to list...
					leaves.Add(obj);
				}
			}
		}

		//Find descendant nodes for the input nodes and node title...
		private static void findNodes(List<OntologyObject> objects, ref List<OntologyObject> nodes)
		{
			//Validate/initialize input parameters...
			if (null == objects)
			{
				return;
			}

			//Scan the input list...
			foreach (var obj in objects)
			{
				bool hasChildern = 0 < obj.children.Count;

				if (hasChildern)
				{
					//A node - add to list, look for more nodes...
					nodes.Add(obj);
					findNodes(obj.children, ref nodes);
				}
			}
		}

		//Find descendant leaves for the input nodes and node title...
		// Returns: Non-empty list: leaves --OR-- empty list: no leave
		public static List<OntologyObject> findLeaves(List<OntologyObject> listObjects, string nodeTitle)
		{
			List<OntologyObject> leaves = new List<OntologyObject>();

			//Validate/initialize input parameters...
			if (null == listObjects || String.IsNullOrWhiteSpace(nodeTitle))
			{
				return leaves;
			}

			//Find the node matching the input node title...
			//foreach (var list in listObjects)
			//{
				OntologyObject obj = findNode(listObjects, nodeTitle);
				if (null != obj && 0 < obj.children.Count)
				{
					//Node exists and is not a leaf - find leaves
					findLeaves(obj.children, ref leaves);
					//break;
				}
			//}

			return leaves;
		}

	}
}