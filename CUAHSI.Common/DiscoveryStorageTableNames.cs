using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUAHSI.Common
{
    /// <summary>
    /// Names of tables in use for CUAHSI Discovery Services. VMs will create if not exist at the end of each boot cycle.
    /// </summary>
    public static class DiscoveryStorageTableNames
    {
        /// <summary>
        /// Index of exceptions that occurred when accessing WaterOneFlow services on behalf of API clients.
        /// </summary>
        public static string GetValuesException = "getvaluesexception";

        /// <summary>
        /// Identifiers for individual sessions of usage on the web client.
        /// </summary>
        public static string SessionStart = "sessionstart";

        /// <summary>
        /// User interactions during a single session.
        /// </summary>
        public static string SessionData = "sessiondata";

        /// <summary>
        /// For publicly-downloadable data.
        /// </summary>
        public static string SeriesDownloads = "seriesdownloads";
    }
}
