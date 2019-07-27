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
				FileName = "components/csharp/Strings.CSharp.exe",
				ArgumentsFormat = "{0} {1}",
			},
			new SearchProvider() {
				Name = "razor",
				FileExtensions = new[] { ".cshtml" },
				FileName = "components/razor/Strings.Razor.exe",
				ArgumentsFormat = "{0} {1}",
			},
			new SearchProvider() {
				Name = "typescript",
				FileExtensions = new[] { ".ts", ".tsx" },
				FileName = "node",
				ArgumentsFormat = "components/typescript/app.js {0} {1}",
				IsRelativeFileName = true,
			},
			new SearchProvider() {
				Name = "tsql",
				FileExtensions = new[]{ ".sql", ".csql" },
				FileName = "components/tsql/Strings.Tsql.exe",
				ArgumentsFormat = "{0} {1}",
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
		public string ArgumentsFormat { get; set; }
		public bool IsRelativeFileName { get; set; }

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
			var inputFile = CreateFileListInput(paths);
			var outputFile = Path.GetTempFileName();

			var process = new Process()
			{
				StartInfo = {
					FileName = GetFileName(),
					Arguments = string.Format(this.ArgumentsFormat, PathHelper.Quote(inputFile), PathHelper.Quote(outputFile)),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			};

			process.OutputDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					Console.Out.Write($"[{this.Name}]");
					Console.Out.WriteLine(e.Data);
				}
			};
			process.ErrorDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					Console.Error.Write($"[{this.Name}]");
					Console.Error.WriteLine(e.Data);
				}
			};

			Console.WriteLine($"Files to process for {this.Name}: ");
			Console.WriteLine(string.Join(Environment.NewLine, paths));
			Console.WriteLine("Starting " + this.Name);

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

		private string GetFileName()
		{
			if (this.IsRelativeFileName)
			{
				return this.FileName;
			}

			var executablePath = Assembly.GetExecutingAssembly().Location;
			var baseDirectory = Path.GetDirectoryName(executablePath);
			var fileName = Path.Combine(baseDirectory, this.FileName);
			return fileName;
		}

		private static string CreateFileListInput(IEnumerable<string> paths)
		{
			var tempFile = Path.GetTempFileName();
			using (var writer = new StreamWriter(tempFile, false, Encoding.UTF8))
			{
				foreach (var path in paths)
				{
					writer.WriteLine(path);
				}
			}
			return tempFile;
		}
	}
}
