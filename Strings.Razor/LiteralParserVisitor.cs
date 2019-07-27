using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Tokenizer.Symbols;
using Strings.Common;
using Strings.CSharp;

namespace Strings.Razor
{
	internal class LiteralParserVisitor : ParserVisitor
	{
		public string Path { get; set; }
		public List<SearchResult> Results { get; } = new List<SearchResult>();
		public HashSet<int> ExcludedLines { get; set; }

		private ParseState parseState = ParseState.Default;
		private List<HtmlSymbol> symbolsWithoutWhitespace = new List<HtmlSymbol>();

		private string lastVisitedAttribute;
		private List<ISymbol> textSymbols = new List<ISymbol>();
		private ParseState textState;

		public override void VisitSpan(Span span)
		{
			switch (span.Kind)
			{
				case SpanKind.Code:

					var capturesText = this.textSymbols != null;
					if (capturesText)
					{
						EndText();
					}

					var text = span.Content;
					var postion = span.Start;

					var extractor = CSharpStringExtractor.FromString(text);
					extractor.Path = this.Path;
					extractor.StartIndex = postion.AbsoluteIndex;
					extractor.StartLine = postion.LineIndex;
					extractor.StartCharacter = postion.CharacterIndex;

					foreach (var item in extractor.Search())
					{
						item.Source3 = item.Source2;
						item.Source2 = item.Source1;
						item.Source1 = "razor";
						this.Results.Add(item);
					}

					if (capturesText)
					{
						BeginText();
					}

					break;

				case SpanKind.Markup:
					foreach (var symbol in span.Symbols.OfType<HtmlSymbol>())
					{
						symbol.OffsetStart(span.Start);
						VisitHtmlSymbol(symbol);
					}
					break;
			}

			base.VisitSpan(span);
		}

		private void VisitHtmlSymbol(HtmlSymbol symbol)
		{
			bool ommitTextCharacter = false;

			switch (symbol.Type)
			{
				case HtmlSymbolType.OpenAngle when this.parseState == ParseState.Default:
					this.parseState = ParseState.InsideTag;
					break;

				case HtmlSymbolType.CloseAngle when this.parseState == ParseState.InsideTag:
					this.parseState = ParseState.Default;
					break;

				case HtmlSymbolType.SingleQuote when this.parseState == ParseState.InsideTag:
				case HtmlSymbolType.DoubleQuote when this.parseState == ParseState.InsideTag:
					if (this.symbolsWithoutWhitespace.Count >= 3)
					{
						var a = this.symbolsWithoutWhitespace[this.symbolsWithoutWhitespace.Count - 2];
						var b = this.symbolsWithoutWhitespace[this.symbolsWithoutWhitespace.Count - 1];

						if (a.Type == HtmlSymbolType.Text && b.Type == HtmlSymbolType.Equals)
						{
							this.lastVisitedAttribute = a.Content;
							this.parseState = symbol.Type == HtmlSymbolType.SingleQuote
								? ParseState.InsideSingleQuoteAttribute
								: ParseState.InsideDoubleQuoteAttribute;
							ommitTextCharacter = true;
							BeginText();
						}
					}
					break;

				case HtmlSymbolType.SingleQuote when this.parseState == ParseState.InsideSingleQuoteAttribute:
				case HtmlSymbolType.DoubleQuote when this.parseState == ParseState.InsideDoubleQuoteAttribute:
					this.parseState = ParseState.InsideTag;
					break;

				case HtmlSymbolType.Text when this.parseState == ParseState.Default:
					BeginText();
					break;
			}

			if (this.parseState == this.textState)
			{
				if (this.textSymbols != null && !ommitTextCharacter)
				{
					this.textSymbols.Add(symbol);
				}
			}
			else
			{
				EndText();
			}

			bool isWhitespace = symbol.Type == HtmlSymbolType.WhiteSpace
							 || symbol.Type == HtmlSymbolType.NewLine;
			if (!isWhitespace)
			{
				this.symbolsWithoutWhitespace.Add(symbol);
			}
		}

		private void BeginText()
		{
			this.textState = this.parseState;
			if (this.textSymbols == null)
			{
				this.textSymbols = new List<ISymbol>();
			}
		}

