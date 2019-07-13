using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Strings.CSharp
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Error.WriteLine("Expected at least two arguments: <output-file> <input-file-1> [ .. <input-file-n> ]");
				return 1;
			}

			var outputPath = args[0];
			var inputPaths = args.Skip(1);

			using (var output = new StreamWriter(outputPath, true, Encoding.UTF8))
			{
				foreach (var input in inputPaths)
				{
					var extractor = CSharpStringExtractor.FromFile(input);
					foreach (var result in extractor.Search())
					{
						result.WriteTo(output);
					}
				}
			}

			if (Debugger.IsAttached)
			{
				Console.WriteLine("Done.");
				Console.ReadKey();
			}

			return 0;
		}
	}
}
