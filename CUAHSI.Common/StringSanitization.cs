using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUAHSI.Common
{
    /// <summary>
    /// Methods for removing unwanted characters from input.
    /// </summary>
    public static class StringSanitization
    {
        public static string SanitizeForFilename(this string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) { fileName = fileName.Replace(c, '-'); }
            return fileName.ToLowerInvariant();
        }
    }
}