		internal void EndText()
		{
			if (this.textSymbols != null && this.textSymbols.Count != 0)
			{
				string source2;
				string source3;

				switch (this.textState)
				{
					case ParseState.Default:
						source2 = "text";
						source3 = "";
						break;

					case ParseState.InsideSingleQuoteAttribute:
					case ParseState.InsideDoubleQuoteAttribute:
						source2 = "attribute";
						source3 = this.lastVisitedAttribute;
						break;

					default:
						throw new InvalidOperationException();
				}
				var results =
					from match in GetMatches(this.textSymbols)
					select new SearchResult()
					{
						Path = Path,
						StartIndex = match.AbsoluteStartIndex,
						EndIndex = match.AbsoluteEndIndex,
						Line = match.LineIndex,
						Character = match.CharIndex,
						Text = match.Text,
						Source1 = "razor",
						Source2 = source2,
						Source3 = source3,
					};

				this.Results.AddRange(results);
			}

			this.textSymbols = null;
		}

		private IEnumerable<TextMatch> GetMatches(List<ISymbol> symbols)
		{
			var matches =
				(from symbol in symbols
				 from match in SplitTextByLine(symbol)
				 select match);

			var currentMatches = new List<TextMatch>();
			TextMatch next;

			foreach (var match in matches)
			{
				if (this.ExcludedLines.Contains(match.LineIndex))
				{
					if (TryGetNextMatch(out next))
					{
						yield return next;
					}
					continue;
				}

				if (currentMatches.Count != 0 || IsNonWhitespaceMatch(match))
				{
					currentMatches.Add(match);
				}
			}

			if (TryGetNextMatch(out next))
			{
				yield return next;
			}

			bool TryGetNextMatch(out TextMatch match)
			{
				Debug.Assert(currentMatches.Count == 0 || !string.IsNullOrWhiteSpace(currentMatches.First().Text));

				var lastIndex = currentMatches.FindLastIndex(IsNonWhitespaceMatch);
				if (lastIndex == -1)
				{
					match = default(TextMatch);
					return false;
				}

				var text = new StringBuilder();
				foreach (var item in currentMatches.Take(lastIndex + 1))
				{
					text.Append(item.Text);
				}

				match = currentMatches[0];
				match.Text = text.ToString();
				match.AbsoluteEndIndex = currentMatches[lastIndex].AbsoluteEndIndex;

				var whitespace = Patterns.TrailingWhitespaceExceptNewLine.Match(match.Text);
				if (whitespace.Success)
				{
					var offset = whitespace.Length;
					match.AbsoluteStartIndex += offset;
					match.CharIndex += offset;
					match.Text = match.Text.Substring(offset);
				}

				currentMatches.Clear();
				return true;
			}

			bool IsNonWhitespaceMatch(TextMatch textMatch)
			{
				return !string.IsNullOrWhiteSpace(textMatch.Text);
			}
		}

		private enum ParseState
		{
			Default,
			InsideTag,
			InsideSingleQuoteAttribute,
			InsideDoubleQuoteAttribute,
		}

		internal static IEnumerable<TextMatch> SplitTextByLine(ISymbol symbol)
		{
			int firstAbsoluteIndex = symbol.Start.AbsoluteIndex;
			int lineIndex = symbol.Start.LineIndex;
			var content = symbol.Content;

			int index = 0;

			while (true)
			{
				var match = Patterns.NewLine.Match(content, index);
				if (!match.Success)
				{
					break;
				}

				yield return NextResult(match.Length);
				index = match.Index + match.Length;
			}

			if (index != content.Length)
			{
				yield return NextResult(content.Length - index);
			}

			TextMatch NextResult(int length)
			{
				var absoluteIndex = firstAbsoluteIndex + index;
				var text = content.Substring(index, length);
				var charIndex = index == 0 ? symbol.Start.CharacterIndex : 0;

				return new TextMatch()
				{
					AbsoluteStartIndex = absoluteIndex,
					AbsoluteEndIndex = absoluteIndex + text.Length,
					LineIndex = lineIndex++,
					CharIndex = charIndex,
					Text = text
				};
			}
		}

		internal struct TextMatch
		{
			public int AbsoluteStartIndex { get; set; }
			public int AbsoluteEndIndex { get; set; }
			public int LineIndex { get; set; }
			public int CharIndex { get; set; }
			public string Text { get; set; }
		}
	}
}
