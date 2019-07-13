using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strings.CSharp;

namespace Strings.Razor.Test
{
	[TestClass]
	public class RazorStringExtractorTest
	{
		[TestMethod]
		public void Empty()
		{
			var results = GetResults("");
			Assert.AreEqual(0, results.Length);
		}

		[DataTestMethod]
		// [DataRow("@model RazorPagesMovie.Pages.Movies.IndexModel")]
		[DataRow("<h1>")]
		[DataRow("</h1>")]
		[DataRow("<h1 />")]
		[DataRow("<h1 hello />")]
		[DataRow("@foo")]
		[DataRow("@{ var hello = 1; }")]
		[DataRow("@* Hello World *@")]
		[DataRow(@"<a asp-route-id=""@item.ID""></a>")]
		public void NoResult(string text)
		{
			var results = GetResults(text);
			Assert.AreEqual(0, results.Length);
		}

		[DataTestMethod]
		[DataRow(@"Hello World", 0, 11, "razor", "text", "")]
		[DataRow(@"<b>Hello World<b>", 3, 14, "razor", "text", "")]
		[DataRow(@"<a title=""Hello World""></a>", 10, 21, "razor", "text", "")]
		[DataRow(@"@{ ""Hello""					}", 3, 10, "razor", "csharp", "StringLiteralToken")]
		[DataRow(@"@{ @""Hello""				}", 3, 11, "razor", "csharp", "StringLiteralToken")]
		[DataRow(@"@{ $""Hello""				}", 3, 11, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"@{ $@""Hello""				}", 3, 12, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"@{ $@""Hello{0}World""		}", 3, 20, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"@{ $@""Hello{0}World{1}.""	}", 3, 24, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"<div a=""@(""Hello World!"")""></div>", 10, 21, "razor", "csharp", "StringLiteralToken")]
		public void SingleResult(string text, int startIndex, int endIndex, string source1, string source2, string source3)
		{
			var results = GetResults(text);
			Assert.AreEqual(1, results.Length);

			var result = results[0];
			Assert.AreEqual(null, result.Path);
			Assert.AreEqual(startIndex, result.StartIndex);
			Assert.AreEqual(endIndex, result.EndIndex);
			Assert.AreEqual(0, result.Line);
			Assert.AreEqual(startIndex, result.Character);
			Assert.AreEqual(text.Substring(startIndex, endIndex - startIndex), result.Text);
			Assert.AreEqual(source1, result.Source1);
			Assert.AreEqual(source2, result.Source2);
			Assert.AreEqual(source3, result.Source3);
		}

		private static SearchResult[] GetResults(string text)
		{
			using (var extractor = RazorStringExtractor.FromString(text))
			{
				return extractor.Search().ToArray();
			}
		}
	}
}
