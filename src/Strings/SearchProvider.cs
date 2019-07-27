using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Strings.Common;

namespace Strings
{
	public class SearchProvider
	{
		private static readonly SearchProvider[] Providers = {
			new SearchProvider() {
				Name = "csharp",
				FileExtensions = new[] { ".cs" },
				FileName = "{directory}/components/csharp/Strings.CSharp.exe",
				Arguments = "{input} {output}",
			},
			new SearchProvider() {
				Name = "razor",
				FileExtensions = new[] { ".cshtml" },
				FileName = "{directory}/components/razor/Strings.Razor.exe",
				Arguments = "{input} {output}",
			},
			new SearchProvider() {
				Name = "typescript",
				FileExtensions = new[] { ".ts", ".tsx", ".js", ".jsx", ".json" },
				FileName = "node",
				Arguments = "\"{directory}/components/typescript/app.js\" {input} {output}",
			},
			new SearchProvider() {
				Name = "tsql",
				FileExtensions = new[]{ ".sql", ".csql" },
				FileName = "{directory}/components/tsql/Strings.Tsql.exe",
				Arguments = "{input} {output}",
			}
		};

		private static readonly Dictionary<string, SearchProvider> ProvidersByExtension =
			(from provider in Providers
			 from extension in provider.FileExtensions
			 select new { provider, extension })
				.ToDictionary(
					item => item.extension.ToLowerInvariant(),
					item => item.provider
				);

		public IEnumerable<string> FileExtensions { get; set; }
		public string Name { get; set; }
		public string FileName { get; set; }
		public string Arguments { get; set; }

		public static SearchProvider ByPath(string path)
		{
			var extension = Path.GetExtension(path);
			return ByExtension(extension);
		}

		public static SearchProvider ByExtension(string extension)
		{
			var pathExtension = extension.ToLowerInvariant();
			ProvidersByExtension.TryGetValue(pathExtension, out var provider);
			return provider;
		}

		public IEnumerable<SearchResult> Run(IEnumerable<string> paths)
		{
			if (!paths.Any())
			{
				return Enumerable.Empty<SearchResult>();
			}

			var inputFile = CreateFileListInput(paths);
			var outputFile = Path.GetTempFileName();

			var variables = new Dictionary<string, string>()
			{
				["directory"] = PathHelper.GetDirectoryOfExecutable(),
				["input"] = PathHelper.Quote(inputFile),
				["output"] = PathHelper.Quote(outputFile),
			};

			var fileName = ResolveVariables(this.FileName, variables);
			var arguments = ResolveVariables(this.Arguments, variables);

			var process = new Process()
			{
				StartInfo = {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			};

			process.OutputDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					Console.Out.Write($"  [{this.Name}]: ");
					Console.Out.WriteLine(e.Data);
				}
			};

			process.ErrorDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					Console.Error.Write($"  [{this.Name}]: ");
					Console.Error.WriteLine(e.Data);
				}
			};

			Console.WriteLine("Starting component " + this.Name);

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();

			Console.WriteLine(this.Name + " terminated with return code " + process.ExitCode);
			Console.WriteLine();

			if (process.ExitCode != 0)
			{
				throw new InvalidOperationException("Process terminated with a non-zero return code.");
			}

			var result = new List<SearchResult>();
			using (var reader = new StreamReader(outputFile, Encoding.UTF8, true))
			{
				while (!reader.EndOfStream)
				{
					result.Add(SearchResult.ReadFrom(reader));
				}
			}

			File.Delete(inputFile);
			File.Delete(outputFile);

			return result;
		}

		private static string CreateFileListInput(IEnumerable<string> paths)
		{
			var tempFile = Path.GetTempFileName();
			using (var writer = new StreamWriter(tempFile, false, Encoding.UTF8))
			{
				foreach (var path in paths)
				{
					writer.WriteLine(Path.GetFullPath(path));
				}
			}
			return tempFile;
		}

		private static string ResolveVariables(string text, Dictionary<string, string> variables)
		{
			foreach (var item in variables)
			{
				text = text.Replace("{" + item.Key + "}", item.Value);
			}
			Debug.Assert(text.IndexOf("{") == -1 && text.IndexOf("}") == -1);
			return text;
		}
	}
}
