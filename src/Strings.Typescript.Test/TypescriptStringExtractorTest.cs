using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strings.Common;

namespace Strings.Typescript.Test
{
	[TestClass]
	public class TypescriptStringExtractorTest
	{
		[DataTestMethod]
		[DataRow("")]
		[DataRow("let foo = 1;")]
		[DataRow("let foo = ~")]
		[DataRow("let foo = { \"key\": 1 };")]
		[DataRow("foo[\"key\"] = 1;")]
		[DataRow("let foo: \"type\"")]
		[DataRow("interface Foo { '.bar': () => void; }")]
		[DataRow("class Foo { '.bar': () => void; }")]
		[DataRow("declare module 'Foo' {}")]
		[DataRow("import * as events from 'events'")]
		[DataRow("export { foo } from 'events';")]
		[DataRow("'use strict';")]
		[DataRow("class Foo { 'bar'() {} }")]
		[DataRow("interface Foo { 'bar'(); }")]
		public void NoMatch(string text)
		{
			var results = GetResults(text);
			Assert.AreEqual(0, results.Length);
		}

		[DataTestMethod]
		[DataRow("let foo = \"Hello\"", "\"Hello\"")]
		[DataRow("let foo = 'Hello'", "'Hello'")]
		[DataRow("let foo = `Hello`", "`Hello`")]
		[DataRow("let foo = `Hello${0}`", "`Hello${0}`")]
		[DataRow("let foo = `Hello${0}World`", "`Hello${0}World`")]
		[DataRow("let foo = `A${ 1 }B`", "`A${ 1 }B`")]
		[DataRow("let foo = /*a*/`A${ 1 }B`/*b*/;", "`A${ 1 }B`")]
		public void SingleMatch(string text, string expected)
		{
			var results = GetResults(text);
			Assert.AreEqual(1, results.Length);

			var index = text.IndexOf(results[0].Text);

			Assert.AreEqual(expected, results[0].Text);
			Assert.AreEqual(0, results[0].Line);
			Assert.AreEqual(index, results[0].StartIndex);
			Assert.AreEqual(index, results[0].Character);
		}

		[TestMethod]
		public void NestedTemplateString()
		{
			var results = GetResults("let foo = `Hello${\"World\"}`");
			Assert.AreEqual(2, results.Length);

			Assert.AreEqual("`Hello${\"World\"}`", results[0].Text);

			Assert.AreEqual("\"World\"", results[1].Text);
		}

		private static SearchResult[] GetResults(string text)
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				using (var writer = new StreamWriter(tempFile, true, Encoding.UTF8))
				{
					writer.Write(text);
				}

				var searchProvider = SearchProvider.ByExtension(".ts");
				var result = searchProvider.Run(new[] { tempFile }).ToArray();

				foreach (var item in result)
				{
					Assert.AreEqual(Path.GetFullPath(tempFile), Path.GetFullPath(item.Path));
					Assert.AreEqual("typescript", item.Source1);
				}

				return result;
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
