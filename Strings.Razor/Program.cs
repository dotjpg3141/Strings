using Strings.Common;

namespace Strings.Razor
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			return CommandLineInterface.Main(
				args, inputFile => RazorStringExtractor.FromFile(inputFile).Search());
		}
	}
}
