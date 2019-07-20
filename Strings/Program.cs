using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Strings.Common;

namespace Strings
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			if (Debugger.IsAttached)
			{
				Run(args);
				Console.WriteLine("Done.");
				Console.ReadKey();
			}
			else
			{
				try
				{
					Run(args);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
					return 4;
				}
			}

			return 0;
		}

		private static void Run(string[] args)
		{
			ParseArguments(args, out var input, out string output, out var patterns);
			var searchResult = ExecuteSearch(input, patterns);
			WriteOutputFile(searchResult, output);
		}

		private static void WriteOutputFile(IEnumerable<SearchResult> result, string path)
		{
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
							orderby path.ToLowerInvariant()
							group path by SearchProvider.ByPath(path) into filesByProvider
							where filesByProvider.Key != null
							orderby filesByProvider.Key.Name
							select filesByProvider;

			var searchResult = new List<SearchResult>();

			foreach (var filesByProvider in inputFilesByProviders)
			{
				var provider = filesByProvider.Key;
				var result = provider.Run(filesByProvider);
				searchResult.AddRange(result);
			}

			return searchResult;
		}

		private static void ParseArguments(string[] args, out string[] input, out string output, out string[] patterns)
		{
			input = new[] { Environment.CurrentDirectory };
			output = "result.csv";
			patterns = new[] { "*" };

			foreach (var arg in args)
			{
				var match = arg.ToLowerInvariant();

				var knownArgument = TryMatchArray("input", ref input)
								 || TryMatchParameter("output", ref output)
								 || TryMatchArray("patterns", ref patterns);

				if (!knownArgument)
				{
					Console.Error.WriteLine("Invalid argument: " + arg);
				}

				bool TryMatchParameter(string name, ref string parameter)
				{
					var prefix = "-" + name + "=";
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

				bool TryMatchArray(string name, ref string[] array)
				{
					string parameter = null;
					if (!TryMatchParameter(name, ref parameter))
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
		}
	}
}
