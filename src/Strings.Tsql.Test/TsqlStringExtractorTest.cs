using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strings.Common;

namespace Strings.Tsql.Test
{
	[TestClass]
	public class TsqlStringExtractorTest
	{
		[DataTestMethod]
		[DataRow("")]
		[DataRow("select 1;")]
		[DataRow("select 1 as \"foo\";")]
		[DataRow("select '123'")]
		[DataRow("select '123.456'")]
		[DataRow("select '2019-07-27'")]
		public void NoMatch(string text)
		{
			var results = GetResults(text);
			Assert.AreEqual(0, results.Length);
		}

		[DataTestMethod]
		[DataRow("select 'foo'", "'foo'")]
		public void SingleMatch(string text, string expected)
		{
			var results = GetResults(text);
			Assert.AreEqual(1, results.Length);

			var index = text.IndexOf(expected);

			Assert.AreEqual(expected, results[0].Text);
			Assert.AreEqual(index, results[0].StartIndex);
			Assert.AreEqual(index + expected.Length, results[0].EndIndex);
			Assert.AreEqual(0, results[0].Line);
			Assert.AreEqual(index, results[0].Character);
		}

		private static SearchResult[] GetResults(string text)
		{
			using (var extractor = TsqlStringExtractor.FromString(text))
			{
				var results = extractor.Search().ToArray();

				foreach (var item in results)
				{
					Assert.AreEqual("tsql", item.Source1);
					Assert.AreEqual(null, item.Path);
				}

				return results;
			}
		}
	}
}
