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
        /// <summary>
        /// Parses a WaterML TimeSeriesResponse XML file
        /// </summary>
        /// <param name="stream">Stream that contains xml file.</param>
        /// <returns>List of series.</returns>
        IList<Series> ParseGetValues(Stream stream);
    }
}
