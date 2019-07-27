using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Strings.Common;

namespace Strings.Tsql
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			return CommandLineInterface.Main(args, TsqlStringExtractor.FromFile);
		}
	}
}
