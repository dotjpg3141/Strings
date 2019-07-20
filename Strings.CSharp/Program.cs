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
			if (args.Length != 2)
			{
				Console.Error.WriteLine("Expected exactly two arguments: <input-file> <output-file>");
				return 1;
			}

			var inputPaths = File.ReadAllLines(args[0]);
			var outputPath = args[1];

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
