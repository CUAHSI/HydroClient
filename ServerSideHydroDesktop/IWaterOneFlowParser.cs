using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServerSideHydroDesktop.ObjectModel;

namespace ServerSideHydroDesktop
{
    /// <summary>
    /// Contains methods for parsing the xml (WaterML) files returned
    /// by different versions of the WaterOneFlow web services
    /// </summary>
    public interface IWaterOneFlowParser
    {
		IList<Site> ParseGetSites(Stream stream);

		IList<SeriesMetadata> ParseGetSiteInfo(Stream stream);

        /// <summary>
        /// Parses a WaterML TimeSeriesResponse XML file
        /// </summary>
        /// <param name="stream">Stream that contains xml file.</param>
        /// <returns>List of series.</returns>
        IList<Series> ParseGetValues(Stream stream);
    }
}
