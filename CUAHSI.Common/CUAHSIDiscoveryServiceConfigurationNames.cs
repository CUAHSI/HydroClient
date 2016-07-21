using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUAHSI.Common
{
    /// <summary>
    /// Constant names of cloud service configuration (.cscfg) values and their definitions specific to the CUAHSI.Discovery service..
    /// </summary>
    public static class CUAHSIDiscoveryServiceConfigurationNames
    {
        /// <summary>
        /// The number of milliseconds to spend waiting for WaterOneFlow service response before declaring it unavailable.
        /// </summary>
        public const string WaterOneFlowTimeoutMilliseconds = "WaterOneFlowTimeoutMilliseconds";

        /// <summary>
        /// Azure storage connection string needed to persist CSV files and other data relating to service to users.
        /// </summary>
        public const string CUAHSIDataExport = "CUAHSIDataExport";

        /// <summary>
        /// Connection string to the root database used to house the HIS Central metadata index.
        /// </summary>
        public const string hisCentralAzureDBConn = "hisCentralAzureDBConn";

        /// <summary>
        /// Connection string to teh root database used to house metadata for Series lookup by ID. May be low-hanging fruit for NoSql storage because of the simple access pattern.
        /// </summary>
        public const string seriesDbConn = "seriesDbConn";

        /// <summary>
        /// The domain considered to be the root of the API web services that power discovery client. 
        /// </summary>
        public const string ServiceDomain = "ServiceDomain";

        /// <summary>
        /// Azure storage connection string needed to persist application logs, including diagnostics, exception reports, and usage metric data.
        /// </summary>
        public const string cuahsiservicelogs = "cuahsiservicelogs";
    }
}
