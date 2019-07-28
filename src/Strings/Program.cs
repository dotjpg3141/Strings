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
					args = new[] { @"--input=.\examples" };
				}

				int returnCode = Run(args);
				Console.Error.WriteLine("Done. RC = " + returnCode);
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
			if (!TryParseArguments(args, out var cli))
			{
				return 1;
			}

			var searchResult = ExecuteSearch(cli.Input, cli.Patterns, cli.Sync);
			WriteOutputFile(searchResult, cli.Output);
			return 0;
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

		private static IEnumerable<SearchResult> ExecuteSearch(string[] input, string[] patterns, bool sync)
		{
			// See: https://referencesource.microsoft.com/#System.Core/System/Linq/Parallel/Scheduling/Scheduling.cs,df4daefa7f756d38,references
			var cpuCount = sync ? 1 : Math.Min(512, Environment.ProcessorCount);

			var inputFiles =
				from directory in input.AsParallel().WithDegreeOfParallelism(cpuCount)
				from pattern in patterns
				from path in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
				select path;

			var filesByProvider =
				from path in inputFiles
				group path by SearchProvider.ByPath(path) into filesByProviderGroup
				where filesByProviderGroup.Key != null
				select new { provider = filesByProviderGroup.Key, files = filesByProviderGroup };

			var searchResults =
				from item in filesByProvider
				from result in item.provider.Run(item.files)
				let normalizedPath = Path.GetFullPath(result.Path ?? ".").ToLowerInvariant()
				orderby normalizedPath, result.StartIndex
				select result;

			return searchResults;
		}

		private static bool TryParseArguments(string[] args, out CliArguments result)
		{
			result.Input = new[] { Environment.CurrentDirectory };
			result.Output = "result.csv";
			result.Patterns = new[] { "*.*" };
			result.Sync = false;

			bool printUsage = args.Select(item => item.ToLowerInvariant()).Intersect(new[] { "-h", "--help", "/help", "/?" }).Any();
			if (printUsage)
			{
				Console.Error.WriteLine("Usage:");
			}

			foreach (var arg in args)
			{
				var match = arg.ToLowerInvariant();

				var knownArgument = TryMatchArray("input", ref result.Input, "Input directories")
								 || TryMatchParameter("output", ref result.Output, "Output file (.csv)")
								 || TryMatchArray("patterns", ref result.Patterns, "Included file patterns")
								 || TryMatchFlag("sync", ref result.Sync, "Synchronized or parallel execution");

				if (printUsage)
				{
					break;
				}

				if (!knownArgument)
				{
					Console.Error.WriteLine("Invalid argument: " + arg);
				}

				bool TryMatchFlag(string name, ref bool parameter, string description)
				{
					if (printUsage && description != null)
					{
						Console.Error.WriteLine($"\t--{name}");
						Console.Error.WriteLine($"\t\t{description}");
						Console.Error.WriteLine("\t\tDefault: " + parameter);
						return false;
					}

					if (match == "--" + name)
					{
						parameter = true;
						return true;
					}
					else
					{
						return false;
					}
				}

				bool TryMatchParameter(string name, ref string parameter, string description)
				{
					if (printUsage && description != null)
					{
						Console.Error.WriteLine($"\t--{name}=<value>");
						Console.Error.WriteLine($"\t\t{description}");
						Console.Error.WriteLine("\t\tDefault: " + parameter);
						return false;
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
						return false;
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

		private struct CliArguments
		{
			public string[] Input;
			public string Output;
			public string[] Patterns;
			public bool Sync;
		}
	}
}
