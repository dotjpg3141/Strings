using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Razor;

namespace Strings.Razor
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
					var extractor = RazorStringExtractor.FromFile(input);
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
