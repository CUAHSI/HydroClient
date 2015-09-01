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
            fileName = fileName.Replace(' ','_');
            return fileName.ToLowerInvariant();
        }

		//BCC - 01-Sep-2015 - Add Url escaping for use with Azure Blob names...
		//NOTE: Blob name restrictions: 
		//			length - 1-1024 characters
		//			case-sensitive
		//			any URL character with reserved characters properly escaped
		//			do not end the name with '.' or '/' or any sequence or combination thereof
		//			do not include '\' in the name
		//Source:	http://blogs.msdn.com/b/jmstall/archive/2014/06/12/azure-storage-naming-rules.aspx
		public static string SanitizeAndUrlEscapeForFilename(this string fileName)
		{
			StringBuilder sb = new StringBuilder(Uri.EscapeDataString(SanitizeForFilename(fileName)));

			if (1024 < sb.Length)
			{
				sb.Length = 1024;
			}

			while (true)
			{
				int last = sb.Length - 1;

				if ('.' == sb[last] || '/' == sb[last])
				{
					sb.Remove(last, 1);
				}
				else
				{
					break;
				}
			}

			if (0 >= sb.Length)
			{
				throw new ArgumentException("SanitizeAndUrlEscapeForFilename(...) - invalid input filename!!");
			}

			return sb.ToString();
		}

    }
}
