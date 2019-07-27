using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Strings.Common
{
	public static class CommandLineInterface
	{
		public static int Main(string[] args, Func<string, IStringExtractor> createExtractor)
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
					using (var extractor = createExtractor(input))
					{
						foreach (var result in extractor.Search())
						{
							result.WriteTo(output);
						}
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
