using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strings.Common;
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
		[DataRow("@model My.Class")]
		[DataRow("@using My.Namespace")]
		[DataRow("<h1>")]
		[DataRow("</h1>")]
		[DataRow("<h1 />")]
		[DataRow("<h1 hello />")]
		[DataRow("@foo")]
		[DataRow("@{ var hello = 1; }")]
		[DataRow("@* Hello World *@")]
		[DataRow("<a asp-route-id=\"@item.ID\"></a>")]
		[DataRow("@using MyNamespace;")]
		public void NoResult(string text)
		{
			var results = GetResults(text);
			Assert.AreEqual(0, results.Length, "no match");
		}

		[DataTestMethod]
		[DataRow(@"Hello World", 0, 11, "razor", "text", "")]
		[DataRow(@"<b>Hello World<b>", 3, 14, "razor", "text", "")]
		[DataRow(@"<a title=""Hello World""></a>", 10, 21, "razor", "attribute", "title")]
		[DataRow(@"@{ ""Hello""					}", 3, 10, "razor", "csharp", "StringLiteralToken")]
		[DataRow(@"@{ @""Hello""				}", 3, 11, "razor", "csharp", "StringLiteralToken")]
		[DataRow(@"@{ $""Hello""				}", 3, 11, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"@{ $@""Hello""				}", 3, 12, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"@{ $@""Hello{0}World""		}", 3, 20, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"@{ $@""Hello{0}World{1}.""	}", 3, 24, "razor", "csharp", "InterpolatedStringToken")]
		[DataRow(@"<div a=""@(""Hello World!"")""></div>", 10, 24, "razor", "csharp", "StringLiteralToken")]
		public void SingleResult(string text, int startIndex, int endIndex, string source1, string source2, string source3)
		{
			var results = GetResults(text);
			Assert.AreEqual(1, results.Length, "single match");

			var result = results[0];
			Assert.AreEqual(startIndex, result.StartIndex);
			Assert.AreEqual(endIndex, result.EndIndex);
			Assert.AreEqual(0, result.Line);
			Assert.AreEqual(startIndex, result.Character);
			Assert.AreEqual(text.Substring(startIndex, endIndex - startIndex), result.Text);
			Assert.AreEqual(source2, result.Source2);
			Assert.AreEqual(source3, result.Source3);
		}

		[TestMethod]
		public void MixedResult()
		{
			var results = GetResults(@"<div a=""AA@(""BB"")CC""></div>");
			Assert.AreEqual(3, results.Length, "three matches");

			Assert.AreEqual(8, results[0].StartIndex);
			Assert.AreEqual(10, results[0].EndIndex);
			Assert.AreEqual("AA", results[0].Text);
			Assert.AreEqual("attribute", results[0].Source2);
			Assert.AreEqual("a", results[0].Source3);

			Assert.AreEqual(12, results[1].StartIndex);
			Assert.AreEqual(16, results[1].EndIndex);
			Assert.AreEqual(@"""BB""", results[1].Text);
			Assert.AreEqual("csharp", results[1].Source2);
			Assert.AreEqual("StringLiteralToken", results[1].Source3);

			Assert.AreEqual(17, results[2].StartIndex);
			Assert.AreEqual(19, results[2].EndIndex);
			Assert.AreEqual("CC", results[2].Text);
			Assert.AreEqual("attribute", results[2].Source2);
			Assert.AreEqual("a", results[2].Source3);
		}

		[DataTestMethod]
		[DataRow("\n", 1)]
		[DataRow("\r\n", 1)]
		[DataRow("\n\n", 2)]
		[DataRow("\r\n\r\n", 2)]
		public void NewLineAndAtSignTest(string lineSeperator, int newLineCount)
		{
			var text = $"@using Foo{lineSeperator}Hello{lineSeperator}@model Bar{lineSeperator}World{lineSeperator}@foo bar";
			var results = GetResults(text);

			var assertMessage = lineSeperator.Replace("\r", "\\r").Replace("\n", "\\n");
			assertMessage += " (" + newLineCount + " lines)";

			Assert.AreEqual(3, results.Length, assertMessage);


			Assert.AreEqual("Hello", results[0].Text.Trim(), assertMessage);
			Assert.AreEqual(newLineCount * 1, results[0].Line, assertMessage);

			Assert.AreEqual("World", results[1].Text.Trim(), assertMessage);
			Assert.AreEqual(newLineCount * 3, results[1].Line, assertMessage);

			Assert.AreEqual("bar", results[2].Text.Trim(), assertMessage);
			Assert.AreEqual(newLineCount * 4, results[2].Line, assertMessage);
		}

		private static SearchResult[] GetResults(string text)
		{
			using (var extractor = RazorStringExtractor.FromString(text))
			{
				var results = extractor.Search().ToArray();

				foreach (var item in results)
				{
					Assert.AreEqual("razor", item.Source1);
					Assert.AreEqual(null, item.Path);
				}

				return results;
			}
		}
	}
}
