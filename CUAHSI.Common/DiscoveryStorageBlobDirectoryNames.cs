using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUAHSI.Common
{
    /// <summary>
    /// Names of directories used to store data in support of CUAHSI Discovery Services..
    /// </summary>
    public class DiscoveryStorageBlobDirectoryNames
    {
        /// <summary>
        /// Name of Blob storage directory for actual results.
        /// </summary>
        public static string SeriesDownloads = "seriesdownloads";

        /// <summary>
        /// Name of Blob storage directory for cached stream responses from WaterOneFlow services.
        /// </summary>
        public static string SeriesCache = "seriescache";
    }
}
