namespace HISWebClient.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CUAHSI.Models;

    /// <summary>
    /// Interface definition for metadata repository access. 
    /// </summary>
    public interface IMetadataQuery
    {
    
        Task<SeriesMetadata> SeriesMetaDataOfSeriesID(int SeriesID, double lat, double lng);
        Task<IEnumerable<SeriesMetadata>> SeriesMetadataOfSiteIDAndSelections(List<OntologyItem> Selections, DateTime BeginTime, DateTime EndTime, int SiteID, double lat, double lng);

        /// <summary>
        /// Return HydroDesktop ObjectModel Series object representing the WaterML response of a WaterOneFlow request on behalf of the client application.
        /// </summary>
        /// <param name="meta">DiscoveryService SeriesMetadata object</param>
        /// <returns>ServerSideHydroDesktop.ObjectModel.Series object</returns>
        Task<ServerSideHydroDesktop.ObjectModel.Series> SeriesOfSeriesID(SeriesMetadata meta);
        Task<ServerSideHydroDesktop.ObjectModel.Series> SeriesOfSOAPMetadata(string servURL, string sitecode, string varcode, DateTime startDate, DateTime endDate);
        Task<SeriesData> GetSeriesDataObjectFromSeriesTriple(int SeriesID, double lat, double lng);
        Task<Tuple<Stream, SeriesData>> GetSeriesDataObjectAndStreamFromSeriesTriple(int seriesId, double lat, double lng);        
    }
}
