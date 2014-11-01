using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HISWebClient.BusinessObjects.Models;
namespace HISWebClient.DataLayer
{
    class SeriesComparer : IEqualityComparer<SeriesDataCartModel.SeriesDataCart>
    {

        public bool Equals(SeriesDataCartModel.SeriesDataCart x, SeriesDataCartModel.SeriesDataCart y)
        {
            if (x.SiteName.Equals(y.SiteName))
            {
                return true;
            }
            return false;
        }

        public int GetHashCode(SeriesDataCartModel.SeriesDataCart obj)
        {
            return obj.SiteName.GetHashCode();
        }
    }
}
