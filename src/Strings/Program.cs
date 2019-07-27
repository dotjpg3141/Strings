using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Strings.Common;

namespace Strings
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			if (Debugger.IsAttached)
			{
				if (args.Length == 0)
				{
					Console.Error.WriteLine("Using debugging command line arguments");
					args = new[] { @"--input=..\..\..\..\examples" };
				}

				int returnCode = Run(args);
				Console.WriteLine("Done. RC = " + returnCode);
				Console.ReadKey();
				return returnCode;
			}
			else
			{
				try
				{
					return Run(args);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
					return 4;
				}
			}
		}

		private static int Run(string[] args)
		{
			if (!TryParseArguments(args, out var input, out string output, out var patterns))
			{
				return 1;
			}

			var searchResult = ExecuteSearch(input, patterns);
			WriteOutputFile(searchResult, output);
			return 1;
		}

		private static void WriteOutputFile(IEnumerable<SearchResult> result, string path)
		{
			Console.WriteLine("Writing result to " + Path.GetFullPath(path));
			using (var writer = new StreamWriter(path, false, Encoding.UTF8))
			{
				writer.WriteLine("sep=,");
				SearchResult.WriteCsvHeader(writer);
				foreach (var item in result)
				{
					item.WriteCsv(writer);
				}
			}
		}

		private static List<SearchResult> ExecuteSearch(string[] input, string[] patterns)
		{
			var inputFilesByProviders =
				from directory in input
				from pattern in patterns
				from path in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
				group path by SearchProvider.ByPath(path) into filesByProvider
				where filesByProvider.Key != null
				orderby filesByProvider.Key.Name
				select filesByProvider;

			var searchResult = new List<SearchResult>();

			foreach (var filesByProvider in inputFilesByProviders)
			{
				var provider = filesByProvider.Key;
				var files = filesByProvider.OrderBy(file => file.ToLowerInvariant());

				var result = provider.Run(files);
				searchResult.AddRange(result);
			}

			return searchResult;
		}

		private static bool TryParseArguments(string[] args, out string[] input, out string output, out string[] patterns)
		{
			input = new[] { Environment.CurrentDirectory };
			output = "result.csv";
			patterns = new[] { "*.*" };

			bool printUsage = args.Select(item => item.ToLowerInvariant()).Intersect(new[] { "-h", "--help", "/help", "/?" }).Any();
			if (printUsage)
			{
				Console.Error.WriteLine("Usage:");
			}

			foreach (var arg in args)
			{
				var match = arg.ToLowerInvariant();

				var knownArgument = TryMatchArray("input", ref input, "Input directories")
								 || TryMatchParameter("output", ref output, "Output file (.csv)")
								 || TryMatchArray("patterns", ref patterns, "Included file patterns");

				if (!knownArgument && !printUsage)
				{
					Console.Error.WriteLine("Invalid argument: " + arg);
				}

				bool TryMatchParameter(string name, ref string parameter, string description)
				{
					if (printUsage && description != null)
					{
						Console.Error.WriteLine($"\t--{name}=<value>");
						Console.Error.WriteLine($"\t\t{description}");
						Console.Error.WriteLine("\t\tDefault: " + parameter);
					}

					var prefix = "--" + name + "=";
					if (match.StartsWith(prefix))
					{
						parameter = arg.Substring(prefix.Length);
						return true;
					}
					else
					{
						return false;
					}
				}

				bool TryMatchArray(string name, ref string[] array, string description)
				{
					if (printUsage && description != null)
					{
						Console.Error.WriteLine($"\t--{name}=<value1,value2,...>");
						Console.Error.WriteLine($"\t\t{description}");
						Console.Error.WriteLine("\t\tDefault: " + string.Join(",", array));
					}

					string parameter = null;
					if (!TryMatchParameter(name, ref parameter, null))
					{
						return false;
					}

					array = parameter
						.Split(new[] { ',' })
						.Select(item => item.Trim())
						.Where(item => !string.IsNullOrEmpty(item))
						.ToArray();

					return true;
				}
			}

			return !printUsage;
		}
	}
}
