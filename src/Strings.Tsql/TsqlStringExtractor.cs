using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Strings.Common;

namespace Strings.Tsql
{
	public sealed class TsqlStringExtractor : IStringExtractor
	{
		private static readonly Regex NumberOrDate = new Regex("^N?'[0-9.,-]+'$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private StreamReader internalReader;

		public ICharStream Stream { get; set; }
		public string Path { get; set; }

		public static TsqlStringExtractor FromFile(string fileName)
		{
			var reader = new StreamReader(fileName, Encoding.UTF8, true);

			return new TsqlStringExtractor()
			{
				Stream = new AntlrInputStream(reader),
				Path = fileName,
				internalReader = reader,
			};
		}

		public static TsqlStringExtractor FromString(string text)
		{
			return new TsqlStringExtractor()
			{
				Stream = new AntlrInputStream(text),
			};
		}

		public void Dispose()
		{
			this.internalReader?.Dispose();
		}

		public IEnumerable<SearchResult> Search(CancellationToken cancelToken = default(CancellationToken))
		{
			var lexer = new TSqlLexer(this.Stream);
			lexer.RemoveErrorListeners();

			var tokenStream = new CommonTokenStream(lexer);
			tokenStream.Fill();

			foreach (var token in tokenStream.GetTokens())
			{
				if (TryGetStringLiteralResult(token, out var result))
				{
					yield return result;
				}
			}
		}

		private bool TryGetStringLiteralResult(IToken token, out SearchResult result)
		{
			if (token.Type != TSqlLexer.STRING)
			{
				result = default(SearchResult);
				return false;
			}

			if (NumberOrDate.IsMatch(token.Text))
			{
				result = default(SearchResult);
				return false;
			}

			result = new SearchResult()
			{
				Path = Path,
				StartIndex = token.StartIndex,
				EndIndex = token.StartIndex + token.Text.Length,
				Line = token.Line - 1,
				Character = token.Column,
				Source1 = "tsql",
				Text = token.Text,
			};
			return true;
		}
	}
}
