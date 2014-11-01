using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    static class KeywordsServicesFactory
    {
        public static IOntologyReader GetKeywordsList(CatalogSettings catalogSettings)
        {
            if (catalogSettings == null) throw new ArgumentNullException("catalogSettings");

            IOntologyReader reader;
            switch (catalogSettings.TypeOfCatalog)
            {
                case TypeOfCatalog.LocalMetadataCache:
                    reader = new DbKeywordsList();
                    break;
                case TypeOfCatalog.HisCentral:
                    reader = new HisCentralKeywordsList();
                    break;
                default:
                    throw new Exception("Unknown TypeOfCatalog");
            }

            return reader;
        }
    }
}
