using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;


// Enum Extension Methods Time series status enum with descriptions
//NOTE: Values defined in this enum and it's JavaScript equivalent MUST always match!!
//Source: http://www.yumeidearmas.com/2015/02/26/associating-enums-with-strings-in-c/ 
namespace HISWebClient.Util
{
    //Usage: <EnumName>.<EnumMember>.GetEnumDescription()
    //
	//BCC - 11-Aug-2016 - Declare the class internal so that the namespace will not be visible outside the DLL...
	//NOTE:  The class is 'added as link' to CUAHSI.Models.  
	//			Since HISWebClient references CUAHSI.Models, the class must be declared internal to avoid compile errors resulting from an ambiguous reference... 
	//Source: http://www.xtremedotnettalk.com/general/89476-hide-namespace.html
    internal static class EnumExtensionMethods
    {
        public static string GetEnumDescription(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;

            //if no attribute was specified, then we will return the regular enum.ToString()
            return value.ToString();
        }
    }
}