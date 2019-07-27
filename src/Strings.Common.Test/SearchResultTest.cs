using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Strings.Common.Test
{
	[TestClass]
	public class SearchResultTest
	{
		[DataTestMethod]
		[DataRow("foo.ts", ".ts")]
		[DataRow("foo.TS", ".ts")]
		[DataRow("foo.d.ts", ".d.ts")]
		[DataRow("lib.es2017.d.ts", ".d.ts")]
		[DataRow("bar.cs", ".cs")]
		[DataRow("bar.g.cs", ".g.cs")]
		[DataRow(".ts", ".ts")]
		[DataRow(".d.ts", ".d.ts")]
		public void FileExtensionTest(string path, string expectedExtension)
		{
			var target = new SearchResult() { Path = path };
			Assert.AreEqual(expectedExtension, target.Extension);
		}
	}
}
