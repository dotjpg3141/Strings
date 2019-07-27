using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Strings.Common;

namespace Strings.CSharp
{
	public sealed class CSharpStringExtractor : IStringExtractor
	{
		public SourceText SourceText { get; private set; }

		public string Path { get; set; }

		public int StartIndex { get; set; }
		public int StartLine { get; set; }
		public int StartCharacter { get; set; }

		public static CSharpStringExtractor FromFile(string path)
		{
			SourceText sourceText;
			using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				sourceText = SourceText.From(reader);
			}

			return new CSharpStringExtractor()
			{
				Path = path,
				SourceText = sourceText,
			};
		}

		public static CSharpStringExtractor FromString(string text)
		{
			return new CSharpStringExtractor()
			{
				Path = null,
				SourceText = SourceText.From(text),
			};
		}

		public void Dispose()
		{
			// nop
		}

		public IEnumerable<SearchResult> Search()
		{
			foreach (var token in SyntaxFactory.ParseTokens(this.SourceText.ToString()))
			{
				var kind = token.Kind();
				switch (kind)
				{
					case SyntaxKind.StringLiteralToken:
					case SyntaxKind.InterpolatedStringToken:
						var span = token.Span;
						var position = this.SourceText.Lines.GetLinePosition(span.Start);

						var line = position.Line + this.StartLine;
						var character = position.Character;
						if (line == this.StartLine)
						{
							character += this.StartCharacter;
						}

						yield return new SearchResult()
						{
							Path = this.Path,
							StartIndex = span.Start + this.StartIndex,
							EndIndex = span.End + this.StartIndex,
							Line = line,
							Character = character,
							Text = token.ToString(),
							Source1 = "csharp",
							Source2 = kind.ToString(),
						};
						break;
				}
			}
		}

		public IEnumerable<SearchResult> Search(CancellationToken cancelToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
