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
			if (!TryParseArguments(args, out var inputPaths, out var output))
			{
				return -1;
			}

			using (output)
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

		private static bool TryParseArguments(string[] args, out string[] inputPaths, out TextWriter output)
		{
			if (args.Length == 2)
			{
				inputPaths = File.ReadAllLines(args[0]);
				output = new StreamWriter(args[1], true, Encoding.UTF8);
			}
			else if (args.Length == 3 && args[0] == "--single")
			{
				inputPaths = new[] { args[1] };
				output = Console.Out;
			}
			else
			{
				inputPaths = null;
				output = null;
				Console.Error.WriteLine("Expected exactly two or three arguments: [--single] <input-file> <output-file>");
				return false;
			}

			return true;
		}
	}
}
