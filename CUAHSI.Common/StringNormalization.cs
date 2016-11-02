using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;


//A simple class supporting miscellaneous string normalization functions:
//	- remove diacritics
//	- replace diacritics
namespace CUAHSI.Common
{
	static class StringNormalization
	{
		public enum processDiacritics
		{
			remove = 0x01,					//NOT ORable...
			replaceWithUnderscore = 0x02
		}

		//Remove (default) or replace diacritics in the input string...
		//Source: http://www.levibotelho.com/development/c-remove-diacritics-accents-from-a-string
		public static string ProcessDiacritics(this string text, processDiacritics pd = processDiacritics.remove)
		{
			//Validate/initialize input parameters...
			if (string.IsNullOrWhiteSpace(text) || !Enum.IsDefined(typeof(processDiacritics), pd))
			{
				return text;
			}

			//Normalize string using full canonical decomposition (no character in the resulting string can be decomposed further)...
			var formdText = text.Normalize(NormalizationForm.FormD);

			IEnumerable<char> processedChars;
			switch (pd)
			{
				case processDiacritics.remove:
					//Remove 'non-spacing-mark' characters...
					processedChars = formdText.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
					break;
				case processDiacritics.replaceWithUnderscore:
					//Replace 'non-spacing-mark characters with '_'
					//Source: http://stackoverflow.com/questions/13343885/how-to-replace-a-character-in-string-using-linq
					processedChars = formdText.Select(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark ? c : '_').ToArray();
					break;
				default:
					//Unknown enum value...
					processedChars = null;
					break;
			}

			if (null != processedChars)
			{
				//Replace fully normalized characters with their primary composites, if possible...
				return new string(processedChars.ToArray()).Normalize(NormalizationForm.FormC);
			}

			//No character processing performed - return input text...
			return text;
		}
	}
}
