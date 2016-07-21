namespace CUAHSI.DataExport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CUAHSI.Models;
    using Microsoft.WindowsAzure.Storage;

    public interface IExportEngine
    {
        /// <summary>
        /// Persists a single SeriesData object as a file in a blob and returns a link for it.
        /// </summary>
        /// <param name="data">Data object to persist in file.</param>
        /// <param name="urlId">Guid used to differentiate and randomize URL.</param>
        /// <param name="requestTime">Time data was originally requested.</param>
        /// <returns></returns>
        Task<SeriesDownload> PersistSeriesData(SeriesData data, string urlId, DateTimeOffset requestTime);

        /// <summary>
        /// Persists the byte stream of a WaterOneFlow document in a blob for later record keeping.
        /// </summary>
        /// <param name="s">Stream of data to persist.</param>
        /// <param name="seriesId">CUAHSI HIS id of the series being persisted</param>
        /// <param name="urlId">Guid used to differentiate and randomize URL.</param>
        /// <param name="requestTime">Time data was originally requested.</param>
        /// <returns></returns>
        Task<Boolean> PersistSeriesDocumentStream(Stream s, int seriesId, string urlId, DateTimeOffset requestTime);

        /// <summary>
        /// Gets a record of a particular series request if one exists in the WDC storage cache already (persists all already-downloaded files).
        /// </summary>
        /// <param name="csa"></param>
        /// <param name="SeriesID"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="requestTime"></param>
        /// <returns></returns>
        Task<DataExportRequest> CheckIfSeriesExistsInStorage(CloudStorageAccount csa, int SeriesID, DateTimeOffset requestTime);

        /// <summary>
        /// Get WaterOneFlow document from cloud storage.
        /// </summary>
        /// <param name="seriesId"></param>
        /// <param name="guid"></param>
        /// <param name="servUrl"></param>
        /// <returns></returns>
        Task<ServerSideHydroDesktop.ObjectModel.Series> GetWaterOneFlowFromCloudCache(string seriesId, string guid, string servUrl);        
    }
}
