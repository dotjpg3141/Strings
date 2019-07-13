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

		private ParseState state = ParseState.Default;
		private List<HtmlSymbol> symbols = new List<HtmlSymbol>();

		private string lastAttribute;
		private List<ISymbol> lastTexts = new List<ISymbol>();
		private ParseState textState;

		public override void VisitSpan(Span span)
		{
			switch (span.Kind)
			{
				case SpanKind.Code:
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

					break;

				case SpanKind.Markup:
					foreach (var symbol in span.Symbols.OfType<HtmlSymbol>())
					{
						VisitHtmlSymbol(symbol);
					}
					break;
			}

			base.VisitSpan(span);
		}

		private void VisitHtmlSymbol(HtmlSymbol symbol)
		{
			Console.WriteLine(symbol);

			switch (symbol.Type)
			{
				case HtmlSymbolType.OpenAngle when this.state == ParseState.Default:
					this.state = ParseState.InsideTag;
					break;

				case HtmlSymbolType.CloseAngle when this.state == ParseState.InsideTag:
					this.state = ParseState.Default;
					break;

				case HtmlSymbolType.SingleQuote when this.state == ParseState.InsideTag:
				case HtmlSymbolType.DoubleQuote when this.state == ParseState.InsideTag:
					if (this.symbols.Count >= 3)
					{
						var a = this.symbols[this.symbols.Count - 2];
						var b = this.symbols[this.symbols.Count - 1];

						if (a.Type == HtmlSymbolType.Text && b.Type == HtmlSymbolType.Equals)
						{
							this.lastAttribute = a.Content;
							this.state = symbol.Type == HtmlSymbolType.SingleQuote
								? ParseState.InsideSingleQuoteAttribute
								: ParseState.InsideDoubleQuoteAttribute;
							BeginText();
						}
					}
					break;

				case HtmlSymbolType.SingleQuote when this.state == ParseState.InsideSingleQuoteAttribute:
				case HtmlSymbolType.DoubleQuote when this.state == ParseState.InsideDoubleQuoteAttribute:
					this.state = ParseState.InsideTag;
					break;

				case HtmlSymbolType.Text when this.state == ParseState.Default:
					BeginText();
					break;
			}

			if (this.state == this.textState)
			{
				this.lastTexts?.Add(symbol);
			}
			else
			{
				EndText();
			}

			if (symbol.Type != HtmlSymbolType.WhiteSpace && symbol.Type != HtmlSymbolType.NewLine)
			{
				this.symbols.Add(symbol);
			}
		}

		private void BeginText()
		{
			this.textState = this.state;
			this.lastTexts = new List<ISymbol>();
		}

		internal void EndText()
		{
			if (this.lastTexts != null && this.lastTexts.Count != 0)
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
						source3 = this.lastAttribute;
						break;

					default:
						throw new InvalidOperationException();
				}

				HandleTextSymbol(this.lastTexts, source2, source3);
			}

			this.lastTexts = null;
		}

		private void HandleTextSymbol(List<ISymbol> symbols, string source2, string source3)
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
