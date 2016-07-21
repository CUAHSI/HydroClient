using HISWebClient.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    public class SearchSettings
    {
        #region Fields

        private CatalogSettings _catalogSettings;
        private DateSettings _dateSettings;
        private KeywordsSettings _keywordsSettings;
        private WebServicesSettings _webServicesSettings;
        private AreaSettings _areaSettings;
        private static bool _andSearch;

        #endregion

        #region Public properties

        public CatalogSettings CatalogSettings
        {
            get
            {
                return _catalogSettings ??
                       (_catalogSettings =
                           new CatalogSettings
                           {
                               TypeOfCatalog = TypeOfCatalog.HisCentral,
                               HISCentralUrl = ConfigurationManager.AppSettings["ServiceUrl1_1_Endpoint"] //http://hiscentral.cuahsi.org/webservices/hiscentral_1_1.asmx"
                           });
            }
        }

        public DateSettings DateSettings
        {
            get { return _dateSettings ?? (_dateSettings = new DateSettings()); }
        }

        public KeywordsSettings KeywordsSettings
        {
            get { return _keywordsSettings ?? (_keywordsSettings = new KeywordsSettings(this)); }
        }

        public WebServicesSettings WebServicesSettings
        {
            get { return _webServicesSettings ?? (_webServicesSettings = new WebServicesSettings(this)); }
        }

        public AreaSettings AreaSettings
        {
            get { return _areaSettings ?? (_areaSettings = new AreaSettings()); }
        }

        public static bool AndSearch
        {
            get { return SearchSettings._andSearch; }
            set { SearchSettings._andSearch = value; }
        }

        #endregion
    }
}
