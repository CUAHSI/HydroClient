using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.MarkerClusterer
{
    /// <summary>
    /// Comparers two pins and returns the sort postion.
    /// Sorts by y then by x
    /// </summary>
    public class PinXYComparer : IComparer<ClusteredPin>
    {
        int IComparer<ClusteredPin>.Compare(ClusteredPin x, ClusteredPin y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is Nothing and y is Nothing, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is Nothing and y is not Nothing, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not Nothing...
                if (y == null)
                {
                    // ...and y is Nothing, x is greater.
                    return 1;
                }
                else
                {
                    // ...and y is not Nothing, compare the 
                    // x values
                    if (x.Loc.Lon > y.Loc.Lon)
                    {
                        //x is greater
                        return 1;
                    }
                    else
                    {
                        if (x.Loc.Lon == y.Loc.Lon)
                        {
                            //compare the y values
                            if (x.Loc.Lat > y.Loc.Lat)
                            {
                                //x is greater
                                return 1;
                            }
                            else
                            {
                                if (x.Loc.Lat == y.Loc.Lat)
                                {
                                    //they're equal. 
                                    return 0;
                                }
                                else
                                {
                                    //y is greater
                                    return -1;
                                }
                            }
                        }
                        else
                        {
                            //y is greater
                            return -1;
                        }
                    }
                }
            }
        }
    }
}
