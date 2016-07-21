using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUAHSI.Common
{
    /// <summary>
    /// Methods for encoding data in lexigraphical order for indexing in Azure Table Storage.
    /// </summary>
    public static class IndexGeneration
    {
        /// <summary>
        /// Encode an event in partition ordered such that Top= queries resolve to the most modern items.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string ToDateTimeDescending(this DateTimeOffset d)
        {
            return string.Format("{0:D19}", DateTime.MaxValue.Ticks - d.UtcDateTime.Ticks);
        }        
    }
}
