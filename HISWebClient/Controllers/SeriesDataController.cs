using CUAHSI.DataExport;
using CUAHSI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace HISWebClient.Controllers
{
    /// <summary>
    /// Transducer for WaterML 1.* to JSON object. Useful for data visualization and thin client scenarios.
    /// </summary>
    public class SeriesDataController : ApiController
    {

        /// <summary>
        /// Means of executing queries for data partitioned by atomic units.
        /// </summary>
        private readonly IMetadataQuery discoveryService;

        /// <summary>
        /// Means of executing data export workflows to create public references to files.
        /// </summary>
        private readonly CUAHSI.DataExport.IExportEngine wdcStore;

        public SeriesDataController() { }
        public SeriesDataController(IMetadataQuery repository, CUAHSI.DataExport.IExportEngine exportEngine)
        {
            this.discoveryService = repository;
            this.wdcStore = exportEngine;
        }

        /// <summary>
        /// Retrieves data from WaterOneFlow services, persists a publicly-accessible copy as a CSV-formatted document
        /// (see <a href="http://www.ietf.org/rfc/rfc4180.txt">rfc-4180</a> and <a href="http://www.creativyst.com/Doc/Articles/CSV/CSV01.htm">this discussion</a>), 
        /// and returns a web-formatted response including a URI to the reference link.
        /// </summary>
        /// <param name="SeriesID"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns></returns>       
        public async Task<SeriesData> Get(int SeriesID, double lat, double lng)
        {
            DateTimeOffset requestTime = DateTimeOffset.UtcNow;

            DataExportRequest cachedResult = await wdcStore.CheckIfSeriesExistsInStorage(CUAHSIDataStorage.LogHelper.GetCUAHSIDataStorage(), SeriesID, requestTime);
            if (cachedResult == null)
            {
                //get series from wateroneflow and return response
                Tuple<Stream, SeriesData> data = await discoveryService.GetSeriesDataObjectAndStreamFromSeriesTriple(SeriesID, lat, lng);
                string nameGuid = Guid.NewGuid().ToString();
                var dl = wdcStore.PersistSeriesData(data.Item2, nameGuid, requestTime);

                //fire and forget => no reason to require consistency with user download.
                var persist = wdcStore.PersistSeriesDocumentStream(data.Item1, SeriesID, nameGuid, DateTime.UtcNow);

                await Task.WhenAll(new List<Task>() { dl, persist });

                data.Item2.wdcCache = dl.Result.Uri;
                return data.Item2;
            }
            else
            {

                //get series from cloud cache and return response
                ServerSideHydroDesktop.ObjectModel.Series dataResult = await wdcStore.GetWaterOneFlowFromCloudCache(SeriesID.ToString(), cachedResult.DownloadGuid, cachedResult.ServUrl);
                SeriesMetadata meta = await discoveryService.SeriesMetaDataOfSeriesID(SeriesID, lat, lng);

                IList<DataValue> dataValues = dataResult.DataValueList.OrderBy(a => a.DateTimeUTC).Select(aa => new DataValue(aa)).ToList();
                return new SeriesData(meta.SeriesID, meta, dataResult.QualityControlLevel.IsValid, dataValues,
                    dataResult.Variable.VariableUnit.Name, dataResult.Variable.VariableUnit.Abbreviation, dataResult.Site.VerticalDatum, dataResult.Site.Elevation_m);
            }
            // SeriesData data = await discoveryService.GetSeriesDataObjectFromSeriesTriple(SeriesID, lat, lng);            
        }
    }
}