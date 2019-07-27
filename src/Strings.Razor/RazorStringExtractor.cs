using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Razor.Parser;
using Strings.Common;

namespace Strings.Razor
{
	public sealed class RazorStringExtractor : IStringExtractor
	{
		public string Path { get; set; }
		public TextReader Reader { get; private set; }

		public static RazorStringExtractor FromFile(string path)
		{
			return new RazorStringExtractor()
			{
				Path = path,
				Reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true)
			};
		}

		public static RazorStringExtractor FromString(string text)
		{
			return new RazorStringExtractor()
			{
				Reader = new StringReader(text)
			};
		}

		public void Dispose()
		{
			this.Reader.Dispose();
		}

		public IEnumerable<SearchResult> Search(CancellationToken cancelToken = default(CancellationToken))
		{
			var visitor = new LiteralParserVisitor()
			{
				Path = Path,
				CancelToken = cancelToken,
				ExcludedLines = GetExcludedLines(),
			};

			var parser = new RazorParser(new CSharpCodeParser(), new HtmlMarkupParser());
			parser.Parse(this.Reader, visitor);

			visitor.EndText();

			return visitor.Results;
		}

		private HashSet<int> GetExcludedLines()
		{
			var fullText = this.Reader.ReadToEnd();
			this.Reader.Dispose();
			this.Reader = new StringReader(fullText);

			var lines = Patterns.NewLine.Split(fullText);
			var excludedLines = new HashSet<int>();

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				if (Patterns.ExcludedRazorLines.IsMatch(line))
				{
					excludedLines.Add(i);
				}
			}

			return excludedLines;
		}
	}
}
