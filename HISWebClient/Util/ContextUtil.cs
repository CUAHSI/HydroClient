using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Util
{
    public static class ContextUtil
    {
        //Utility methods...
        public static string GetIPAddress(System.Web.HttpContext contextIn)
        {
            //Validate/initialize input parameters...
            if ( null == contextIn )
            {
                return ("Uknown");  //Invalid parameter - return early...
            }

            string ipAddress = contextIn.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return contextIn.Request.ServerVariables["REMOTE_ADDR"];
        }
    }
}