using Strings.Common;

namespace Strings.CSharp
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			return CommandLineInterface.Main(
				args, inputFile => CSharpStringExtractor.FromFile(inputFile).Search());
		}
	}
}
