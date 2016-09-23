using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Xml;

namespace HISWebClient.Util
{
	//Utility methods for XML content...
	public static class XmlContext
	{
		//Check for empty element - advance input reader to next non-empty content, if indicated...
		public static bool AdvanceReaderPastEmptyElement( XmlReader reader )
		{
			//Check reader... 
			if ((null != reader) && 
				(XmlNodeType.Element == reader.NodeType) && reader.IsEmptyElement)
			{
				//Success - current element is a self-closing, non-content node without an 'EndElement' (e.g., <methodLink />)- 
				//	advance to next content node (which can be an 'EndElement', among others) - return true...
				reader.MoveToContent();
				return true;
			}

			//Reader check NOT successful - DO NOT advance reader - return false
			return false;
		}

		//Async version...
		public static bool AdvanceReaderPastEmptyElementAsync(XmlReader reader)
		{
			//Check reader... 
			if ((null != reader) &&
				(XmlNodeType.Element == reader.NodeType) && reader.IsEmptyElement)
			{
				//Current element is a self-closing, non-content node without an 'EndElement' (e.g., <methodLink />)- 
				//	advance to next content node (which can be an 'EndElement', among others), return true...
				reader.MoveToContentAsync();
				return true;
			}

			//Reader check NOT successful - DO NOT advance reader - return false
			return false;
		}

	}
}