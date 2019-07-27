using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strings.Common;

namespace Strings.CSharp.Test
{
	[TestClass]
	public class CSharpStringExtractorTest
	{
		[TestMethod]
		public void EmptySource()
		{
			var results = GetResults("");
			Assert.AreEqual(0, results.Length);
		}

		[TestMethod]
		public void NoStringLiteral()
		{
			var results = GetResults("var helloWorld = 1;");
			Assert.AreEqual(0, results.Length);
		}

		[TestMethod]
		public void InvalidToken()
		{
			Assert.AreEqual(0, GetResults(@"var foo = $").Length);
		}

		[TestMethod]
		public void SingleStringLiteral()
		{
			var results = GetResults(
@"public class Foo {
	string Bar = ""Baz"";
}");
			Assert.AreEqual(1, results.Length);

			var result = results[0];
			Assert.AreEqual(null, result.Path);
			Assert.AreEqual(34, result.StartIndex);
			Assert.AreEqual(1, result.Line);
			Assert.AreEqual(14, result.Character);
			Assert.AreEqual(@"""Baz""", result.Text);
			Assert.AreEqual("csharp", result.Source1);
			Assert.AreEqual("StringLiteralToken", result.Source2);
		}

		[DataTestMethod]
		[DataRow(@"""Hello""", "StringLiteralToken")]
		[DataRow(@"@""Hello""", "StringLiteralToken")]
		[DataRow(@"$""Hello""", "InterpolatedStringToken")]
		[DataRow(@"$@""Hello""", "InterpolatedStringToken")]
		[DataRow(@"$@""Hello{0}World""", "InterpolatedStringToken")]
		[DataRow(@"$@""Hello{0}World{1}.""", "InterpolatedStringToken")]
		public void StringVariants(string text, string source2)
		{
			var results = GetResults(text);
			Assert.AreEqual(1, results.Length);

			var result = results[0];
			Assert.AreEqual(null, result.Path);
			Assert.AreEqual(0, result.StartIndex);
			Assert.AreEqual(0, result.Line);
			Assert.AreEqual(0, result.Character);
			Assert.AreEqual(text, result.Text);
			Assert.AreEqual("csharp", result.Source1);
			Assert.AreEqual(source2, result.Source2);
		}

		[TestMethod]
		public void CommentTest()
		{
			var results = GetResults("/*a*/\"b\"//c");
			Assert.AreEqual(1, results.Length);

			Assert.AreEqual("\"b\"", results[0].Text);
		}

		private static SearchResult[] GetResults(string text)
		{
			var extractor = CSharpStringExtractor.FromString(text);
			return extractor.Search().ToArray();
		}
	}
}
