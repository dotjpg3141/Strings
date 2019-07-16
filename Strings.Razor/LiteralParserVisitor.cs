using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Tokenizer.Symbols;
using Strings.CSharp;

namespace Strings.Razor
{
	internal class LiteralParserVisitor : ParserVisitor
	{
		public string Path { get; set; }
		public List<SearchResult> Results { get; } = new List<SearchResult>();

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

				HandleTextSymbols(this.textSymbols, source2, source3);
			}

			this.textSymbols = null;
		}

		private void HandleTextSymbols(List<ISymbol> symbols, string source2, string source3)
		{
			var content = string.Concat(symbols.Select(sym => sym.Content));
			if (string.IsNullOrWhiteSpace(content))
			{
				return;
			}

			var firstSymbol = symbols.First();
			var lastSymbol = symbols.Last();

			var startIndex = firstSymbol.Start.AbsoluteIndex;
			var endIndex = lastSymbol.Start.AbsoluteIndex + lastSymbol.Content.Length;

			var location = firstSymbol.Start;

			var result = new SearchResult()
			{
				Path = Path,
				StartIndex = startIndex,
				EndIndex = endIndex,
				Line = location.LineIndex,
				Character = location.CharacterIndex,
				Text = content,
				Source1 = "razor",
				Source2 = source2,
				Source3 = source3,
			};

			this.Results.Add(result);
		}

		private enum ParseState
		{
			Default,
			InsideTag,
			InsideSingleQuoteAttribute,
			InsideDoubleQuoteAttribute,
		}
	}
}
