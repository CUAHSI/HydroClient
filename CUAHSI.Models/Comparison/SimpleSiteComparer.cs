using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CUAHSI.Models
{
    public class SimpleSiteComparer : IEqualityComparer<Site>
    {
        public bool Equals(Site x, Site y)
        {
            if (Object.ReferenceEquals(x, y)) return true;

            if (Object.ReferenceEquals(x, null) ||
                Object.ReferenceEquals(y, null))
                return false;

            return x.SiteID == y.SiteID;
        }

        public int GetHashCode(Site o)
        {
            if (Object.ReferenceEquals(o, null)) return 0;

            return o.SiteID.GetHashCode();
        }
    }
}