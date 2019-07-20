using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Razor.Parser;
using Strings.Common;
using Strings.CSharp;

namespace Strings.Razor
{
	public sealed class RazorStringExtractor : IDisposable
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
			var visitor = new LiteralParserVisitor() { Path = Path };
			visitor.CancelToken = cancelToken;

			var parser = new RazorParser(new CSharpCodeParser(), new HtmlMarkupParser());
			parser.Parse(this.Reader, visitor);

			visitor.EndText();

			return visitor.Results;
		}
	}
}
