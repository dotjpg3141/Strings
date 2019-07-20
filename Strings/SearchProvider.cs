using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			},
		};

		public IEnumerable<string> FileExtensions { get; set; }
		public string Name { get; set; }
		public string FileName { get; set; }
		public string ArgumentsFormat { get; set; }

		public static SearchProvider ByPath(string path)
		{
			var query = from provider in Providers
						from extension in provider.FileExtensions
						select Tuple.Create(provider, extension);

			var pathExtension = Path.GetExtension(path).ToLowerInvariant();
			return query.SingleOrDefault(tuple => tuple.Item2 == pathExtension)?.Item1;
		}

		public IEnumerable<SearchResult> Run(IEnumerable<string> paths)
		{
			var inputFile = CreateFileListInput(paths);
			var outputFile = Path.GetTempFileName();

			var process = new Process()
			{
				StartInfo = {
					FileName = FileName,
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
