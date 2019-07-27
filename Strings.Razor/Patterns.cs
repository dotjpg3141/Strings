using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Strings.Razor
{
	internal static class Patterns
	{
		public static readonly Regex ExcludedRazorLines = new Regex(
			   @"^@(?:model|page|addTagHelper|using)\b", RegexOptions.Compiled);

		public static readonly Regex NewLine = new Regex(
			@"\r?\n", RegexOptions.Compiled);

		public static readonly Regex TrailingWhitespaceExceptNewLine = new Regex(
			@"^[\s-[\r\n]]+", RegexOptions.Compiled);
	}
}
