using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ServerSideHydroDesktop.ObjectModel;

namespace ServerSideHydroDesktop
{
    public class ParserFactory
    {
        public IWaterOneFlowParser GetParser(DataServiceInfo dataService)
        {
            IWaterOneFlowParser parser;
            switch (dataService.Version.ToString("F1", CultureInfo.InvariantCulture))
            {
                case "1.0":
                    parser = new WaterOneFlow10Parser();
                    break;
                default:
                    parser = new WaterOneFlow11Parser();
                    break;
            }
            return parser;
        }
    }
}
